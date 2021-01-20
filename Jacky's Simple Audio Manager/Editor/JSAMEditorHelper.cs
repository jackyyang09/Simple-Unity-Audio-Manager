using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
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
    }
}