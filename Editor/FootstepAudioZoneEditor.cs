using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Froggi.AudioKit
{

    [CustomEditor(typeof(FootstepAudioZone))]
    public class FootstepAudioZoneEditor : Editor
    {
        private FootstepAudioZone zone;
        private bool showZoneSettings = true;
        private bool showAudioSettings = true;
        private bool showTriggerSettings = true;
        private bool showDebugSettings = true;

        // add field to hold the "Add Selected" popup choice
        private int addClipSelection = -1;

        void OnEnable()
        {
            zone = (FootstepAudioZone)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("🔊 Footstep Audio Zone", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Defines a zone that overrides footstep audio clips when the player enters. Higher priority zones override lower priority ones.", MessageType.Info);

            // Zone Settings
            showZoneSettings = EditorGUILayout.Foldout(showZoneSettings, "Zone Settings", true);
            if (showZoneSettings)
            {
                EditorGUI.indentLevel++;
                DrawZoneSettings();
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

            // Trigger Settings
            showTriggerSettings = EditorGUILayout.Foldout(showTriggerSettings, "Trigger Settings", true);
            if (showTriggerSettings)
            {
                EditorGUI.indentLevel++;
                DrawTriggerSettings();
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

        private void DrawZoneSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("zoneName"), new GUIContent("Zone Name", "Name of this zone for debugging and identification"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("zonePriority"), new GUIContent("Zone Priority", "Priority of this zone (higher values override lower values)"));
        }

        private void DrawAudioSettings()
        {
            // Build AudioManager-backed options (prefer clipKeys)
            FootstepAudio[] footstepAudios = FindObjectsOfType<FootstepAudio>();
            AudioManager audioManager = null;
            foreach (FootstepAudio fa in footstepAudios)
            {
                if (fa.audioManager != null)
                {
                    audioManager = fa.audioManager;
                    break;
                }
            }

            string[] options = null;
            if (audioManager != null)
            {
                if (audioManager.clipKeys != null && audioManager.clipKeys.Length > 0)
                {
                    options = audioManager.clipKeys;
                }
                else if (audioManager.audioClips != null)
                {
                    var tmp = new List<string>();
                    for (int i = 0; i < audioManager.audioClips.Length; i++)
                    {
                        var c = audioManager.audioClips[i];
                        tmp.Add(c != null ? c.name : "");
                    }
                    options = tmp.ToArray();
                }
            }

            var clipNamesProp = serializedObject.FindProperty("zoneFootstepClips");

            if (clipNamesProp != null)
            {
                EditorGUILayout.LabelField("Zone Footstep Clips", EditorStyles.label);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                int size = clipNamesProp.arraySize;
                for (int i = 0; i < size; i++)
                {
                    var element = clipNamesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();

                    if (options != null && options.Length > 0)
                    {
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
                            element.stringValue = EditorGUILayout.TextField(element.stringValue);
                        }
                    }
                    else
                    {
                        element.stringValue = EditorGUILayout.TextField(element.stringValue);
                    }

                    if (GUILayout.Button("-", GUILayout.Width(22)))
                    {
                        clipNamesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

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
                EditorGUILayout.LabelField("Zone Footstep Clips", EditorStyles.label);
                if (zone.zoneFootstepClips != null)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    for (int i = 0; i < zone.zoneFootstepClips.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string currentVal = zone.zoneFootstepClips[i] ?? "";
                        if (options != null && options.Length > 0)
                        {
                            int currentIndex = -1;
                            for (int k = 0; k < options.Length; k++) if (options[k] == currentVal) { currentIndex = k; break; }
                            int chosen = EditorGUILayout.Popup(currentIndex, options);
                            if (chosen >= 0 && chosen < options.Length)
                            {
                                zone.zoneFootstepClips[i] = options[chosen];
                            }
                            else
                            {
                                zone.zoneFootstepClips[i] = EditorGUILayout.TextField(zone.zoneFootstepClips[i]);
                            }
                        }
                        else
                        {
                            zone.zoneFootstepClips[i] = EditorGUILayout.TextField(zone.zoneFootstepClips[i]);
                        }

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                        {
                            var list = new List<string>(zone.zoneFootstepClips);
                            list.RemoveAt(i);
                            zone.zoneFootstepClips = list.ToArray();
                            EditorUtility.SetDirty(zone);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Add Empty"))
                    {
                        var list = new List<string>(zone.zoneFootstepClips ?? new string[0]);
                        list.Add("");
                        zone.zoneFootstepClips = list.ToArray();
                        EditorUtility.SetDirty(zone);
                    }
                    if (options != null && options.Length > 0)
                    {
                        addClipSelection = Mathf.Clamp(addClipSelection, 0, options.Length - 1);
                        addClipSelection = EditorGUILayout.Popup(addClipSelection, options);
                        if (GUILayout.Button("Add Selected", GUILayout.Width(100)))
                        {
                            var list = new List<string>(zone.zoneFootstepClips ?? new string[0]);
                            list.Add(options[Mathf.Clamp(addClipSelection, 0, options.Length - 1)]);
                            zone.zoneFootstepClips = list.ToArray();
                            EditorUtility.SetDirty(zone);
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

        private void DrawTriggerSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("zoneTrigger"), new GUIContent("Zone Trigger", "The collider that defines this zone (must be set as trigger)"));

            // Auto-assign trigger if missing
            if (zone.zoneTrigger == null)
            {
                Collider collider = zone.GetComponent<Collider>();
                if (collider != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Found collider on this GameObject", EditorStyles.miniLabel);
                    if (GUILayout.Button("Auto-Assign", GUILayout.Width(100)))
                    {
                        zone.zoneTrigger = collider;
                        EditorUtility.SetDirty(zone);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("No collider found. Add a collider to this GameObject and set it as a trigger.", MessageType.Warning);
                    
                    if (GUILayout.Button("Add Box Collider"))
                    {
                        BoxCollider boxCollider = zone.gameObject.AddComponent<BoxCollider>();
                        boxCollider.isTrigger = true;
                        boxCollider.size = new Vector3(4f, 2f, 4f);
                        zone.zoneTrigger = boxCollider;
                        EditorUtility.SetDirty(zone);
                    }
                }
            }
            else
            {
                // Show basic trigger validation
                Collider trigger = zone.zoneTrigger;
                
                if (!trigger.isTrigger)
                {
                    EditorGUILayout.HelpBox("Collider is not set as a trigger! Click below to fix.", MessageType.Error);
                    if (GUILayout.Button("Set as Trigger"))
                    {
                        trigger.isTrigger = true;
                        EditorUtility.SetDirty(trigger);
                    }
                }
            }

            // Player status (runtime only)
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Status:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Player In Zone: {zone.IsPlayerInZone()}", EditorStyles.miniLabel);
            }
        }

        private void DrawDebugSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugOutput"), new GUIContent("Enable Debug Output", "Show debug messages when player enters/exits this zone"));

            if (zone.enableDebugOutput)
            {
                EditorGUILayout.HelpBox("Debug output enabled for this zone. Check console for enter/exit messages.", MessageType.Info);
            }
        }

        private void DrawStatusInfo()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            // Check for issues
            if (zone.zoneTrigger == null)
            {
                issues.Add("No trigger collider assigned!");
            }
            else if (!zone.zoneTrigger.isTrigger)
            {
                issues.Add("Collider is not set as a trigger!");
            }

            if (zone.zoneFootstepClips == null || zone.zoneFootstepClips.Length == 0)
            {
                warnings.Add("No footstep clips assigned for this zone!");
            }

            if (string.IsNullOrEmpty(zone.zoneName))
            {
                warnings.Add("Zone name is empty - consider adding one for debugging!");
            }

            // Check if zone is referenced by any FootstepAudio
            FootstepAudio[] footstepAudios = FindObjectsOfType<FootstepAudio>();
            bool isReferenced = false;
            foreach (FootstepAudio fa in footstepAudios)
            {
                if (fa.footstepZones != null)
                {
                    foreach (FootstepAudioZone refZone in fa.footstepZones)
                    {
                        if (refZone == zone)
                        {
                            isReferenced = true;
                            break;
                        }
                    }
                }
                if (isReferenced) break;
            }

            if (!isReferenced)
            {
                warnings.Add("This zone is not referenced by any FootstepAudio component!");
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
                EditorGUILayout.HelpBox("✓ FootstepAudioZone is properly configured!", MessageType.Info);
            }

            // Quick actions
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find FootstepAudio"))
            {
                FootstepAudio footstepAudio = FindObjectOfType<FootstepAudio>();
                if (footstepAudio != null)
                {
                    Selection.activeGameObject = footstepAudio.gameObject;
                    EditorGUIUtility.PingObject(footstepAudio.gameObject);
                }
                else
                {
                    EditorUtility.DisplayDialog("FootstepAudio Not Found", "No FootstepAudio found in the scene.", "OK");
                }
            }

            if (GUILayout.Button("Add to FootstepAudio"))
            {
                AddToFootstepAudio();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Duplicate Zone"))
            {
                DuplicateZone();
            }
        }

        private void AddToFootstepAudio()
        {
            FootstepAudio footstepAudio = FindObjectOfType<FootstepAudio>();
            if (footstepAudio != null)
            {
                var zones = new List<FootstepAudioZone>(footstepAudio.footstepZones ?? new FootstepAudioZone[0]);
                if (!zones.Contains(zone))
                {
                    zones.Add(zone);
                    footstepAudio.footstepZones = zones.ToArray();
                    EditorUtility.SetDirty(footstepAudio);
                    Debug.Log($"Added zone '{zone.zoneName}' to FootstepAudio component.");
                }
                else
                {
                    Debug.Log($"Zone '{zone.zoneName}' is already in FootstepAudio component.");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("FootstepAudio Not Found", "No FootstepAudio found in the scene to add this zone to.", "OK");
            }
        }

        private void AddClipToZone(string clipName)
        {
            var clipNames = new List<string>(zone.zoneFootstepClips ?? new string[0]);
            if (!clipNames.Contains(clipName))
            {
                clipNames.Add(clipName);
                zone.zoneFootstepClips = clipNames.ToArray();
                EditorUtility.SetDirty(zone);
            }
        }

        private void DuplicateZone()
        {
            GameObject duplicate = Instantiate(zone.gameObject);
            duplicate.name = zone.gameObject.name + " (Copy)";
            duplicate.transform.position = zone.transform.position + Vector3.right * 5f;
            
            FootstepAudioZone duplicateZone = duplicate.GetComponent<FootstepAudioZone>();
            duplicateZone.zoneName = zone.zoneName + " Copy";
            duplicateZone.zonePriority = zone.zonePriority;
            
            Selection.activeGameObject = duplicate;
            EditorUtility.SetDirty(duplicateZone);
        }

        // Scene view visualization
        void OnSceneGUI()
        {
            if (zone.zoneTrigger != null)
            {
                // Draw zone bounds in scene view
                Handles.color = zone.IsPlayerInZone() ? Color.green : Color.cyan;
                Bounds bounds = zone.zoneTrigger.bounds;
                
                // Draw wireframe box
                Handles.DrawWireCube(bounds.center, bounds.size);
                
                // Draw label
                Handles.Label(bounds.center + Vector3.up * (bounds.size.y * 0.5f + 0.5f), 
                            $"{zone.zoneName}\nPriority: {zone.zonePriority}");
            }
        }
    }
}