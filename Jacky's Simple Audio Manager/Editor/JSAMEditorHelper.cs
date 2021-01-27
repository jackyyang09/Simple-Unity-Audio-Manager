using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

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

        public static string GetAudioManagerPath()
        {
            AudioManager a = null;
            GameObject g = null;
            if (!AudioManager.instance)
            {
                g = new GameObject();
                a = g.AddComponent<AudioManager>();
            }
            else a = AudioManager.instance;
            string path = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(a));
            if (!AudioManager.instance)
            {
                Object.DestroyImmediate(g);
            }
            return path;
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

        static Color guiColor;
        public static void BeginColourChange(Color color)
        {
            guiColor = GUI.color;
            GUI.color = color;
        }

        public static void EndColourChange() => GUI.color = guiColor;

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

        public static GUIStyle ApplyBoldTextToStyle(GUIStyle referenceStyle)
        {
            var style = new GUIStyle(referenceStyle);
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    }
}