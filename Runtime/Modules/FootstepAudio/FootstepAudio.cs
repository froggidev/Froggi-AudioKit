// Modules are unfinished and not officially supported yet

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{

    public class FootstepAudio : AudioModule
    {
        [Header("Footstep Settings")]
        public float stepDistance = 2.0f;
   
        [Range(0f, 1f)]
        public float volume = 0.8f;
        public float minTimeBetweenSteps = 0.3f;
        
        
        public string[] footstepClipNames = { "Footstep 1", "Footstep 2" };
        
        public FootstepAudioZone[] footstepZones;
        
        [Header("Debug")]
        public bool enableDebugOutput = false;
        
        private VRCPlayerApi localPlayer;
        private Vector3 lastPosition;
        private float totalDistanceTravelled;
        private float lastStepTime;
        private int currentFootstepIndex = 0;
        private bool wasGroundedLastFrame;
        
        private FootstepAudioZone currentActiveZone;
        private string[] currentClipNames;
        
        public override int GetRequiredOneShotSources()
        {
            return 4;
        }
        
        public override void InitializeModule()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                Debug.LogWarning("FootstepAudio: LocalPlayer is null! This script will not function.");
                this.enabled = false;
                return;
            }
            
            lastPosition = localPlayer.GetPosition();
            totalDistanceTravelled = 0f;
            lastStepTime = 0f;
            wasGroundedLastFrame = localPlayer.IsPlayerGrounded();
            
            currentActiveZone = null;
            currentClipNames = footstepClipNames;
            
            if (enableDebugOutput)
            {
                Debug.Log($"FootstepAudio: Initialized with step distance: {stepDistance}m, volume: {volume}");
            }
        }

        void Update()
        {
            if (localPlayer == null || audioManager == null) return;
            
            UpdateCurrentZone();
            
            Vector3 currentPosition = localPlayer.GetPosition();
            bool isGrounded = localPlayer.IsPlayerGrounded();
            
            if (isGrounded)
            {
                float distanceThisFrame = Vector3.Distance(currentPosition, lastPosition);
                
                Vector3 horizontalMovement = new Vector3(
                    currentPosition.x - lastPosition.x, 
                    0f, 
                    currentPosition.z - lastPosition.z
                );
                float horizontalDistance = horizontalMovement.magnitude;
                
                totalDistanceTravelled += horizontalDistance;
                
                if (totalDistanceTravelled >= stepDistance && 
                    Time.time - lastStepTime >= minTimeBetweenSteps)
                {
                    Vector3 footstepPosition = new Vector3(currentPosition.x, currentPosition.y - 1f, currentPosition.z);
                    PlayFootstepSound(footstepPosition);
                    
                    totalDistanceTravelled = 0f;
                    lastStepTime = Time.time;
                    
                    if (enableDebugOutput)
                    {
                        Debug.Log($"FootstepAudio: Played footstep at position {footstepPosition}, horizontal distance: {horizontalDistance:F2}m");
                    }
                }
            }
            else
            {
                totalDistanceTravelled = 0f;
            }
            
            lastPosition = currentPosition;
            wasGroundedLastFrame = isGrounded;
        }
        
        private void UpdateCurrentZone()
        {
            FootstepAudioZone newActiveZone = null;
            int highestPriority = -1;
            
            if (footstepZones != null)
            {
                for (int i = 0; i < footstepZones.Length; i++)
                {
                    FootstepAudioZone zone = footstepZones[i];
                    if (zone != null && zone.IsPlayerInZone() && zone.GetZonePriority() > highestPriority)
                    {
                        newActiveZone = zone;
                        highestPriority = zone.GetZonePriority();
                    }
                }
            }
            
            if (newActiveZone != currentActiveZone)
            {
                FootstepAudioZone previousZone = currentActiveZone;
                currentActiveZone = newActiveZone;
                currentClipNames = currentActiveZone != null ? currentActiveZone.GetZoneFootstepClips() : footstepClipNames;
                currentFootstepIndex = 0;
                
                totalDistanceTravelled = 0f;
                
                if (enableDebugOutput)
                {
                    string previousZoneName = previousZone != null ? previousZone.GetZoneName() : "Default";
                    string newZoneName = currentActiveZone != null ? currentActiveZone.GetZoneName() : "Default";
                    int clipCount = currentClipNames != null ? currentClipNames.Length : 0;
                    Debug.Log($"FootstepAudio: Zone changed from '{previousZoneName}' to '{newZoneName}', clips: {clipCount}");
                }
            }
        }
        
        private void PlayFootstepSound(Vector3 position)
        {
            if (currentClipNames == null || currentClipNames.Length == 0)
            {
                Debug.LogWarning("FootstepAudio: No footstep clip names available!");
                return;
            }
            
            string clipName = currentClipNames[currentFootstepIndex % currentClipNames.Length];
            currentFootstepIndex = (currentFootstepIndex + 1) % currentClipNames.Length;
            
            float playVolume = volume;
            
            audioManager.PlayOneShot(clipName, position, playVolume);
            
            if (enableDebugOutput)
            {
                string source = currentActiveZone != null ? $"zone '{currentActiveZone.GetZoneName()}'" : "default";
                Debug.Log($"FootstepAudio: Playing '{clipName}' from {source} at {position} with volume {playVolume:F2}");
            }
        }
    }
}