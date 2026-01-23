using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using VadDotNet;
using Whisper;
using Whisper.Utils;
using Debug = UnityEngine.Debug;

namespace OPEN.PandemicAI
{
    /// VAD (utterance) → whisper.unity (WhisperManager) using AudioChunk
    public class Transcription : MonoBehaviour
    {
        [Header("Wiring (auto-filled if left empty)")]
        public SileroVadRunner vad;
        public WhisperManager whisper;

        [Header("Options")]
        public bool printLanguage = true;
        public bool logTiming = true;

        private readonly Queue<float[]> _queue = new Queue<float[]>();
        private bool _running;

        private void OnEnable()
        {
            if (vad) vad.OnUtteranceReady += OnUtteranceReady;
        }

        private void OnDisable()
        {
            if (vad) vad.OnUtteranceReady -= OnUtteranceReady;
            _queue.Clear();
            _running = false;
        }

        // VAD delivers mono@16k PCM (-1..1)
        private void OnUtteranceReady(float[] mono16k)
        {
            if (mono16k == null || mono16k.Length == 0) return;
            if (!whisper)
            {
                Debug.LogError("[ASR] WhisperManager not found/assigned.");
                return;
            }

            _queue.Enqueue(mono16k);
            if (!_running) StartCoroutine(ProcessQueue());
        }

        private System.Collections.IEnumerator ProcessQueue()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                var pcm = _queue.Dequeue();
                bool done = false;
                TranscribeAsync(pcm).ContinueWith(_ => done = true);
                while (!done) yield return null;
            }
            _running = false;
        }

        private async Task TranscribeAsync(float[] mono16k)
        {
            try
            {
                // Build the same container the MicrophoneDemo uses
                var chunk = new AudioChunk
                {
                    Data = mono16k,
                    Frequency = 16000,
                    Channels = 1
                };

                Debug.Log($"[ASR] Transcribing {chunk.Data.Length} samples @ {chunk.Frequency} Hz (~{chunk.Length:0.00}s)…");

                var sw = new Stopwatch();
                if (logTiming) sw.Start();

                // Exactly like MicrophoneDemo: float[] + sample rate + channels
                var res = await whisper.GetTextAsync(chunk.Data, chunk.Frequency, chunk.Channels);

                if (logTiming) sw.Stop();

                if (res == null)
                {
                    Debug.LogWarning("[ASR] Whisper returned null result.");
                    return;
                }

                string text = res.Result ?? "";
                if (printLanguage && !string.IsNullOrEmpty(res.Language))
                    text += $"  [lang: {res.Language}]";

                if (logTiming)
                {
                    float rt = chunk.Length / Mathf.Max(0.001f, sw.ElapsedMilliseconds / 1000f);
                    Debug.Log($"[ASR] {text}\n[time: {sw.ElapsedMilliseconds} ms | rate: {rt:F1}x]");
                    Furhat.Instance.speechRecognized(text);
                }
                else
                {
                    Debug.Log("[ASR] " + text);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ASR] Whisper error: " + e.Message);
            }
        }
    }
}