// Modules are unfinished and not officially supported yet

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{
    public class FootstepAudioZone : UdonSharpBehaviour
    {
        [Header("Zone Settings")]
        public string[] zoneFootstepClips = { "Footstep 1", "Footstep 2" };
        
        public int zonePriority = 0;
        
        public string zoneName = "Footstep Zone";
        
        [Header("Zone Trigger")]
        public Collider zoneTrigger;
        
        [Header("Debug")]
        public bool enableDebugOutput = false;
        
        private bool playerInZone = false;
        private VRCPlayerApi localPlayer;
        
        void Start()
        {
            localPlayer = Networking.LocalPlayer;
            
            if (zoneTrigger == null)
            {
                zoneTrigger = GetComponent<Collider>();
            }
            
            if (zoneTrigger == null)
            {
                Debug.LogWarning($"FootstepAudioZone '{zoneName}': No trigger collider found!");
                this.enabled = false;
                return;
            }
            
            if (!zoneTrigger.isTrigger)
            {
                Debug.LogWarning($"FootstepAudioZone '{zoneName}': Collider is not set as trigger! Setting it now.");
                zoneTrigger.isTrigger = true;
            }
            
            if (enableDebugOutput)
            {
                Debug.Log($"FootstepAudioZone '{zoneName}' initialized with {zoneFootstepClips.Length} clips, priority: {zonePriority}");
            }
        }
        
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            
            playerInZone = true;
            
            if (enableDebugOutput)
            {
                Debug.Log($"FootstepAudioZone '{zoneName}': Player entered zone");
            }
        }
        
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            
            playerInZone = false;
            
            if (enableDebugOutput)
            {
                Debug.Log($"FootstepAudioZone '{zoneName}': Player exited zone");
            }
        }
        
        public string[] GetZoneFootstepClips()
        {
            return zoneFootstepClips;
        }
        
        public int GetZonePriority()
        {
            return zonePriority;
        }
        
        public string GetZoneName()
        {
            return zoneName;
        }
        
        public bool IsPlayerInZone()
        {
            return playerInZone;
        }
    }
}