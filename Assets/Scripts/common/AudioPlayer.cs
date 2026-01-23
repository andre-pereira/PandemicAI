using UnityEngine;
using System.Collections.Generic;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Manages the playback of audio clips using a singleton pattern.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlayer : MonoBehaviour
    {
        #region Nested Types

        /// <summary>
        /// Enumeration for referencing audio clips by name.
        /// </summary>
        public enum AudioClipEnum
        {
            INVALID = -1,
            INTRO,
            CLICK,
            CARD_FLIP,
            CHIP,
            SHUFFLE
        }

        #endregion

        #region Fields

        /// <summary>
        /// Singleton instance of the AudioPlayer.
        /// </summary>
        private static AudioPlayer thePlayer = null;

        /// <summary>
        /// Array of audio clips corresponding to the AudioClipEnum values.
        /// </summary>
        public AudioClip[] EnumeratedClips;

        /// <summary>
        /// Cached reference to the attached AudioSource component.
        /// </summary>
        private AudioSource _mySource = null;

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Initializes the singleton instance and caches the AudioSource component.
        /// </summary>
        private void Awake()
        {
            thePlayer = this;
            _mySource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Clears the singleton reference when this instance is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (thePlayer == this)
                thePlayer = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays the specified audio clip, with an optional delay and volume.
        /// </summary>
        /// <param name="clipEnum">The enumeration value identifying the clip to play.</param>
        /// <param name="delay">Delay before playback starts (in seconds). Default is 0.</param>
        /// <param name="volume">Playback volume. Default is 1.</param>
        public static void PlayClip(AudioClipEnum clipEnum, float delay = 0, float volume = 1)
        {
            if (thePlayer == null)
            {
                Debug.LogError("AudioPlayer instance is not available.");
                return;
            }

            int index = (int)clipEnum;
            if (index < 0 || index >= thePlayer.EnumeratedClips.Length)
            {
                Debug.LogError("Index out of range for clip: " + clipEnum.ToString());
                return;
            }

            AudioClip clip = thePlayer.EnumeratedClips[index];
            if (clip == null)
            {
                Debug.LogError("Clip not found: " + clipEnum.ToString());
                return;
            }

            if (delay > 0)
            {
                // Assumes an extension method ExecuteLater exists on MonoBehaviour.
                thePlayer.ExecuteLater(delay, () =>
                {
                    thePlayer._mySource.PlayOneShot(clip, volume);
                });
            }
            else
            {
                thePlayer._mySource.PlayOneShot(clip, volume);
            }
        }

        /// <summary>
        /// Repeats playing the specified audio clip a number of times with delays between repeats.
        /// </summary>
        /// <param name="clipEnum">The enumeration value identifying the clip to repeat.</param>
        /// <param name="times">Number of times to repeat the clip.</param>
        /// <param name="delay">Initial delay before the first playback (in seconds). Default is 0.</param>
        public static void RepeatClip(AudioClipEnum clipEnum, int times, float delay = 0)
        {
            if (thePlayer == null)
            {
                Debug.LogError("AudioPlayer instance is not available.");
                return;
            }

            int index = (int)clipEnum;
            if (index < 0 || index >= thePlayer.EnumeratedClips.Length)
            {
                Debug.LogError("Index out of range for clip: " + clipEnum.ToString());
                return;
            }

            AudioClip clip = thePlayer.EnumeratedClips[index];
            if (clip == null)
            {
                Debug.LogError("Clip not found: " + clipEnum.ToString());
                return;
            }

            // Set the clip on the AudioSource
            thePlayer._mySource.clip = clip;

            for (int i = 0; i < times; ++i)
            {
                float scheduledDelay = delay + clip.length * i;
                thePlayer.ExecuteLater(scheduledDelay, () => thePlayer._mySource.Play());
            }
        }

        /// <summary>
        /// Stops any audio that is currently playing.
        /// </summary>
        public static void Stop()
        {
            if (thePlayer == null)
            {
                Debug.LogError("AudioPlayer instance is not available.");
                return;
            }
            thePlayer._mySource.Stop();
        }

        /// <summary>
        /// Gets the length of the audio clip corresponding to the provided enumeration value.
        /// </summary>
        /// <param name="clipEnum">The enumeration value identifying the clip.</param>
        /// <returns>The length of the clip in seconds; returns 0 if the clip is not found.</returns>
        public static float ClipLength(AudioClipEnum clipEnum)
        {
            if (thePlayer == null)
            {
                Debug.LogError("AudioPlayer instance is not available.");
                return 0;
            }

            int index = (int)clipEnum;
            if (index < 0 || index >= thePlayer.EnumeratedClips.Length)
            {
                Debug.LogError("Index out of range for clip: " + clipEnum.ToString());
                return 0;
            }

            AudioClip clip = thePlayer.EnumeratedClips[index];
            if (clip == null)
            {
                Debug.LogError("Clip not found: " + clipEnum.ToString());
                return 0;
            }
            return clip.length;
        }

        #endregion
    }
}
