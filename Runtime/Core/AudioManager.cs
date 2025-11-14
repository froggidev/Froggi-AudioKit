
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


namespace Froggi.AudioKit
{
    public class AudioManager : UdonSharpBehaviour
    {
        public AudioClip[] audioClips;
        public string[] clipKeys;

        public AudioModule[] audioModules;

        public AudioSource[] oneShotPool;
        public AudioSource[] loopingPool;
        public bool[] oneShotInUse;
        public bool[] loopingInUse;

        public float masterVolume = 1f;
        public float fadeOutDuration = 0.5f;

        private AudioSource[] scheduledSrc;
        private AudioSource[][] scheduledPool;
        private bool[][] scheduledInUse;
        private float[] scheduledReleaseTime;
        private bool[] scheduledActive;

        private int[] oneShotFreeStack;
        private int oneShotFreeTop = -1;

        private int[] loopingFreeStack;
        private int loopingFreeTop = -1;

        [Header("Zone Management")]
        public AudioZone[] registeredZones;
        public UdonSharpBehaviour[] registeredZoneGroups;

        private AudioZone[] playerCurrentZones;
        private int playerCurrentZoneCount = 0;



        void Start() {
            if (audioClips == null) audioClips = new AudioClip[0];

            if (oneShotPool == null) oneShotPool = new AudioSource[0];
            if (loopingPool == null) loopingPool = new AudioSource[0]; 

            if (oneShotInUse == null || oneShotInUse.Length != oneShotPool.Length) oneShotInUse = new bool[oneShotPool.Length];
            if (loopingInUse == null || loopingInUse.Length != loopingPool.Length) loopingInUse = new bool[loopingPool.Length];

            for (int i = 0; i < oneShotInUse.Length; i++) oneShotInUse[i] = false;
            for (int i = 0; i < loopingInUse.Length; i++) loopingInUse[i] = false;

            int cap = (oneShotPool.Length + loopingPool.Length);
            if (cap < 16) cap = 16;

            scheduledSrc = new AudioSource[cap];
            scheduledPool = new AudioSource[cap][];
            scheduledInUse = new bool[cap][];
            scheduledReleaseTime = new float[cap];
            scheduledActive = new bool[cap];

            InitializeFreeStacks();

            
            if (registeredZones != null && registeredZones.Length > 0)
            {
                playerCurrentZones = new AudioZone[registeredZones.Length];
                playerCurrentZoneCount = 0;
            }
        }

        private void InitializeFreeStacks()
        {
            oneShotFreeStack = new int[oneShotPool.Length];
            oneShotFreeTop = -1;
            for (int i = oneShotPool.Length - 1; i >= 0; i--)
            {
                oneShotFreeStack[++oneShotFreeTop] = i;
            }

            loopingFreeStack = new int[loopingPool.Length];
            loopingFreeTop = -1;
            for (int i = loopingPool.Length - 1; i >= 0; i--)
            {
                loopingFreeStack[++loopingFreeTop] = i;
            }
        }

        void Update()
        {
            if (scheduledActive == null) return;

            float now = Time.time;
            for (int i = 0; i < scheduledActive.Length; i++)
            {
                if (!scheduledActive[i]) continue;
                if (now >= scheduledReleaseTime[i])
                {
                    ReleaseToPool(scheduledSrc[i], scheduledPool[i], scheduledInUse[i]);

                    scheduledSrc[i] = null;
                    scheduledPool[i] = null;
                    scheduledInUse[i] = null;
                    scheduledReleaseTime[i] = 0f;
                    scheduledActive[i] = false;
                }
            }
        }

        public AudioSource AllocateFromPool(AudioSource[] pool, bool[] inUse) {
            if (pool == oneShotPool && oneShotFreeStack != null)
            {
                if (oneShotFreeTop >= 0)
                {
                    int idx = oneShotFreeStack[oneShotFreeTop--];
                    inUse[idx] = true;
                    return oneShotPool[idx];
                }
                return null;
            }
            else if (pool == loopingPool && loopingFreeStack != null)
            {
                if (loopingFreeTop >= 0)
                {
                    int idx = loopingFreeStack[loopingFreeTop--];
                    inUse[idx] = true;
                    return loopingPool[idx];
                }
                return null;
            }

            for (int i = 0; i < pool.Length; i++) {
                if (!inUse[i]) {
                    inUse[i] = true;
                    return pool[i];
                }
            }
            return null;
        }

