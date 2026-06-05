using System;
using System.Collections;
using UnityEngine;

namespace Ambience
{
    /// <summary>
    /// Transition curve types for audio fading
    /// </summary>
    public enum TransitionCurve
    {
        Linear,              // Straight line fade
        ExponentialIn,       // Slow start, fast end
        ExponentialOut,      // Fast start, slow end
        LogarithmicIn,       // Fast start, slow end (perceptually linear)
        LogarithmicOut,      // Slow start, fast end (perceptually linear)
        SCurve,              // Smooth start and end
        ConstantPower        // Maintains constant perceived loudness during crossfade
    }

    /// <summary>
    /// Helper class for smooth audio transitions between music tracks
    /// </summary>
    public class MusicTransitionHelper : MonoBehaviour
    {
        #region Singleton

        private static MusicTransitionHelper instance;
        public static MusicTransitionHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("MusicTransitionHelper");
                    instance = go.AddComponent<MusicTransitionHelper>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Active Transitions

        private Coroutine currentFadeOutCoroutine;
        private Coroutine currentFadeInCoroutine;
        private Coroutine currentCrossfadeCoroutine;

        #endregion

        #region Public API - Single Track Transitions

        /// <summary>
        /// Fade out an audio source
        /// </summary>
        public void FadeOut(AudioSource source, float duration, TransitionCurve curve = TransitionCurve.Linear, Action onComplete = null)
        {
            if (source == null) return;

            if (currentFadeOutCoroutine != null)
            {
                StopCoroutine(currentFadeOutCoroutine);
            }

            currentFadeOutCoroutine = StartCoroutine(FadeCoroutine(source, source.volume, 0f, duration, curve, () =>
            {
                source.Pause();
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Fade in an audio source
        /// </summary>
        public void FadeIn(AudioSource source, float targetVolume, float duration, TransitionCurve curve = TransitionCurve.Linear, Action onComplete = null)
        {
            if (source == null) return;

            source.volume = 0f;
            if (!source.isPlaying)
            {
                source.Play();
            } else
            {
                source.UnPause();
            }

            if (currentFadeInCoroutine != null)
            {
                StopCoroutine(currentFadeInCoroutine);
            }

            currentFadeInCoroutine = StartCoroutine(FadeCoroutine(source, source.volume, targetVolume, duration, curve, onComplete));
        }

        /// <summary>
        /// Stop all active transitions
        /// </summary>
        public void StopAllTransitions()
        {
            if (currentFadeOutCoroutine != null)
            {
                StopCoroutine(currentFadeOutCoroutine);
                currentFadeOutCoroutine = null;
            }

            if (currentFadeInCoroutine != null)
            {
                StopCoroutine(currentFadeInCoroutine);
                currentFadeInCoroutine = null;
            }

            if (currentCrossfadeCoroutine != null)
            {
                StopCoroutine(currentCrossfadeCoroutine);
                currentCrossfadeCoroutine = null;
            }
        }

        #endregion

        #region Public API - Crossfade

        /// <summary>
        /// Crossfade between two audio sources
        /// </summary>
        public void Crossfade(
            AudioSource fadeOutSource,
            AudioSource fadeInSource,
            float duration,
            float targetVolume,
            TransitionCurve curve = TransitionCurve.ConstantPower,
            Action onComplete = null)
        {
            if (fadeOutSource == null || fadeInSource == null) return;

            if (currentCrossfadeCoroutine != null)
            {
                StopCoroutine(currentCrossfadeCoroutine);
            }

            currentCrossfadeCoroutine = StartCoroutine(CrossfadeCoroutine(
                fadeOutSource,
                fadeInSource,
                duration,
                targetVolume,
                curve,
                onComplete));
        }

        #endregion

        #region Coroutines

        private IEnumerator FadeCoroutine(
            AudioSource source,
            float startVolume,
            float targetVolume,
            float duration,
            TransitionCurve curve,
            Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Apply curve
                float curvedT = ApplyCurve(t, curve);

                // Set volume
                source.volume = Mathf.Lerp(startVolume, targetVolume, curvedT);

                yield return null;
            }

            // Ensure final volume is set
            source.volume = targetVolume;

            onComplete?.Invoke();
        }

        private IEnumerator CrossfadeCoroutine(
            AudioSource fadeOutSource,
            AudioSource fadeInSource,
            float duration,
            float targetVolume,
            TransitionCurve curve,
            Action onComplete)
        {
            float startVolumeOut = fadeOutSource.volume;
            float startVolumeIn = 0f;

            // Start fade-in source if not playing
            if (!fadeInSource.isPlaying)
            {
                fadeInSource.volume = 0f;
                fadeInSource.Play();
            }
            else
            {
                startVolumeIn = fadeInSource.volume;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (curve == TransitionCurve.ConstantPower)
                {
                    // Constant power crossfade (uses sin/cos for equal power)
                    float angle = t * Mathf.PI * 0.5f;
                    fadeOutSource.volume = Mathf.Cos(angle) * startVolumeOut;
                    fadeInSource.volume = Mathf.Sin(angle) * targetVolume;
                }
                else
                {
                    // Apply curve to both sources
                    float curvedT = ApplyCurve(t, curve);
                    fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, curvedT);
                    fadeInSource.volume = Mathf.Lerp(startVolumeIn, targetVolume, curvedT);
                }

                yield return null;
            }

            // Ensure final volumes are set
            fadeOutSource.volume = 0f;
            fadeOutSource.Stop();
            fadeInSource.volume = targetVolume;

            onComplete?.Invoke();
        }

        #endregion

        #region Curve Functions

        /// <summary>
        /// Apply transition curve to normalized time value (0-1)
        /// </summary>
        private float ApplyCurve(float t, TransitionCurve curve)
        {
            switch (curve)
            {
                case TransitionCurve.Linear:
                    return t;

                case TransitionCurve.ExponentialIn:
                    // y = x^3 (slow start, fast end)
                    return t * t * t;

                case TransitionCurve.ExponentialOut:
                    // y = 1 - (1-x)^3 (fast start, slow end)
                    float invT = 1f - t;
                    return 1f - (invT * invT * invT);

                case TransitionCurve.LogarithmicIn:
                    // Perceptually linear fade-in
                    return Mathf.Log10(1f + t * 9f);

                case TransitionCurve.LogarithmicOut:
                    // Perceptually linear fade-out
                    return 1f - Mathf.Log10(1f + (1f - t) * 9f);

                case TransitionCurve.SCurve:
                    // Smoothstep (smooth start and end)
                    return t * t * (3f - 2f * t);

                case TransitionCurve.ConstantPower:
                    // This is handled specially in CrossfadeCoroutine
                    // For single fades, use S-curve as fallback
                    return t * t * (3f - 2f * t);

                default:
                    return t;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Immediately stop an audio source without fade
        /// </summary>
        public void ImmediateStop(AudioSource source)
        {
            if (source == null) return;

            StopAllTransitions();
            source.Stop();
            source.volume = 0f;
        }

        /// <summary>
        /// Swap audio source content (for layer swapping like in AmbienceProvider)
        /// </summary>
        public void SwapAudioSourceContent(AudioSource source, AudioSource target)
        {
            if (source == null || target == null) return;

            target.clip = source.clip;
            target.volume = source.volume;
            target.time = source.time;
            target.loop = source.loop;

            if (source.isPlaying)
            {
                target.Play();
            }
        }

        /// <summary>
        /// WrapTime helper
        /// </summary>
        private static float WrapTime(float time, float length)
        {
            if (length <= 0f)
                return 0f;

            time %= length;

            if (time < 0f)
                time += length;

            return time;
        }


        /// <summary>
        /// Calculate song start point from TransitionOffsetMode
        /// </summary>
        
        public static float CalculateTransitionEnterTimeSeconds(
            MusicClipData fromData,
            AudioSource fromSource,
            MusicClipData toData
        )
        {
            // Safety
            if (fromData == null || toData == null || fromSource.clip == null)
                return 0f;

            if (toData.transitionOffset == TransitionOffsetMode.None)
                return 0f;

            float fromSecondsPerBar = fromData.SecondsPerBar;
            float toSecondsPerBar   = toData.SecondsPerBar;

            // How many bars have passed in the CURRENT song
            float barsPassed = fromSource.time / fromSecondsPerBar;

            // Convert that bar count into seconds in the DESTINATION song
            float offsetSeconds = barsPassed * toSecondsPerBar;

            float result;

            switch (toData.transitionOffset)
            {
                case TransitionOffsetMode.Relative:
                    // start + previousBarsPassed
                    result = offsetSeconds;
                    break;

                case TransitionOffsetMode.Inverted:
                    // end - previousBarsPassed
                    result = toData.Clip.length - offsetSeconds;
                    break;

                default:
                    result = 0f;
                    break;
            }

            return WrapTime(result, toData.Clip.length);
        }


        #endregion
    }
}
