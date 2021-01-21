using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Callbacks;
using System;

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
            list.footerHeight = 0;
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element);
        }

        public void Draw()
        {
            list.DoLayoutList();
            EditorGUILayout.LabelField("Category Options: ", new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
        }
    }

    public class AudioLibraryEditor : JSAMBaseEditorWindow<AudioLibraryEditor>
    {
        public static AudioLibrary asset;

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
        }

        private void OnDisable()
        {
        }

        SerializedProperty sounds;
        SerializedProperty soundFoldout;
        SerializedProperty soundCategories;
        SerializedProperty soundCategoriesToList;

        protected override void DesignateSerializedProperties()
        {
            Debug.Log("Designating");
            serializedObject = new SerializedObject(asset);

            sounds = FindProp(nameof(asset.sounds));
            soundCategories = FindProp(nameof(asset.soundCategories));
            soundFoldout = FindProp(nameof(asset.soundFoldout));
            soundCategoriesToList = FindProp("soundCategoriesToList");

            InitializeCategories();

            window.Repaint();
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

            for (int i = 0; i < asset.soundCategories.Count; i++)
            {
                string category = asset.soundCategories[i];

                // If new category encountered
                if (!categoryToSoundStructs.ContainsKey(category))
                {
                    var element = soundCategoriesToList.AddNewArrayElement();
                    element.FindPropertyRelative("name").stringValue = category;
                
                    var array = element.FindPropertyRelative("files");
                    for (int j = soundsToSort.Count - 1; i > 0; i--)
                    {
                        if (soundsToSort[i].category.Equals(category))
                        {
                            array.AddNewArrayElement().objectReferenceValue = soundsToSort[i];
                            soundsToSort.RemoveAt(i);
                        }
                    }
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
                        if (soundsToSort[i].category.Equals(category))
                        {
                            newFiles.Add(soundsToSort[i]);
                            // File has been sorted
                            soundsToSort.RemoveAt(i);
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
                Debug.Log(category);
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
                        EditorCompatability.SpecialFoldouts(true, category);
                        reorderableLists[category].Draw();
                        EditorCompatability.EndSpecialFoldoutGroup();
                    }
                }

                EditorGUILayout.EndVertical();

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
                            }

                            sounds.AddNewArrayElement().objectReferenceValue = simport[i];
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

            int index = soundCategoriesToList.arraySize;
            soundCategories.InsertArrayElementAtIndex(index);
            soundCategories.GetArrayElementAtIndex(index).stringValue = category;

            soundCategoriesToList.InsertArrayElementAtIndex(index);
            var element = soundCategoriesToList.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("name").stringValue = category;
            element.FindPropertyRelative("foldout").boolValue = true;

            ApplyChanges();
        }
    }
}