
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Froggi.AudioKit
{
    public class AudioEvent : UdonSharpBehaviour
    {
        [Header("Audio Proxy Reference")]
        public AudioProxy audioProxy;

        void Start()
        {
            
        }

        public override void Interact()
        {
            if (audioProxy != null)
            {
                audioProxy.PlayAudio();
            }
        }
    }
}