        public void ReleaseToPool(AudioSource src, AudioSource[] pool, bool[] inUse) {
            if (src == null || pool == null || inUse == null) return;

            if (pool == oneShotPool)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i] == src)
                    {
                        if (!inUse[i]) return;
                        inUse[i] = false;
                        src.Stop();
                        src.clip = null;
                        if (oneShotFreeStack != null && oneShotFreeTop < oneShotFreeStack.Length - 1)
                        {
                            oneShotFreeStack[++oneShotFreeTop] = i;
                        }
                        return;
                    }
                }
                return;
            }

            if (pool == loopingPool)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i] == src)
                    {
                        if (!inUse[i]) return;
                        inUse[i] = false;
                        src.Stop();
                        src.clip = null;
                        if (loopingFreeStack != null && loopingFreeTop < loopingFreeStack.Length - 1)
                        {
                            loopingFreeStack[++loopingFreeTop] = i;
                        }
                        return;
                    }
                }
                return;
            }

            for (int i = 0; i < pool.Length; i++) {
                if (pool[i] == src) {
                    inUse[i] = false;
                    src.Stop();
                    src.clip = null;
                    break;
                }
            }
        }

        public int AllocateIndexFromOneShot()
        {
            if (oneShotFreeStack == null) return -1;
            if (oneShotFreeTop < 0) return -1;
            int idx = oneShotFreeStack[oneShotFreeTop--];
            oneShotInUse[idx] = true;
            return idx;
        }

        public int AllocateIndexFromLooping()
        {
            if (loopingFreeStack == null) return -1;
            if (loopingFreeTop < 0) return -1;
            int idx = loopingFreeStack[loopingFreeTop--];
            loopingInUse[idx] = true;
            return idx;
        }

        public void ReleaseIndexToOneShot(int idx)
        {
            if (idx < 0 || idx >= oneShotPool.Length) return;
            if (!oneShotInUse[idx]) return;
            oneShotInUse[idx] = false;
            AudioSource src = oneShotPool[idx];
            if (src != null)
            {
                src.Stop();
                src.clip = null;
            }
            if (oneShotFreeStack != null && oneShotFreeTop < oneShotFreeStack.Length - 1)
            {
                oneShotFreeStack[++oneShotFreeTop] = idx;
            }
        }

        public void ReleaseIndexToLooping(int idx)
        {
            if (idx < 0 || idx >= loopingPool.Length) return;
            if (!loopingInUse[idx]) return;
            loopingInUse[idx] = false;
            AudioSource src = loopingPool[idx];
            if (src != null)
            {
                src.Stop();
                src.clip = null;
            }
            if (loopingFreeStack != null && loopingFreeTop < loopingFreeStack.Length - 1)
            {
                loopingFreeStack[++loopingFreeTop] = idx;
            }
        }

        public void ScheduleRelease(AudioSource src, AudioSource[] pool, bool[] inUse, float delay)
        {
            if (src == null || pool == null || inUse == null) return;
            if (scheduledActive == null) return;

            float releaseAt = Time.time + Mathf.Max(0f, delay);

            for (int i = 0; i < scheduledActive.Length; i++)
            {
                if (!scheduledActive[i])
                {
                    scheduledActive[i] = true;
                    scheduledSrc[i] = src;
                    scheduledPool[i] = pool;
                    scheduledInUse[i] = inUse;
                    scheduledReleaseTime[i] = releaseAt;
                    return;
                }
            }

            SendCustomEventDelayedSeconds("_FallbackRelease", delay);
        }

        public AudioClip GetClip(string key) {
            if (string.IsNullOrEmpty(key)) return null;

            if (clipKeys != null)
            {
                for (int i = 0; i < clipKeys.Length; i++)
                {
                    string k = clipKeys[i];
                    if (!string.IsNullOrEmpty(k) && k == key)
                    {
                        if (audioClips != null && i < audioClips.Length) return audioClips[i];
                        break;
                    }
                }
            }

            if (audioClips == null) return null;

            for (int i = 0; i < audioClips.Length; i++)
            {
                AudioClip clip = audioClips[i];
                if (clip != null)
                {
                    string clipName = clip.name;
                    if (!string.IsNullOrEmpty(clipName) && clipName == key)
                    {
                        return clip;
                    }
                }
            }

            return null;
        }

        public void PlayOneShot(string key, Vector3 pos, float volume) {
            AudioClip clip = GetClip(key);
            if (clip == null) {
                Debug.LogWarning("Audio clip not found: " + key);
                return;
            }

            AudioSource src = AllocateFromPool(oneShotPool, oneShotInUse);
            if (src == null) {
                Debug.LogWarning("No available source in oneShot pool");
                return;
            }

            src.volume = volume * masterVolume;
            src.transform.position = pos;
            src.clip = clip;
            src.loop = false;
            src.Play();

            float clipLength = clip.length;
            ScheduleRelease(src, oneShotPool, oneShotInUse, clipLength + fadeOutDuration); // Updated to include fade-out duration
        }

        public void PlayLooping(string key, Vector3 pos, float volume) {
            AudioClip clip = GetClip(key);
            if (clip == null) {
                Debug.LogWarning("Audio clip not found: " + key);
                return;
            }

            AudioSource src = AllocateFromPool(loopingPool, loopingInUse);
            if (src == null) {
                Debug.LogWarning("No available source in looping pool");
                return;
            }

            src.volume = volume * masterVolume;
            src.transform.position = pos;
            src.clip = clip;
            src.loop = true;
            src.Play();
        }

        public void StopLooping(string key) {
            if (audioClips == null) return;
            
            AudioClip clip = GetClip(key);
            if (clip == null) return;

            for (int i = 0; i < loopingPool.Length; i++) {
                if (loopingPool[i] != null && loopingPool[i].clip == clip && loopingPool[i].isPlaying) {
                    loopingPool[i].Stop();
                    ReleaseToPool(loopingPool[i], loopingPool, loopingInUse);
                    break;
                }
            }
        }



        public void _FallbackRelease()
        {
            for (int i = 0; i < oneShotPool.Length; i++)
            {
                if (oneShotPool[i] != null && !oneShotPool[i].isPlaying && oneShotInUse[i])
                {
                    ReleaseToPool(oneShotPool[i], oneShotPool, oneShotInUse);
                    return;
                }
            }

            for (int i = 0; i < loopingPool.Length; i++)
            {
                if (loopingPool[i] != null && !loopingPool[i].isPlaying && loopingInUse[i])
                {
                    ReleaseToPool(loopingPool[i], loopingPool, loopingInUse);
                    return;
                }
            }
        }
        
        public int GetRequiredOneShotPoolSize()
        {
            int baseSize = 10; 
            int moduleRequirement = 0;
            
            if (audioModules != null)
            {
                for (int i = 0; i < audioModules.Length; i++)
                {
                    if (audioModules[i] != null)
                    {
                        moduleRequirement += audioModules[i].GetRequiredOneShotSources();
                    }
                }
            }
            
            return baseSize + moduleRequirement;
        }
        
        public int GetRequiredLoopingPoolSize()
        {
            int baseSize = 5; 
            int moduleRequirement = 0;
            
            if (audioModules != null)
            {
                for (int i = 0; i < audioModules.Length; i++)
                {
                    if (audioModules[i] != null)
                    {
                        moduleRequirement += audioModules[i].GetRequiredLoopingSources();
                    }
                }
            }
            
            return baseSize + moduleRequirement;
        }

        public void OnPlayerEnteredZone(VRCPlayerApi player, AudioZone zone)
        {
            if (zone == null) return;
            
            string playerName = player != null ? (player.isLocal ? "LocalPlayer" : player.displayName) : "Unknown";
            Debug.Log($"[AudioManager] Player '{playerName}' entered zone '{zone.name}'");
            
            
            if (player != null && player.isLocal && playerCurrentZones != null)
            {
                
                bool alreadyTracking = false;
                for (int i = 0; i < playerCurrentZoneCount; i++)
                {
                    if (playerCurrentZones[i] == zone)
                    {
                        alreadyTracking = true;
                        break;
                    }
                }
                
                if (!alreadyTracking && playerCurrentZoneCount < playerCurrentZones.Length)
                {
                    playerCurrentZones[playerCurrentZoneCount] = zone;
                    playerCurrentZoneCount++;
                    Debug.Log($"[AudioManager] Now tracking {playerCurrentZoneCount} zone(s) for local player");
                }
            }
        }

        public void OnPlayerExitedZone(VRCPlayerApi player, AudioZone zone)
        {
            if (zone == null) return;
            
            string playerName = player != null ? (player.isLocal ? "LocalPlayer" : player.displayName) : "Unknown";
            //Debug.Log($"[AudioManager] Player '{playerName}' exited zone '{zone.name}'");
            
            // Remove zone from tracking for local player
            if (player != null && player.isLocal && playerCurrentZones != null)
            {
                for (int i = 0; i < playerCurrentZoneCount; i++)
                {
                    if (playerCurrentZones[i] == zone)
                    {
                        
                        for (int j = i; j < playerCurrentZoneCount - 1; j++)
                        {
                            playerCurrentZones[j] = playerCurrentZones[j + 1];
                        }
                        playerCurrentZones[playerCurrentZoneCount - 1] = null;
                        playerCurrentZoneCount--;
                        //Debug.Log($"[AudioManager] Now tracking {playerCurrentZoneCount} zone(s) for local player");
                        break;
                    }
                }
            }
        }
        
        public void RegisterZoneGroup(UdonSharpBehaviour zoneGroup)
        {
            if (zoneGroup == null) return;
            
            if (registeredZoneGroups == null)
            {
                registeredZoneGroups = new UdonSharpBehaviour[1];
                registeredZoneGroups[0] = zoneGroup;
                return;
            }
            
            // Check if already registered
            for (int i = 0; i < registeredZoneGroups.Length; i++)
            {
                if (registeredZoneGroups[i] == zoneGroup) return;
            }
            
            // Add to array
            UdonSharpBehaviour[] newArray = new UdonSharpBehaviour[registeredZoneGroups.Length + 1];
            for (int i = 0; i < registeredZoneGroups.Length; i++)
            {
                newArray[i] = registeredZoneGroups[i];
            }
            newArray[registeredZoneGroups.Length] = zoneGroup;
            registeredZoneGroups = newArray;
        }
        
        public void UnregisterZoneGroup(UdonSharpBehaviour zoneGroup)
        {
            if (zoneGroup == null || registeredZoneGroups == null) return;
            
            int removeIndex = -1;
            for (int i = 0; i < registeredZoneGroups.Length; i++)
            {
                if (registeredZoneGroups[i] == zoneGroup)
                {
                    removeIndex = i;
                    break;
                }
            }
            
            if (removeIndex == -1) return;
            
            if (registeredZoneGroups.Length == 1)
            {
                registeredZoneGroups = new UdonSharpBehaviour[0];
                return;
            }
            
            UdonSharpBehaviour[] newArray = new UdonSharpBehaviour[registeredZoneGroups.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < registeredZoneGroups.Length; i++)
            {
                if (i != removeIndex)
                {
                    newArray[newIndex] = registeredZoneGroups[i];
                    newIndex++;
                }
            }
            registeredZoneGroups = newArray;
        }

        public void LogCurrentZones()
        {
            if (playerCurrentZones == null || playerCurrentZoneCount == 0)
            {
                Debug.Log("[AudioManager] Local player is not in any tracked zones");
                return;
            }
            
            Debug.Log($"[AudioManager] Local player is currently in {playerCurrentZoneCount} zone(s):");
            for (int i = 0; i < playerCurrentZoneCount; i++)
            {
                if (playerCurrentZones[i] != null)
                {
                    Debug.Log($"  - {playerCurrentZones[i].name}");
                }
            }
        }
    }
}

