using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Callbacks;
using System;
using System.IO;
using System.Linq;

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
        bool isMusic;

        public AudioList()
        {

        }

        public AudioList(SerializedObject obj, SerializedProperty prop, string _category, bool _isMusic)
        {
            list = new ReorderableList(obj, prop, true, false, false, false);
            isMusic = _isMusic;
            if (isMusic)
            {
                list.drawElementCallback += DrawMusicElement;
            }
            else
            {
                list.drawElementCallback += DrawSoundElement;
            }
            list.headerHeight = 1;
            list.footerHeight = 0;
            category = _category;
        }

        private void DrawMusicElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var file = element.objectReferenceValue as BaseAudioFileObject;

            Rect prevRect = new Rect(rect);
            Rect currentRect = new Rect(prevRect);

            currentRect.xMax = currentRect.xMin + 20;
            GUIContent blontent = new GUIContent("T", "Change the category of this Audio File object");
            if (EditorGUI.DropdownButton(currentRect, blontent, FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent(AudioLibraryEditor.CATEGORY_NONE), category.Equals(string.Empty),
                    SetAudioFileCategory, new Tuple<BaseAudioFileObject, string>(file, string.Empty));
                menu.AddSeparator(string.Empty);
                var categories = AudioLibraryEditor.Asset.musicCategories;
                for (int i = 0; i < categories.Count; i++)
                {
                    if (categories[i].Equals(string.Empty)) continue;
                    menu.AddItem(new GUIContent(categories[i]), category.Equals(categories[i]),
                        SetAudioFileCategory, new Tuple<BaseAudioFileObject, string>(file, categories[i]));
                }
                menu.ShowAsContext();
            }

            string enumName = string.Empty;
            bool registered = AudioLibraryEditor.Window.GetMusicEnumName(file, out enumName);
            using (new EditorGUI.DisabledScope(true))
            {
                prevRect = new Rect(currentRect);
                currentRect.xMin = prevRect.xMax + 5;
                currentRect.xMax = rect.xMax - 110;
                if (registered)
                {
                    blontent = new GUIContent(file.SafeName, enumName);
                }
                else
                {
                    blontent = new GUIContent(file.SafeName + "*", "Re-generate your Audio Library to get this Audio File's enum name");
                }
            }
            // Force a normal-colored label in a disabled scope
            JSAMEditorHelper.BeginColourChange(Color.clear);
            Rect decoyRect = EditorGUI.PrefixLabel(currentRect, blontent);
            JSAMEditorHelper.EndColourChange();
            if (!registered) JSAMEditorHelper.BeginColourChange(new Color(0.75f, 0.75f, 0.75f));
            EditorGUI.LabelField(currentRect, blontent);
            JSAMEditorHelper.EndColourChange();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(decoyRect, element, GUIContent.none);
            }

            using (new EditorGUI.DisabledScope(!registered))
            {
                prevRect = new Rect(currentRect);
                currentRect.xMin = prevRect.xMax + 2;
                currentRect.xMax = currentRect.xMax + 85;
                if (registered)
                {
                    blontent = new GUIContent("Copy Enum", "Click to copy this Audio File's enum name to your clipboard");
                }
                else
                {
                    blontent = new GUIContent("Copy Enum", "Re-generate your Audio Library to copy this Audio File's enum name");
                }
                if (GUI.Button(currentRect, blontent))
                {
                    JSAMEditorHelper.CopyToClipboard(enumName);
                }
            }

            JSAMEditorHelper.BeginColourChange(Color.red);
            prevRect = new Rect(currentRect);
            currentRect.xMin = prevRect.xMax + 2;
            currentRect.xMax = currentRect.xMax + 25;
            blontent = new GUIContent("X", "Remove this Audio File from the library, can be undone with Edit -> Undo");
            if (GUI.Button(currentRect, blontent))
            {
                AudioLibraryEditor.Window.RemoveAudioFile(file, isMusic);
            }
            JSAMEditorHelper.EndColourChange();
        }

        private void DrawSoundElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            var file = element.objectReferenceValue as BaseAudioFileObject;

            Rect prevRect = new Rect(rect);
            Rect currentRect = new Rect(prevRect);

            currentRect.xMax = currentRect.xMin + 20;
            GUIContent blontent = new GUIContent("T", "Change the category of this Audio File object");
            if (EditorGUI.DropdownButton(currentRect, blontent, FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent(AudioLibraryEditor.CATEGORY_NONE), category.Equals(string.Empty),
                    SetAudioFileCategory, new Tuple<BaseAudioFileObject, string>(file, string.Empty));
                var categories = AudioLibraryEditor.Asset.soundCategories;
                menu.AddSeparator(string.Empty);
                for (int i = 0; i < categories.Count; i++)
                {
                    if (categories[i].Equals(string.Empty)) continue;
                    menu.AddItem(new GUIContent(categories[i]), category.Equals(categories[i]),
                        SetAudioFileCategory, new Tuple<BaseAudioFileObject, string>(file, categories[i]));
                }
                menu.ShowAsContext();
            }

            string enumName = string.Empty;
            bool registered = AudioLibraryEditor.Window.GetSoundEnumName(file, out enumName);
            using (new EditorGUI.DisabledScope(true))
            {
                prevRect = new Rect(currentRect);
                currentRect.xMin = prevRect.xMax + 5;
                currentRect.xMax = rect.xMax - 110;
                if (registered)
                {
                    blontent = new GUIContent(file.SafeName, enumName);
                }
                else
                {
                    blontent = new GUIContent(file.SafeName + "*", "Re-generate your Audio Library to get this Audio File's enum name");
                }
            }
            // Force a normal-colored label in a disabled scope
            JSAMEditorHelper.BeginColourChange(Color.clear);
            Rect decoyRect = EditorGUI.PrefixLabel(currentRect, blontent);
            JSAMEditorHelper.EndColourChange();
            if (!registered) JSAMEditorHelper.BeginColourChange(new Color(0.75f, 0.75f, 0.75f));
            EditorGUI.LabelField(currentRect, blontent);
            JSAMEditorHelper.EndColourChange();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(decoyRect, element, GUIContent.none);
            }

            using (new EditorGUI.DisabledScope(!registered))
            {
                prevRect = new Rect(currentRect);
                currentRect.xMin = prevRect.xMax + 2;
                currentRect.xMax = currentRect.xMax + 85;
                if (registered)
                {
                    blontent = new GUIContent("Copy Enum", "Click to copy this Audio File's enum name to your clipboard");
                }
                else
                {
                    blontent = new GUIContent("Copy Enum", "Re-generate your Audio Library to copy this Audio File's enum name");
                }
                if (GUI.Button(currentRect, blontent))
                {
                    JSAMEditorHelper.CopyToClipboard(enumName);
                }
            }

            JSAMEditorHelper.BeginColourChange(Color.red);
            prevRect = new Rect(currentRect);
            currentRect.xMin = prevRect.xMax + 2;
            currentRect.xMax = currentRect.xMax + 25;
            blontent = new GUIContent("X", "Remove this Audio File from the library, can be undone with Edit -> Undo");
            if (GUI.Button(currentRect, blontent))
            {
                AudioLibraryEditor.Window.RemoveAudioFile(file, isMusic);
            }
            JSAMEditorHelper.EndColourChange();
        }

        public void Draw()
        {
            list.DoLayoutList();

            Rect rect = EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Category Options: ", new GUILayoutOption[] { GUILayout.Width(105) });

            GUIContent blontent = new GUIContent("Rename", "Change the name of this category, also changes the category field for all Audio File objects that share this category");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                var utility = JSAMUtilityWindow.Init("Enter New Category Name", true, true);
                utility.AddField(new GUIContent("New Category Name"), category);
                JSAMUtilityWindow.onSubmitField += RenameCategory;
            }

            using (new EditorGUI.DisabledScope(AudioLibraryEditor.Window.CanCategoryMove(category, true, isMusic)))
            {
                blontent = new GUIContent("Move Up", "Swap the order of this category with the one above it");
                if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                {
                    AudioLibraryEditor.Window.MoveCategory(category, true, isMusic);
                }
            }

            using (new EditorGUI.DisabledScope(AudioLibraryEditor.Window.CanCategoryMove(category, false, isMusic)))
            {
                blontent = new GUIContent("Move Down", "Swap the order of this category with the one below it");
                if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                {
                    AudioLibraryEditor.Window.MoveCategory(category, false, isMusic);
                }
            }

            JSAMEditorHelper.BeginColourChange(Color.red);
            blontent = new GUIContent("Delete", "Remove this category and any Audio Files inside it from the library");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                AudioLibraryEditor.Window.DeleteCategory(category, isMusic);
            }
            JSAMEditorHelper.EndColourChange();

            rect.xMin = rect.xMax - 100;
            GUI.Label(rect, "File Count: " + list.serializedProperty.arraySize,
                JSAMEditorHelper.ApplyTextAnchorToStyle(EditorStyles.label, TextAnchor.MiddleRight));

            EditorGUILayout.EndHorizontal();
        }

        public void RenameCategory(string[] input)
        {
            AudioLibraryEditor.Window.RenameCategory(category, input[0], isMusic);
        }

        public void SetAudioFileCategory(object input)
        {
            Tuple<BaseAudioFileObject, string> tuple = (Tuple<BaseAudioFileObject, string>)input;
            AudioLibraryEditor.Window.ChangeAudioFileCategory(tuple.Item1, tuple.Item2);
        }
    }

    public class AudioLibraryEditor : JSAMSerializedEditorWindow<AudioLibrary, AudioLibraryEditor>
    {
        public const string CATEGORY_NONE = "Uncategorized";

        List<string> newSoundNames = new List<string>();
        List<string> missingSoundNames = new List<string>();
        List<AudioFileSoundObject> registeredSounds = new List<AudioFileSoundObject>();
        Dictionary<string, SerializedProperty> categoryToSoundStructs = new Dictionary<string, SerializedProperty>();
        Dictionary<string, AudioList> reorderableSoundLists = new Dictionary<string, AudioList>();

        List<string> newMusicNames = new List<string>();
        List<string> missingMusicNames = new List<string>();
        List<AudioFileMusicObject> registeredMusic = new List<AudioFileMusicObject>();
        Dictionary<string, SerializedProperty> categoryToMusicStructs = new Dictionary<string, SerializedProperty>();
        Dictionary<string, AudioList> reorderableMusicLists = new Dictionary<string, AudioList>();

        static Vector2 scrollProgress;

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
        SerializedProperty soundEnumName;
        SerializedProperty musicEnumName;

        SerializedProperty showMusic;

        SerializedProperty sounds;
        SerializedProperty soundCategories;
        SerializedProperty soundCategoriesToList;

        SerializedProperty music;
        SerializedProperty musicCategories;
        SerializedProperty musicCategoriesToList;

        protected override void DesignateSerializedProperties()
        {
            Debug.Log("Designating");
            serializedObject = new SerializedObject(asset);

            safeName = FindProp("safeName");
            showMusic = FindProp(nameof(asset.showMusic));

            soundEnumName = FindProp(nameof(asset.soundEnumName));

            sounds = FindProp(nameof(asset.sounds));
            soundCategories = FindProp(nameof(asset.soundCategories));
            soundCategoriesToList = FindProp("soundCategoriesToList");

            musicEnumName = FindProp(nameof(asset.musicEnumName));

            music = FindProp(nameof(asset.music));
            musicCategories = FindProp(nameof(asset.musicCategories));
            musicCategoriesToList = FindProp("musicCategoriesToList");

            InitializeCategories();
            DocumentAudioFiles();

            Window.Repaint();
        }

        void DocumentAudioFiles()
        {
            if (!soundEnumName.stringValue.IsNullEmptyOrWhiteSpace())
            {
                var type = AudioLibrary.GetEnumType(soundEnumName.stringValue);
                List<string> registeredNames = new List<string>(Enum.GetNames(type));
                List<string> libraryNames = new List<string>();
                for (int i = 0; i < asset.sounds.Count; i++)
                {
                    libraryNames.Add(asset.sounds[i].SafeName);
                }
                // Get all sound names that are registered but not in the library
                missingSoundNames = new List<string>(registeredNames.Except(libraryNames));
                // Get all sound names that are in the library but aren't registered
                newSoundNames = new List<string>(libraryNames.Except(registeredNames));
            }
            if (missingSoundNames == null) missingSoundNames = new List<string>();
            if (newSoundNames == null) newSoundNames = new List<string>();

            if (!musicEnumName.stringValue.IsNullEmptyOrWhiteSpace())
            {
                var type = AudioLibrary.GetEnumType(musicEnumName.stringValue);
                List<string> registeredNames = new List<string>(Enum.GetNames(type));
                List<string> libraryNames = new List<string>();
                for (int i = 0; i < asset.music.Count; i++)
                {
                    libraryNames.Add(asset.music[i].SafeName);
                }
                // Get all sound names that are registered but not in the library
                missingMusicNames = new List<string>(registeredNames.Except(libraryNames));
                // Get all sound names that are in the library but aren't registered
                newMusicNames = new List<string>(libraryNames.Except(registeredNames));
            }
            if (missingMusicNames == null) missingMusicNames = new List<string>();
            if (newMusicNames == null) newMusicNames = new List<string>();
        }

        void InitializeCategories()
        {
            // Add new Categories from Audio File arrays
            {
                // Sounds
                for (int i = 0; i < sounds.arraySize; i++)
                {
                    var s = sounds.GetArrayElementAtIndex(i).objectReferenceValue as BaseAudioFileObject;
                    if (!asset.soundCategories.Contains(s.category))
                    {
                        var newCategory = soundCategories.AddNewArrayElement();
                        newCategory.stringValue = s.category;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                // Music
                for (int i = 0; i < music.arraySize; i++)
                {
                    var m = music.GetArrayElementAtIndex(i).objectReferenceValue as BaseAudioFileObject;
                    if (!asset.musicCategories.Contains(m.category))
                    {
                        var newCategory = musicCategories.AddNewArrayElement();
                        newCategory.stringValue = m.category;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            // Link existing Category-to-Structs to Categories
            {
                // Sound
                categoryToSoundStructs = new Dictionary<string, SerializedProperty>();
                for (int i = 0; i < soundCategoriesToList.arraySize; i++)
                {
                    var element = soundCategoriesToList.GetArrayElementAtIndex(i);
                    string name = element.FindPropertyRelative("name").stringValue;
                    categoryToSoundStructs[name] = element;
                }

                // Music
                categoryToMusicStructs = new Dictionary<string, SerializedProperty>();
                for (int i = 0; i < musicCategoriesToList.arraySize; i++)
                {
                    var element = musicCategoriesToList.GetArrayElementAtIndex(i);
                    string name = element.FindPropertyRelative("name").stringValue;
                    categoryToMusicStructs[name] = element;
                }
            }

            List<BaseAudioFileObject> audioToSort = null;
            // Repopulate Library Category structs
            {
                // Sounds
                audioToSort = new List<BaseAudioFileObject>(asset.sounds);
                for (int i = 0; i < asset.soundCategories.Count/* && audioToSort.Count > 0*/; i++)
                {
                    string category = asset.soundCategories[i];
                    // If new category encountered
                    if (!categoryToSoundStructs.ContainsKey(category))
                    {
                        var element = soundCategoriesToList.AddNewArrayElement();
                        element.FindPropertyRelative("name").stringValue = category;
                        element.FindPropertyRelative("foldout").boolValue = true;

                        var array = element.FindPropertyRelative("files");
                        array.ClearArray();
                        for (int j = audioToSort.Count - 1; j > -1; j--)
                        {
                            if (audioToSort[j].category.Equals(category))
                            {
                                array.AddNewArrayElement().objectReferenceValue = audioToSort[j];
                                audioToSort.RemoveAt(j);
                            }
                        }
                        // Save new struct to Dictionary
                        categoryToSoundStructs[category] = element;
                    }
                    else
                    {
                        var element = soundCategoriesToList.GetArrayElementAtIndex(i);

                        var array = element.FindPropertyRelative("files");
                        var newFiles = new List<BaseAudioFileObject>();
                        // Copy over sorted files
                        for (int j = 0; j < array.arraySize; j++)
                        {
                            var subElement = array.GetArrayElementAtIndex(j).objectReferenceValue as AudioFileSoundObject;
                            if (subElement.category.Equals(category))
                            {
                                newFiles.Add(subElement);
                                // File has been sorted
                                audioToSort.Remove(subElement);
                            }
                        }
                        // Add new files
                        for (int j = audioToSort.Count - 1; j > -1; j--)
                        {
                            if (audioToSort[j].category.Equals(category))
                            {
                                newFiles.Add(audioToSort[j]);
                                // File has been sorted
                                audioToSort.RemoveAt(j);
                            }
                        }

                        // Apply changes
                        array.ClearArray();
                        for (int j = 0; j < newFiles.Count; j++)
                        {
                            array.AddNewArrayElement().objectReferenceValue = newFiles[j];
                        }
                    }
                }

                // Music
                audioToSort = new List<BaseAudioFileObject>(asset.music);
                for (int i = 0; i < asset.musicCategories.Count/* && audioToSort.Count > 0*/; i++)
                {
                    string category = asset.musicCategories[i];
                    // If new category encountered
                    if (!categoryToMusicStructs.ContainsKey(category))
                    {
                        var element = musicCategoriesToList.AddNewArrayElement();
                        element.FindPropertyRelative("name").stringValue = category;
                        element.FindPropertyRelative("foldout").boolValue = true;

                        var array = element.FindPropertyRelative("files");
                        array.ClearArray();
                        for (int j = audioToSort.Count - 1; j > -1; j--)
                        {
                            if (audioToSort[j].category.Equals(category))
                            {
                                array.AddNewArrayElement().objectReferenceValue = audioToSort[j];
                                audioToSort.RemoveAt(j);
                            }
                        }
                        // Save new struct to Dictionary
                        categoryToMusicStructs[category] = element;
                    }
                    else
                    {
                        var element = musicCategoriesToList.GetArrayElementAtIndex(i);

                        var array = element.FindPropertyRelative("files");
                        var newFiles = new List<BaseAudioFileObject>();
                        // Copy over sorted files
                        for (int j = 0; j < array.arraySize; j++)
                        {
                            var subElement = array.GetArrayElementAtIndex(j).objectReferenceValue as BaseAudioFileObject;
                            if (subElement.category.Equals(category))
                            {
                                newFiles.Add(subElement);
                                // File has been sorted
                                audioToSort.Remove(subElement);
                            }
                        }
                        // Add new files
                        for (int j = audioToSort.Count - 1; j > -1; j--)
                        {
                            if (audioToSort[j].category.Equals(category))
                            {
                                newFiles.Add(audioToSort[j]);
                                // File has been sorted
                                audioToSort.RemoveAt(j);
                            }
                        }

                        // Apply changes
                        array.ClearArray();
                        for (int j = 0; j < newFiles.Count; j++)
                        {
                            array.AddNewArrayElement().objectReferenceValue = newFiles[j];
                        }
                    }
                }

                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Initialize reorderable sound lists
            {
                reorderableSoundLists = new Dictionary<string, AudioList>();
                reorderableMusicLists = new Dictionary<string, AudioList>();

                // Sounds
                var keys = new string[categoryToSoundStructs.Keys.Count];
                categoryToSoundStructs.Keys.CopyTo(keys, 0);
                for (int i = 0; i < keys.Length; i++)
                {
                    var files = categoryToSoundStructs[keys[i]].FindPropertyRelative("files");
                    var category = categoryToSoundStructs[keys[i]].FindPropertyRelative("name").stringValue;
                    AudioList newList = new AudioList(serializedObject, files, category, false);
                    reorderableSoundLists[category] = newList;
                }

                keys = new string[categoryToMusicStructs.Keys.Count];
                categoryToMusicStructs.Keys.CopyTo(keys, 0);
                for (int i = 0; i < keys.Length; i++)
                {
                    var files = categoryToMusicStructs[keys[i]].FindPropertyRelative("files");
                    var category = categoryToMusicStructs[keys[i]].FindPropertyRelative("name").stringValue;
                    AudioList newList = new AudioList(serializedObject, files, category, true);
                    reorderableMusicLists[category] = newList;
                }
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

                if (missingSoundNames.Count > 0)
                {
                    EditorGUILayout.HelpBox("Generated Audio Library files have been removed! " +
                        "Please re-generate your Audio Library to prevent errors during runtime!", 
                        MessageType.Warning);
                }

                scrollProgress = EditorGUILayout.BeginScrollView(scrollProgress);

                EditorGUILayout.BeginHorizontal();

                if (showMusic.boolValue) JSAMEditorHelper.BeginColourChange(GUI.color.Subtract(new Color(0.2f, 0.2f, 0.2f, 0)));
                string topLabel = "Sound Library";
                if (newSoundNames.Count > 0 || missingSoundNames.Count > 0) topLabel += "*";
                if (GUILayout.Button(topLabel, JSAMEditorHelper.ApplyTextColorToStyle(EditorStyles.miniButtonLeft, Color.white)))
                {
                    showMusic.boolValue = false;
                }
                if (showMusic.boolValue) JSAMEditorHelper.EndColourChange();

                topLabel = "Music Library";
                if (!showMusic.boolValue) JSAMEditorHelper.BeginColourChange(GUI.color.Subtract(new Color(0.2f, 0.2f, 0.2f, 0)));
                if (newMusicNames.Count > 0 || missingMusicNames.Count > 0) topLabel += "*";
                if (GUILayout.Button(topLabel, EditorStyles.miniButtonRight))
                {
                    showMusic.boolValue = true;
                }
                if (!showMusic.boolValue) JSAMEditorHelper.EndColourChange();

                EditorGUILayout.EndHorizontal();

                // Sound Library
                if (!showMusic.boolValue)
                {
                    string title = string.Empty;
                    title += "New: " + newSoundNames.Count + " | ";
                    title += "Missing: " + missingSoundNames.Count + " | ";
                    title += "Total Files: " + asset.sounds.Count + " | ";
                    title += "Categories: " + soundCategories.arraySize;
                    blontent = new GUIContent(title);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(blontent);
                    blontent = new GUIContent("Hide All", "Collapse all category foldouts in the Sound Library");

                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        for (int i = 0; i < asset.soundCategories.Count; i++)
                        {
                            SetSoundCategoryFoldout(asset.soundCategories[i], false);
                        }
                    }
                    blontent = new GUIContent("Show All", "Expand all category foldouts in the Sound Library");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        for (int i = 0; i < asset.soundCategories.Count; i++)
                        {
                            SetSoundCategoryFoldout(asset.soundCategories[i], true);
                        }
                    }
                    blontent = new GUIContent("Add New Category", "Add a new sound category");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        var utility = JSAMUtilityWindow.Init("Enter Category Name", true, true);
                        utility.AddField(new GUIContent("Category Name"), "New Category");
                        JSAMUtilityWindow.onSubmitField += AddNewSoundCategory;
                    }

                    EditorGUILayout.EndHorizontal();

                    for (int i = 0; i < asset.soundCategories.Count; i++)
                    {
                        string category = asset.soundCategories[i];
                        string categoryName = category;
                        if (category.Equals(string.Empty)) categoryName = CATEGORY_NONE;
                        bool foldout = GetSoundCategoryFoldout(category);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUI.BeginChangeCheck();
                        foldout = EditorCompatability.SpecialFoldouts(foldout, categoryName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetSoundCategoryFoldout(category, foldout);
                        }
                        if (foldout)
                        {
                            reorderableSoundLists[category].Draw();
                        }
                        EditorCompatability.EndSpecialFoldoutGroup();
                        EditorGUILayout.EndVertical();
                    }
                }
                else
                // Music Library
                {
                    string title = string.Empty;
                    title += "New: " + newMusicNames.Count + " | ";
                    title += "Missing: " + missingMusicNames.Count + " | ";
                    title += "Total Files: " + asset.music.Count + " | ";
                    title += "Categories: " + musicCategories.arraySize;
                    blontent = new GUIContent(title);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(blontent);
                    blontent = new GUIContent("Hide All", "Collapse all category foldouts in the Music Library");

                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        for (int i = 0; i < asset.musicCategories.Count; i++)
                        {
                            SetMusicCategoryFoldout(asset.musicCategories[i], false);
                        }
                    }
                    blontent = new GUIContent("Show All", "Expand all category foldouts in the Music Library");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        for (int i = 0; i < asset.musicCategories.Count; i++)
                        {
                            SetMusicCategoryFoldout(asset.musicCategories[i], true);
                        }
                    }
                    blontent = new GUIContent("Add New Category", "Add a new sound category");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                    {
                        var utility = JSAMUtilityWindow.Init("Enter Category Name", true, true);
                        utility.AddField(new GUIContent("Category Name"), "New Category");
                        JSAMUtilityWindow.onSubmitField += AddNewMusicCategory;
                    }

                    EditorGUILayout.EndHorizontal();

                    for (int i = 0; i < asset.musicCategories.Count; i++)
                    {
                        string category = asset.musicCategories[i];
                        string categoryName = category;
                        if (category.Equals(string.Empty)) categoryName = CATEGORY_NONE;
                        bool foldout = GetMusicCategoryFoldout(category);
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUI.BeginChangeCheck();
                        foldout = EditorCompatability.SpecialFoldouts(foldout, categoryName);
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetMusicCategoryFoldout(category, foldout);
                        }
                        if (foldout)
                        {
                            reorderableMusicLists[category].Draw();
                        }
                        EditorCompatability.EndSpecialFoldoutGroup();
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndScrollView();

                blontent = new GUIContent("Re-Generate Audio Library", "Click to generate your Audio enums and make the files in your Audio Library usable. " +
                    "Do this every time you Add/Remove Audio File objects in your library.");
                if (GUILayout.Button(blontent))
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
                DocumentAudioFiles();
                window.Repaint();
            }
        }

        void OnUndoRedoPerformed()
        {
            if (serializedObject != null)
            {
                DesignateSerializedProperties();
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
                    List<BaseAudioFileObject> duplicates = new List<BaseAudioFileObject>();
                    int showPriority = 0;

                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        string filePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[i]);
                        var mimport = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<AudioFileMusicObject>(filePath);
                        var simport = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<AudioFileSoundObject>(filePath);

                        for (int j = 0; j < mimport.Count; j++)
                        {
                            if (asset.music.Contains(mimport[j]))
                            {
                                duplicates.Add(mimport[j]);
                                continue;
                            }

                            showPriority++;
                            music.AddNewArrayElement().objectReferenceValue = mimport[j];
                        }

                        for (int j = 0; j < simport.Count; j++)
                        {
                            if (asset.sounds.Contains(simport[j]))
                            {
                                duplicates.Add(simport[j]);
                                continue;
                            }

                            showPriority--;
                            sounds.AddNewArrayElement().objectReferenceValue = simport[j];
                        }
                    }

                    // Active library changes depending on ratio of Audio added
                    if (showPriority != 0) showMusic.boolValue = showPriority > 0;

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

        /// <summary>
        /// Returns true if this sound is registered in the library
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool GetSoundEnumName(BaseAudioFileObject file, out string enumName)
        {
            enumName = soundEnumName.stringValue + "." + file.SafeName;
            return !newSoundNames.Contains(file.SafeName);
        }

        /// <summary>
        /// Returns true if this sound is registered in the library
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool GetMusicEnumName(BaseAudioFileObject file, out string enumName)
        {
            enumName = musicEnumName.stringValue + "." + file.SafeName;
            return !newMusicNames.Contains(file.SafeName);
        }

        public void RemoveAudioFile(BaseAudioFileObject file, bool isMusic)
        {
            SerializedProperty array = null;
            Dictionary<string, SerializedProperty> categoryToStruct = null;

            if (isMusic)
            {
                array = music;
                categoryToStruct = categoryToMusicStructs;
            }
            else
            {
                array = sounds;
                categoryToStruct = categoryToSoundStructs;
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == file)
                {
                    // Twice again I guess
                    array.DeleteArrayElementAtIndex(i);
                    array.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            // Delete the category listing too
            SerializedProperty ctl = categoryToStruct[file.category].FindPropertyRelative("files");
            for (int i = 0; i < ctl.arraySize; i++)
            {
                if (ctl.GetArrayElementAtIndex(i).objectReferenceValue == file)
                {
                    ctl.DeleteArrayElementAtIndex(i);
                    ctl.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            ApplyChanges();
            GUIUtility.ExitGUI();
        }

        #region Category Operations

        bool GetSoundCategoryFoldout(string category)
        {
            return categoryToSoundStructs[category].FindPropertyRelative("foldout").boolValue;
        }

        void SetSoundCategoryFoldout(string category, bool state)
        {
            categoryToSoundStructs[category].FindPropertyRelative("foldout").boolValue = state;
        }

        bool GetMusicCategoryFoldout(string category)
        {
            return categoryToMusicStructs[category].FindPropertyRelative("foldout").boolValue;
        }

        void SetMusicCategoryFoldout(string category, bool state)
        {
            categoryToMusicStructs[category].FindPropertyRelative("foldout").boolValue = state;
        }

        void AddNewSoundCategory(string[] input)
        {
            string category = input[0];
            if (category.IsNullEmptyOrWhiteSpace())
            {
                EditorUtility.DisplayDialog("Category Name Error",
                    "Category name field was left blank, please enter a valid name.", "OK");
                GUIUtility.ExitGUI();
            }
            else if (asset.soundCategories.Contains(category))
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

        void AddNewMusicCategory(string[] input)
        {
            string category = input[0];
            if (category.IsNullEmptyOrWhiteSpace())
            {
                EditorUtility.DisplayDialog("Category Name Error",
                    "Category name field was left blank, please enter a valid name.", "OK");
                GUIUtility.ExitGUI();
            }
            else if (asset.musicCategories.Contains(category))
            {
                EditorUtility.DisplayDialog("Duplicate Category!",
                    "A category with this name already exists in the music library!", "OK");
                GUIUtility.ExitGUI();
                return;
            }

            musicCategories.AddNewArrayElement().stringValue = category;

            var element = musicCategoriesToList.AddNewArrayElement();
            element.FindPropertyRelative("name").stringValue = category;
            element.FindPropertyRelative("foldout").boolValue = true;

            ApplyChanges();
        }

        public void RenameCategory(string prevName, string newName, bool isMusic)
        {
            if (prevName.Equals(newName))
            {
                EditorUtility.DisplayDialog("Category Rename Error",
                    "Category name is identical to previous name!", "OK");
                FocusWindowIfItsOpen<JSAMUtilityWindow>();
                GUIUtility.ExitGUI();
            }
            else if (newName.Equals(CATEGORY_NONE))
            {
                newName = string.Empty;
            }

            int index = 0;
            SerializedProperty categories = null;
            Dictionary<string, SerializedProperty> categoryToStructs = null;
            if (isMusic)
            {

            }
            else
            {
                categories = soundCategories;
                categoryToStructs = categoryToSoundStructs;

                // This category should not be renamed, clone it
                //if (prevName.Equals(string.Empty))
                //{
                //    soundCategories.AddNewArrayElement();
                //    index = soundCategories.arraySize - 1;
                //}
                // Proceed as normal otherwise
                //else
                {
                }
                index = asset.soundCategories.IndexOf(prevName);
            }
            
            categories.GetArrayElementAtIndex(index).stringValue = newName;

            var array = categoryToStructs[prevName].FindPropertyRelative("files");
            for (int i = 0; i < array.arraySize; i++)
            {
                ChangeAudioFileCategory(array.GetArrayElementAtIndex(i).objectReferenceValue as BaseAudioFileObject, newName);
            }

            // Category with this name exists, delete this one to complete merge
            if (categoryToStructs.ContainsKey(newName))
            {
                // Don't actually delete if you're the important category
                if (!prevName.Equals(string.Empty))
                {
                    categoryToStructs[prevName].DeleteCommand();
                }
                categories.DeleteArrayElementAtIndex(index);
            }
            else
            {
                categoryToStructs[prevName].FindPropertyRelative("name").stringValue = newName;
            }

            ApplyChanges();
        }

        public void ChangeAudioFileCategory(BaseAudioFileObject file, string category)
        {
            var so = new SerializedObject(file);
            so.FindProperty("category").stringValue = category;
            so.ApplyModifiedProperties();
        }

        public bool CanCategoryMove(string category, bool isUp, bool isMusic)
        {
            List<string> categoryList = null;
            if (isMusic)
            {
                categoryList = asset.musicCategories;
            }
            else
            {
                categoryList = asset.soundCategories;
            }

            if (isUp)
            {
                return categoryList.IndexOf(category) == 0;
            }
            else
            {
                return categoryList.IndexOf(category) == categoryList.Count - 1;
            }
        }

        /// <summary>
        /// Moving up at the top and down at the bottom disabled 
        /// Out of scope for the time being
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="isUp"></param>
        /// <param name="isMusic"></param>
        public void MoveCategory(string categoryName, bool isUp, bool isMusic)
        {
            // Initialize variables
            int startIndex = -1;
            SerializedProperty propCategories = null;
            SerializedProperty propCategoriesToList = null;
            int ogSize = -1;
            int startOffset = -1;
            int destinationIndex = -1;
            int destinationOffset = -1;
            int deletionTarget = -1;
            string destinationCategory = string.Empty;

            if (isMusic)
            {
                // Search for start position of category
                startIndex = asset.musicCategories.IndexOf(categoryName);

                // Cache initial size of category array
                ogSize = musicCategories.arraySize;

                propCategories = musicCategories;
                propCategoriesToList = musicCategoriesToList;
            }
            else
            {
                // Search for start position of category
                startIndex = asset.soundCategories.IndexOf(categoryName);

                // Cache initial size of category array
                ogSize = soundCategories.arraySize;

                propCategories = soundCategories;
                propCategoriesToList = soundCategoriesToList;
            }

            // Insert a helper element into array
            propCategories.InsertArrayElementAtIndex(startIndex);
            propCategoriesToList.InsertArrayElementAtIndex(startIndex);

            if (isUp)
            {
                startOffset = startIndex + 1;
                // Moving up while at the top
                if (startIndex == 0)
                {
                    destinationIndex = ogSize;
                    destinationCategory = propCategories.GetArrayElementAtIndex(destinationIndex).stringValue;
                }
                else
                {
                    destinationIndex = startIndex - 1;
                    destinationCategory = propCategories.GetArrayElementAtIndex(destinationIndex).stringValue;
                }
                destinationOffset = destinationIndex;

                deletionTarget = startOffset;
            }
            else
            {
                startOffset = startIndex + 1;
                // Moving down while at the bottom
                if (startIndex == ogSize - 1)
                {
                    destinationIndex = 0;
                    destinationOffset = destinationIndex + 1;
                    destinationCategory = propCategories.GetArrayElementAtIndex(destinationIndex).stringValue;
                    deletionTarget = ogSize;
                }
                else
                {
                    destinationIndex = startOffset;
                    destinationOffset = destinationIndex + 1;
                    destinationCategory = propCategories.GetArrayElementAtIndex(destinationOffset).stringValue;
                    deletionTarget = destinationOffset;
                }
            }

            // Move destination to original position
            propCategories.GetArrayElementAtIndex(startIndex).stringValue = destinationCategory;
            propCategoriesToList.GetArrayElementAtIndex(startIndex).FindPropertyRelative("name").stringValue = destinationCategory;
            var startArray = propCategoriesToList.GetArrayElementAtIndex(startIndex).FindPropertyRelative("files");
            var destArray = propCategoriesToList.GetArrayElementAtIndex(destinationOffset).FindPropertyRelative("files");
            startArray.ClearArray();
            for (int i = 0; i < destArray.arraySize; i++)
            {
                var element = startArray.AddNewArrayElement();
                element.objectReferenceValue = destArray.GetArrayElementAtIndex(i).objectReferenceValue;
            }

            // Move original to destination
            propCategories.GetArrayElementAtIndex(destinationIndex).stringValue = categoryName;
            propCategoriesToList.GetArrayElementAtIndex(destinationIndex).FindPropertyRelative("name").stringValue = categoryName;
            var startOffsetArray = propCategoriesToList.GetArrayElementAtIndex(startOffset).FindPropertyRelative("files");
            destArray.ClearArray();
            for (int i = 0; i < startOffsetArray.arraySize; i++)
            {
                var element = destArray.AddNewArrayElement();
                element.objectReferenceValue = startOffsetArray.GetArrayElementAtIndex(i).objectReferenceValue;
            }

            // Delete the outlying index
            propCategories.DeleteArrayElementAtIndex(deletionTarget);
            propCategoriesToList.DeleteArrayElementAtIndex(deletionTarget);

            ApplyChanges();
        }

        public void DeleteCategory(string categoryName, bool isMusic)
        {
            Dictionary<string, SerializedProperty> categoryToStruct = null;
            SerializedProperty categories = null;
            SerializedProperty audioProp = null;
            int index = -1;
            
            if (isMusic)
            {
                categoryToStruct = categoryToMusicStructs;
                index = asset.musicCategories.IndexOf(categoryName);
                categories = musicCategories;
                audioProp = music;
            }
            else
            {
                categoryToStruct = categoryToSoundStructs;
                index = asset.soundCategories.IndexOf(categoryName);
                categories = soundCategories;
                audioProp = sounds;
            }

            int categorySize = categoryToStruct[categoryName].FindPropertyRelative("files").arraySize;
            if (categorySize > 0)
            {
                bool cancel = EditorUtility.DisplayDialog("Delete Category?", "This category holds " + categorySize + " Audio File object(s). " +
                    "Are you sure you want to remove the category as well all its Audio Files from this library?\n" +
                    "(Note: This process can be undone with Edit -> Undo)", "Yes", "No");
                if (!cancel)
                {
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            categories.DeleteArrayElementAtIndex(index);
            for (int i = 0; i < audioProp.arraySize; i++)
            {
                var audio = audioProp.GetArrayElementAtIndex(i).objectReferenceValue as BaseAudioFileObject;
                if (audio.category.Equals(categoryName))
                {
                    // A dirty hack, but Unity serialization is real messy
                    // https://answers.unity.com/questions/555724/serializedpropertydeletearrayelementatindex-leaves.html
                    if (audioProp.GetArrayElementAtIndex(i) != null)
                        audioProp.DeleteArrayElementAtIndex(i);
                    audioProp.DeleteArrayElementAtIndex(i);
                    i--; // Decrement current index so we can keep iterating through the array scot-free
                }
            }

            categoryToStruct[categoryName].DeleteCommand();

            ApplyChanges();
            GUIUtility.ExitGUI();
        }

        #endregion

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
            string namespaceName = JSAMSettings.Settings.EnumNamespace;
            if (JSAMSettings.Settings.UseNamespace)
            {
                writer.WriteLine("namespace " + name + "{");
            }

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
            if (JSAMSettings.Settings.UseNamespace)
            {
                writer.WriteLine("}");
            }
            writer.Close();

            //Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(filePath);
            AssetDatabase.Refresh();

            if (JSAMSettings.Settings.UseNamespace)
            {
                soundEnumName.stringValue = namespaceName + "." + safeLibraryName + "Sounds";
                musicEnumName.stringValue = namespaceName + "." + safeLibraryName + "Music";
            }
            else
            {
                soundEnumName.stringValue = safeLibraryName + "Sounds";
                musicEnumName.stringValue = safeLibraryName + "Music";
            }
            safeName.stringValue = safeLibraryName;
            serializedObject.ApplyModifiedProperties();

            return true;
        }
    }
}