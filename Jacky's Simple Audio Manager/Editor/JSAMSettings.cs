using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public class JSAMSettings : ScriptableObject
    {
        [SerializeField] string packagePath;
        public string PackagePath
        {
            get
            {
                if (string.IsNullOrEmpty(packagePath) || string.IsNullOrWhiteSpace(packagePath))
                {
                    packagePath = JSAMEditorHelper.GetAudioManagerPath();
                    packagePath.Remove(packagePath.IndexOf("/Scripts/AudioManager.cs"));
                }
                return packagePath;
            }
        }
        [SerializeField] string presetsPath;
        [SerializeField] string enumPath;

        static string settingsAssetName = "JSAMSettings.asset";

        public static string SettingsSavePath
        {
            get
            {
                string path = JSAMEditorHelper.GetAudioManagerPath();
                path = path.Remove(path.IndexOf("/Scripts/AudioManager.cs"));
                path += "/Editor/Preferences";
                return path;
            }
        }
        
        public static JSAMSettings Settings
        {
            get
            {
                var settings = AssetDatabase.LoadAssetAtPath<JSAMSettings>(SettingsSavePath + "/" + settingsAssetName);
                if (settings == null)
                {
                    if (!AssetDatabase.IsValidFolder(SettingsSavePath))
                    {
                        JSAMEditorHelper.GenerateFolderStructure(SettingsSavePath, true);
                    }
                    settings = ScriptableObject.CreateInstance<JSAMSettings>();
                    JSAMEditorHelper.CreateAssetSafe(settings, SettingsSavePath + "/" + settingsAssetName);
                    AssetDatabase.SaveAssets();
                }
                return settings;
            }
        }
        
        public static SerializedObject SerializedObject
        {
            get
            {
                return new SerializedObject(Settings);
            }
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class JSAMSettingsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/Audio - JSAM", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                //label = "Audio - JSAM",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings = JSAMSettings.SerializedObject;
                    SerializedProperty packagePath = settings.FindProperty("packagePath");
                    SerializedProperty presetsPath = settings.FindProperty("presetsPath");
                    SerializedProperty enumPath = settings.FindProperty("enumPath");

                    EditorGUILayout.PropertyField(packagePath);
                    EditorGUILayout.PropertyField(presetsPath);
                    EditorGUILayout.PropertyField(enumPath);
                    if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        //packagePath.stringValue = 
                    }
                },
    
                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "JSAM", "AudioManager", "Package", "Presets", "Enums" })
            };
    
            return provider;
        }
    }
}