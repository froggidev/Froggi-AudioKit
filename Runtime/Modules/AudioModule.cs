// Modules are unfinished and not officially supported yet

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{
    public class AudioModule : UdonSharpBehaviour
    {
        [Header("Audio Module Base")]
        public AudioManager audioManager;
        
        
        public virtual int GetRequiredOneShotSources() { return 0; }
        public virtual int GetRequiredLoopingSources() { return 0; }
        
    
        public virtual void InitializeModule() { }
        
        void Start()
        {
            if (audioManager == null)
            {
                Debug.LogWarning($"{GetType().Name}: No AudioManager assigned! Please assign an AudioManager in the inspector.");
                this.enabled = false;
                return;
            }
            
            InitializeModule();
            Debug.Log($"{GetType().Name} initialized successfully.");
        }
    }
}