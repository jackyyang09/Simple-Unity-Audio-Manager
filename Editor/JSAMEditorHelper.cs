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
        public static SerializedProperty AddAndReturnNewArrayElement(this SerializedProperty prop)
        {
            int index = prop.arraySize;
            prop.InsertArrayElementAtIndex(index);
            return prop.GetArrayElementAtIndex(index);
        }

        /// <summary>
        /// Removes all null or missing elements from an array. 
        /// For missing elements, invoke this method in Update or OnGUI
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static SerializedProperty RemoveNullElementsFromArray(this SerializedProperty prop)
        {
            for (int i = prop.arraySize - 1; i > -1; i--)
            {
                if (prop.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    int size = prop.arraySize;
                    // A dirty hack, but Unity serialization is real messy
                    // https://answers.unity.com/questions/555724/serializedpropertydeletearrayelementatindex-leaves.html
                    prop.DeleteArrayElementAtIndex(i);
                    if (size == prop.arraySize) prop.DeleteArrayElementAtIndex(i);
                }
            }
            return prop;
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

        public static GUIContent GUIContent(this SerializedProperty property)
        {
            return new GUIContent(property.displayName, property.tooltip);
        }

        public static string TimeToString(this float time)
        {
            time *= 1000;
            int minutes = (int)time / 60000;
            int seconds = (int)time / 1000 - 60 * minutes;
            int milliseconds = (int)time - minutes * 60000 - 1000 * seconds;
            return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        }

        /// <summary>
        /// Returns true if this AudioFile houses a .WAV
        /// </summary>
        /// <returns></returns>
        public static bool IsWavFile(this AudioClip audioClip)
        {
            string filePath = AssetDatabase.GetAssetPath(audioClip);
            string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;
            string fileExtension = trueFilePath.Substring(trueFilePath.Length - 4);
            return fileExtension == ".wav";
        }

        public static GUIStyle ApplyRichText(this GUIStyle referenceStyle)
        {
            var style = new GUIStyle(referenceStyle);
            style.richText = true;
            return style;
        }

        public static GUIStyle SetTextColor(this GUIStyle referenceStyle, Color color)
        {
            var style = new GUIStyle(referenceStyle);
            style.normal.textColor = color;
            return style;
        }

        public static GUIStyle ApplyTextAnchor(this GUIStyle referenceStyle, TextAnchor anchor)
        {
            var style = new GUIStyle(referenceStyle);
            style.alignment = anchor;
            return style;
        }

        public static GUIStyle SetFontSize(this GUIStyle referenceStyle, int fontSize)
        {
            var style = new GUIStyle(referenceStyle);
            style.fontSize = fontSize;
            return style;
        }

        public static GUIStyle ApplyBoldText(this GUIStyle referenceStyle)
        {
            var style = new GUIStyle(referenceStyle);
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        public static GUIStyle ApplyWordWrap(this GUIStyle referenceStyle)
        {
            var style = new GUIStyle(referenceStyle);
            style.wordWrap = true;
            return style;
        }
    }

    public class JSAMEditorHelper
    {
        /// <summary>
        /// January 20th 2021, don't you ever forget you dingus
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="path"></param>
        /// <returns>False if the operation was unsuccessful or was cancelled, 
        /// True if an asset was created.</returns>
        public static bool CreateAssetSafe(Object asset, string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                Debug.LogError("Error! Attempted to write an asset over a folder!");
                return false;
            }
            string folderPath = path.Substring(0, path.LastIndexOf("/"));
            if (GenerateFolderStructureAt(folderPath))
            {
                AssetDatabase.CreateAsset(asset, path);
                return true;
            }
            return false;
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
                if (!AudioManager.Instance)
                {
                    g = new GameObject();
                    // Audio Events just so its safer
                    a = MonoScript.FromMonoBehaviour(g.AddComponent<AudioEvents>());
                    alt = true;
                }
                else a = MonoScript.FromMonoBehaviour(AudioManager.Instance);
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

        /// <summary>
        /// Generates the folder structure to a specified path if it doesn't already exist. 
        /// Will perform the check itself first
        /// </summary>
        /// <param name="folderPath">The FOLDER path, this should NOT include any file names</param>
        /// <param name="ask">Asks if you want to generate the folder structure</param>
        /// <returns>False if the user cancels the operation, 
        /// True if there was no need to generate anything or if the operation was successful</returns>
        public static bool GenerateFolderStructureAt(string folderPath, bool ask = true)
        {
            // Convert slashes so we can use the Equals operator together with other file-system operations
            folderPath = folderPath.Replace("/", "\\");
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string existingPath = "Assets";
                string unknownPath = folderPath.Remove(0, existingPath.Length + 1); 
                // Remove the "Assets/" at the start of the path name
                string folderName = (unknownPath.Contains("\\")) ? unknownPath.Substring(0, (unknownPath.IndexOf("\\"))) : unknownPath;
                do
                {
                    string newPath = Path.Combine(existingPath, folderName);
                    // Begin checking down the file path to see if it's valid
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        bool createFolder = true;
                        if (ask)
                        {
                            createFolder = EditorUtility.DisplayDialog("Path does not exist!", "The folder " +
                                "\"" +
                                newPath +
                                "\" does not exist! Would you like to create this folder?", "Yes", "No");
                        }

                        if (createFolder)
                        {
                            AssetDatabase.CreateFolder(existingPath, folderName);
                        }
                        else return false;
                    }
                    existingPath = newPath;
                    // Full path still doesn't exist
                    if (!existingPath.Equals(folderPath))
                    {
                        unknownPath = unknownPath.Remove(0, folderName.Length + 1);
                        folderName = (unknownPath.Contains("\\")) ? unknownPath.Substring(0, (unknownPath.IndexOf("\\"))) : unknownPath;
                    }
                }
                while (!AssetDatabase.IsValidFolder(folderPath));
            }
            return true;
        }

        public static string RenderSmartFolderProperty(GUIContent content, string folder, bool limitToAssetFolder = true)
        {
            string[] paths = new string[2];
            EditorGUILayout.BeginHorizontal();
            paths[0] = SmartFolderField(content, folder, limitToAssetFolder);
            paths[1] = SmartBrowseButton(folder, limitToAssetFolder);
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] != folder) return paths[i];
            }
            return folder;
        }

        public static string SmartFolderField(GUIContent content, string folder, bool limitToAssetsFolder = true)
        {
            string folderPath = folder;
            //if (folderPath == string.Empty) folderPath = Application.dataPath;
            bool touchedFolder = false;
            bool touchedString = false;

            Rect rect = EditorGUILayout.GetControlRect();
            if (DragAndDropRegion(rect, "", ""))
            {
                DefaultAsset da = DragAndDrop.objectReferences[0] as DefaultAsset;
                if (da) folder = AssetDatabase.GetAssetPath(da);
                return folder;
            }
            if (limitToAssetsFolder) rect.width *= 2f / 3f;
            EditorGUI.BeginChangeCheck();
            folderPath = EditorGUI.TextField(rect, content, folderPath);
            touchedString = EditorGUI.EndChangeCheck();

            rect.position += new Vector2(rect.width + 5, 0);
            rect.width = rect.width / 2f - 5;

            if (limitToAssetsFolder)
            {
                DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
                EditorGUI.BeginChangeCheck();
                folderAsset = (DefaultAsset)EditorGUI.ObjectField(rect, GUIContent.none, folderAsset, typeof(DefaultAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    touchedFolder = true;
                    folderPath = AssetDatabase.GetAssetPath(folderAsset);
                }
            }

            if (touchedString || touchedFolder)
            {
                // If the user presses "cancel"
                if (folderPath.Equals(string.Empty))
                {
                    return folder;
                }
                // or specifies something outside of this folder, reset filePath and don't proceed
                else if (limitToAssetsFolder)
                {
                    if (!folderPath.Contains("Assets"))
                    {
                        EditorUtility.DisplayDialog("Folder Browsing Error!", "Please choose a different folder inside the project's Assets folder.", "OK");
                        return folder;
                    }
                    else
                    {
                        // Fix path to be usable for AssetDatabase.FindAssets
                        if (folderPath[folderPath.Length - 1] == '/') folderPath = folderPath.Remove(folderPath.Length - 1, 1);
                    }
                }
            }
            return folderPath;
        }

        public static string SmartBrowseButton(string folder, bool limitToAssetFolder = true)
        {
            GUIContent buttonContent = new GUIContent("Browse", "Designate a New Folder");
            if (GUILayout.Button(buttonContent, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(55) }))
            {
                string filePath = folder;
                filePath = EditorUtility.OpenFolderPanel("Specify a New Folder", filePath, string.Empty);

                // If the user presses "cancel"
                if (filePath.Equals(string.Empty))
                {
                    return folder;
                }
                if (limitToAssetFolder)
                {
                    // or specifies something outside of this folder, reset filePath and don't proceed
                    if (!filePath.Contains("Assets"))
                    {
                        EditorUtility.DisplayDialog("Folder Browsing Error!", "AudioManager is a Unity editor tool and can only " +
                            "function inside the project's Assets folder. Please choose a different folder.", "OK");
                        return folder;
                    }
                    else if (filePath.Contains(Application.dataPath))
                    {
                        // Fix path to be usable for AssetDatabase.FindAssets
                        filePath = filePath.Remove(0, filePath.IndexOf("Assets"));
                    }
                }
                return filePath;
            }

            return folder;
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

        public static string OpenSmartSaveFileDialog<T>(out T asset, string defaultName = "New Object", string startingPath = "Assets") where T : ScriptableObject
        {
            string savePath = EditorUtility.SaveFilePanel("Designate save path", startingPath, defaultName, "asset");
            asset = null;
            if (savePath != "") // Make sure user didn't press "Cancel"
            {
                asset = ScriptableObject.CreateInstance<T>();
                savePath = savePath.Remove(0, savePath.IndexOf("Assets/"));
                CreateAssetSafe(asset, savePath);
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
            return savePath;
        }

        public static void SmartBrowseButton(SerializedProperty pathProp, string panelTitle = "Specify a New File", string extension = "", bool limitToAssetsFolder = true)
        {
            GUIContent buttonContent = new GUIContent(" Browse ", "Designate a New Folder");
            if (GUILayout.Button(buttonContent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                string filePath = pathProp.stringValue;
                filePath = EditorUtility.OpenFilePanel(panelTitle, filePath, extension);

                // If the user presses "cancel"
                if (filePath.Equals(string.Empty))
                {
                    return;
                }
                if (limitToAssetsFolder)
                {
                    // or specifies something outside of this folder, reset filePath and don't proceed
                    if (!filePath.Contains("Assets"))
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
                }

                pathProp.stringValue = filePath;
            }
        }

        public static void SmartBrowseButton(SerializedProperty folderProp)
        {
            GUIContent buttonContent = new GUIContent("Browse", "Designate a new folder");
            if (GUILayout.Button(buttonContent, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(55) }))
            {
                string filePath = folderProp.stringValue;
                if (!Directory.Exists(filePath))
                {
                    filePath = Application.dataPath;
                }
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
        }

        public static Vector2 lastGuideSize;
        public static void StartMeasureLastGuideSize() => lastGuideSize = Vector2.zero;
        
        public static bool CondensedButton(string label)
        {
            return GUILayout.Button(" " + label + " ", new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
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
                        GUIStyle style = EditorStyles.boldLabel.SetFontSize(JSAMSettings.Settings.QuickReferenceFontSize);
                        EditorGUILayout.LabelField(text[i], style);
                        lastGuideSize += style.CalcSize(new GUIContent(text[i]));
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
                GUIStyle style = EditorStyles.boldLabel.SetFontSize(JSAMSettings.Settings.QuickReferenceFontSize);
                EditorGUILayout.LabelField(text, style);
                lastGuideSize += style.CalcSize(new GUIContent(text));
            }
            else
            {
                GUIStyle style = EditorStyles.helpBox.SetFontSize(JSAMSettings.Settings.QuickReferenceFontSize);
                EditorGUILayout.LabelField(text, style);
                lastGuideSize += style.CalcSize(new GUIContent(text));
            }
        }

        public static bool RightClickRegion(Rect clickRect)
        {
            if (clickRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.ContextClick)
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        public static bool IsDragging(Rect dragRect) => dragRect.Contains(Event.current.mousePosition) && DragAndDrop.objectReferences.Length > 0;

        const int DAD_FONTSIZE = 40;
        const int DAD_BUFFER = 60;
        public static bool DragAndDropRegion(Rect dragRect, string normalLabel, string dragLabel, GUIStyle style = null)
        {
            switch (Event.current.type)
            {
                case EventType.Repaint:
                case EventType.Layout:
                    string label;

                    if (IsDragging(dragRect))
                    {
                        if (style == null) style = GUI.skin.box.SetFontSize(DAD_FONTSIZE).ApplyWordWrap();
                        label = dragLabel;
                    }
                    else
                    {
                        if (style == null) style = EditorStyles.label.SetFontSize(DAD_FONTSIZE).ApplyWordWrap();
                        label = normalLabel;
                    }

                    style = style
                        .ApplyTextAnchor(TextAnchor.MiddleCenter)
                        .SetFontSize((int)Mathf.Lerp(1f, (float)style.fontSize, dragRect.height / (float)(DAD_BUFFER)))
                        .ApplyBoldText();

                    GUI.Box(dragRect, label, style);

                    return false;
            }

            if (dragRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    Event.current.Use();
                    return true;
                }
            }
            return false;
        }

        static Color guiColor;
        public static void BeginColourChange(Color color)
        {
            guiColor = GUI.color;
            GUI.color = color;
        }

        public static void EndColourChange() => GUI.color = guiColor;

        static Color guiBackgroundColor;
        public static void BeginBackgroundColourChange(Color color)
        {
            guiBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
        }

        public static void EndBackgroundColourChange() => GUI.backgroundColor = guiBackgroundColor;
    }

    public class JSAMAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        /// <summary>
        /// <para>string filePath</para>
        /// Used to reinitialize SerializedObjects in case the asset was being used at time of deletion
        /// </summary>
        public static System.Action<string> OnJSAMAssetDeleted;

        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
        {
            OnJSAMAssetDeleted?.Invoke(path);
            return AssetDeleteResult.DidNotDelete;
        }
    }
}