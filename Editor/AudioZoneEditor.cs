using UnityEngine;
using UnityEditor;
using Froggi.AudioKit;

namespace Froggi.AudioKit.Editors
{
    [CustomEditor(typeof(AudioZone))]
    public class AudioZoneEditor : Editor
    {
        private AudioZone zone;
        private bool showAudioProxies = true;
        private bool showZoneSettings = true;
        private bool showFadeSettings = true;
        private int addClipSelection = -1;

        void OnEnable()
        {
            zone = (AudioZone)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AudioKit by Froggi.Dev", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("🎵 Audio Zone", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("childZoneMode"), new GUIContent("Child Zone Mode", "Enable this if this zone is part of an AudioZoneGroup"));
            
            if (zone.childZoneMode)
            {
                EditorGUILayout.HelpBox("CHILD ZONE MODE: This zone should be assigned to an AudioZoneGroup component. The group will handle all audio playback.", MessageType.Info);
                
                if (zone.audioProxies != null && zone.audioProxies.Length > 0)
                {
                    EditorGUILayout.HelpBox("⚠️ Audio Proxies are still assigned! In Child Zone Mode, remove these and assign audio to the parent AudioZoneGroup instead.", MessageType.Warning);
                    
                    if (GUILayout.Button("Clear Audio Proxies"))
                    {
                        zone.audioProxies = new AudioProxy[0];
                        EditorUtility.SetDirty(zone);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("STANDARD ZONE: This zone manages AudioProxy components independently. Players trigger audio when entering/exiting.", MessageType.Info);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Manager Reference", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioManager"), new GUIContent("Audio Manager", "Direct reference to the AudioManager"));
            
            if (zone.audioManager == null)
            {
                EditorGUILayout.HelpBox("⚠️ No AudioManager assigned! This zone won't function properly without one.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            if (!zone.childZoneMode)
            {
                showAudioProxies = EditorGUILayout.Foldout(showAudioProxies, "Audio Proxies", true);
                if (showAudioProxies)
                {
                    EditorGUI.indentLevel++;
                    DrawAudioProxies();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                showFadeSettings = EditorGUILayout.Foldout(showFadeSettings, "Fade Settings", true);
                if (showFadeSettings)
                {
                    EditorGUI.indentLevel++;
                    DrawFadeSettings();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
            
            showZoneSettings = EditorGUILayout.Foldout(showZoneSettings, "Zone Settings", true);
            if (showZoneSettings)
            {
                EditorGUI.indentLevel++;
                DrawZoneSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            
            DrawUtilityButtons();
            
            EditorGUILayout.Space();
            DrawStatusInfo();

            serializedObject.ApplyModifiedProperties();

            if (zone != null && !zone.childZoneMode)
            {
                zone.SetFadeTimes(zone.fadeInDuration, zone.fadeOutDuration);
                EditorUtility.SetDirty(zone);
            }
        }

        private void DrawAudioProxies()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioProxies"), new GUIContent("Audio Proxies", "List of AudioProxy components to control"), true);

            string[] globalOptions = null;
            if (zone.audioManager != null)
            {
                if (zone.audioManager.clipKeys != null && zone.audioManager.clipKeys.Length > 0) 
                    globalOptions = zone.audioManager.clipKeys;
                else if (zone.audioManager.audioClips != null)
                {
                    var tmp = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < zone.audioManager.audioClips.Length; i++)
                    {
                        var c = zone.audioManager.audioClips[i];
                        tmp.Add(c != null ? c.name : "");
                    }
                    globalOptions = tmp.ToArray();
                }
            }

            if (zone.audioProxies != null && zone.audioProxies.Length > 0)
            {
                EditorGUILayout.LabelField("Assigned Proxies:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < zone.audioProxies.Length; i++)
                {
                    AudioProxy proxy = zone.audioProxies[i];
                    EditorGUILayout.BeginHorizontal();

                    proxy = (AudioProxy)EditorGUILayout.ObjectField(proxy, typeof(AudioProxy), true);
                    if (proxy != zone.audioProxies[i])
                    {
                        var list = new System.Collections.Generic.List<AudioProxy>(zone.audioProxies);
                        list[i] = proxy;
                        zone.audioProxies = list.ToArray();
                        EditorUtility.SetDirty(zone);
                    }

                    if (proxy != null)
                    {
                        string[] options = null;
                        if (proxy.audioManager != null)
                        {
                            if (proxy.audioManager.clipKeys != null && proxy.audioManager.clipKeys.Length > 0) options = proxy.audioManager.clipKeys;
                            else if (proxy.audioManager.audioClips != null)
                            {
                                var tmp = new System.Collections.Generic.List<string>();
                                for (int k = 0; k < proxy.audioManager.audioClips.Length; k++)
                                {
                                    var c = proxy.audioManager.audioClips[k];
                                    tmp.Add(c != null ? c.name : "");
                                }
                                options = tmp.ToArray();
                            }
                        }
                        if (options == null) options = globalOptions;

                        if (options != null && options.Length > 0)
                        {
                            int currentIndex = -1;
                            string cur = proxy.audioKey ?? "";
                            for (int k = 0; k < options.Length; k++) if (options[k] == cur) { currentIndex = k; break; }

                            int chosen = EditorGUILayout.Popup(currentIndex, options, GUILayout.MaxWidth(200));
                            if (chosen >= 0 && chosen < options.Length)
                            {
                                if (proxy.audioKey != options[chosen])
                                {
                                    proxy.audioKey = options[chosen];
                                    EditorUtility.SetDirty(proxy);
                                }
                            }
                            else
                            {
                                string newKey = EditorGUILayout.TextField(proxy.audioKey ?? "", GUILayout.MaxWidth(200));
                                if (newKey != proxy.audioKey)
                                {
                                    proxy.audioKey = newKey;
                                    EditorUtility.SetDirty(proxy);
                                }
                            }
                        }
                        else
                        {
                            string newKey = EditorGUILayout.TextField(proxy.audioKey ?? "", GUILayout.MaxWidth(200));
                            if (newKey != proxy.audioKey)
                            {
                                proxy.audioKey = newKey;
                                EditorUtility.SetDirty(proxy);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No AudioProxies assigned! Add some to make this zone functional.", MessageType.Warning);
            }
        }

        private void DrawZoneSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnter"), new GUIContent("Play on Enter", "Start audio when player enters zone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnExit"), new GUIContent("Stop on Exit", "Stop audio when player exits zone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requireLocalPlayer"), new GUIContent("Local Player Only", "Only trigger for local player"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("oneTimePlay"), new GUIContent("One Time Only", "Only play once per session"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startInside"), new GUIContent("Start Inside", "Assume the player starts inside the zone at Start()"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Delay Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enterDelay"), new GUIContent("Enter Delay", "Delay before triggering enter (prevents rapid toggling)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("exitDelay"), new GUIContent("Exit Delay", "Delay before triggering exit (prevents rapid toggling)"));
            
            if (zone.enterDelay > 0 || zone.exitDelay > 0)
            {
                EditorGUILayout.HelpBox("Delays help prevent audio from rapidly starting/stopping when quickly entering and exiting zones.", MessageType.Info);
            }
        }

        private void DrawFadeSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInDuration"), new GUIContent("Fade In Duration", "How long to fade in (seconds)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOutDuration"), new GUIContent("Fade Out Duration", "How long to fade out (seconds)"));

            if (zone.fadeInDuration > 0 || zone.fadeOutDuration > 0)
            {
                EditorGUILayout.HelpBox("Fade settings will be applied to all AudioProxies in this zone.", MessageType.Info);
            }
        }

        private void DrawStatusInfo()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            bool hasManager = zone.audioManager != null;
            bool hasCollider = zone.GetComponent<Collider>() != null;

            if (!hasManager)
            {
                EditorGUILayout.HelpBox("⚠️ No AudioManager assigned!", MessageType.Error);
            }
            else if (!zone.childZoneMode)
            {
                bool hasValidProxies = zone.audioProxies != null && zone.audioProxies.Length > 0;
                if (!hasValidProxies)
                {
                    EditorGUILayout.HelpBox("⚠️ No AudioProxies assigned!", MessageType.Error);
                }
                else if (!hasCollider)
                {
                    EditorGUILayout.HelpBox("⚠️ No Collider found! Add a Collider component to make this a trigger zone.", MessageType.Warning);
                }
                else
                {
                    string proxyCount = zone.audioProxies.Length.ToString();
                    EditorGUILayout.HelpBox($"✅ Zone configured with {proxyCount} AudioProxies and is ready!", MessageType.Info);
                }
            }
            else
            {
                if (!hasCollider)
                {
                    EditorGUILayout.HelpBox("⚠️ No Collider found! Add a Collider component to make this a trigger zone.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ Child Zone ready! Assign this to an AudioZoneGroup to manage audio.", MessageType.Info);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("Zone Registered", IsZoneRegistered());
                    if (!zone.childZoneMode)
                    {
                        EditorGUILayout.Toggle("Player Inside", zone.IsPlayerInside());
                    }
                }
            }
        }

        private bool IsZoneRegistered()
        {
            if (zone.audioManager == null || zone.audioManager.registeredZones == null) return false;
            
            for (int i = 0; i < zone.audioManager.registeredZones.Length; i++)
            {
                if (zone.audioManager.registeredZones[i] == zone) return true;
            }
            return false;
        }

        private void RegisterZoneWithManager()
        {
            if (zone.audioManager == null) return;

            var registeredZones = zone.audioManager.registeredZones;
            if (registeredZones == null)
            {
                zone.audioManager.registeredZones = new AudioZone[] { zone };
            }
            else
            {
                var newArray = new AudioZone[registeredZones.Length + 1];
                for (int i = 0; i < registeredZones.Length; i++)
                {
                    newArray[i] = registeredZones[i];
                }
                newArray[registeredZones.Length] = zone;
                zone.audioManager.registeredZones = newArray;
            }
            
            EditorUtility.SetDirty(zone.audioManager);
            Debug.Log($"Registered {zone.name} with {zone.audioManager.name}");
        }

        private void SetupAsTrigger()
        {
            Collider existingCollider = zone.GetComponent<Collider>();
            if (existingCollider == null)
            {
                BoxCollider boxCollider = zone.gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(5f, 2f, 5f);
                Debug.Log("Added BoxCollider as trigger to AudioZone");
            }
            else
            {
                existingCollider.isTrigger = true;
                Debug.Log("Set existing collider as trigger");
            }
        }
        
        private void DrawUtilityButtons()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!zone.childZoneMode)
                {
                    if (GUILayout.Button("Play All Audio"))
                    {
                        if (Application.isPlaying)
                        {
                            zone.PlayAllProxies();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Play Audio", "This function only works in Play Mode.", "OK");
                        }
                    }

                    if (GUILayout.Button("Stop All Audio"))
                    {
                        if (Application.isPlaying)
                        {
                            zone.StopAllProxies();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Stop Audio", "This function only works in Play Mode.", "OK");
                        }
                    }
                }
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Setup as Trigger"))
                {
                    SetupAsTrigger();
                }

                if (zone.audioManager != null && !IsZoneRegistered())
                {
                    if (GUILayout.Button("Register with Manager"))
                    {
                        RegisterZoneWithManager();
                    }
                }
            }
        }
    }
}