using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Froggi.AudioKit;


namespace Froggi.AudioKit.Editors
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : Editor
    {
        private AudioManager audioManager;
        
        void OnEnable()
        {
            audioManager = (AudioManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("�️ Audio Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Central hub for audio management. Handles clip storage, source pooling, and zone coordination.", MessageType.Info);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("masterVolume"), new GUIContent("Master Volume", "Global volume multiplier for all audio"));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Audio Clips", EditorStyles.boldLabel);
            var audioClipsProp = serializedObject.FindProperty("audioClips");
            EditorGUILayout.PropertyField(audioClipsProp, new GUIContent("Audio Clips", "Array of audio clips - their names will be used as keys"), true);

            bool needsSyncNow = false;
            if (audioManager != null)
            {
                AudioClip[] clipsNow = audioManager.audioClips;
                string[] keysNow = audioManager.clipKeys;

                if (clipsNow == null)
                {
                    if (keysNow != null && keysNow.Length != 0) needsSyncNow = true;
                }
                else
                {
                    if (keysNow == null || keysNow.Length != clipsNow.Length) needsSyncNow = true;
                    else
                    {
                        for (int i = 0; i < clipsNow.Length; i++)
                        {
                            string expectedNow = clipsNow[i] != null ? clipsNow[i].name : "";
                            if (keysNow[i] != expectedNow)
                            {
                                needsSyncNow = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (needsSyncNow)
            {
                EditorGUILayout.HelpBox("Audio clip keys are out of sync with the Audio Clips list. Press 'Apply' to update the mapping.", MessageType.Error);
                if (GUILayout.Button("Apply"))
                {
                    SyncClipKeys();
                    serializedObject.Update();
                }
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.LabelField("Audio Modules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioModules"), new GUIContent("Audio Modules", "Modules that require additional audio sources"), true);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Zone Management", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("registeredZones"), new GUIContent("Registered Zones", "AudioZones that use this manager"), true);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Scene Audio Info", EditorStyles.boldLabel);
            
            AudioProxy[] allProxies = FindObjectsOfType<AudioProxy>();
            AudioProxy[] connectedProxies = System.Array.FindAll(allProxies, proxy => proxy.audioManager == audioManager);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Audio Components:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"• Total AudioProxies in scene: {allProxies.Length}");
            EditorGUILayout.LabelField($"• Proxies using this manager: {connectedProxies.Length}");
            EditorGUILayout.LabelField($"• Registered zones: {(audioManager.registeredZones != null ? audioManager.registeredZones.Length : 0)}");
            EditorGUILayout.LabelField($"• Audio clips loaded: {(audioManager.audioClips != null ? audioManager.audioClips.Length : 0)}");
            EditorGUILayout.LabelField($"• Audio modules: {(audioManager.audioModules != null ? audioManager.audioModules.Length : 0)}");
            EditorGUILayout.EndVertical();
            
            int oneShotPoolSize = audioManager.oneShotPool != null ? audioManager.oneShotPool.Length : 0;
            int loopingPoolSize = audioManager.loopingPool != null ? audioManager.loopingPool.Length : 0;
            int totalPoolSize = oneShotPoolSize + loopingPoolSize;
            
            int recommendedOneShotSize = audioManager.GetRequiredOneShotPoolSize();
            int recommendedLoopingSize = audioManager.GetRequiredLoopingPoolSize();
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Audio Pool Status:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"• One Shot Pool: {oneShotPoolSize} sources (recommended: {recommendedOneShotSize})");
            EditorGUILayout.LabelField($"• Looping Pool: {loopingPoolSize} sources (recommended: {recommendedLoopingSize})");
            EditorGUILayout.LabelField($"• Total AudioSources: {totalPoolSize}");
            
            if (oneShotPoolSize < recommendedOneShotSize)
            {
                EditorGUILayout.HelpBox($"⚠️ One Shot pool is smaller than recommended! Consider updating to {recommendedOneShotSize} sources.", MessageType.Warning);
            }
            if (loopingPoolSize < recommendedLoopingSize)
            {
                EditorGUILayout.HelpBox($"⚠️ Looping pool is smaller than recommended! Consider updating to {recommendedLoopingSize} sources.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Audio Source Pools", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("oneShotPool"), new GUIContent("One Shot Pool", "AudioSources for one-shot sounds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loopingPool"), new GUIContent("Looping Pool", "AudioSources for looping sounds"), true);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sync Clip Keys"))
                {
                    SyncClipKeys();
                }
                
                if (GUILayout.Button("Update Pools"))
                {
                    int requiredOneShotSize = audioManager.GetRequiredOneShotPoolSize();
                    int requiredLoopingSize = audioManager.GetRequiredLoopingPoolSize();
                    GeneratePool(ref audioManager.oneShotPool, requiredOneShotSize, "OneShot");
                    GeneratePool(ref audioManager.loopingPool, requiredLoopingSize, "Looping");
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate OneShot Pool (10)"))
                {
                    GeneratePool(ref audioManager.oneShotPool, 10, "OneShot");
                }
                
                if (GUILayout.Button("Generate Looping Pool (5)"))
                {
                    GeneratePool(ref audioManager.loopingPool, 5, "Looping");
                }
            }
            
            if (GUILayout.Button("Setup Default Pools"))
            {
                GeneratePool(ref audioManager.oneShotPool, 10, "OneShot");
                GeneratePool(ref audioManager.loopingPool, 5, "Looping");
                SyncClipKeys();
            }
            
            serializedObject.ApplyModifiedProperties();

            if (IsClipSyncNeeded())
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("⚠️ Audio clip keys need to be synchronized with the clips array.", MessageType.Warning);
            }
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    if (audioManager.registeredZones != null)
                    {
                        EditorGUILayout.IntField("Registered Zones", audioManager.registeredZones.Length);
                    }
                }
            }
        }
        
        private bool IsClipSyncNeeded()
        {
            if (audioManager == null) return false;
            
            AudioClip[] clips = audioManager.audioClips;
            string[] keys = audioManager.clipKeys;

            if (clips == null)
            {
                return keys != null && keys.Length != 0;
            }
            else
            {
                if (keys == null || keys.Length != clips.Length) return true;
                
                for (int i = 0; i < clips.Length; i++)
                {
                    string expected = clips[i] != null ? clips[i].name : "";
                    if (keys[i] != expected) return true;
                }
            }
            return false;
        }
        
        private void GeneratePool(ref AudioSource[] pool, int size, string poolType)
        {
            if (pool != null)
            {
                foreach (AudioSource source in pool)
                {
                    if (source != null)
                    {
                        DestroyImmediate(source.gameObject);
                    }
                }
            }
            
            pool = new AudioSource[size];
            for (int i = 0; i < size; i++)
            {
                GameObject go = new GameObject($"{poolType}_AudioSource_{i}");
                go.transform.SetParent(audioManager.transform);
                go.transform.localPosition = Vector3.zero;
                
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = 25f;
                
                pool[i] = source;
            }
            
            if (poolType == "OneShot")
            {
                audioManager.oneShotPool = pool;
                audioManager.oneShotInUse = new bool[pool.Length];
            }
            else if (poolType == "Looping")
            {
                audioManager.loopingPool = pool;
                audioManager.loopingInUse = new bool[pool.Length];
            }

            EditorUtility.SetDirty(audioManager);
            
            Debug.Log($"{poolType} pool generated with {size} sources.");
        }

        private void SyncClipKeys()
        {
            if (audioManager == null) return;

            AudioClip[] clips = audioManager.audioClips;
            if (clips == null)
            {
                audioManager.clipKeys = new string[0];
                EditorUtility.SetDirty(audioManager);
                return;
            }

            audioManager.clipKeys = new string[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                audioManager.clipKeys[i] = clips[i] != null ? clips[i].name : "";
            }

            var names = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < audioManager.clipKeys.Length; i++)
            {
                string k = audioManager.clipKeys[i];
                if (string.IsNullOrEmpty(k)) continue;
                if (names.ContainsKey(k))
                {
                    Debug.LogWarning($"Duplicate audio clip name detected: {k}. This may cause ambiguous lookups.");
                }
                else names[k] = i;
            }

            EditorUtility.SetDirty(audioManager);
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }
    }
}