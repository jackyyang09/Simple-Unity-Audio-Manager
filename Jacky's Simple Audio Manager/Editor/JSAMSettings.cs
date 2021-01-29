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
                if (packagePath.IsNullEmptyOrWhiteSpace())
                {
                    packagePath = JSAMEditorHelper.GetAudioManagerPath();
                    packagePath = packagePath.Remove(packagePath.IndexOf("/Scripts/AudioManager.cs"));
                }
                return packagePath;
            }
        }
        [SerializeField] string presetsPath;
        public string PresetsPath
        {
            get
            {
                if (presetsPath.IsNullEmptyOrWhiteSpace())
                {
                    presetsPath = PackagePath + "/Presets";
                }
                return presetsPath;
            }
        }
        [SerializeField] string generatedEnumsPath;
        public string GeneratedEnumsPath
        {
            get
            {
                if (generatedEnumsPath.IsNullEmptyOrWhiteSpace())
                {
                    generatedEnumsPath = PackagePath + "/JSAMGenerated";
                }
                return generatedEnumsPath;
            }
        }

        [SerializeField] bool useNamespace = false;
        public bool UseNamespace
        {
            get
            {
                return useNamespace;
            }
        }
        [SerializeField] string enumNamespace = "JSAM";
        public string EnumNamespace
        {
            get
            {
                return enumNamespace;
            }
        }

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
        
        public static SerializedObject SerializedSettings
        {
            get
            {
                return new SerializedObject(Settings);
            }
        }

        [SerializeField] string selectedLibrary;
        public AudioLibrary SelectedLibrary
        {
            get
            {
                return AssetDatabase.LoadAssetAtPath<AudioLibrary>(selectedLibrary);
            }
            set
            {
                selectedLibrary = AssetDatabase.GetAssetPath(value);
            }
        }

        public void Reset()
        {
            packagePath = JSAMEditorHelper.GetAudioManagerPath();
            packagePath = packagePath.Remove(packagePath.IndexOf("/Scripts/AudioManager.cs"));
            presetsPath = packagePath + "/Presets";
            generatedEnumsPath = packagePath + "/JSAMGenerated";
            useNamespace = false;
            enumNamespace = "JSAM";
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
                    var settings = JSAMSettings.SerializedSettings;
                    SerializedProperty packagePath = settings.FindProperty("packagePath");
                    SerializedProperty presetsPath = settings.FindProperty("presetsPath");
                    SerializedProperty enumPath = settings.FindProperty("generatedEnumsPath");
                    SerializedProperty useNamespace = settings.FindProperty("useNamespace");
                    SerializedProperty enumNamespace = settings.FindProperty("enumNamespace");

                    JSAMEditorHelper.SmartFolderField(packagePath);
                    JSAMEditorHelper.SmartFolderField(presetsPath);
                    JSAMEditorHelper.SmartFolderField(enumPath);
                    JSAMEditorHelper.SmartFolderField(packagePath);
                    EditorGUILayout.PropertyField(useNamespace);
                    using (new EditorGUI.DisabledScope(!useNamespace.boolValue))
                    {
                        EditorGUILayout.PropertyField(enumNamespace);
                    }

                    if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        JSAMSettings.Settings.Reset();
                    }

                    if (settings.hasModifiedProperties)
                    {
                        settings.ApplyModifiedProperties();
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "JSAM", "AudioManager", "Package", "Presets", "Enums" })
            };
    
            return provider;
        }
    }
}