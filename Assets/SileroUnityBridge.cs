using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VadDotNet; // your SileroVadOnnxModel namespace

public class SileroVadUnityBridge : MonoBehaviour
{
    [Header("Model")]
    [Tooltip("ONNX filename relative to StreamingAssets (e.g. 'silero_vad.onnx')")]
    public string onnxModelFileName = "silero_vad.onnx";

    [Header("VAD thresholds")]
    [Range(0f, 1f)] public float threshold = 0.5f;
    public int minSpeechMs = 250;
    public int minSilenceMs = 200;

    [Header("Mic")]
    [Tooltip("Leave empty for default device")]
    public string deviceName = null;

    [Header("Diagnostics")]
    [Tooltip("Log probability every N frames (0 = off).")]
    public int logEveryNFrames = 10;
    [Tooltip("Clamp output of model into [0,1] (safety).")]
    public bool clampProb = true;

    [Header("Events")]
    public UnityEvent OnSpeechStart;
    public UnityEvent OnSpeechEnd;
    [Serializable] public class FloatEvent : UnityEvent<float> { }
    public FloatEvent OnProbability;

    // ---- internals ----
    private const int TargetRate = 16000;
    private const int FrameLen = 512; // 32ms @ 16k
    private AudioClip micClip;
    private int micRate, micChannels, lastPos;
    private int deviceFrameSamples;
    private float[] micBlock, monoBlock, frame16k;
    private SileroVadOnnxModel model;
    private bool inSpeech;
    private int speechAccMs, silenceAccMs;
    private int frameCounter;

    void Start()
    {
        Application.runInBackground = true;

        var modelPath = Path.Combine(Application.streamingAssetsPath, onnxModelFileName);
        if (!File.Exists(modelPath))
        {
            Debug.LogError($"[SileroVAD] Model not found at: {modelPath}");
            enabled = false; return;
        }

        try
        {
            model = new SileroVadOnnxModel(modelPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SileroVAD] ONNX init failed: {e.Message}");
            enabled = false; return;
        }

        // Start mic (Unity may override requested rate)
        micClip = Microphone.Start(deviceName, true, 1, 48000);
        var t0 = Time.realtimeSinceStartup;
        while (Microphone.GetPosition(deviceName) <= 0 && Time.realtimeSinceStartup - t0 < 3f) { }

        if (!micClip)
        {
            Debug.LogError("[SileroVAD] Microphone failed to start. Check permissions & device name.");
            enabled = false; return;
        }

        micRate = micClip.frequency;
        micChannels = micClip.channels;
        deviceFrameSamples = Mathf.Max(1, micRate * 32 / 1000);

        micBlock = new float[deviceFrameSamples * micChannels];
        monoBlock = new float[deviceFrameSamples];
        frame16k = new float[FrameLen];

        lastPos = 0;
        frameCounter = 0;

        Debug.Log($"[SileroVAD] Mic OK. rate={micRate}Hz, channels={micChannels}. " +
                  $"Frame: {deviceFrameSamples} (device) -> {FrameLen} (16k). " +
                  $"Model: {onnxModelFileName}");
    }

    void OnDestroy()
    {
        try { model?.Dispose(); } catch { }
        if (micClip && Microphone.IsRecording(deviceName)) Microphone.End(deviceName);
    }

    void Update()
    {
        if (!micClip || model == null) return;

        int pos = Microphone.GetPosition(deviceName);
        int avail = pos - lastPos;
        if (avail < 0) avail += micClip.samples;

        while (avail >= deviceFrameSamples)
        {
            // 1) read device frame
            micClip.GetData(micBlock, lastPos);

            // 2) mix to mono
            MixToMono(micBlock, micChannels, monoBlock);

            // 3) resample to 16k / 512
            ResampleLinear(monoBlock, micRate, frame16k, TargetRate);

            // 4) run VAD model
            float prob = RunVadFrame(frame16k);
            if (clampProb) prob = Mathf.Clamp01(prob);
            if (logEveryNFrames > 0 && (frameCounter++ % logEveryNFrames == 0))
                Debug.Log($"[SileroVAD] p={prob:0.000}");

            OnProbability?.Invoke(prob);

            // 5) hysteresis
            bool speechNow = prob >= threshold;
            if (speechNow)
            {
                speechAccMs += 32;
                silenceAccMs = 0;
                if (!inSpeech && speechAccMs >= minSpeechMs)
                {
                    inSpeech = true;
                    Debug.Log("[SileroVAD] START");
                    OnSpeechStart?.Invoke();
                }
            }
            else
            {
                silenceAccMs += 32;
                if (inSpeech && silenceAccMs >= minSilenceMs)
                {
                    inSpeech = false;
                    speechAccMs = 0;
                    Debug.Log("[SileroVAD] END");
                    OnSpeechEnd?.Invoke();
                }
            }

            // 6) advance ring
            lastPos += deviceFrameSamples;
            lastPos %= micClip.samples;
            avail -= deviceFrameSamples;
        }
    }

    float RunVadFrame(float[] frame)
    {
        try
        {
            // model.Call expects float[batch][] with exact length 512 at 16k
            var batch = new float[1][] { frame };
            var outArr = model.Call(batch, TargetRate);
            if (outArr == null || outArr.Length == 0) return 0f;
            return outArr[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"[SileroVAD] Inference error: {e.Message}");
            return 0f;
        }
    }

    static void MixToMono(float[] interleaved, int channels, float[] monoOut)
    {
        if (channels <= 1) { Array.Copy(interleaved, monoOut, monoOut.Length); return; }
        int frames = monoOut.Length;
        for (int i = 0; i < frames; i++)
        {
            float sum = 0f;
            int baseIdx = i * channels;
            for (int c = 0; c < channels; c++) sum += interleaved[baseIdx + c];
            monoOut[i] = sum / channels;
        }
    }

    static void ResampleLinear(float[] inMono, int inRate, float[] outMono, int outRate)
    {
        if (inRate == outRate) { Array.Copy(inMono, outMono, outMono.Length); return; }
        float ratio = (float)inRate / outRate;
        int outLen = outMono.Length;
        int inLast = Mathf.Max(0, inMono.Length - 1);
        for (int i = 0; i < outLen; i++)
        {
            float srcPos = i * ratio;
            int i0 = (int)srcPos;
            int i1 = Mathf.Min(i0 + 1, inLast);
            float t = srcPos - i0;
            outMono[i] = inMono[i0] * (1f - t) + inMono[i1] * t;
        }
    }
}
