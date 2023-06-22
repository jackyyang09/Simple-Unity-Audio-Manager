using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
#if UNITY_2021_3_OR_NEWER
    [FilePath("ProjectSettings/JSAMPaths.asset", FilePathAttribute.Location.ProjectFolder)]
    public class JSAMPaths : ScriptableSingleton<JSAMPaths>
#else
    public class JSAMPaths : ScriptableObject
#endif
    {
        public static JSAMPaths Instance
        {
            get
            {
#if UNITY_2021_3_OR_NEWER
                return instance;
#else
                if (paths == null)
                {
                    var asset = Resources.Load(nameof(JSAMSettings));
                    paths = asset as JSAMPaths;
                    if (paths == null) TryCreateNewPathAsset();
                }
                return paths;
#endif
            }
        }

#if !UNITY_2021_3_OR_NEWER
        static JSAMPaths paths;
#endif

        public static void Save()
        {
#if UNITY_2021_3_OR_NEWER
            Instance.Save(true);
#endif
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

#if !UNITY_2021_3_OR_NEWER
        static readonly string FULL_PATH = ASSET_PATH + ASSET_NAME;
        static readonly string ASSET_PATH = "Assets/Settings/Resources/" + ASSET_NAME;
        static readonly string ASSET_NAME = nameof(JSAMPaths) + ".asset";

        public static void TryCreateNewPathAsset()
        {
            if (EditorApplication.isCompiling) return;

            if (!EditorUtility.DisplayDialog(
                "JSAM First Time Setup",
                "In order to function, JSAM needs a place to store paths. By default, a " +
                "Settings asset will be created at " + FULL_PATH + ", but you may move it " +
                "elsewhere, so long as it's in a Resources folder.\n" +
                "Moving it out of the Resources folder will prompt this message to appear again erroneously!",
                "Ok Create It.", "Not Yet!")) return;

            var asset = CreateInstance<JSAMPaths>();
            JSAMEditorHelper.GenerateFolderStructureAt(ASSET_NAME, false);
            AssetDatabase.CreateAsset(asset, ASSET_PATH);
            asset.ResetPresetsPathIfInvalid();

            paths = asset;
            EditorUtility.DisplayDialog("JSAM First Time Setup", "Path asset created successfully!", "Cool.");
        }
#endif
    }
}