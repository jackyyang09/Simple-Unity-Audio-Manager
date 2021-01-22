using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Callbacks;
using System;
using System.IO;

namespace JSAM.JSAMEditor
{
    public class AudioList
    {
        ReorderableList list;
        public ReorderableList List
        {
            get
            {
                return list;
            }
        }
        string category;

        public AudioList()
        {

        }

        public AudioList(SerializedObject obj, SerializedProperty prop, string category, bool isMusic)
        {
            list = new ReorderableList(obj, prop, true, false, false, false);
            list.drawElementCallback += DrawElement;
            list.headerHeight = 1;
            list.footerHeight = 0;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var file = element.objectReferenceValue as AudioFileSoundObject;


            Rect prevRect = new Rect(rect);
            Rect currentRect = new Rect(prevRect);

            currentRect.xMax = currentRect.xMin + 20;
            GUIContent blontent = new GUIContent("X", "Change the category of this Audio File object");
            EditorGUI.DropdownButton(currentRect, blontent, FocusType.Passive);

            using (new EditorGUI.DisabledScope(true))
            {
                prevRect = new Rect(currentRect);
                currentRect.xMin = prevRect.xMax + 5;
                currentRect.xMax = rect.xMax - 110;
                blontent = new GUIContent(file.SafeName);
                EditorGUI.PropertyField(currentRect, element, blontent);
            }

            prevRect = new Rect(currentRect);
            currentRect.xMin = prevRect.xMax + 4;
            currentRect.xMax = currentRect.xMax + 85;
            if (GUI.Button(currentRect, "Copy Enum"))
            {

            }

            JSAMEditorHelper.BeginColourChange(Color.red);
            prevRect = new Rect(currentRect);
            currentRect.xMin = prevRect.xMax + 1;
            currentRect.xMax = currentRect.xMax + 25;
            GUI.Button(currentRect, "X");
            JSAMEditorHelper.EndColourChange();
        }

        public void Draw()
        {
            list.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category Options: ", new GUILayoutOption[] { GUILayout.Width(105) });

            GUIContent blontent = new GUIContent("Rename");
            if (GUILayout.Button(blontent, new GUILayoutOption[]{ GUILayout.ExpandWidth(false) }))
            {

            }

            blontent = new GUIContent("Move Up");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {

            }

            blontent = new GUIContent("Move Down");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {

            }

            JSAMEditorHelper.BeginColourChange(Color.red);
            blontent = new GUIContent("Delete");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {

            }
            JSAMEditorHelper.EndColourChange();
            EditorGUILayout.EndHorizontal();
        }
    }

    public class AudioLibraryEditor : JSAMBaseEditorWindow<AudioLibrary, AudioLibraryEditor>
    {
        const string CATEGORY_NONE = "Uncategorized";

        Dictionary<string, SerializedProperty> categoryToSoundStructs = new Dictionary<string, SerializedProperty>();
        Dictionary<string, AudioList> reorderableLists = new Dictionary<string, AudioList>();

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            var newAsset = AssetDatabase.LoadAssetAtPath<AudioLibrary>(assetPath);
            if (newAsset)
            {
                Init();
                if (newAsset != asset)
                {
                    asset = newAsset;
                    window.DesignateSerializedProperties();
                }
                return true;
            }
            return false;
        }

