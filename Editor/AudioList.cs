using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

namespace JSAM.JSAMEditor
{
    public class AudioList
    {
        ReorderableList list;
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
                    AudioManager.DebugLog("Copied " + enumName + " to clipboard!");
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
                EditorStyles.label.ApplyTextAnchor(TextAnchor.MiddleRight));

            EditorGUILayout.EndHorizontal();
        }

        public void RenameCategory(string[] input)
        {
            AudioLibraryEditor.Window.RenameCategory(category, input[0], isMusic);
        }

        public void SetAudioFileCategory(object input)
        {
            Tuple<BaseAudioFileObject, string> tuple = (Tuple<BaseAudioFileObject, string>)input;
            AudioLibraryEditor.Window.ChangeAudioFileCategory(tuple.Item1, tuple.Item2, isMusic);
        }
    }
}