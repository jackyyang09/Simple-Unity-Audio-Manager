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
                    var files = AssetDatabase.FindAssets("t:" + nameof(JSAMPaths));
                    foreach (var item in files)
                    {
                        var p = AssetDatabase.GUIDToAssetPath(item);
                        var i = AssetDatabase.LoadAssetAtPath<JSAMPaths>(p);
                        paths = i as JSAMPaths;
                        if (paths) break;
                    }
                    if (paths == null) TryCreateNewPathAsset();
                }
                return paths;
#endif
            }
        }

#if !UNITY_2021_3_OR_NEWER
        static JSAMPaths paths;
#endif

        public static void TrySave(bool b = false)
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
                    TrySave(true);
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
                TrySave(true);
            }
        }

#if !UNITY_2021_3_OR_NEWER
        static string FULL_PATH => ASSET_PATH + ASSET_NAME;
        static string ASSET_PATH = "Assets/";
        static string ASSET_NAME = nameof(JSAMPaths) + ".asset";

        public static void TryCreateNewPathAsset()
        {
            if (EditorApplication.isCompiling) return;

            if (!EditorUtility.DisplayDialog(
                "JSAM First Time Setup",
                "In order to function, JSAM needs a place to store paths. By default, a " +
                "Paths asset will be created at " + FULL_PATH + ", but you may move it " +
                "elsewhere, so long as it's within the Project.\n" +
                "Moving the asset file out of the Project will prompt this message to appear again!",
                "Ok Create It.", "Not Yet!")) return;

            var asset = CreateInstance<JSAMPaths>();
            AssetDatabase.CreateAsset(asset, FULL_PATH);
            asset.ResetPresetsPathIfInvalid();

            paths = asset;
            EditorUtility.DisplayDialog("JSAM First Time Setup", "Path asset created successfully!", "Cool.");
        }
#endif
    }

#if !UNITY_2021_3_OR_NEWER
    [CustomEditor(typeof(JSAMPaths))]
    public class JSAMPathsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var style = new GUIStyle(EditorStyles.helpBox).SetFontSize(JSAMSettings.Settings.QuickReferenceFontSize);
            EditorGUILayout.LabelField("I'm necessary to JSAM's function, please don't delete me!", style);
        }
    }
#endif
}