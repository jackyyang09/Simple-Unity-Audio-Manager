using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public class JSAMSettings : ScriptableObject
    {
        [Tooltip("The root folder of JSAM")]
        [SerializeField] string packagePath;
        public string PackagePath
        {
            get
            {
                if (packagePath.IsNullEmptyOrWhiteSpace() || !AssetDatabase.IsValidFolder(packagePath))
                {
                    packagePath = JSAMEditorHelper.GetAudioManagerPath;
                    packagePath = packagePath.Remove(packagePath.IndexOf("/Scripts/AudioManager.cs"));
                }
                return packagePath;
            }
        }

        [Tooltip("The folder that holds all JSAM-related presets. Audio File object presets will be saved here automatically.")]
        [SerializeField] string presetsPath;
        public string PresetsPath
        {
            get
            {
                ResetPresetsPathIfInvalid();
                return presetsPath;
            }
        }
        public void ResetPresetsPathIfInvalid()
        {
            if (!AssetDatabase.IsValidFolder(presetsPath)) presetsPath = PackagePath + "/Presets";
        }

        [Tooltip("The font size used when rendering \"quick reference guides\" in JSAM editor windows")]
        [SerializeField] int quickReferenceFontSize = 10;
        public int QuickReferenceFontSize
        {
            get
            {
                return quickReferenceFontSize;
            }
        }

        [Tooltip("If true, prevents this window from showing up on Unity startup. You can find this window under Window -> JSAM -> JSAM Startup")]
        [SerializeField] bool hideStartupMessage = false;
        public bool HideStartupMessage
        {
            get
            {
                return hideStartupMessage;
            }
        }

        static string settingsAssetName = "JSAMSettings.asset";

        public static string SettingsSavePath
        {
            get
            {
                string path = JSAMEditorHelper.GetAudioManagerPath;
                path = path.Remove(path.IndexOf("/Scripts/AudioManager.cs"));
                path += "/Editor/Preferences";
                return path;
            }
        }
        
        public static JSAMSettings Settings
        {
            get
            {
                string settingsSavePath = SettingsSavePath;
                var settings = AssetDatabase.LoadAssetAtPath<JSAMSettings>(settingsSavePath + "/" + settingsAssetName);
                if (settings == null)
                {
                    if (JSAMEditorHelper.GenerateFolderStructureAt(settingsSavePath, false))
                    {
                        settings = ScriptableObject.CreateInstance<JSAMSettings>();
                        JSAMEditorHelper.CreateAssetSafe(settings, settingsSavePath + "/" + settingsAssetName);
                        AssetDatabase.SaveAssets();
                    }
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

        [SerializeField] EditorCompatability.AgnosticGUID<AudioLibrary> selectedLibrary;

        public AudioLibrary SelectedLibrary
        {
            get
            {
                return selectedLibrary.SavedObject;
            }
            set
            {
                selectedLibrary.SavedObject = value;
            }
        }

        [SerializeField] string selectedSettings;
        public AudioManagerSettings SelectedSettings
        {
            get
            {
                return AssetDatabase.LoadAssetAtPath<AudioManagerSettings>(selectedSettings);
            }
            set
            {
                selectedSettings = AssetDatabase.GetAssetPath(value);
            }
        }

        public void Reset()
        {
            packagePath = JSAMEditorHelper.GetAudioManagerPath;
            packagePath = packagePath.Remove(packagePath.IndexOf("/Scripts/AudioManager.cs"));
            presetsPath = packagePath + "/Presets";
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class JSAMSettingsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            JSAMSettings.Settings.ResetPresetsPathIfInvalid();

            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/Audio - JSAM", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                //label = "Audio - JSAM",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    EditorGUIUtility.labelWidth += 50;

                    var settings = JSAMSettings.SerializedSettings;
                    SerializedProperty packagePath = settings.FindProperty(nameof(packagePath));
                    SerializedProperty presetsPath = settings.FindProperty(nameof(presetsPath));
                    SerializedProperty fontSize = settings.FindProperty("quickReferenceFontSize");
                    SerializedProperty hideStartup = settings.FindProperty("hideStartupMessage");

                    JSAMEditorHelper.RenderSmartFolderProperty(packagePath.GUIContent(), packagePath);
                    JSAMEditorHelper.RenderSmartFolderProperty(presetsPath.GUIContent(), presetsPath);

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(fontSize, new GUILayoutOption[] { GUILayout.ExpandWidth(false)});
                    if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        fontSize.intValue--;
                    }
                    else if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        fontSize.intValue++;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (GUILayout.Button("Reset to Default", new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        JSAMSettings.Settings.Reset();
                    }

                    if (settings.hasModifiedProperties)
                    {
                        settings.ApplyModifiedProperties();
                    }

                    EditorGUIUtility.labelWidth -= 50;
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "JSAM", "AudioManager", "Package", "Presets", "Enums" })
        };
    
            return provider;
        }
    }
}