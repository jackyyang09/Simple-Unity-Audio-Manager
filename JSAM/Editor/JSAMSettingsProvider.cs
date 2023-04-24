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
            masterVolumeParam,
            musicGroup,
            musicVolumeParam,
            soundGroup,
            soundVolumeParam,
            voiceGroup,
            voiceVolumeParam,
            saveVolumeToPlayerPrefs,
            masterVolumeKey,
            masterMutedKey,
            musicVolumeKey,
            musicMutedKey,
            soundVolumeKey,
            soundMutedKey,
            packagePath,
            presetsPath,
            fontSize;

        protected JSAMSettings Settings => JSAMSettings.Settings;
        protected SerializedObject SerializedObject => JSAMSettings.SerializedObject;
        SerializedObject pathSO;

        protected SerializedProperty FindProp(string prop) => SerializedObject.FindProperty(prop);
        void FindSerializedProperties()
        {
            pathSO = new SerializedObject(JSAMPaths.Instance);

            spatialSound = SerializedObject.FindProperty(nameof(spatialSound));
            startingSoundChannels = SerializedObject.FindProperty(nameof(startingSoundChannels));
            startingMusicChannels = SerializedObject.FindProperty(nameof(startingMusicChannels));
            defaultSoundMaxDistance = SerializedObject.FindProperty(nameof(defaultSoundMaxDistance));
            disableConsoleLogs = SerializedObject.FindProperty(nameof(disableConsoleLogs));
            dontDestroyOnLoad = SerializedObject.FindProperty(nameof(dontDestroyOnLoad));
            dynamicSourceAllocation = SerializedObject.FindProperty(nameof(dynamicSourceAllocation));
            soundChannelPrefabOverride = SerializedObject.FindProperty(nameof(soundChannelPrefabOverride));
            musicChannelPrefabOverride = SerializedObject.FindProperty(nameof(musicChannelPrefabOverride));
            stopSoundsOnSceneLoad = SerializedObject.FindProperty(nameof(stopSoundsOnSceneLoad));
            spatializationMode = SerializedObject.FindProperty(nameof(spatializationMode));
            timeScaledSounds = SerializedObject.FindProperty(nameof(timeScaledSounds));
            mixer = SerializedObject.FindProperty(nameof(mixer));
            masterGroup = SerializedObject.FindProperty(nameof(masterGroup));
            masterVolumeParam = SerializedObject.FindProperty(nameof(masterVolumeParam));
            musicGroup = SerializedObject.FindProperty(nameof(musicGroup));
            musicVolumeParam = SerializedObject.FindProperty(nameof(musicVolumeParam));
            soundGroup = SerializedObject.FindProperty(nameof(soundGroup));
            soundVolumeParam = SerializedObject.FindProperty(nameof(soundVolumeParam));
            voiceGroup = SerializedObject.FindProperty(nameof(voiceGroup));
            voiceVolumeParam = SerializedObject.FindProperty(nameof(voiceVolumeParam));
            saveVolumeToPlayerPrefs = SerializedObject.FindProperty(nameof(saveVolumeToPlayerPrefs));
            masterVolumeKey = SerializedObject.FindProperty(nameof(masterVolumeKey));
            masterMutedKey = SerializedObject.FindProperty(nameof(masterMutedKey));
            musicVolumeKey = SerializedObject.FindProperty(nameof(musicVolumeKey));
            musicMutedKey = SerializedObject.FindProperty(nameof(musicMutedKey));
            soundVolumeKey = SerializedObject.FindProperty(nameof(soundVolumeKey));
            soundMutedKey = SerializedObject.FindProperty(nameof(soundMutedKey));

            packagePath = pathSO.FindProperty(nameof(packagePath));
            presetsPath = pathSO.FindProperty(nameof(presetsPath));
            fontSize = SerializedObject.FindProperty("quickReferenceFontSize");
        }
        #endregion

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            FindSerializedProperties();
        }

        public override void OnGUI(string searchContext)
        {
            JSAMPaths.Instance.ResetPresetsPathIfInvalid();

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
                EditorGUILayout.PropertyField(masterVolumeParam);
                EditorGUILayout.PropertyField(musicGroup);
                EditorGUILayout.PropertyField(musicVolumeParam);
                EditorGUILayout.PropertyField(soundGroup);
                EditorGUILayout.PropertyField(soundVolumeParam);
                EditorGUILayout.PropertyField(voiceGroup);
                EditorGUILayout.PropertyField(voiceVolumeParam);
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

            packagePath.stringValue = JSAMEditorHelper.RenderSmartFolderProperty(packagePath.GUIContent(), packagePath.stringValue);
            presetsPath.stringValue = JSAMEditorHelper.RenderSmartFolderProperty(presetsPath.GUIContent(), presetsPath.stringValue);

            if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                JSAMSettings.Settings.Reset();
            }

            SerializedObject.ApplyModifiedProperties();
            pathSO.ApplyModifiedProperties();

            EditorGUIUtility.labelWidth -= 50;
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