        private void OnSelectionChange()
        {
            AudioLibrary newLibrary = Selection.activeObject as AudioLibrary;
            if (newLibrary)
            {
                asset = newLibrary;
                DesignateSerializedProperties();
            }
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/JSAM/Audio Library")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Window.Show();
            window.SetWindowTitle();
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Audio Library");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            Window.wantsMouseEnterLeaveWindow = true;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        SerializedProperty safeName;

        SerializedProperty sounds;
        SerializedProperty soundFoldout;
        SerializedProperty soundCategories;
        SerializedProperty soundCategoriesToList;

        protected override void DesignateSerializedProperties()
        {
            Debug.Log("Designating");
            serializedObject = new SerializedObject(asset);

            safeName = FindProp("safeName");

            sounds = FindProp(nameof(asset.sounds));
            soundCategories = FindProp(nameof(asset.soundCategories));
            soundFoldout = FindProp(nameof(asset.soundFoldout));
            soundCategoriesToList = FindProp("soundCategoriesToList");

            InitializeCategories();

            Window.Repaint();
        }

        void InitializeCategories()
        {
            // Add new Categories from Audio File arrays
            for (int i = 0; i < sounds.arraySize; i++)
            {
                var s = sounds.GetArrayElementAtIndex(i).objectReferenceValue as AudioFileSoundObject;
                if (!asset.soundCategories.Contains(s.category))
                {
                    var newCategory = soundCategories.AddNewArrayElement();
                    newCategory.stringValue = s.category;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Link existing Category-to-Structs to Categories
            categoryToSoundStructs = new Dictionary<string, SerializedProperty>();
            for (int i = 0; i < soundCategoriesToList.arraySize; i++)
            {
                var element = soundCategoriesToList.GetArrayElementAtIndex(i);
                string name = element.FindPropertyRelative("name").stringValue;
                categoryToSoundStructs[name] = element;
            }

            // Repopulate Library Category structs
            List<AudioFileSoundObject> soundsToSort = new List<AudioFileSoundObject>(asset.sounds);

            for (int i = 0; i < asset.soundCategories.Count/* && soundsToSort.Count > 0*/; i++)
            {
                string category = asset.soundCategories[i];
                // If new category encountered
                if (!categoryToSoundStructs.ContainsKey(category))
                {
                    var element = soundCategoriesToList.AddNewArrayElement();
                    element.FindPropertyRelative("name").stringValue = category;

                    var array = element.FindPropertyRelative("files");
                    array.ClearArray();
                    for (int j = soundsToSort.Count - 1; j > -1; j--)
                    {
                        if (soundsToSort[j].category.Equals(category))
                        {
                            array.AddNewArrayElement().objectReferenceValue = soundsToSort[j];
                            soundsToSort.RemoveAt(j);
                        }
                    }
                    // Save new struct to Dictionary
                    categoryToSoundStructs[category] = element;
                }
                else
                {
                    var element = soundCategoriesToList.GetArrayElementAtIndex(i);

                    var array = element.FindPropertyRelative("files");
                    var newFiles = new List<AudioFileSoundObject>();
                    // Copy over sorted files
                    for (int j = 0; j < array.arraySize; j++)
                    {
                        var subElement = array.GetArrayElementAtIndex(j).objectReferenceValue as AudioFileSoundObject;
                        if (subElement.category.Equals(category))
                        {
                            newFiles.Add(subElement);
                            // File has been sorted
                            soundsToSort.Remove(subElement);
                        }
                    }
                    // Add new files
                    for (int j = soundsToSort.Count - 1; j > -1; j--)
                    {
                        if (soundsToSort[j].category.Equals(category))
                        {
                            newFiles.Add(soundsToSort[j]);
                            // File has been sorted
                            soundsToSort.RemoveAt(j);
                        }   
                    }

                    // Apply changes
                    array.ClearArray();
                    for (int j = 0; j < newFiles.Count; j++)
                    {
                        array.AddNewArrayElement().objectReferenceValue = newFiles[j];
                    }
                }
                serializedObject.ApplyModifiedProperties();
            }

            // Initialize reorderable sound lists
            reorderableLists = new Dictionary<string, AudioList>();
            var keys = new string[categoryToSoundStructs.Keys.Count];
            categoryToSoundStructs.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                var files = categoryToSoundStructs[keys[i]].FindPropertyRelative("files");
                var category = categoryToSoundStructs[keys[i]].FindPropertyRelative("name").stringValue;
                AudioList newList = new AudioList(serializedObject, files, category, false);
                reorderableLists[category] = newList;
            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnGUI()
        {
            if (serializedObject == null)
            {

            }
            else
            {
                serializedObject.Update();

                HandleMouseEnterLeave();

                HandleDragAndDrop();

                GUIContent blontent = new GUIContent();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string arrow = (soundFoldout.boolValue) ? "▼" : "▶";
                blontent = new GUIContent(arrow + " Sound Library");
                EditorGUILayout.BeginHorizontal();
                soundFoldout.boolValue = EditorGUILayout.Foldout(soundFoldout.boolValue, blontent, true, EditorStyles.boldLabel);
                blontent = new GUIContent("Add New Category", "Add a new sound category");
                if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                {
                    
                }
                EditorGUILayout.EndHorizontal();

                if (soundFoldout.boolValue)
                {
                    for (int i = 0; i < asset.soundCategories.Count; i++)
                    {
                        string category = asset.soundCategories[i];
                        string categoryName = category;
                        if (category.Equals(string.Empty)) categoryName = CATEGORY_NONE;
                        bool foldout = categoryToSoundStructs[category].FindPropertyRelative("foldout").boolValue;
                        EditorGUI.BeginChangeCheck();
                        foldout = EditorCompatability.SpecialFoldouts(foldout, categoryName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            categoryToSoundStructs[category].FindPropertyRelative("foldout").boolValue = foldout;
                        }
                        if (foldout)
                        {
                            reorderableLists[category].Draw();
                        }
                        EditorCompatability.EndSpecialFoldoutGroup();
                    }
                }

                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Re-Generate Audio Library"))
                {
                    GenerateEnumFile(JSAMSettings.Settings.GeneratedEnumsPath);
                }

                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        void ApplyChanges()
        {
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                InitializeCategories();
                window.Repaint();
            }
        }

        void OnUndoRedoPerformed()
        {
            if (serializedObject != null)
            {
                window.Repaint();
            }
        }

        void HandleMouseEnterLeave()
        {
            if (Event.current.type == EventType.MouseEnterWindow)
            {
                InitializeCategories();
                window.Repaint();
            }
            else if (Event.current.type == EventType.MouseLeaveWindow)
            {
            }
        }

        /// <summary>
        /// https://answers.unity.com/questions/1548292/gui-editor-drag-and-drop-inspector.html
        /// </summary>
        void HandleDragAndDrop()
        {
            Rect dragRect = Window.position;
            dragRect.x = 0;
            dragRect.y = 0;

            if (dragRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    List<AudioFileSoundObject> duplicates = new List<AudioFileSoundObject>();

                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        string filePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[i]);
                        var simport = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<AudioFileSoundObject>(filePath);

                        for (int j = 0; j < simport.Count; j++)
                        {
                            if (asset.sounds.Contains(simport[j]))
                            {
                                duplicates.Add(simport[j]);
                                continue;
                            }

                            sounds.AddNewArrayElement().objectReferenceValue = simport[j];
                        }
                    }

                    Event.current.Use();

                    if (duplicates.Count > 0)
                    {
                        string multiLine = string.Empty;
                        for (int i = 0; i < duplicates.Count; i++)
                        {
                            multiLine = duplicates[i].name + "\n";
                        }
                        EditorUtility.DisplayDialog("Duplicate Audio Files!",
                            "The following Audio File Objects are already present in the Audio Library! They have been skipped.\n" + multiLine,
                            "OK");
                    }

                    ApplyChanges();
                }
            }
        }

        void AddNewSoundCategory(string category)
        {
            if (asset.soundCategories.Contains(category))
            {
                EditorUtility.DisplayDialog("Duplicate Category!",
                    "A category with this name already exists in the sound library!", "OK");
                GUIUtility.ExitGUI();
                return;
            }

            soundCategories.AddNewArrayElement().stringValue = category;

            var element = soundCategoriesToList.AddNewArrayElement();
            element.FindPropertyRelative("name").stringValue = category;
            element.FindPropertyRelative("foldout").boolValue = true;

            ApplyChanges();
        }

        /// <summary>
        /// With help from Daniel Robledo
        /// https://support.unity3d.com/hc/en-us/articles/115000341143-How-do-I-read-and-write-data-from-a-text-file-
        /// </summary>
        public bool GenerateEnumFile(string filePath)
        {
            // Evaluate potential enum names before writing to file
            List<AudioFileSoundObject> soundLibrary = asset.sounds;

            List<string> soundNames = new List<string>();
            for (int i = 0; i < soundLibrary.Count; i++)
            {
                string newName = soundLibrary[i].SafeName;

                if (!soundNames.Contains(newName))
                {
                    soundNames.Add(newName);
                }
                else
                {
                    string problemString = soundLibrary[soundNames.IndexOf(newName)].name;
                    EditorUtility.DisplayDialog("Audio Library Generation Error!", "\"" + soundLibrary[i].name + "\" shares the same name with \"" + problemString + "\"! " +
                        "When converting to Audio Enums, AudioManager strips away non Alphanumeric characters to ensure proper C# syntax and " +
                        "in the process they've ended with the same name! Please make \"" + soundLibrary[i].name + "\" more different and try again!", "OK");
                    return false;
                }
            }

            List<AudioFileMusicObject> musicLibrary = asset.music;

            List<string> musicNames = new List<string>();
            for (int i = 0; i < musicLibrary.Count; i++)
            {
                string newName = musicLibrary[i].SafeName;

                if (!musicNames.Contains(newName))
                {
                    musicNames.Add(newName);
                }
                else
                {
                    string problemString = musicLibrary[musicNames.IndexOf(newName)].name;
                    EditorUtility.DisplayDialog("Audio Library Generation Error!", "\"" + musicLibrary[i].name + "\" shares the same name with \"" + problemString + "\"! " +
                        "When converting to Audio Enums, AudioManager strips away non Alphanumeric characters to ensure proper C# syntax and " +
                        "in the process they've ended with the same name! Please make \"" + musicLibrary[i].name + "\" more different and try again!", "OK");
                    return false;
                }
            }

            string safeLibraryName = asset.name.ConvertToAlphanumeric();

            string fileName = "\\AudioEnums - " + safeLibraryName + ".cs";
            string prevName = "\\AudioEnums - " + asset.safeName + ".cs";

            bool overwriting = false;
            // Looking for previous AudioEnums
            string[] GUIDs = AssetDatabase.FindAssets("AudioEnums", new[] { filePath });
            for (int i = 0; i < GUIDs.Length; i++)
            {
                var p = GUIDs[i];
                // Make the detected file match the format of expected filenames up above
                string assetPath = AssetDatabase.GUIDToAssetPath(p);
                string assetName = "\\" + assetPath.Remove(0, assetPath.LastIndexOf('/') + 1);
                if (assetName.Equals(fileName))
                {
                    overwriting = true;
                    continue; // We're overwriting this anyway
                }
                else if (assetName.Equals(prevName))
                if (EditorUtility.DisplayDialog("Old AudioEnums file Found!", "An AudioEnums file whose name matches this library's " +
                    "previous name has been found. It is likely a remnant left behind after this library was renamed. " +
                    "Would you like to delete this Audio Enums file?", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            // Check for existing Enums of the same name
            if (!overwriting)
            {
                if (AudioLibrary.GetEnumType("JSAM.Sounds" + safeLibraryName) != null)
                {
                    if (EditorUtility.DisplayDialog("Duplicate Enum Type Found!", "An existing script in the project already " +
                        "contains an Enum called \"JSAM.Sounds" + safeLibraryName + "\"! AudioManager cannot regenerate the library " +
                        "until the scene name is changed to something different or the existing enum name is modified!", "OK"))
                    {
                        return false;
                    }
                }
                else if (AudioLibrary.GetEnumType("JSAM.Music" + safeLibraryName) != null)
                {
                    if (EditorUtility.DisplayDialog("Duplicate Enum Type Found!", "An existing script in the project already " +
                        "contains an Enum called \"JSAM.Music" + safeLibraryName + "\"! AudioManager cannot regenerate the library " +
                        "until the scene name is changed to something different or the existing enum name is modified!", "OK"))
                    {
                        return false;
                    }
                }
            }

            // May want to use filePath = "//" + fileName instead?
            filePath += fileName;
            File.WriteAllText(filePath, string.Empty);
            StreamWriter writer = new StreamWriter(filePath, true);
            writer.WriteLine("namespace JSAM {");

            writer.WriteLine("    public enum " + safeLibraryName + "Sounds" + " {");

            if (soundLibrary != null)
            {
                if (soundLibrary.Count > 0)
                {
                    for (int i = 0; i < soundLibrary.Count - 1; i++)
                    {
                        writer.WriteLine("        " + soundLibrary[i].SafeName + ",");
                    }
                    writer.WriteLine("        " + soundLibrary[soundLibrary.Count - 1].SafeName);
                }
            }

            writer.WriteLine("    }");

            writer.WriteLine("    public enum " + safeLibraryName + "Music" + " {");

            if (musicLibrary != null)
            {
                if (musicLibrary.Count > 0)
                {
                    for (int i = 0; i < musicLibrary.Count - 1; i++)
                    {
                        writer.WriteLine("        " + musicLibrary[i].SafeName + ",");
                    }
                    writer.WriteLine("        " + musicLibrary[musicLibrary.Count - 1].SafeName);
                }
            }

            writer.WriteLine("    }");
            writer.WriteLine("}");
            writer.Close();

            //Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(filePath);
            AssetDatabase.Refresh();

            asset.soundEnumName = "JSAM." + safeLibraryName + "Sounds";
            asset.musicEnumName = "JSAM." + safeLibraryName + "Music";
            safeName.stringValue = safeLibraryName;
            serializedObject.ApplyModifiedProperties();

            return true;
        }
    }
}