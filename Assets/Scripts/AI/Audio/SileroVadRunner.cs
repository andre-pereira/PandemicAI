using OPEN.PandemicAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace VadDotNet
{
    public class SileroVadRunner : MonoBehaviour
    {
        [Header("ONNX Model Settings")]
        public string onnxModelFileName = "silero_vad.onnx";
        public int sampleRate = 16000;
        public float threshold = 0.5f;
        public int minSpeechDurationMs = 250;
        public float minSilenceDurationMs = 200;
        public float maxSpeechDurationSeconds = float.PositiveInfinity;
        public int speechPadMs = 30;

        [Header("Audio Settings")]
        public string audioFileName;
        [Tooltip("Silero expects ~32 ms frames (512 @ 16k).")]
        public float micAnalysisWindowSeconds = 0.032f;
        public bool isEcho = false;

        // ------------ Events ------------
        [Header("Events")]
        public UnityEvent OnVadStart;
        public UnityEvent OnVadEnd;

        [Header("Segmentation (for utterance output)")]
        public int preRollMs = 300;
        public int postRollMs = 150;

        [Header("Utterance Event (mono @ 16k)")]
        public UnityEvent<string> OnAsrResult;
        public event Action<float[]> OnUtteranceReady;

        // ------------ Internals ------------
        private SileroVadDetector vadDetector;
        private List<SileroSpeechSegment> speechSegments;
        private AudioSource audioSource;
        private string modelPath;

        // Mic mode
        private bool isMicModeActive = false;
        private string micDevice;

        // Sliding window @ 16k for detector
        private readonly List<float> live16k = new List<float>(16000);
        private int minSpeechSamples16k;
        private int callWindowSamples16k;

        // Hysteresis state
        private bool inSpeech = false;
        private int speechAccMs = 0;
        private int silenceAccMs = 0;

        // Utterance building
        private readonly Queue<float> preRollQ = new Queue<float>();
        private readonly List<float> currentUtt = new List<float>(16000 * 5);
        private int preRollSamples16k;
        private int postRollSamples16k;
        private float[] lastDetBlock; // last 32ms 16k block for post-roll

        private void Start()
        {
            modelPath = Path.Combine(Application.streamingAssetsPath, onnxModelFileName);
            vadDetector = new SileroVadDetector(
                modelPath,
                threshold,
                sampleRate,
                minSpeechDurationMs,
                maxSpeechDurationSeconds,
                (int)minSilenceDurationMs,
                speechPadMs
            );

            minSpeechSamples16k = Mathf.CeilToInt(sampleRate * (minSpeechDurationMs / 1000f)); // e.g. 4000
            callWindowSamples16k = Mathf.CeilToInt(sampleRate * 0.5f);                           // 500 ms
            preRollSamples16k = Mathf.Max(1, Mathf.RoundToInt(sampleRate * (preRollMs / 1000f)));
            postRollSamples16k = Mathf.Max(0, Mathf.RoundToInt(sampleRate * (postRollMs / 1000f)));
        }

        public void StartMicrophone()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone devices found.");
                return;
            }

            micDevice = Microphone.devices[0];

            audioSource = gameObject.AddComponent<AudioSource>();

            int requestRate = 48000;
            audioSource.clip = Microphone.Start(micDevice, true, 10, requestRate);

            float t0 = Time.realtimeSinceStartup;
            while (Microphone.GetPosition(micDevice) <= 0 && Time.realtimeSinceStartup - t0 < 3f) { }

            if (!audioSource.clip)
            {
                Debug.LogError("Mic failed to start.");
                return;
            }

            if (isEcho) audioSource.Play();

            isMicModeActive = true;

            int devRate = audioSource.clip.frequency;
            Debug.Log($"[SileroVAD] Mic OK. rate={devRate} Hz, ch={audioSource.clip.channels}. " +
                      $"Frame {Mathf.RoundToInt(devRate * micAnalysisWindowSeconds)} (device) -> " +
                      $"{Mathf.RoundToInt(sampleRate * micAnalysisWindowSeconds)} (16k). Model: {onnxModelFileName}");

            StartCoroutine(ProcessMicrophoneAudio());
        }

        public void StopMicrophone()
        { 
            if (audioSource != null)
            {
                Microphone.End(micDevice);
                Destroy(audioSource);
                audioSource = null;
                isMicModeActive = false;
            }
        }
        IEnumerator ProcessMicrophoneAudio()
        {
            int detRate = sampleRate;   
            int devRate = audioSource.clip.frequency;
            int channels = audioSource.clip.channels;

            int devFrames = Mathf.Max(1, Mathf.RoundToInt(devRate * micAnalysisWindowSeconds));
            int detFrames = Mathf.Max(1, Mathf.RoundToInt(detRate * micAnalysisWindowSeconds));

            float[] devInterleaved = new float[devFrames * channels];
            float[] devMono = new float[devFrames];
            float[] detMono = new float[detFrames];

            while (isMicModeActive)
            {
                int totalFrames = audioSource.clip.samples;
                int cur = Microphone.GetPosition(micDevice);

                int start = cur - devFrames;
                if (start < 0) start += totalFrames;

                if (start + devFrames <= totalFrames)
                {
                    audioSource.clip.GetData(devInterleaved, start);
                }
                else
                {
                    int aFrames = totalFrames - start;
                    int bFrames = devFrames - aFrames;

                    float[] a = new float[aFrames * channels];
                    float[] b = new float[bFrames * channels];
                    audioSource.clip.GetData(a, start);
                    audioSource.clip.GetData(b, 0);

                    Buffer.BlockCopy(a, 0, devInterleaved, 0, a.Length * sizeof(float));
                    Buffer.BlockCopy(b, 0, devInterleaved, a.Length * sizeof(float), b.Length * sizeof(float));
                }

                if (channels == 1)
                {
                    Buffer.BlockCopy(devInterleaved, 0, devMono, 0, devMono.Length * sizeof(float));
                }
                else
                {
                    int idx = 0;
                    for (int f = 0; f < devFrames; f++)
                    {
                        float sum = 0f;
                        for (int c = 0; c < channels; c++) sum += devInterleaved[idx++];
                        devMono[f] = sum / channels;
                    }
                }

                ResampleLinearBlock(devMono, devRate, detMono, detRate);

                lastDetBlock = (float[])detMono.Clone();

                if (!inSpeech)
                {
                    for (int i = 0; i < detMono.Length; i++)
                    {
                        preRollQ.Enqueue(detMono[i]);
                        while (preRollQ.Count > preRollSamples16k) preRollQ.Dequeue();
                    }
                }

                live16k.AddRange(detMono);
                if (live16k.Count > callWindowSamples16k)
                    live16k.RemoveRange(0, live16k.Count - callWindowSamples16k);

                if (live16k.Count >= minSpeechSamples16k)
                {
                    bool speech = vadDetector.IsSpeechDetected(live16k.ToArray(), 1); // mono
                    int frameMs = Mathf.RoundToInt(micAnalysisWindowSeconds * 1000f);

                    if (speech)
                    {
                        if (!inSpeech)
                        {
                            while (preRollQ.Count > 0) currentUtt.Add(preRollQ.Dequeue());
                        }
                        currentUtt.AddRange(detMono);

                        speechAccMs += frameMs;
                        silenceAccMs = 0;

                        if (!inSpeech && speechAccMs >= minSpeechDurationMs)
                        {
                            inSpeech = true;
                            Debug.Log("[VAD] START");
                            Furhat.Instance.partialSpeechRecognized();
                            OnVadStart?.Invoke();
                        }
                    }
                    else
                    {
                        speechAccMs = 0;
                        silenceAccMs += frameMs;

                        if (inSpeech && silenceAccMs >= minSilenceDurationMs)
                        {
                            if (lastDetBlock != null && postRollSamples16k > 0)
                            {
                                int n = Mathf.Min(postRollSamples16k, lastDetBlock.Length);
                                for (int i = 0; i < n; i++) currentUtt.Add(lastDetBlock[i]);
                            }

                            inSpeech = false;
                            Debug.Log("[VAD] END");
                            OnVadEnd?.Invoke();

                            if (currentUtt.Count > 0)
                            {
                                var utt = currentUtt.ToArray();
                                currentUtt.Clear();
                                preRollQ.Clear();
                                OnUtteranceReady?.Invoke(utt);
                                Debug.Log($"[VAD] Utterance ready: {utt.Length} samples (~{utt.Length / (float)sampleRate:0.00}s)");
                            }
                            silenceAccMs = 0;
                        }
                    }
                }

                // near real-time pacing
                yield return new WaitForSeconds(micAnalysisWindowSeconds);
            }
        }

        // ------------- Resample -------------
        public static void ResampleLinearBlock(float[] inMono, int inRate, float[] outMono, int outRate)
        {
            if (inRate == outRate)
            {
                Buffer.BlockCopy(inMono, 0, outMono, 0, outMono.Length * sizeof(float));
                return;
            }
            int inN = inMono.Length;
            int outN = outMono.Length;
            double ratio = (double)(inN - 1) / Math.Max(1, outN - 1); // map [0..out-1] -> [0..in-1]
            for (int i = 0; i < outN; i++)
            {
                double src = i * ratio;
                int i0 = (int)src;
                int i1 = Math.Min(i0 + 1, inN - 1);
                double t = src - i0;
                outMono[i] = (float)((1.0 - t) * inMono[i0] + t * inMono[i1]);
            }
        }

        // --------- File playback update ----------
        void Update()
        {
            if (speechSegments != null && audioSource != null && audioSource.clip != null && audioSource.isPlaying)
            {
                float currentTime = audioSource.time;
                bool isSpeech = false;

                foreach (var segment in speechSegments)
                {
                    if (currentTime >= segment.StartSecond && currentTime <= segment.EndSecond)
                    {
                        isSpeech = true;
                        break;
                    }
                }

                if (isSpeech)
                    Debug.Log("Voice detected.");
            }
        }
    }
}
