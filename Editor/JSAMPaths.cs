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
            get => selectedLibrary.SavedObject;
            set => selectedLibrary.SavedObject = value;
        }
        
        public string PackagePath => "Packages/com.jackyyang09.simple-unity-audio-manager/";

        [Tooltip("The folder that holds all JSAM-related presets. Audio File object presets will be saved here automatically.")]
        [SerializeField] string presetsPath = "Assets/JSAM-Presets";
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
                presetsPath = "Assets/JSAM-Presets";
                Save(true);
            }
        }
    }
}