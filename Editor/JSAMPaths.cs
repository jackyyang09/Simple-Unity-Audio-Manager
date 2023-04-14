using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [FilePath("ProjectSettings/JSAMSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class JSAMPaths : ScriptableSingleton<JSAMPaths>
    {
        public static JSAMPaths Instance => instance;
        public static void Save() => Instance.Save(true);

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
                    Save(true);
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
            if (!AssetDatabase.IsValidFolder(presetsPath))
            {
                presetsPath = PackagePath + "/Presets";
                Save(true);
            }
        }
    }
}