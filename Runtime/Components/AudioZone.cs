using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{
    public class AudioZone : UdonSharpBehaviour
    {
        [Header("Zone Mode")]
        public bool childZoneMode = false;
        
        [Header("Manager Reference")]
        public AudioManager audioManager;

        [Header("Audio Zone Settings")]
        public AudioProxy[] audioProxies;
        public bool playOnEnter = true;
        public bool stopOnExit = true;

        [Header("Fade Settings")]
        public float fadeInDuration = 1f;
        public float fadeOutDuration = 1f;

        [Header("Zone Settings")]
        public bool requireLocalPlayer = true;
        public bool oneTimePlay = false;
        public bool startInside = false;
        
        [Header("Delay Settings")]
        public float enterDelay = 0.1f;
        public float exitDelay = 0.3f;

        private bool hasPlayed = false;
        private bool isPlayerInside = false;
        private bool isRegistered = false;
        private float enterTimer = 0f;
        private float exitTimer = 0f;
        private bool enterQueued = false;
        private bool exitQueued = false;
        private int[] proxyFadeStates;
        
        // Zone Group Support
        private UdonSharpBehaviour parentGroup;
        private bool isGroupMember = false;

        void Start()
        {
            
            if (audioManager != null && !isRegistered)
            {
                
                bool alreadyInArray = false;
                if (audioManager.registeredZones != null)
                {
                    for (int i = 0; i < audioManager.registeredZones.Length; i++)
                    {
                        if (audioManager.registeredZones[i] == this)
                        {
                            alreadyInArray = true;
                            break;
                        }
                    }
                }
                
                if (!alreadyInArray)
                {
                    audioManager.RegisterZone(this);
                }
                isRegistered = true;
            }

            if (audioProxies != null)
            {
                proxyFadeStates = new int[audioProxies.Length];
                for (int i = 0; i < proxyFadeStates.Length; i++)
                {
                    proxyFadeStates[i] = 0;
                }
                
                foreach (AudioProxy proxy in audioProxies)
                {
                    if (proxy != null)
                    {
                        proxy.fadeInTime = fadeInDuration;
                        proxy.fadeOutTime = fadeOutDuration;
                    }
                }
            }

        VRCPlayerApi local = Networking.LocalPlayer;

            if (startInside)
            {
                if (!requireLocalPlayer || (requireLocalPlayer && local != null))
                {
                    
                    isPlayerInside = false;
                    enterQueued = true;
                    enterTimer = 0f;
                }
            }
            else if (requireLocalPlayer)
            {
                if (local != null && IsLocalPlayerInsideZone())
                {
                    isPlayerInside = false;
                    enterQueued = true;
                    enterTimer = 0f;
                }
            }
        }

        public override void PostLateUpdate()
        {
            
            if (enterQueued)
            {
                enterTimer += Time.deltaTime;
                if (enterTimer >= enterDelay)
                {
                    ExecuteEnter();
                    enterQueued = false;
                    enterTimer = 0f;
                }
            }
            if (exitQueued)
            {
                exitTimer += Time.deltaTime;
                if (exitTimer >= exitDelay)
                {
                    ExecuteExit();
                    exitQueued = false;
                    exitTimer = 0f;
                }
            }
        UpdateProxyFadeStates();
        }

        private void UpdateProxyFadeStates()
        {
        if (audioProxies == null || proxyFadeStates == null) return;
            for (int i = 0; i < audioProxies.Length; i++)
            {
                AudioProxy proxy = audioProxies[i];
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

        public void OnDestroy()
        {
            if (audioManager != null && isRegistered)
            {
                audioManager.UnregisterZone(this);
                isRegistered = false;
            }
        }

        public void SetAudioManager(AudioManager manager)
        {
            if (audioManager != manager)
            {
            
                if (audioManager != null && isRegistered)
                {
                    audioManager.UnregisterZone(this);
                    isRegistered = false;
                }
                
                audioManager = manager;
                
                
                if (audioManager != null)
                {
                    audioManager.RegisterZone(this);
                    isRegistered = true;
                }
            }
        }

        public bool IsLocalPlayerInsideZone()
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (local == null) return false;

        return false;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (requireLocalPlayer && !player.isLocal) return;
            if (oneTimePlay && hasPlayed) return;

            
            if (audioManager != null)
                audioManager.OnPlayerEnteredZone(player, this);

            
            if (exitQueued)
            {
                exitQueued = false;
                exitTimer = 0f;
                Debug.Log("[AudioZone] Cancelled exit - player re-entered quickly");
            }

            if (isGroupMember && parentGroup != null)
            {
                parentGroup.SetProgramVariable("_lastEnteredZone", this);
                parentGroup.SetProgramVariable("_lastPlayer", player);
                parentGroup.SendCustomEvent("OnChildZoneEnter");
            }

            if (!isPlayerInside && !enterQueued)
            {
                enterQueued = true;
                enterTimer = 0f;
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (requireLocalPlayer && !player.isLocal) return;

            
            if (audioManager != null)
                audioManager.OnPlayerExitedZone(player, this);

            
            if (enterQueued)
            {
                enterQueued = false;
                enterTimer = 0f;
                Debug.Log("[AudioZone] Cancelled enter - player exited quickly");
            }

            if (isGroupMember && parentGroup != null)
            {
                parentGroup.SetProgramVariable("_lastExitedZone", this);
                parentGroup.SetProgramVariable("_lastPlayer", player);
                parentGroup.SendCustomEvent("OnChildZoneExit");
            }

            if (isPlayerInside && !exitQueued)
            {
                exitQueued = true;
                exitTimer = 0f;
            }
        }

        private void ExecuteEnter()
        {
            if (isPlayerInside) return;
            isPlayerInside = true;
            if (playOnEnter && audioProxies != null)
            {
                for (int i = 0; i < audioProxies.Length; i++)
                {
                    AudioProxy proxy = audioProxies[i];
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
        }

        private void ExecuteExit()
        {
            if (!isPlayerInside) return;
            isPlayerInside = false;
            if (stopOnExit && audioProxies != null)
            {
                for (int i = 0; i < audioProxies.Length; i++)
                {
                    AudioProxy proxy = audioProxies[i];
                    if (proxy == null) continue;
                    if (proxyFadeStates[i] == 1)
                    {
                        proxy.StopAudio();
                        proxyFadeStates[i] = 3;
                    }
                    else if (proxyFadeStates[i] == 2)
                    {
                        proxy.StopAudio();
                        proxyFadeStates[i] = 3;
                    }
                }
            }
        }

        public void PlayAllProxies()
        {
            if (audioProxies != null)
            {
                foreach (AudioProxy proxy in audioProxies)
                {
                    if (proxy != null)
                    {
                        proxy.PlayAudio();
                    }
                }
            }
        }

        public void StopAllProxies()
        {
            if (audioProxies != null)
            {
                foreach (AudioProxy proxy in audioProxies)
                {
                    if (proxy != null)
                    {
                        proxy.StopAudio();
                    }
                }
            }
        }

        public void ForceStopAllProxies()
        {
            if (audioProxies != null)
            {
                for (int i = 0; i < audioProxies.Length; i++)
                {
                    AudioProxy proxy = audioProxies[i];
                    if (proxy != null)
                    {
                        proxy.ForceStopAudio();
                        if (proxyFadeStates != null && i < proxyFadeStates.Length)
                        {
                            proxyFadeStates[i] = 0;
                        }
                    }
                }
            }
        }

        public void SetFadeTimes(float fadeIn, float fadeOut)
        {
            fadeInDuration = fadeIn;
            fadeOutDuration = fadeOut;

            if (audioProxies != null)
            {
                foreach (AudioProxy proxy in audioProxies)
                {
                    if (proxy != null)
                    {
                        proxy.fadeInTime = fadeIn;
                        proxy.fadeOutTime = fadeOut;
                    }
                }
            }
        }
        
        public void SetZoneGroup(UdonSharpBehaviour group)
        {
            parentGroup = group;
            isGroupMember = (group != null);
        }
        
        public bool IsGroupMember()
        {
            return isGroupMember;
        }
        
        public bool IsPlayerInside()
        {
            return isPlayerInside;
        }
    }
}