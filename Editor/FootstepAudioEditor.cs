using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Froggi.AudioKit;

namespace Froggi.AudioKit.Editors
{
    [CustomEditor(typeof(FootstepAudio))]
    public class FootstepAudioEditor : Editor
    {
        private FootstepAudio footstepAudio;
        private bool showFootstepSettings = true;
        private bool showZoneManagement = true;
        private bool showAudioSettings = true;
        private bool showDebugSettings = true;

        // add selection index for Add Selected popup (matches FootstepAudioZoneEditor)
        private int addClipSelection = -1;

        void OnEnable()
        {
            footstepAudio = (FootstepAudio)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("👣 Footstep Audio System", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Automatically plays footstep sounds based on player movement. Supports zone-based audio switching.", MessageType.Info);

            // Audio Manager Reference (always visible)
            var audioManagerProp = serializedObject.FindProperty("audioManager");
            if (audioManagerProp != null)
            {
                EditorGUILayout.PropertyField(audioManagerProp, new GUIContent("Audio Manager", "Reference to the AudioManager in your scene"));
            }
            else
            {
                // Fallback for UdonSharp serialization issues
                footstepAudio.audioManager = (AudioManager)EditorGUILayout.ObjectField("Audio Manager", footstepAudio.audioManager, typeof(AudioManager), true);
            }
            
            if (footstepAudio.audioManager == null)
            {
                EditorGUILayout.HelpBox("AudioManager is required! This module inherits from AudioModule and needs an AudioManager to function.", MessageType.Error);
            }

            EditorGUILayout.Space();

            // Footstep Settings
            showFootstepSettings = EditorGUILayout.Foldout(showFootstepSettings, "Footstep Settings", true);
            if (showFootstepSettings)
            {
                EditorGUI.indentLevel++;
                DrawFootstepSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Audio Settings
            showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "Audio Settings", true);
            if (showAudioSettings)
            {
                EditorGUI.indentLevel++;
                DrawAudioSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Zone Management
            showZoneManagement = EditorGUILayout.Foldout(showZoneManagement, "Zone Management", true);
            if (showZoneManagement)
            {
                EditorGUI.indentLevel++;
                DrawZoneManagement();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Debug Settings
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                DrawDebugSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            DrawStatusInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFootstepSettings()
        {
            var stepDistanceProp = serializedObject.FindProperty("stepDistance");
            if (stepDistanceProp != null)
            {
                EditorGUILayout.PropertyField(stepDistanceProp, new GUIContent("Step Distance", "Minimum distance the player must travel to trigger a footstep sound"));
            }
            else
            {
                footstepAudio.stepDistance = EditorGUILayout.FloatField("Step Distance", footstepAudio.stepDistance);
            }

            var minTimeProp = serializedObject.FindProperty("minTimeBetweenSteps");
            if (minTimeProp != null)
            {
                EditorGUILayout.PropertyField(minTimeProp, new GUIContent("Min Time Between Steps", "Minimum time between footstep sounds in seconds"));
            }
            else
            {
                footstepAudio.minTimeBetweenSteps = EditorGUILayout.FloatField("Min Time Between Steps", footstepAudio.minTimeBetweenSteps);
            }

            // Info about required audio sources
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio Source Requirements:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"One-Shot Sources: {footstepAudio.GetRequiredOneShotSources()}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Looping Sources: {footstepAudio.GetRequiredLoopingSources()}", EditorStyles.miniLabel);
        }

        private void DrawAudioSettings()
        {
            var volumeProp = serializedObject.FindProperty("volume");
            if (volumeProp != null)
            {
                EditorGUILayout.PropertyField(volumeProp, new GUIContent("Volume", "Volume of footstep sounds (0.0 to 1.0)"));
            }
            else
            {
                footstepAudio.volume = EditorGUILayout.Slider("Volume", footstepAudio.volume, 0f, 1f);
            }

        // Volume variation disabled message removed to reduce inspector noise.

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Footstep Clips:", EditorStyles.miniBoldLabel);
            
            var clipNamesProp = serializedObject.FindProperty("footstepClipNames");

            // Build options from AudioManager (prefer clipKeys if available)
            string[] options = null;
            if (footstepAudio.audioManager != null)
            {
                if (footstepAudio.audioManager.clipKeys != null && footstepAudio.audioManager.clipKeys.Length > 0)
                {
                    options = footstepAudio.audioManager.clipKeys;
                }
                else if (footstepAudio.audioManager.audioClips != null)
                {
                    var temp = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < footstepAudio.audioManager.audioClips.Length; i++)
                    {
                        var c = footstepAudio.audioManager.audioClips[i];
                        temp.Add(c != null ? c.name : "");
                    }
                    options = temp.ToArray();
                }
            }

            // If we have a serialized property, render per-element dropdowns that update the serialized array
            if (clipNamesProp != null)
            {
                EditorGUILayout.LabelField("Footstep Clip Names", EditorStyles.label);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                int size = clipNamesProp.arraySize;
                for (int i = 0; i < size; i++)
                {
                    var element = clipNamesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();

                    // Dropdown when options available
                    if (options != null && options.Length > 0)
                    {
                        // Find current index
                        int currentIndex = -1;
                        string currentVal = element.stringValue ?? "";
                        for (int k = 0; k < options.Length; k++) if (options[k] == currentVal) { currentIndex = k; break; }

                        int chosen = EditorGUILayout.Popup(currentIndex, options);
                        if (chosen >= 0 && chosen < options.Length)
                        {
                            if (element.stringValue != options[chosen])
                            {
                                element.stringValue = options[chosen];
                            }
                        }
                        else
                        {
                            // show editable text if no selection
                            element.stringValue = EditorGUILayout.TextField(element.stringValue);
                        }
                    }
                    else
                    {
                        // No AudioManager/options - fall back to plain text
                        element.stringValue = EditorGUILayout.TextField(element.stringValue);
                    }

                    // Remove button
                    if (GUILayout.Button("-", GUILayout.Width(22)))
                    {
                        clipNamesProp.DeleteArrayElementAtIndex(i);
                        break; // break to avoid iteration invalidation
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                // Add controls identical to FootstepAudioZoneEditor
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Empty"))
                {
                    int newIndex = clipNamesProp.arraySize;
                    clipNamesProp.InsertArrayElementAtIndex(newIndex);
                    clipNamesProp.GetArrayElementAtIndex(newIndex).stringValue = "";
                }
                
                if (options != null && options.Length > 0)
                {
                    addClipSelection = Mathf.Clamp(addClipSelection, 0, options.Length - 1);
                    addClipSelection = EditorGUILayout.Popup(addClipSelection, options);
                    if (GUILayout.Button("Add Selected", GUILayout.Width(100)))
                    {
                        int newIndex = clipNamesProp.arraySize;
                        clipNamesProp.InsertArrayElementAtIndex(newIndex);
                        clipNamesProp.GetArrayElementAtIndex(newIndex).stringValue = options[Mathf.Clamp(addClipSelection, 0, options.Length - 1)];
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Fallback when serialized property unavailable: operate on the raw array
                EditorGUILayout.LabelField("Footstep Clip Names", EditorStyles.label);
                if (footstepAudio.footstepClipNames != null)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    for (int i = 0; i < footstepAudio.footstepClipNames.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string currentVal = footstepAudio.footstepClipNames[i] ?? "";
                        if (options != null && options.Length > 0)
                        {
                            int currentIndex = -1;
                            for (int k = 0; k < options.Length; k++) if (options[k] == currentVal) { currentIndex = k; break; }
                            int chosen = EditorGUILayout.Popup(currentIndex, options);
                            if (chosen >= 0 && chosen < options.Length)
                            {
                                footstepAudio.footstepClipNames[i] = options[chosen];
                            }
                            else
                            {
                                footstepAudio.footstepClipNames[i] = EditorGUILayout.TextField(footstepAudio.footstepClipNames[i]);
                            }
                        }
                        else
                        {
                            footstepAudio.footstepClipNames[i] = EditorGUILayout.TextField(footstepAudio.footstepClipNames[i]);
                        }

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            var list = new System.Collections.Generic.List<string>(footstepAudio.footstepClipNames);
                            list.RemoveAt(i);
                            footstepAudio.footstepClipNames = list.ToArray();
                            EditorUtility.SetDirty(footstepAudio);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    
                    // Add controls to match FootstepAudioZoneEditor
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Empty"))
                    {
                        var list = new System.Collections.Generic.List<string>(footstepAudio.footstepClipNames ?? new string[0]);
                        list.Add("");
                        footstepAudio.footstepClipNames = list.ToArray();
                        EditorUtility.SetDirty(footstepAudio);
                    }
                    if (options != null && options.Length > 0)
                    {
                        addClipSelection = Mathf.Clamp(addClipSelection, 0, options.Length - 1);
                        addClipSelection = EditorGUILayout.Popup(addClipSelection, options);
                        if (GUILayout.Button("Add Selected", GUILayout.Width(100)))
                        {
                            var list = new System.Collections.Generic.List<string>(footstepAudio.footstepClipNames ?? new string[0]);
                            list.Add(options[Mathf.Clamp(addClipSelection, 0, options.Length - 1)]);
                            footstepAudio.footstepClipNames = list.ToArray();
                            EditorUtility.SetDirty(footstepAudio);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No footstep clips assigned.");
                }
            }

        
            
        }

        private void DrawZoneManagement()
        {
            var zonesProp = serializedObject.FindProperty("footstepZones");
            if (zonesProp != null)
            {
                EditorGUILayout.PropertyField(zonesProp, new GUIContent("Footstep Zones", "FootstepAudioZones that can override the default clips"));
            }
            else
            {
                EditorGUILayout.LabelField("Footstep Zones (Array editing not available)");
            }

            // Clean up any null (deleted) entries so the inspector doesn't show 'Missing Reference'
            if (footstepAudio.footstepZones != null)
            {
                var cleaned = new System.Collections.Generic.List<FootstepAudioZone>();
                for (int i = 0; i < footstepAudio.footstepZones.Length; i++)
                {
                    if (footstepAudio.footstepZones[i] != null) cleaned.Add(footstepAudio.footstepZones[i]);
                }
                if (cleaned.Count != footstepAudio.footstepZones.Length)
                {
                    footstepAudio.footstepZones = cleaned.ToArray();
                    EditorUtility.SetDirty(footstepAudio);
                    // Refresh serialized object so the inspector updates immediately
                    serializedObject.Update();
                }
            }

            if (footstepAudio.footstepZones != null && footstepAudio.footstepZones.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Zone Overview:", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < footstepAudio.footstepZones.Length; i++)
                {
                    FootstepAudioZone zone = footstepAudio.footstepZones[i];
                    if (zone != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {zone.GetZoneName()}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Priority: {zone.GetZonePriority()}", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Clips: {zone.GetZoneFootstepClips()?.Length ?? 0}", EditorStyles.miniLabel, GUILayout.Width(60));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeGameObject = zone.gameObject;
                            EditorGUIUtility.PingObject(zone.gameObject);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"• Zone {i}: Missing Reference", EditorStyles.miniLabel);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No zones assigned. Add FootstepAudioZone components to GameObjects and assign them here to enable zone-based footstep audio.", MessageType.Info);
            }
        }

        private void DrawDebugSettings()
        {
            var debugProp = serializedObject.FindProperty("enableDebugOutput");
            if (debugProp != null)
            {
                EditorGUILayout.PropertyField(debugProp, new GUIContent("Enable Debug Output", "Show debug messages in the console"));
            }
            else
            {
                footstepAudio.enableDebugOutput = EditorGUILayout.Toggle("Enable Debug Output", footstepAudio.enableDebugOutput);
            }

            if (footstepAudio.enableDebugOutput)
            {
                EditorGUILayout.HelpBox("Debug output is enabled. Check the console for footstep and zone change messages.", MessageType.Info);
            }
        }

        private void DrawStatusInfo()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            // Check for issues
            if (footstepAudio.audioManager == null)
            {
                issues.Add("No AudioManager assigned!");
            }

            if (footstepAudio.footstepClipNames == null || footstepAudio.footstepClipNames.Length == 0)
            {
                warnings.Add("No default footstep clips assigned!");
            }

            if (footstepAudio.stepDistance <= 0)
            {
                warnings.Add("Step distance should be greater than 0!");
            }

            if (footstepAudio.volume <= 0)
            {
                warnings.Add("Volume is set to 0 or less!");
            }

            // Display issues and warnings
            foreach (string issue in issues)
            {
                EditorGUILayout.HelpBox(issue, MessageType.Error);
            }

            foreach (string warning in warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            if (issues.Count == 0 && warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ FootstepAudio is properly configured!", MessageType.Info);
            }

            // Quick actions
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find AudioManager"))
            {
                AudioManager manager = FindObjectOfType<AudioManager>();
                if (manager != null)
                {
                    footstepAudio.audioManager = manager;
                    EditorUtility.SetDirty(footstepAudio);
                }
                else
                {
                    EditorUtility.DisplayDialog("AudioManager Not Found", "No AudioManager found in the scene. Please add one first.", "OK");
                }
            }

            if (GUILayout.Button("Create Test Zone"))
            {
                CreateTestZone();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CreateTestZone()
        {
            GameObject zoneGO = new GameObject("FootstepAudioZone_Test");
            zoneGO.transform.position = footstepAudio.transform.position + Vector3.right * 5f;
            
            // Add collider
            BoxCollider collider = zoneGO.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(4f, 2f, 4f);
            
            // Add FootstepAudioZone component
            FootstepAudioZone zone = zoneGO.AddComponent<FootstepAudioZone>();
            zone.zoneName = "Test Zone";
            zone.zonePriority = 1;
            zone.zoneFootstepClips = new string[] { "Footstep 1", "Footstep 2" };
            
            // Add to footstep zones array
            var zones = new List<FootstepAudioZone>(footstepAudio.footstepZones ?? new FootstepAudioZone[0]);
            zones.Add(zone);
            footstepAudio.footstepZones = zones.ToArray();
            
            EditorUtility.SetDirty(footstepAudio);
            Selection.activeGameObject = zoneGO;
            
            Debug.Log("Created test FootstepAudioZone and added it to the FootstepAudio component.");
        }

        private void AddClipToDefault(string clipName)
        {
            var clipNames = new List<string>(footstepAudio.footstepClipNames ?? new string[0]);
            if (!clipNames.Contains(clipName))
            {
                clipNames.Add(clipName);
                footstepAudio.footstepClipNames = clipNames.ToArray();
                EditorUtility.SetDirty(footstepAudio);
            }
        }
    }
}