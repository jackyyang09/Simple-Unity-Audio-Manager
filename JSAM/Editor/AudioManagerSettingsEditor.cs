using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioManagerSettings))]
    public class AudioManagerSettingsInspector : Editor
    {
        bool DEBUG_MODE = false;
        public override void OnInspectorGUI()
        {
            if (DEBUG_MODE) DrawDefaultInspector();
            else
            {
                if (!AudioManagerSettingsEditor.IsOpen)
                {
                    if (GUILayout.Button("Open Audio Manager Settings"))
                    {
                        AudioManagerSettingsEditor.Init();
                    }
                }
            }
        }
    }

    public class AudioManagerSettingsEditor : JSAMSerializedEditorWindow<AudioManagerSettings, AudioManagerSettingsEditor>
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            var newAsset = AssetDatabase.LoadAssetAtPath<AudioManagerSettings>(assetPath);
            if (newAsset)
            {
                Init();
                if (newAsset != asset)
                {
                    window.AssignAsset(newAsset);
                }
                return true;
            }
            return false;
        }

        private void OnSelectionChange()
        {
            AudioManagerSettings newLibrary = Selection.activeObject as AudioManagerSettings;
            if (newLibrary)
            {
                AssignAsset(newLibrary);
            }
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/JSAM/AudioManager Settings")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            Window.Show();
            window.SetWindowTitle();
            window.Focus();
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("AudioManager Settings");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            //Undo.undoRedoPerformed += OnUndoRedoPerformed;
            JSAMAssetModificationProcessor.OnJSAMAssetDeleted += OnJSAMAssetDeleted;
            Window.wantsMouseEnterLeaveWindow = true;
            AssignAsset();
        }

        protected void OnDisable()
        {
            JSAMAssetModificationProcessor.OnJSAMAssetDeleted -= OnJSAMAssetDeleted;
        }

        void AssignAsset(AudioManagerSettings newAsset = null)
        {
            if (newAsset != null) asset = newAsset;

            // Assign selected asset to settings
            if (JSAMSettings.Settings.SelectedSettings != asset && asset != null)
            {
                if (asset != null)
                {
                    JSAMSettings.Settings.SelectedSettings = asset;
                }
            }
            else if (asset == null)
            {
                if (AudioManager.InternalInstance) asset = AudioManager.Instance.Settings;
                else asset = JSAMSettings.Settings.SelectedSettings;
            }

            if (asset != null)
            {
                serializedObject = new SerializedObject(asset);
                DesignateSerializedProperties();
            }
        }

        private void OnJSAMAssetDeleted(string filePath)
        {
            if (filePath.Equals(AssetDatabase.GetAssetPath(JSAMSettings.Settings.SelectedSettings)))
            {
                serializedObject = null;
            }
        }

        SerializedProperty disableConsoleLogs;
        SerializedProperty dontDestroyOnLoad;
        SerializedProperty dynamicSourceAllocation;
        SerializedProperty spatializationMode;
        SerializedProperty spatialSound;
        SerializedProperty startingSoundChannels;
        SerializedProperty startingMusicChannels;
        SerializedProperty stopSoundsOnSceneLoad;
        SerializedProperty timeScaledSounds;

        SerializedProperty masterGroup;
        SerializedProperty masterVolumeParam;
        SerializedProperty musicGroup;
        SerializedProperty musicVolumeParam;
        SerializedProperty soundGroup;
        SerializedProperty soundVolumeParam;

        SerializedProperty saveVolumeToPlayerPrefs;
        SerializedProperty masterVolumeKey;
        SerializedProperty masterMutedKey;
        SerializedProperty musicVolumeKey;
        SerializedProperty musicMutedKey;
        SerializedProperty soundVolumeKey;
        SerializedProperty soundMutedKey;

        protected override void DesignateSerializedProperties()
        {
            disableConsoleLogs = FindProp(nameof(disableConsoleLogs));
            dontDestroyOnLoad = FindProp(nameof(dontDestroyOnLoad));
            dynamicSourceAllocation = FindProp(nameof(dynamicSourceAllocation));
            spatializationMode = FindProp(nameof(spatializationMode));
            spatialSound = FindProp(nameof(spatialSound));
            startingMusicChannels = FindProp(nameof(startingMusicChannels));
            startingSoundChannels = FindProp(nameof(startingSoundChannels));
            stopSoundsOnSceneLoad = FindProp(nameof(stopSoundsOnSceneLoad));
            timeScaledSounds = FindProp(nameof(timeScaledSounds));

            masterGroup = FindProp(nameof(masterGroup));
            masterVolumeParam = FindProp(nameof(masterVolumeParam));
            musicGroup = FindProp(nameof(musicGroup));
            musicVolumeParam = FindProp(nameof(musicVolumeParam));
            soundGroup = FindProp(nameof(soundGroup));
            soundVolumeParam = FindProp(nameof(soundVolumeParam));

            saveVolumeToPlayerPrefs = FindProp(nameof(saveVolumeToPlayerPrefs));
            masterVolumeKey = FindProp(nameof(masterVolumeKey));
            masterMutedKey = FindProp(nameof(masterMutedKey));
            musicVolumeKey = FindProp(nameof(musicVolumeKey));
            musicMutedKey = FindProp(nameof(musicMutedKey));
            soundVolumeKey = FindProp(nameof(soundVolumeKey));
            soundMutedKey = FindProp(nameof(soundMutedKey));

            Window.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            asset = EditorGUILayout.ObjectField(new GUIContent("Selected Library"), asset, typeof(AudioManagerSettings), false) as AudioManagerSettings;
            if (EditorGUI.EndChangeCheck())
            {
                if (asset) AssignAsset(asset);
                else serializedObject = null;
            }

            GUIContent blontent = new GUIContent("  Create  ", "Click to create a new AudioManager Settings asset");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                JSAMEditorHelper.OpenSmartSaveFileDialog<AudioManagerSettings>("New Settings");
            }
            EditorGUILayout.EndHorizontal();

            if (serializedObject == null)
            {
                EditorGUILayout.LabelField("Select an AudioManagerSettings asset to get started!");
            }
            else
            {
                serializedObject.Update();

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUIUtility.labelWidth += 50;
                EditorGUILayout.PropertyField(disableConsoleLogs);
                EditorGUILayout.PropertyField(dontDestroyOnLoad);
                EditorGUILayout.PropertyField(dynamicSourceAllocation);
                EditorGUILayout.PropertyField(spatializationMode);
                EditorGUILayout.PropertyField(spatialSound);
                EditorGUILayout.PropertyField(startingMusicChannels);
                EditorGUILayout.PropertyField(startingSoundChannels);
                EditorGUILayout.PropertyField(stopSoundsOnSceneLoad);
                EditorGUILayout.PropertyField(timeScaledSounds);

                EditorGUILayout.PropertyField(masterGroup);
                //EditorGUILayout.PropertyField(masterVolumeParam);
                EditorGUILayout.PropertyField(musicGroup);
                //EditorGUILayout.PropertyField(musicVolumeParam);
                EditorGUILayout.PropertyField(soundGroup);
                //EditorGUILayout.PropertyField(soundVolumeParam);
                EditorGUILayout.PropertyField(saveVolumeToPlayerPrefs);
                
                if (saveVolumeToPlayerPrefs.boolValue)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.PropertyField(masterVolumeKey);
                    EditorGUILayout.PropertyField(masterMutedKey);
                    EditorGUILayout.PropertyField(musicVolumeKey);
                    EditorGUILayout.PropertyField(musicMutedKey);
                    EditorGUILayout.PropertyField(soundVolumeKey);
                    EditorGUILayout.PropertyField(soundMutedKey);
                    EditorGUILayout.EndVertical();
                }
                
                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUIUtility.labelWidth -= 50;
                EditorGUILayout.EndVertical();
            }
        }
    }
}