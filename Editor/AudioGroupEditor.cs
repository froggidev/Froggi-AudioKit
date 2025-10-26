using UnityEngine;
using UnityEditor;
using Froggi.AudioKit;

namespace Froggi.AudioKit.Editors
{
    [CustomEditor(typeof(AudioGroup))]
    public class AudioGroupEditor : Editor
    {
        private AudioGroup audioGroup;
        private bool showAudioProxies = true;
        private bool showSettings = true;

        void OnEnable()
        {
            audioGroup = (AudioGroup)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("🎚️ Audio Group", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Groups AudioProxies together for unified volume control and batch operations.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Volume Control", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groupVolume"), new GUIContent("Group Volume", "Master volume multiplier for all proxies in this group"));

            EditorGUILayout.Space();
            showAudioProxies = EditorGUILayout.Foldout(showAudioProxies, "Audio Proxies", true);
            if (showAudioProxies)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("audioProxies"), new GUIContent("Audio Proxies", "AudioProxy components controlled by this group"), true);
                
                if (audioGroup.audioProxies == null || audioGroup.audioProxies.Length == 0)
                {
                    EditorGUILayout.HelpBox("No audio proxies assigned. Add AudioProxy components to make this group functional.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            showSettings = EditorGUILayout.Foldout(showSettings, "UI Slider Settings", true);
            if (showSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeSlider"), new GUIContent("Volume Slider", "Optional UI Slider for runtime volume control"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultValue"), new GUIContent("Default Value", "Initial volume value"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minValue"), new GUIContent("Min Value", "Minimum slider value"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxValue"), new GUIContent("Max Value", "Maximum slider value"));
                EditorGUI.indentLevel--;
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.FloatField("Current Volume", audioGroup.GetVolume());
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play All"))
                {
                    if (Application.isPlaying)
                    {
                        audioGroup.PlayAll();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Play All", "This function only works in Play Mode.", "OK");
                    }
                }

                if (GUILayout.Button("Stop All"))
                {
                    if (Application.isPlaying)
                    {
                        audioGroup.StopAll();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Stop All", "This function only works in Play Mode.", "OK");
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Original Volumes"))
                {
                    audioGroup.RefreshOriginalVolumes();
                    EditorUtility.SetDirty(audioGroup);
                }

                if (GUILayout.Button("Select All Proxies"))
                {
                    if (audioGroup.audioProxies != null && audioGroup.audioProxies.Length > 0)
                    {
                        UnityEngine.Object[] objects = new UnityEngine.Object[audioGroup.audioProxies.Length];
                        for (int i = 0; i < audioGroup.audioProxies.Length; i++)
                        {
                            if (audioGroup.audioProxies[i] != null)
                                objects[i] = audioGroup.audioProxies[i].gameObject;
                        }
                        Selection.objects = objects;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}