using UnityEngine;
using UnityEditor;
using Froggi.AudioKit;

namespace Froggi.AudioKit.Editors
{

    [CustomEditor(typeof(AudioZoneGroup))]
    public class AudioZoneGroupEditor : Editor
    {
        private AudioZoneGroup zoneGroup;
        private bool showChildZones = true;
        private bool showGroupAudio = true;
        private bool showGroupSettings = true;

        void OnEnable()
        {
            zoneGroup = (AudioZoneGroup)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("🎵 Audio Zone Group", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Groups multiple AudioZones to act as a unified zone. Child zones are managed collectively when enabled.", MessageType.Info);

            // Audio Manager Reference
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manager Reference", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioManager"), new GUIContent("Audio Manager", "Direct reference to the AudioManager"));
            
            if (zoneGroup.audioManager == null)
            {
                EditorGUILayout.HelpBox("⚠️ No AudioManager assigned! This zone group won't function properly without one.", MessageType.Warning);
            }

            // Child Zones
            EditorGUILayout.Space();
            showChildZones = EditorGUILayout.Foldout(showChildZones, "Child Zones", true);
            if (showChildZones)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("childZones"), new GUIContent("Child Zones", "AudioZones that are part of this group"));
                
                if (zoneGroup.childZones == null || zoneGroup.childZones.Length == 0)
                {
                    EditorGUILayout.HelpBox("No child zones assigned. Add AudioZone components to make this group functional.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            // Group Audio
            EditorGUILayout.Space();
            showGroupAudio = EditorGUILayout.Foldout(showGroupAudio, "Group Audio", true);
            if (showGroupAudio)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("groupAudioProxies"), new GUIContent("Group Audio Proxies", "AudioProxies that play when entering this zone group"));
                EditorGUI.indentLevel--;
            }

            // Group Settings
            EditorGUILayout.Space();
            showGroupSettings = EditorGUILayout.Foldout(showGroupSettings, "Group Settings", true);
            if (showGroupSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("treatAsOneZone"), new GUIContent("Treat As One Zone", "When enabled, child zones act as a unified zone"));
                
                if (zoneGroup.treatAsOneZone)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("requireAllZones"), new GUIContent("Require All Zones", "True = must be in ALL child zones, False = any child zone"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnter"), new GUIContent("Play On Enter", "Play group audio when entering the zone group"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnExit"), new GUIContent("Stop On Exit", "Stop group audio when exiting the zone group"));
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Fade Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInDuration"), new GUIContent("Fade In Duration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOutDuration"), new GUIContent("Fade Out Duration"));
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Delay Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("enterDelay"), new GUIContent("Enter Delay"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("exitDelay"), new GUIContent("Exit Delay"));
                }
                else
                {
                    EditorGUILayout.HelpBox("When 'Treat As One Zone' is disabled, child zones will operate independently. The group will only manage references.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            // Runtime Information
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("Player In Group", zoneGroup.IsPlayerInGroup());
                    EditorGUILayout.IntField("Active Zone Count", zoneGroup.GetActiveZoneCount());
                }
            }
            
            // Utility buttons
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play Group Audio"))
                {
                    if (Application.isPlaying)
                    {
                        zoneGroup.PlayGroupAudio();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Play Audio", "This function only works in Play Mode.", "OK");
                    }
                }

                if (GUILayout.Button("Stop Group Audio"))
                {
                    if (Application.isPlaying)
                    {
                        zoneGroup.StopGroupAudio();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Stop Audio", "This function only works in Play Mode.", "OK");
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All Child Zones"))
                {
                    if (zoneGroup.childZones != null && zoneGroup.childZones.Length > 0)
                    {
                        UnityEngine.Object[] objects = new UnityEngine.Object[zoneGroup.childZones.Length];
                        for (int i = 0; i < zoneGroup.childZones.Length; i++)
                        {
                            if (zoneGroup.childZones[i] != null)
                                objects[i] = zoneGroup.childZones[i].gameObject;
                        }
                        Selection.objects = objects;
                    }
                }

                if (zoneGroup.audioManager == null && GUILayout.Button("Find AudioManager"))
                {
                    AudioManager manager = FindObjectOfType<AudioManager>();
                    if (manager != null)
                    {
                        zoneGroup.audioManager = manager;
                        EditorUtility.SetDirty(zoneGroup);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AudioManager Not Found", "No AudioManager found in the scene.", "OK");
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}