using System.Collections;
using System.Collections.Generic;
using Froggi.AudioKit;
using UnityEditor.Callbacks;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

public class AudioKitBuildTools : IVRCSDKBuildRequestedCallback
{
     //<-- make this your Class Inheritence
    public int callbackOrder => -9999;

    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        return true;
    }

    [PostProcessScene(-9999)]
    private static void ProcessScene()
    {
        AudioManager _manger = GameObject.FindObjectOfType<AudioManager>(true);
        AudioZone[] _zones = GameObject.FindObjectsOfType<AudioZone>(true);
        AudioZoneGroup[] _zoneGroups = GameObject.FindObjectsOfType<AudioZoneGroup>(true);
        AudioProxy[] _proxys = GameObject.FindObjectsOfType<AudioProxy>(true);

        _manger.registeredZones = _zones;
        foreach (var _zone in _zones)
        {
            _zone.audioManager =  _manger;
            _zone.zoneCollider = _zone.GetComponent<Collider>();
        }

        foreach (var _zoneGroup in _zones)
        {
            if (_zoneGroup.audioProxies != null &&  _zoneGroup.audioProxies.Length > 0) continue;
            AudioProxy[] __proxys = _zoneGroup.GetComponentsInChildren<AudioProxy>(true);
            _zoneGroup.audioProxies = __proxys;
        }
        
        foreach (var _zoneGroup in _zoneGroups)
        {
            if (_zoneGroup.groupAudioProxies != null &&  _zoneGroup.groupAudioProxies.Length > 0) continue;
            AudioProxy[] __proxys = _zoneGroup.GetComponentsInChildren<AudioProxy>(true);
            _zoneGroup.groupAudioProxies = __proxys;
        }

        foreach (var _proxy in _proxys)
        {
            _proxy.audioManager =  _manger;
        }
    }
}
