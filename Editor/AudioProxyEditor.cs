using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Froggi.AudioKit;

namespace Froggi.AudioKit.Editors
{
    [CustomEditor(typeof(AudioProxy))]
    public class AudioProxyEditor : Editor
    {
        private AudioProxy proxy;
        private bool showAudioSettings = true;
        private bool showTimingSettings = true;
        private bool show3DAudioSettings = true;
        private bool showReferences = true;
        

        void OnEnable()
        {
            proxy = (AudioProxy)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("🔊 Audio Proxy", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio Proxy handles playback of a single audio clip with 3D positioning and fade controls.", MessageType.Info);

            showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "Audio Settings", true);
            if (showAudioSettings)
            {
                EditorGUI.indentLevel++;
                DrawAudioSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showTimingSettings = EditorGUILayout.Foldout(showTimingSettings, "Timing & Fade Settings", true);
            if (showTimingSettings)
            {
                EditorGUI.indentLevel++;
                DrawTimingSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            show3DAudioSettings = EditorGUILayout.Foldout(show3DAudioSettings, "3D Audio Settings", true);
            if (show3DAudioSettings)
            {
                EditorGUI.indentLevel++;
                Draw3DAudioSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showReferences = EditorGUILayout.Foldout(showReferences, "References", true);
            if (showReferences)
            {
                EditorGUI.indentLevel++;
                DrawReferences();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            DrawQuickActions();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAudioSettings()
        {
            SerializedProperty audioKeyProp = serializedObject.FindProperty("audioKey");
            SerializedProperty volumeProp = serializedObject.FindProperty("volume");
            SerializedProperty loopProp = serializedObject.FindProperty("loop");

            string[] options = null;
            AudioManager sceneManager = FindObjectOfType<AudioManager>();
            AudioManager useManager = proxy.audioManager ?? sceneManager;
            if (useManager != null)
            {
                if (useManager.clipKeys != null && useManager.clipKeys.Length > 0) options = useManager.clipKeys;
                else if (useManager.audioClips != null)
                {
                    var tmp = new List<string>();
                    for (int i = 0; i < useManager.audioClips.Length; i++)
                    {
                        var c = useManager.audioClips[i];
                        tmp.Add(c != null ? c.name : "");
                    }
                    options = tmp.ToArray();
                }
            }

            if (options != null && options.Length > 0)
            {
                int currentIndex = -1;
                string cur = audioKeyProp.stringValue ?? "";
                for (int i = 0; i < options.Length; i++) if (options[i] == cur) { currentIndex = i; break; }

                int chosen = EditorGUILayout.Popup("Select Audio Clip:", currentIndex, options);
                if (chosen >= 0 && chosen < options.Length)
                {
                    string newVal = options[chosen];
                    if (audioKeyProp.stringValue != newVal)
                    {
                        audioKeyProp.stringValue = newVal;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(proxy);
                    }
                }
                else
                {
                    audioKeyProp.stringValue = EditorGUILayout.TextField("Select Audio Clip:", audioKeyProp.stringValue);
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No AudioManager assigned to populate clip options.", MessageType.Info);
            }
    
            EditorGUILayout.Slider(volumeProp, 0f, 2f, new GUIContent("Volume", "Base volume multiplier (0-2x)"));
    
            EditorGUILayout.PropertyField(loopProp, new GUIContent("Loop", "Should this audio loop continuously?"));
    
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Source Settings", EditorStyles.miniBoldLabel);

            SerializedProperty priorityProp = serializedObject.FindProperty("priority");
            SerializedProperty pitchProp = serializedObject.FindProperty("pitch");
            SerializedProperty dopplerProp = serializedObject.FindProperty("dopplerLevel");
            SerializedProperty panProp = serializedObject.FindProperty("panStereo");

            if (priorityProp != null)
                EditorGUILayout.PropertyField(priorityProp, new GUIContent("Priority", "Audio source priority (0-256)"));
            else
                proxy.GetType().GetField("priority")?.SetValue(proxy, proxy.GetType().GetField("priority")?.GetValue(proxy));

            if (pitchProp != null)
                EditorGUILayout.Slider(pitchProp, 0f, 1f, new GUIContent("Pitch (0..1)", "Playback pitch: 0.5 = neutral, <0.5 lowers, >0.5 raises"));

            if (dopplerProp != null)
                EditorGUILayout.PropertyField(dopplerProp, new GUIContent("Doppler Level", "Doppler effect multiplier"));

            if (panProp != null)
                EditorGUILayout.Slider(panProp, -1f, 1f, new GUIContent("Pan Stereo", "Stereo pan (-1 left, 1 right)"));

            if (!string.IsNullOrEmpty(proxy.audioKey) && proxy.audioManager != null)
            {
                AudioClip clip = proxy.audioManager.GetClip(proxy.audioKey);
                if (clip != null)
                {
                    EditorGUILayout.LabelField("Clip Info:", $"{clip.length:F2}s, {clip.frequency}Hz", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawTimingSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"), new GUIContent("Delay", "Delay before audio starts (seconds)"));

            SerializedProperty _zoneControlsFade = serializedObject.FindProperty(nameof(AudioProxy.zoneControlsFade));
            EditorGUILayout.PropertyField(_zoneControlsFade, new GUIContent("Zone Controls Fade Times", "Determines if the Zone sets the fade times for this proxy"));
                
            EditorGUI.BeginDisabledGroup(_zoneControlsFade.boolValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInTime"), new GUIContent("Fade In Time", "Time to fade in from silence (seconds)"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOutTime"), new GUIContent("Fade Out Time", "Time to fade out to silence (seconds)"));
            EditorGUI.EndDisabledGroup();
            

        }

        private void Draw3DAudioSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spatialBlend"), new GUIContent("Spatial Blend", "0 = 2D, 1 = 3D"));

            if (proxy.spatialBlend > 0)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minDistance"), new GUIContent("Min Distance", "Distance where volume is max"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDistance"), new GUIContent("Max Distance", "Distance where volume reaches zero"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rolloffMode"), new GUIContent("Rolloff Mode", "How volume decreases with distance"));
                SerializedProperty _useSpatializeDistance = serializedObject.FindProperty(nameof(AudioProxy.useSpatializeDistance));
                EditorGUILayout.PropertyField(_useSpatializeDistance, new GUIContent("Use Spatialize Distance", "Turns Spatialize Distance on or off"));
                
                EditorGUI.BeginDisabledGroup(!_useSpatializeDistance.boolValue);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AudioProxy.spatializeDistance)), new GUIContent("Spatialize Distance", "Determines when the source is Specialized between 2D and 3D audio"));
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawReferences()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioManager"), new GUIContent("Audio Manager", "Reference to the AudioManager in your scene"));

            if (proxy.audioManager == null)
            {
                EditorGUILayout.HelpBox("AudioManager is required for this AudioProxy to function!", MessageType.Warning);
            }
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play Audio"))
                {
                    if (Application.isPlaying)
                    {
                        proxy.PlayAudio();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Play Audio", "This function only works in Play Mode.", "OK");
                    }
                }

                if (GUILayout.Button("Stop Audio"))
                {
                    if (Application.isPlaying)
                    {
                        proxy.StopAudio();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Stop Audio", "This function only works in Play Mode.", "OK");
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find AudioManager"))
                {
                    AudioManager manager = FindObjectOfType<AudioManager>();
                    if (manager != null)
                    {
                        Selection.activeGameObject = manager.gameObject;
                        EditorGUIUtility.PingObject(manager.gameObject);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AudioManager Not Found", "No AudioManager found in the scene.", "OK");
                    }
                }

                if (proxy.audioManager == null && GUILayout.Button("Auto-Assign Manager"))
                {
                    AudioManager manager = FindObjectOfType<AudioManager>();
                    if (manager != null)
                    {
                        proxy.audioManager = manager;
                        EditorUtility.SetDirty(proxy);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AudioManager Not Found", "No AudioManager found in the scene. Create one first.", "OK");
                    }
                }
            }
        }

        private void DrawStatusInfo()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(proxy.audioKey))
            {
                EditorGUILayout.HelpBox("No audio key assigned!", MessageType.Warning);
            }
            else if (proxy.audioManager == null)
            {
                EditorGUILayout.HelpBox("No AudioManager assigned!", MessageType.Error);
            }
            else
            {
                if (proxy.audioManager == null)
                {
                    EditorGUILayout.HelpBox("No AudioManager assigned! Assign one to validate the audio key.", MessageType.Warning);
                }
                else
                {
                    AudioClip clip = proxy.audioManager.GetClip(proxy.audioKey);
                    if (clip == null)
                    {
                        EditorGUILayout.HelpBox($"Audio key '{proxy.audioKey}' not found in AudioManager!", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Ready to use! Audio key is valid.", MessageType.Info);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview (Editor Only)", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Play"))
            {
                if (!string.IsNullOrEmpty(proxy.audioKey) && proxy.audioManager != null)
                {
                    AudioClip clip = proxy.audioManager.GetClip(proxy.audioKey);
                    if (clip != null)
                    {
                        AudioUtils.PlayClip(clip, proxy.volume);
                    }
                }
            }

            if (GUILayout.Button("⏹ Stop"))
            {
                AudioUtils.StopAllClips();
            }
            EditorGUILayout.EndHorizontal();
        }

        void OnSceneGUI()
        {
            AudioProxy proxy = (AudioProxy)target;

            if (proxy == null) return;

            Vector3 position = proxy.transform.position;

            string labelText = string.IsNullOrEmpty(proxy.audioKey) ? "AudioProxy" : proxy.audioKey;
            Handles.Label(position + Vector3.up * 1.5f, labelText, EditorStyles.boldLabel);
            
            
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawGizmoForAudioProxy(AudioProxy proxy, GizmoType gizmoType)
        {
            if (proxy == null) return;

            Vector3 position = proxy.transform.position;

            Gizmos.color = Color.white;
            Gizmos.DrawIcon(position, "Assets/FroggiAudioKit/Assets/Gizmos/AudioProxy.png", true, Color.white);
        }
        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmoForAudioProxy(AudioProxy proxy, GizmoType gizmoType)
        {
            if (proxy == null) return;
            Vector3 position = proxy.transform.position;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(position,proxy.minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position,proxy.maxDistance);
            

            if (proxy.useSpatializeDistance)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(position,proxy.spatializeDistance);
            }
            Gizmos.color = Color.white;
        }

        public static class AudioUtils
        {
            public static void PlayClip(AudioClip clip, float volume = 1f)
            {
    #if UNITY_EDITOR
                if (clip != null)
                {
                    GameObject tempGO = new GameObject("AudioPreview");
                    AudioSource tempSource = tempGO.AddComponent<AudioSource>();
                    tempSource.clip = clip;
                    tempSource.volume = volume;
                    tempSource.spatialBlend = 0f;
                    tempSource.Play();

                    float destroyDelay = clip.length + 0.1f;
                    double destroyTime = EditorApplication.timeSinceStartup + destroyDelay;

                    void CheckDestroy()
                    {
                        if (EditorApplication.timeSinceStartup >= destroyTime)
                        {
                            if (tempGO != null)
                            {
                                UnityEngine.Object.DestroyImmediate(tempGO);
                            }
                            EditorApplication.update -= CheckDestroy;
                        }
                    }

                    EditorApplication.update += CheckDestroy;
                }
    #endif
            }

            public static void StopAllClips()
            {
    #if UNITY_EDITOR
                GameObject[] previewObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in previewObjects)
                {
                    if (obj.name == "AudioPreview")
                    {
                        UnityEngine.Object.DestroyImmediate(obj);
                    }
                }
    #endif
            }
        }
    }
}