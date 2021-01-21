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

        /// <summary>
        /// Helpful method by Stack Overflow user ata
        /// https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ConvertToAlphanumeric(string input)
        {
            char[] arr = input.ToCharArray();

            arr = System.Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c)
            || c == '_')));

            // If the first index is a number
            while (char.IsDigit(arr[0]))
            {
                List<char> newArray = new List<char>();
                newArray = new List<char>(arr);
                newArray.RemoveAt(0);
                arr = newArray.ToArray();
                if (arr.Length == 0) break; // No valid characters to use, returning empty
            }

            return new string(arr);
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
    }
}