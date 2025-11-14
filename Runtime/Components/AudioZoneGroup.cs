using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{
    public class AudioZoneGroup : UdonSharpBehaviour
    {
        [Header("Zone Group Settings")]
        public AudioZone[] childZones;
        public AudioProxy[] groupAudioProxies;

        [Header("Group Behavior")]
        public bool treatAsOneZone = true;
        public bool requireAllZones = false;
        public bool playOnEnter = true;
        public bool stopOnExit = true;

        [Header("Group Fade Settings")]
        public float fadeInDuration = 1f;
        public float fadeOutDuration = 1f;

        [Header("Group Delay Settings")]
        public float enterDelay = 0.1f;
        public float exitDelay = 0.3f;

        [Header("Manager Reference")]
        public AudioManager audioManager;

        private bool isPlayerInGroup = false;
        private bool[] zoneStates;
        private int activeZoneCount = 0;
        private bool hasPlayed = false;

        private float enterTimer = 0f;
        private float exitTimer = 0f;
        private bool enterQueued = false;
        private bool exitQueued = false;
        private int[] proxyFadeStates;

        [System.NonSerialized]
        public AudioZone _lastEnteredZone;
        [System.NonSerialized]
        public AudioZone _lastExitedZone;
        [System.NonSerialized]
        public VRCPlayerApi _lastPlayer;

        void Start()
        {
            if (childZones == null || childZones.Length == 0)
            {
                Debug.LogWarning($"{gameObject.name}: No child zones assigned!");
                return;
            }

            zoneStates = new bool[childZones.Length];

            if (groupAudioProxies != null)
            {
                proxyFadeStates = new int[groupAudioProxies.Length];
                for (int i = 0; i < proxyFadeStates.Length; i++)
                {
                    proxyFadeStates[i] = 0;
                }

                foreach (AudioProxy proxy in groupAudioProxies)
                {
                    if (proxy != null)
                    {
                        proxy._SetFadeTimes(fadeInDuration,fadeOutDuration);
                    }
                }
            }

            if (audioManager != null)
            {
                audioManager.RegisterZoneGroup(this);
            }

            if (treatAsOneZone)
            {
                DisableChildZoneAudio();
            }

            SetupChildZoneReferences();
        }

        private void SetupChildZoneReferences()
        {
            if (childZones == null) return;

            foreach (AudioZone zone in childZones)
            {
                if (zone != null)
                {
                    zone.SetZoneGroup(this);
                }
            }
        }

        public override void PostLateUpdate()
        {
            if (!treatAsOneZone) return;

            if (enterQueued)
            {
                enterTimer += Time.deltaTime;
                if (enterTimer >= enterDelay)
                {
                    ExecuteGroupEnter();
                    enterQueued = false;
                    enterTimer = 0f;
                }
            }
            if (exitQueued)
            {
                exitTimer += Time.deltaTime;
                if (exitTimer >= exitDelay)
                {
                    ExecuteGroupExit();
                    exitQueued = false;
                    exitTimer = 0f;
                }
            }

            UpdateProxyFadeStates();
        }

        private void UpdateProxyFadeStates()
        {
            if (groupAudioProxies == null || proxyFadeStates == null) return;

            for (int i = 0; i < groupAudioProxies.Length; i++)
            {
                AudioProxy proxy = groupAudioProxies[i];
                if (proxy == null) continue;

                switch (proxyFadeStates[i])
                {
                    case 1:
                        if (proxy.HasActiveSource() && proxy.IsPlaying() && !proxy.IsFadingIn())
                        {
                            proxyFadeStates[i] = 2;
                        }
                        else if (!proxy.HasActiveSource() || !proxy.IsPlaying())
                        {
                            proxyFadeStates[i] = 0;
                        }
                        break;
                    case 3:
                        if (!proxy.HasActiveSource() || (!proxy.IsPlaying() && !proxy.IsFadingOut()))
                        {
                            proxyFadeStates[i] = 0;
                        }
                        break;
                    case 2:
                        if (!proxy.HasActiveSource() || !proxy.IsPlaying())
                        {
                            proxyFadeStates[i] = 0;
                        }
                        break;
                }
            }
        }

        private void DisableChildZoneAudio()
        {
            if (childZones == null) return;

            foreach (AudioZone zone in childZones)
            {
                if (zone != null)
                {
                    zone.playOnEnter = false;
                    zone.stopOnExit = false;
                }
            }
        }

        public void OnChildZoneEnter(AudioZone zone, VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            int zoneIndex = GetZoneIndex(zone);
            if (zoneIndex == -1) return;

            if (!zoneStates[zoneIndex])
            {
                zoneStates[zoneIndex] = true;
                activeZoneCount++;

                CheckGroupEntry();
            }
        }

        public void OnChildZoneExit(AudioZone zone, VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            int zoneIndex = GetZoneIndex(zone);
            if (zoneIndex == -1) return;

            if (zoneStates[zoneIndex])
            {
                zoneStates[zoneIndex] = false;
                activeZoneCount--;

                CheckGroupExit();
            }
        }

        private void CheckGroupEntry()
        {
            if (!treatAsOneZone) return;

            bool shouldEnterGroup = requireAllZones ?
                (activeZoneCount == childZones.Length) :
                (activeZoneCount > 0);

            if (shouldEnterGroup && !isPlayerInGroup && !enterQueued)
            {
                if (exitQueued)
                {
                    exitQueued = false;
                    exitTimer = 0f;
                    Debug.Log("Cancelled group exit - player re-entered quickly");
                }

                enterQueued = true;
                enterTimer = 0f;
            }
        }

        private void CheckGroupExit()
        {
            if (!treatAsOneZone) return;

            bool shouldExitGroup = requireAllZones ?
                (activeZoneCount < childZones.Length) :
                (activeZoneCount == 0);

            if (shouldExitGroup && isPlayerInGroup && !exitQueued)
            {
                if (enterQueued)
                {
                    enterQueued = false;
                    enterTimer = 0f;
                    Debug.Log("Cancelled group enter - player exited quickly");
                }

                exitQueued = true;
                exitTimer = 0f;
            }
        }

        private void ExecuteGroupEnter()
        {
            if (isPlayerInGroup) return;
            isPlayerInGroup = true;

            if (playOnEnter && groupAudioProxies != null)
            {
                for (int i = 0; i < groupAudioProxies.Length; i++)
                {
                    AudioProxy proxy = groupAudioProxies[i];
                    if (proxy != null)
                    {
                        if (proxyFadeStates[i] == 3)
                        {
                            proxy.ForceStopAudio();
                            proxy.PlayAudio();
                            proxyFadeStates[i] = 1;
                        }
                        else if (proxyFadeStates[i] == 0)
                        {
                            proxy.PlayAudio();
                            proxyFadeStates[i] = 1;
                        }
                    }
                }
                hasPlayed = true;
            }

            Debug.Log($"Entered group: {gameObject.name}");
        }

        private void ExecuteGroupExit()
        {
            if (!isPlayerInGroup) return;
            isPlayerInGroup = false;

            if (stopOnExit && groupAudioProxies != null)
            {
                for (int i = 0; i < groupAudioProxies.Length; i++)
                {
                    AudioProxy proxy = groupAudioProxies[i];
                    if (proxy == null) continue;

                    if (proxyFadeStates[i] == 1 || proxyFadeStates[i] == 2)
                    {
                        proxy.StopAudio();
                        proxyFadeStates[i] = 3;
                    }
                }
            }

            Debug.Log($"Exited group: {gameObject.name}");
        }

        private int GetZoneIndex(AudioZone zone)
        {
            if (childZones == null) return -1;

            for (int i = 0; i < childZones.Length; i++)
            {
                if (childZones[i] == zone) return i;
            }
            return -1;
        }

        public void PlayGroupAudio()
        {
            if (groupAudioProxies != null)
            {
                foreach (AudioProxy proxy in groupAudioProxies)
                {
                    if (proxy != null)
                    {
                        proxy.PlayAudio();
                    }
                }
            }
        }

        public void StopGroupAudio()
        {
            if (groupAudioProxies != null)
            {
                foreach (AudioProxy proxy in groupAudioProxies)
                {
                    if (proxy != null)
                    {
                        proxy.StopAudio();
                    }
                }
            }
        }

        public bool HasPlayed() 
        { 
            return hasPlayed; 
        }

        public bool IsPlayerInGroup()
        {
            return isPlayerInGroup;
        }

        public int GetActiveZoneCount()
        {
            return activeZoneCount;
        }

        public void OnChildZoneEnter()
        {
            OnChildZoneEnter(_lastEnteredZone, _lastPlayer);
        }

        public void OnChildZoneExit()
        {
            OnChildZoneExit(_lastExitedZone, _lastPlayer);
        }

        void OnDestroy()
        {
            if (audioManager != null)
            {
                audioManager.UnregisterZoneGroup(this);
            }
        }
        
    }
    
}