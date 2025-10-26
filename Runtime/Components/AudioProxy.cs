using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{
    public class AudioProxy : UdonSharpBehaviour
    {
        [Header("Audio Settings")]
        [HideInInspector] public string audioKey;
        public float volume = 1f;
        public bool loop = false;

        [Header("Timing Settings")]
        public float delay = 0f;
        public float fadeInTime = 0f;
        public float fadeOutTime = 0f;

        [Header("3D Audio Settings")]
        public float minDistance = 1f;
        public float maxDistance = 25f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
        public float spatialBlend = 1f;

        [Header("Source Settings")]
        public int priority = 128;
        public float pitch = 0.5f;
        public float dopplerLevel = 1f;
        public float panStereo = 0f;

        [Header("References")]
        public AudioManager audioManager;

        private AudioSource currentSource;
        private bool isPlaying = false;
        private float targetVolume;
        private float currentFadeTime;
        private bool isFadingIn = false;
        private bool isFadingOut = false;
        private AudioClip cachedClip;
        private int currentSourceIndex = -1;

        void Start()
        {
            if (!string.IsNullOrEmpty(audioKey) && audioManager != null)
            {
                cachedClip = audioManager.GetClip(audioKey);
                targetVolume = volume * audioManager.masterVolume;
            }
        }

        void Update()
        {
            if (!isPlaying && !isFadingIn && !isFadingOut) return;

            if (audioManager == null) return;
            if (currentSource != null && isPlaying && !isFadingIn && !isFadingOut)
            {
                float expectedVolume = volume * audioManager.masterVolume;
                if (!Mathf.Approximately(currentSource.volume, expectedVolume))
                {
                    currentSource.volume = expectedVolume;
                }
            }

            if (isFadingIn || isFadingOut)
            {
                currentFadeTime += Time.deltaTime;

                    if (isFadingIn && currentSource != null)
                    {
                        float fadeProgress = Mathf.Clamp01(currentFadeTime / fadeInTime);
                        currentSource.volume = Mathf.Lerp(0f, volume * audioManager.masterVolume, fadeProgress);

                        if (fadeProgress >= 1f)
                        {
                            isFadingIn = false;
                            currentFadeTime = 0f;
                        }
                    }
                else if (isFadingOut && currentSource != null)
                {
                    float fadeProgress = Mathf.Clamp01(currentFadeTime / fadeOutTime);
                    currentSource.volume = Mathf.Lerp(targetVolume, 0f, fadeProgress);

                    if (fadeProgress >= 1f)
                    {
                        isFadingOut = false;
                        currentFadeTime = 0f;
                        currentSource.Stop();
                        currentSource.clip = null;

                        if (currentSourceIndex >= 0)
                        {
                            if (loop) audioManager.ReleaseIndexToLooping(currentSourceIndex);
                            else audioManager.ReleaseIndexToOneShot(currentSourceIndex);
                            currentSourceIndex = -1;
                        }
                        else
                        {
                            AudioSource[] pool = loop ? audioManager.loopingPool : audioManager.oneShotPool;
                            bool[] inUse = loop ? audioManager.loopingInUse : audioManager.oneShotInUse;
                            audioManager.ReleaseToPool(currentSource, pool, inUse);
                        }

                        isPlaying = false;
                        currentSource = null;
                    }
                }
            }
        }

        public void PlayAudio()
        {
            if (string.IsNullOrEmpty(audioKey) || audioManager == null) return;

            AudioClip audioClip = cachedClip != null ? cachedClip : audioManager.GetClip(audioKey);
            if (audioClip == null) return;

            AudioSource[] pool = loop ? audioManager.loopingPool : audioManager.oneShotPool;
            bool[] inUse = loop ? audioManager.loopingInUse : audioManager.oneShotInUse;

            currentSourceIndex = -1;
            if (audioManager != null)
            {
                if (!loop)
                {
                    currentSourceIndex = audioManager.AllocateIndexFromOneShot();
                    if (currentSourceIndex >= 0) currentSource = audioManager.oneShotPool[currentSourceIndex];
                }
                else
                {
                    currentSourceIndex = audioManager.AllocateIndexFromLooping();
                    if (currentSourceIndex >= 0) currentSource = audioManager.loopingPool[currentSourceIndex];
                }
            }

            if (currentSource == null)
            {
                currentSource = audioManager.AllocateFromPool(pool, inUse);
                if (currentSource == null) return; 
            }

            currentSource.clip = audioClip;
            currentSource.loop = loop;
            currentSource.minDistance = minDistance;
            currentSource.maxDistance = maxDistance;
            currentSource.rolloffMode = rolloffMode;
            currentSource.spatialBlend = spatialBlend;
            currentSource.priority = Mathf.Clamp(priority, 0, 256);
            currentSource.pitch = Mathf.Pow(2f, (pitch - 0.5f) * 2f);
            currentSource.dopplerLevel = dopplerLevel;
            currentSource.panStereo = Mathf.Clamp(panStereo, -1f, 1f);
            currentSource.transform.position = transform.position;

            if (fadeInTime > 0f)
            {
                currentSource.volume = 0f;
                isFadingIn = true;
                currentFadeTime = 0f;
            }
                    else
                    {
                        currentSource.volume = volume * audioManager.masterVolume;
                    }

            if (delay > 0f)
            {
                currentSource.PlayDelayed(delay);
            }
            else
            {
                currentSource.Play();
            }

            isPlaying = true;

            if (!loop)
            {
                audioManager.ScheduleRelease(currentSource, pool, inUse, audioClip.length + delay);
            }
        }

        public void StopAudio()
        {
            if (currentSource == null || !isPlaying) return;

            if (fadeOutTime > 0f && !isFadingOut)
            {
                targetVolume = currentSource.volume;
                isFadingOut = true;
                currentFadeTime = 0f;
                return;
            }
            
            if (isFadingOut)
            {
                return;
            }

            ForceStopAudio();
        }

        public void ForceStopAudio()
        {
            if (currentSource == null) return;

            isFadingIn = false;
            isFadingOut = false;
            currentFadeTime = 0f;

            currentSource.Stop();
            currentSource.clip = null;

            if (currentSourceIndex >= 0)
            {
                if (loop) audioManager.ReleaseIndexToLooping(currentSourceIndex);
                else audioManager.ReleaseIndexToOneShot(currentSourceIndex);
                currentSourceIndex = -1;
            }
            else
            {
                AudioSource[] pool = loop ? audioManager.loopingPool : audioManager.oneShotPool;
                bool[] inUse = loop ? audioManager.loopingInUse : audioManager.oneShotInUse;
                if (audioManager != null)
                {
                    audioManager.ReleaseToPool(currentSource, pool, inUse);
                }
            }

            isPlaying = false;
            currentSource = null;
        }

        public void UpdateVolume()
        {
            if (currentSource != null && isPlaying)
            {
                float newVolume = volume * audioManager.masterVolume;

                if (!isFadingIn && !isFadingOut)
                {
                    currentSource.volume = newVolume;
                }
                else
                {
                    targetVolume = volume * audioManager.masterVolume;
                }
            }
        }

        public bool IsPlaying()
        {
            return isPlaying && currentSource != null && currentSource.isPlaying;
        }

        public bool IsFadingIn()
        {
            return isFadingIn;
        }

        public bool IsFadingOut()
        {
            return isFadingOut;
        }

        public bool HasActiveSource()
        {
            return currentSource != null;
        }
    }
}
