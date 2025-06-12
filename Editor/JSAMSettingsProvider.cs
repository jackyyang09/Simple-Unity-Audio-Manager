using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [InitializeOnLoad]
    class SettingsHook
    {
        static SettingsHook()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        static void PlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;

            if (JSAMSettings.Settings.MasterTrack == null)
            {
                if (EditorUtility.DisplayDialog("JSAM Warning",
                "The Master Volume track is unset in the JSAM Settings!\n" +
                "Starting in version 3.1.2, JSAM requires the use of Volume Track assets to control volume.\n" +
                "These fields should have been populated with default assets automatically, but may be nulled " +
                "if you're upgrading from an older version of JSAM.\n" +
                "You can choose to set them to default values are continue into Play Mode or return to Edit Mode " +
                "and set them yourself.", "Populate and Play", "Return to Edit Mode"))
                {
                    JSAMSettings.Settings.AssignDefaultTracks();

                    EditorUtility.DisplayDialog("Track Settings Updated",
                        "Track settings have been set to their defaults! Now continuing to Play Mode", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Returning to Edit Mode",
                        "JSAM will not function properly unless the Master track field has been set.", "Understood.");

                    EditorApplication.isPlaying = false;
                    JSAMSettingsProvider.OpenSettingsWindow();
                }
            }
        }
    }

    public class JSAMSettingsProvider : SettingsProvider
    {
        public JSAMSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
        {
        }

        #region Properties
        SerializedProperty
            spatialSound,
            startingSoundChannels,
            startingMusicChannels,
            defaultSoundMaxDistance,
            dontDestroyOnLoad,
            dynamicSourceAllocation,
            soundChannelPrefabOverride,
            musicChannelPrefabOverride,
            stopSoundsOnSceneChanged,
            stopMusicOnSceneChanged,
            spatializationMode,
            timeScaledSounds,

            saveVolumeToPlayerPrefs,
            masterTrack,
            tracks,

            disableConsoleLogs,
            quickReferenceFontSize,
            useBuiltInAudioListRenderer,
            packagePath,
            presetsPath;

        JSAMSettings Settings => JSAMSettings.Settings;
        SerializedObject SettingsSO => JSAMSettings.SerializedObject;
        SerializedObject PathSO => JSAMPaths.SerializedObject;

        const string SettingsPath = "Project/Audio - JSAM";
        public static void OpenSettingsWindow() => SettingsService.OpenProjectSettings(SettingsPath);

        protected SerializedProperty FindProp(string prop) => SettingsSO.FindProperty(prop);
        void FindSerializedProperties()
        {
            spatialSound = SettingsSO.FindProperty(nameof(spatialSound));
            startingSoundChannels = SettingsSO.FindProperty(nameof(startingSoundChannels));
            startingMusicChannels = SettingsSO.FindProperty(nameof(startingMusicChannels));
            defaultSoundMaxDistance = SettingsSO.FindProperty(nameof(defaultSoundMaxDistance));
            dontDestroyOnLoad = SettingsSO.FindProperty(nameof(dontDestroyOnLoad));
            dynamicSourceAllocation = SettingsSO.FindProperty(nameof(dynamicSourceAllocation));
            soundChannelPrefabOverride = SettingsSO.FindProperty(nameof(soundChannelPrefabOverride));
            musicChannelPrefabOverride = SettingsSO.FindProperty(nameof(musicChannelPrefabOverride));
            stopSoundsOnSceneChanged = SettingsSO.FindProperty(nameof(stopSoundsOnSceneChanged));
            stopMusicOnSceneChanged = SettingsSO.FindProperty(nameof(stopMusicOnSceneChanged));
            spatializationMode = SettingsSO.FindProperty(nameof(spatializationMode));
            timeScaledSounds = SettingsSO.FindProperty(nameof(timeScaledSounds));

            saveVolumeToPlayerPrefs = SettingsSO.FindProperty(nameof(saveVolumeToPlayerPrefs));
            masterTrack = SettingsSO.FindProperty(nameof(masterTrack));
            tracks = SettingsSO.FindProperty(nameof(tracks));

            disableConsoleLogs = SettingsSO.FindProperty(nameof(disableConsoleLogs));
            quickReferenceFontSize = SettingsSO.FindProperty(nameof(quickReferenceFontSize));
            useBuiltInAudioListRenderer = SettingsSO.FindProperty(nameof(useBuiltInAudioListRenderer));
            packagePath = PathSO.FindProperty(nameof(packagePath));
            presetsPath = PathSO.FindProperty(nameof(presetsPath));
        }
        #endregion

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            FindSerializedProperties();
        }

        public override void OnGUI(string searchContext)
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Changes will not take into effect until you stop and re-enter Play Mode!", 
                    MessageType.Warning);
            }

            // This makes prefix labels larger
            EditorGUIUtility.labelWidth += 50;

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSoundMaxDistance);
            EditorGUILayout.PropertyField(dontDestroyOnLoad);
            EditorGUILayout.PropertyField(dynamicSourceAllocation);
            EditorGUILayout.PropertyField(spatializationMode);
            EditorGUILayout.PropertyField(startingMusicChannels);
            EditorGUILayout.PropertyField(startingSoundChannels);
            EditorGUILayout.PropertyField(stopSoundsOnSceneChanged);
            EditorGUILayout.PropertyField(stopMusicOnSceneChanged);
            EditorGUILayout.PropertyField(timeScaledSounds);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(soundChannelPrefabOverride);
            if (EditorGUI.EndChangeCheck())
            {
                var go = soundChannelPrefabOverride.objectReferenceValue as GameObject;
                if (go)
                {
                    if (!go.GetComponent<AudioSource>())
                    {
                        soundChannelPrefabOverride.objectReferenceValue = null;
                        EditorUtility.DisplayDialog("Prefab Validation Error!",
                        "Your prefab is missing an AudioSource component!",
                        "Damn.");
                    }
                    else if (!go.GetComponent<SoundChannelHelper>())
                    {
                        go.AddComponent<SoundChannelHelper>();
                    }

                    if (go.TryGetComponent(out MusicChannelHelper musicHelper))
                    {
                        GameObject.DestroyImmediate(musicHelper, true);
                    }
                }
            }
            if (GUILayout.Button(" Clear ", GUILayout.ExpandWidth(false)))
            {
                var go = soundChannelPrefabOverride.objectReferenceValue as GameObject;
                if (go.TryGetComponent(out SoundChannelHelper soundHelper))
                {
                    GameObject.DestroyImmediate(soundHelper, true);
                }
                soundChannelPrefabOverride.objectReferenceValue = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(musicChannelPrefabOverride);
            if (EditorGUI.EndChangeCheck())
            {
                var go = musicChannelPrefabOverride.objectReferenceValue as GameObject;
                if (go)
                {
                    if (!go.GetComponent<AudioSource>())
                    {
                        musicChannelPrefabOverride.objectReferenceValue = null;
                        EditorUtility.DisplayDialog("Prefab Validation Error!",
                        "Your prefab is missing an AudioSource component!",
                        "Damn.");
                    }
                    else if (!go.GetComponent<MusicChannelHelper>())
                    {
                        go.AddComponent<MusicChannelHelper>();
                    }

                    if (go.TryGetComponent(out SoundChannelHelper soundHelper))
                    {
                        GameObject.DestroyImmediate(soundHelper, true);
                    }
                }
            }
            if (GUILayout.Button(" Clear ", GUILayout.ExpandWidth(false)))
            {
                var go = musicChannelPrefabOverride.objectReferenceValue as GameObject;
                if (go.TryGetComponent(out MusicChannelHelper musicHelper))
                {
                    GameObject.DestroyImmediate(musicHelper, true);
                }
                musicChannelPrefabOverride.objectReferenceValue = null;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Volume Tracks", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(saveVolumeToPlayerPrefs);

            var backup = GUI.color;
            if (masterTrack.objectReferenceValue == null)
            {
                GUI.color = Color.red;
            }
            EditorGUILayout.PropertyField(masterTrack);
            GUI.color = backup;

            EditorGUILayout.PropertyField(tracks);

            if (GUILayout.Button("Reset Tracks to Default", GUILayout.ExpandWidth(false)))
            {
                JSAMSettings.Settings.AssignDefaultTracks();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(disableConsoleLogs);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(quickReferenceFontSize, new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
            if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                quickReferenceFontSize.intValue--;
            }
            else if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                quickReferenceFontSize.intValue++;
            }
            EditorGUILayout.EndHorizontal();

#if UNITY_2020_3_OR_NEWER
            EditorGUILayout.PropertyField(useBuiltInAudioListRenderer);
#endif

            EditorGUI.BeginChangeCheck();
            packagePath.stringValue = JSAMEditorHelper.RenderSmartFolderProperty(packagePath.GUIContent(), packagePath.stringValue);
            presetsPath.stringValue = JSAMEditorHelper.RenderSmartFolderProperty(presetsPath.GUIContent(), presetsPath.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                JSAMPaths.TrySave(true);
            }

            GUIContent content = new GUIContent("Reset Editor Settings to Default");
            if (GUILayout.Button(content, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                ResetEditorSettings();
            }

            SettingsSO.ApplyModifiedProperties();
            PathSO.ApplyModifiedProperties();

            EditorGUIUtility.labelWidth -= 50;
        }

        public void ResetEditorSettings()
        {
            JSAMSettings.Settings.ResetEditor();
            JSAMPaths.Instance.ResetPaths();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new JSAMSettingsProvider(SettingsPath, SettingsScope.Project);
            provider.keywords = GetSearchKeywordsFromSerializedObject(JSAMSettings.SerializedObject);

            return provider;
        }
    }
}