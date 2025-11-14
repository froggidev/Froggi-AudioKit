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

        public bool zoneControlsFade = false;
        
        // --- CONSOLIDATED FADE TIME ---
        [SerializeField] private float fadeInTime = 0f;
        // [SerializeField] private float fadeInTime = 0f; // Removed
        // [SerializeField] private float fadeOutTime = 0f; // Removed

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
        private AudioClip cachedClip;
        private int currentSourceIndex = -1;

        public bool useSpatializeDistance = false;
        public float spatializeDistance = 1f;

        // --- NEW FADE MODEL ---
        private bool _isLooping = false;
        private float rawAudioVolumeTarget; // The volume we are fading TOWARDS

        void Start()
        {
            if (!string.IsNullOrEmpty(audioKey) && audioManager != null)
            {
                cachedClip = audioManager.GetClip(audioKey);
            }
            rawAudioVolumeTarget = 0f; // Start at 0
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

            // --- Set initial volume based on fadeTime ---
            //float targetMaxVolume = volume * audioManager.masterVolume;
            //if (fadeInTime > 0f)
            //{
            //    currentSource.volume = 0f;
            //}
            //else
            //{
            //    currentSource.volume = targetMaxVolume;
            //}
            // (The loop will set the target)

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
            _LoopStart(true); // Tell the loop to fade IN
        }
        public void StopAudio()
        {
            _LoopStart(false); // Tell the loop to fade OUT
        }
        public void ForceStopAudio()
        {
            if (currentSource == null) return;

            rawAudioVolumeTarget = 0f; // Set target to 0

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
                // If the target isn't 0, update it to the new max volume
                if (!Mathf.Approximately(rawAudioVolumeTarget, 0f))
                {
                    rawAudioVolumeTarget = volume * audioManager.masterVolume;
                }
            }
        }
        public bool IsPlaying()
        {
            return isPlaying && currentSource != null && currentSource.isPlaying;
        }

        // --- Fading status is now based on volume vs target ---
        public bool IsFadingIn()
        {
            return currentSource != null && 
                   !Mathf.Approximately(currentSource.volume, rawAudioVolumeTarget) && 
                   rawAudioVolumeTarget > currentSource.volume;
        }
        public bool IsFadingOut()
        {
            return currentSource != null &&
                   !Mathf.Approximately(currentSource.volume, rawAudioVolumeTarget) &&
                   rawAudioVolumeTarget < currentSource.volume;
        }
        public bool HasActiveSource()
        {
            return currentSource != null;
        }

        public void _SetFadeTimes(float _in, float _out)
        {
            if (!zoneControlsFade) return;
            // --- Use the 'in' time as the new unified fadeTime ---
            // You could also average them: (_in + _out) / 2f
            fadeInTime = _in;
        }

        // #############################################################################################################
        #region Loop Stuff

        // --- __GetFadeTime is no longer needed ---

        public void _LoopStart(bool __playerEnter)
        {
            // --- This function just sets the target volume ---
            float targetMaxVolume = volume * audioManager.masterVolume;
            rawAudioVolumeTarget = __playerEnter ? targetMaxVolume : 0f;

            if (!_isLooping && __playerEnter)
            {
                _isLooping = true;
                _Loop();
            }
        }
        public void _LoopEnd()
        {
            _isLooping = false;
            
            if (currentSource == null) return;
            
            currentSource.Stop();
            currentSource.clip = null;
            currentSource.volume = 0f;

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

        public void _RequestLoopEnd()
        {
            _isLooping = false;
        }
        public void _Loop()
        {
            if (currentSource == null || !_isLooping)
            {
                _LoopEnd();
                return;
            }

            __FadeLoop();
            __SpatializeLoop();
            
            SendCustomEventDelayedFrames(nameof(_Loop), 0);
        }

        private void __FadeLoop()
        {
            // Get the maximum possible volume (used to calculate fade speed)
            float targetMaxVolume = volume * audioManager.masterVolume;
            float localFadeTime = fadeInTime;

            // Calculate the speed (volume per second)
            // We use targetMaxVolume as the "range"
            float speed = targetMaxVolume / localFadeTime;
                
            // Calculate the max change for this frame
            float maxDelta = speed * Time.deltaTime;

            // Move the current volume towards the target
            currentSource.volume = Mathf.MoveTowards(currentSource.volume, rawAudioVolumeTarget, maxDelta);
            
            // Handle instant fades (or master volume is 0)
            if (localFadeTime <= 0.001f || targetMaxVolume <= 0.001f)
            {
                // If master volume is 0, targetMaxVolume is 0. 
                // rawAudioVolumeTarget will be 0 (if fading out) or 0 (if fading in).
                // So snapping to rawAudioVolumeTarget is correct.
                //currentSource.volume = rawAudioVolumeTarget;
            }
            else
            {
                
            }

            // Check if we've reached the target
            if (Mathf.Approximately(currentSource.volume, rawAudioVolumeTarget))
            {
                // If the target was 0, we are done fading out, so stop the loop.
                if (Mathf.Approximately(rawAudioVolumeTarget, 0f))
                {
                    _RequestLoopEnd(); // The loop will stop automatically.
                }
                // If the target was > 0, we just sit at the max volume.
                // The loop continues, but MoveTowards does nothing.
            }
        }

        private void __SpatializeLoop()
        {
            if (!useSpatializeDistance) return;
            float _playerDistance = Vector3.Distance(Networking.LocalPlayer.GetPosition(),transform.position);
            currentSource.spatialize = !(_playerDistance <= spatializeDistance);
        }

        #endregion
        // #############################################################################################################
    }
}
