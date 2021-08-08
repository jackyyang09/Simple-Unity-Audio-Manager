using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace JSAM.JSAMEditor
{
    public static class JSAMEditorExtensions
    {
        /// <summary>
        /// Adds a new element to the end of the array and returns the new element
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static SerializedProperty AddNewArrayElement(this SerializedProperty prop)
        {
            int index = prop.arraySize;
            prop.InsertArrayElementAtIndex(index);
            return prop.GetArrayElementAtIndex(index);
        }

        public static PropertyModification FindProp(this Preset preset, string propName)
        {
            for (int i = 0; i < preset.PropertyModifications.Length; i++)
            {
                if (preset.PropertyModifications[i].propertyPath.Equals(propName))
                {
                    return preset.PropertyModifications[i];
                }
            }
            return null;
        }
    }

    public class JSAMEditorHelper
    {
        /// <summary>
        /// January 20th 2021, don't you ever forget you dingus
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static void CreateAssetSafe(Object asset, string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                Debug.LogError("Error! Attempted to write an asset over a folder!");
                return;
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        /// <summary>
        /// This operation manipulates the active scene, so cache this value whenever possible
        /// </summary>
        public static string GetAudioManagerPath
        {
            get
            {
                MonoScript a = null;
                GameObject g = null;
                bool alt = false;
                if (!AudioManager.instance)
                {
                    g = new GameObject();
                    // Audio Events just so its safer
                    a = MonoScript.FromMonoBehaviour(g.AddComponent<AudioEvents>());
                    alt = true;
                }
                else a = MonoScript.FromMonoBehaviour(AudioManager.instance);
                string path = AssetDatabase.GetAssetPath(a);
                if (g != null) Object.DestroyImmediate(g);
                if (alt)
                {
                    path = path.Remove(path.IndexOf("AudioEvents.cs"));
                    path += "AudioManager.cs";
                }
                return path;
            }
        }

        public static string TimeToString(float time)
        {
            time *= 1000;
            int minutes = (int)time / 60000;
            int seconds = (int)time / 1000 - 60 * minutes;
            int milliseconds = (int)time - minutes * 60000 - 1000 * seconds;
            return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        }

        public static void GenerateFolderStructure(string filePath, bool ask = false)
        {
            if (!AssetDatabase.IsValidFolder(filePath))
            {
                string existingPath = "Assets";
                string unknownPath = filePath.Remove(0, existingPath.Length + 1); // Remove the Assets/ at the start of the path name
                string folderName = (unknownPath.Contains("/")) ? unknownPath.Substring(0, (unknownPath.IndexOf("/"))) : unknownPath;
                do
                {
                    if (!AssetDatabase.IsValidFolder(existingPath + "/" + folderName))
                    {
                        // Begin checking down the file path to see if it's valid
                        if (EditorUtility.DisplayDialog("Path does not exist!", "The folder " +
                            "\"" + existingPath + "/" + folderName +
                            "\" does not exist! Would you like to create this folder?", "Yes", "No"))
                        {
                            AssetDatabase.CreateFolder(existingPath, folderName);
                        }
                        else return;
                    }
                    existingPath += "/" + folderName;
                    // Full path still doesn't exist
                    if (!existingPath.Equals(filePath))
                    {
                        unknownPath = unknownPath.Remove(0, folderName.Length + 1);
                        folderName = (unknownPath.Contains("/")) ? unknownPath.Substring(0, (unknownPath.IndexOf("/"))) : unknownPath;
                    }
                }
                while (!AssetDatabase.IsValidFolder(filePath));
            }
        }

        public static void SmartFolderField(SerializedProperty folderProp)
        {
            EditorGUILayout.BeginHorizontal();
            string filePath = folderProp.stringValue;
            if (filePath == string.Empty) filePath = Application.dataPath;
            GUIContent blontent = new GUIContent(folderProp.displayName, folderProp.tooltip);
            EditorGUI.BeginChangeCheck();
            filePath = EditorGUILayout.DelayedTextField(blontent, filePath);
            if (EditorGUI.EndChangeCheck())
            {
                // If the user presses "cancel"
                if (filePath.Equals(string.Empty))
                {
                    return;
                }
                // or specifies something outside of this folder, reset filePath and don't proceed
                else if (!filePath.Contains("Assets"))
                {
                    EditorUtility.DisplayDialog("Folder Browsing Error!", "AudioManager is a Unity editor tool and can only " +
                        "function inside the project's Assets folder. Please choose a different folder.", "OK");
                    return;
                }
                else
                {
                    // Fix path to be usable for AssetDatabase.FindAssets
                    filePath = filePath.Remove(0, filePath.IndexOf("Assets"));
                    if (filePath[filePath.Length - 1] == '/') filePath = filePath.Remove(filePath.Length - 1, 1);
                }
            }
            SmartBrowseButton(folderProp);
            EditorGUILayout.EndHorizontal();
        }
		
		public static void OpenSmartSaveFileDialog<T>(string defaultName = "New Object", string startingPath = "Assets") where T : ScriptableObject
        {
            string savePath = EditorUtility.SaveFilePanel("Designate save path", startingPath, defaultName, "asset");
            if (savePath != "") // Make sure user didn't press "Cancel"
            {
                var asset = ScriptableObject.CreateInstance<T>();
                savePath = savePath.Remove(0, savePath.IndexOf("Assets/"));
                CreateAssetSafe(asset, savePath);
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
        }

        public static void SmartBrowseButton(SerializedProperty folderProp)
        {
            GUIContent buttonContent = new GUIContent("Browse", "Designate a new folder");
            if (GUILayout.Button(buttonContent, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(55) }))
            {
                string filePath = folderProp.stringValue;
                filePath = EditorUtility.OpenFolderPanel("Specify a new folder", filePath, string.Empty);

                // If the user presses "cancel"
                if (filePath.Equals(string.Empty))
                {
                    return;
                }
                // or specifies something outside of this folder, reset filePath and don't proceed
                else if (!filePath.Contains("Assets"))
                {
                    EditorUtility.DisplayDialog("Folder Browsing Error!", "AudioManager is a Unity editor tool and can only " +
                        "function inside the project's Assets folder. Please choose a different folder.", "OK");
                    return;
                }
                else if (filePath.Contains(Application.dataPath))
                {
                    // Fix path to be usable for AssetDatabase.FindAssets
                    filePath = filePath.Remove(0, filePath.IndexOf("Assets"));
                }

                folderProp.stringValue = filePath;
            }
        }

        public static List<T> ImportAssetsOrFoldersAtPath<T>(string filePath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(filePath);
            if (!AssetDatabase.IsValidFolder(filePath))
            {
                if (asset != null)
                {
                    return new List<T> { asset };
                }
            }
            else
            {
                List<T> imports = new List<T>();
                List<string> importTarget = new List<string>(Directory.GetDirectories(filePath));
                importTarget.AddRange(Directory.GetFiles(filePath));
                for (int i = 0; i < importTarget.Count; i++)
                {
                    imports.AddRange(ImportAssetsOrFoldersAtPath<T>(importTarget[i]));
                }
                return imports;
            }

            return new List<T>();
        }

        /// <summary>
        /// Copies the given string to your clipboard
        /// </summary>
        /// <param name="text"></param>
        public static void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            AudioManager.instance.DebugLog("Copied " + text + " to clipboard!");
        }

        public static bool RenderQuickReferenceGuide(bool foldout, string[] text)
        {
            foldout = EditorCompatability.SpecialFoldouts(foldout, "Quick Reference Guide");
            if (foldout)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i].Equals("Overview") || text[i].Equals("Tips"))
                    {
                        EditorGUILayout.LabelField(text[i], ApplyFontSizeToStyle(EditorStyles.boldLabel, JSAMSettings.Settings.QuickReferenceFontSize));
                        continue;
                    }
                    RenderHelpbox(text[i]);
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();
            return foldout;
        }

        public static void RenderHelpbox(string text)
        {
            if (text.Equals("Overview") || text.Equals("Tips"))
            {
                EditorGUILayout.LabelField(text, ApplyFontSizeToStyle(EditorStyles.boldLabel, JSAMSettings.Settings.QuickReferenceFontSize));
            }
            else
            {
                EditorGUILayout.LabelField(text, ApplyFontSizeToStyle(EditorStyles.helpBox, JSAMSettings.Settings.QuickReferenceFontSize));
            }
        }

        static Color guiColor;
        public static void BeginColourChange(Color color)
        {
            guiColor = GUI.color;
            GUI.color = color;
        }

        public static void EndColourChange() => GUI.color = guiColor;

        public static GUIStyle ApplyRichTextToStyle(GUIStyle referenceStyle)
        {
            var style = new GUIStyle(referenceStyle);
            style.richText = true;
            return style;
        }

        public static GUIStyle ApplyTextColorToStyle(GUIStyle referenceStyle, Color color)
        {
            var style = new GUIStyle(referenceStyle);
            style.normal.textColor = color;
            return style;
        }

        public static GUIStyle ApplyTextAnchorToStyle(GUIStyle referenceStyle, TextAnchor anchor)
        {
            var style = new GUIStyle(referenceStyle);
            style.alignment = anchor;
            return style;
        }

        public static GUIStyle ApplyFontSizeToStyle(GUIStyle referenceStyle, int fontSize)
        {
            var style = new GUIStyle(referenceStyle);
            style.fontSize = fontSize;
            return style;
        }

        public static GUIStyle ApplyBoldTextToStyle(GUIStyle referenceStyle)
        {
            var style = new GUIStyle(referenceStyle);
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    }
}