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
            Window.wantsMouseEnterLeaveWindow = true;
            AssignAsset();
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
                asset = JSAMSettings.Settings.SelectedSettings;
            }

            if (asset != null)
            {
                serializedObject = new SerializedObject(asset);
                DesignateSerializedProperties();
            }
        }

        SerializedProperty disableConsoleLogs;
        SerializedProperty dontDestroyOnLoad;
        SerializedProperty dynamicSourceAllocation;
        SerializedProperty spatializeOnLateUpdate;
        SerializedProperty spatialSound;
        SerializedProperty startingAudioSources;
        SerializedProperty stopSoundsOnSceneLoad;
        SerializedProperty timeScaledSounds;

        protected override void DesignateSerializedProperties()
        {
            disableConsoleLogs = FindProp("disableConsoleLogs");
            dontDestroyOnLoad = FindProp("dontDestroyOnLoad");
            dynamicSourceAllocation = FindProp("dynamicSourceAllocation");
            spatializeOnLateUpdate = FindProp("spatializeLateUpdate");
            spatialSound = FindProp("spatialSound");
            startingAudioSources = FindProp("startingAudioSources");
            stopSoundsOnSceneLoad = FindProp("stopSoundsOnSceneLoad");

            Window.Repaint();
        }

        private void OnGUI()
        {
            if (serializedObject == null)
            {

            }
            else
            {
                serializedObject.Update();

                EditorGUIUtility.labelWidth += 50;
                EditorGUILayout.PropertyField(disableConsoleLogs);
                EditorGUILayout.PropertyField(dontDestroyOnLoad);
                EditorGUILayout.PropertyField(dynamicSourceAllocation);
                EditorGUILayout.PropertyField(spatializeOnLateUpdate);
                EditorGUILayout.PropertyField(spatialSound);
                EditorGUILayout.PropertyField(startingAudioSources);
                EditorGUILayout.PropertyField(stopSoundsOnSceneLoad);

                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUIUtility.labelWidth -= 50;
            }
        }
    }
}