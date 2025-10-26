
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace Froggi.AudioKit
{
    public class AudioGroup : UdonSharpBehaviour
    {
        [Header("Audio Group Settings")]
        [Range(0f, 2f)]
        public float groupVolume = 1f;
        
        [Header("Audio Proxies")]
        public AudioProxy[] audioProxies;
        
        [Header("UI Slider")]
        public UnityEngine.UI.Slider volumeSlider;

        [Header("Slider Settings")]
        public float defaultValue = 1f;
        public float minValue = 0f;
        public float maxValue = 2f;

        // Persistence removed for now
        
    
        private float[] originalVolumes;
        private bool isInitialized = false;

        void Start()
        {
            InitializeGroup();

        
            if (volumeSlider != null)
            {
                volumeSlider.minValue = minValue;
                volumeSlider.maxValue = maxValue;
            }

        
        
            SetVolumeInternal(defaultValue, true);
        }

        void InitializeGroup()
        {
            if (audioProxies == null || audioProxies.Length == 0)
            {
                isInitialized = true;
                return;
            }

            
            originalVolumes = new float[audioProxies.Length];
            for (int i = 0; i < audioProxies.Length; i++)
            {
                if (audioProxies[i] != null)
                {
                    originalVolumes[i] = audioProxies[i].volume;
                }
            }

            isInitialized = true;
            UpdateProxyVolumes();
        }

        public void UpdateProxyVolumes()
        {
            if (!isInitialized || audioProxies == null || originalVolumes == null)
                return;

            for (int i = 0; i < audioProxies.Length; i++)
            {
                if (audioProxies[i] != null && i < originalVolumes.Length)
                {
                    
                    audioProxies[i].volume = originalVolumes[i] * groupVolume;
                    
                    
                    audioProxies[i].UpdateVolume();
                }
            }
        }

        
        public void SetVolume(float newVolume)
        {
            groupVolume = Mathf.Clamp(newVolume, 0f, 2f);
            UpdateProxyVolumes();
        }


        public void UpdateAudioGroup(float newVolume)
        {
            SetVolumeInternal(newVolume, true);
        }

        public void _UpdateAudioGroup()
        {
            if (volumeSlider == null) return;
            float sliderValue = Mathf.Clamp(volumeSlider.value, minValue, maxValue);
            SetVolumeInternal(sliderValue, true);
        }

        private void SetVolumeInternal(float volume, bool save)
        {
            float clamped = Mathf.Clamp(volume, minValue, maxValue);
            groupVolume = clamped;

            UpdateProxyVolumes();

            if (volumeSlider != null)
            {
                volumeSlider.value = clamped;
            }

        }

        public float GetVolume()
        {
            return groupVolume;
        }

        
        public void PlayAll()
        {
            if (audioProxies == null) return;

            for (int i = 0; i < audioProxies.Length; i++)
            {
                if (audioProxies[i] != null)
                {
                    audioProxies[i].PlayAudio();
                }
            }
        }

        public void StopAll()
        {
            if (audioProxies == null) return;

            for (int i = 0; i < audioProxies.Length; i++)
            {
                if (audioProxies[i] != null)
                {
                    audioProxies[i].StopAudio();
                }
            }
        }

    
        

        public void RefreshOriginalVolumes()
        {
            if (audioProxies == null) return;

            if (originalVolumes == null || originalVolumes.Length != audioProxies.Length)
            {
                originalVolumes = new float[audioProxies.Length];
            }

            for (int i = 0; i < audioProxies.Length; i++)
            {
                if (audioProxies[i] != null)
                {
                    originalVolumes[i] = groupVolume > 0 ? audioProxies[i].volume / groupVolume : audioProxies[i].volume;
                }
            }
        }

        void OnValidate()
        {
            if (isInitialized)
            {
                UpdateProxyVolumes();
            }
        }
    }
}