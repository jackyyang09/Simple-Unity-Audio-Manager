using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public class JSAMSettingsProvider : SettingsProvider
    {
        static bool mixerFoldout = false;
        static bool prefsFoldout = false;

        public JSAMSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
        {
        }

        #region Properties
        SerializedProperty
            spatialSound,
            startingSoundChannels,
            startingMusicChannels,
            defaultSoundMaxDistance,
            disableConsoleLogs,
            dontDestroyOnLoad,
            dynamicSourceAllocation,
            soundChannelPrefabOverride,
            musicChannelPrefabOverride,
            stopSoundsOnSceneLoad,
            spatializationMode,
            timeScaledSounds,
            mixer,
            masterGroup,
            musicGroup,
            soundGroup,
            voiceGroup,
            saveVolumeToPlayerPrefs,
            masterVolumeKey,
            masterMutedKey,
            musicVolumeKey,
            musicMutedKey,
            soundVolumeKey,
            soundMutedKey,
            voiceVolumeKey,
            voiceMutedKey,
            packagePath,
            presetsPath,
            fontSize;

        JSAMSettings Settings => JSAMSettings.Settings;
        SerializedObject SettingsSO => JSAMSettings.SerializedObject;
        SerializedObject PathSO => JSAMPaths.SerializedObject;

        protected SerializedProperty FindProp(string prop) => SettingsSO.FindProperty(prop);
        void FindSerializedProperties()
        {
            spatialSound = SettingsSO.FindProperty(nameof(spatialSound));
            startingSoundChannels = SettingsSO.FindProperty(nameof(startingSoundChannels));
            startingMusicChannels = SettingsSO.FindProperty(nameof(startingMusicChannels));
            defaultSoundMaxDistance = SettingsSO.FindProperty(nameof(defaultSoundMaxDistance));
            disableConsoleLogs = SettingsSO.FindProperty(nameof(disableConsoleLogs));
            dontDestroyOnLoad = SettingsSO.FindProperty(nameof(dontDestroyOnLoad));
            dynamicSourceAllocation = SettingsSO.FindProperty(nameof(dynamicSourceAllocation));
            soundChannelPrefabOverride = SettingsSO.FindProperty(nameof(soundChannelPrefabOverride));
            musicChannelPrefabOverride = SettingsSO.FindProperty(nameof(musicChannelPrefabOverride));
            stopSoundsOnSceneLoad = SettingsSO.FindProperty(nameof(stopSoundsOnSceneLoad));
            spatializationMode = SettingsSO.FindProperty(nameof(spatializationMode));
            timeScaledSounds = SettingsSO.FindProperty(nameof(timeScaledSounds));
            mixer = SettingsSO.FindProperty(nameof(mixer));
            masterGroup = SettingsSO.FindProperty(nameof(masterGroup));
            musicGroup = SettingsSO.FindProperty(nameof(musicGroup));
            soundGroup = SettingsSO.FindProperty(nameof(soundGroup));
            voiceGroup = SettingsSO.FindProperty(nameof(voiceGroup));
            saveVolumeToPlayerPrefs = SettingsSO.FindProperty(nameof(saveVolumeToPlayerPrefs));
            masterVolumeKey = SettingsSO.FindProperty(nameof(masterVolumeKey));
            masterMutedKey = SettingsSO.FindProperty(nameof(masterMutedKey));
            musicVolumeKey = SettingsSO.FindProperty(nameof(musicVolumeKey));
            musicMutedKey = SettingsSO.FindProperty(nameof(musicMutedKey));
            soundVolumeKey = SettingsSO.FindProperty(nameof(soundVolumeKey));
            soundMutedKey = SettingsSO.FindProperty(nameof(soundMutedKey));
            voiceVolumeKey = SettingsSO.FindProperty(nameof(voiceVolumeKey));
            voiceMutedKey = SettingsSO.FindProperty(nameof(voiceMutedKey));

            packagePath = PathSO.FindProperty(nameof(packagePath));
            presetsPath = PathSO.FindProperty(nameof(presetsPath));
            fontSize = SettingsSO.FindProperty("quickReferenceFontSize");
        }
        #endregion

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            FindSerializedProperties();
        }

        public override void OnGUI(string searchContext)
        {
            // This makes prefix labels larger
            EditorGUIUtility.labelWidth += 50;

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSoundMaxDistance);
            EditorGUILayout.PropertyField(dontDestroyOnLoad);
            EditorGUILayout.PropertyField(dynamicSourceAllocation);
            EditorGUILayout.PropertyField(spatializationMode);
            EditorGUILayout.PropertyField(startingMusicChannels);
            EditorGUILayout.PropertyField(startingSoundChannels);
            EditorGUILayout.PropertyField(stopSoundsOnSceneLoad);
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
            mixerFoldout = EditorCompatability.SpecialFoldouts(mixerFoldout, "Mixer Settings");
            if (mixerFoldout)
            {
                EditorGUILayout.PropertyField(mixer);
                EditorGUILayout.PropertyField(masterGroup);
                EditorGUILayout.PropertyField(musicGroup);
                EditorGUILayout.PropertyField(soundGroup);
                EditorGUILayout.PropertyField(voiceGroup);
            }
            EditorCompatability.EndSpecialFoldoutGroup();

            EditorGUILayout.Space();
            prefsFoldout = EditorCompatability.SpecialFoldouts(prefsFoldout, "Player Prefs Volume");
            if (prefsFoldout)
            {
                EditorGUILayout.PropertyField(saveVolumeToPlayerPrefs);
                EditorGUI.BeginDisabledGroup(!saveVolumeToPlayerPrefs.boolValue);
                EditorGUILayout.PropertyField(masterVolumeKey);
                EditorGUILayout.PropertyField(masterMutedKey);
                EditorGUILayout.PropertyField(musicVolumeKey);
                EditorGUILayout.PropertyField(musicMutedKey);
                EditorGUILayout.PropertyField(soundVolumeKey);
                EditorGUILayout.PropertyField(soundMutedKey);
                EditorGUILayout.PropertyField(voiceVolumeKey);
                EditorGUILayout.PropertyField(voiceMutedKey);
                EditorGUI.EndDisabledGroup();
            }
            EditorCompatability.EndSpecialFoldoutGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(disableConsoleLogs);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(fontSize, new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
            if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                fontSize.intValue--;
            }
            else if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                fontSize.intValue++;
            }
            EditorGUILayout.EndHorizontal();

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

        //
        public void ResetEditorSettings()
        {
            JSAMSettings.Settings.ResetEditor();
            JSAMPaths.Instance.Reset();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new JSAMSettingsProvider("Project/Audio - JSAM", SettingsScope.Project);
            provider.keywords = GetSearchKeywordsFromSerializedObject(JSAMSettings.SerializedObject);

            return provider;
        }
    }
}