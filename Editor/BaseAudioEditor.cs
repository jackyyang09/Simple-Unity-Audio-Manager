using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseAudioEditor<T> : Editor where T : BaseAudioFileObject
    {
        protected List<string> options = new List<string>();
        protected List<List<string>> moreOptions = new List<List<string>>();
        protected static bool showHowTo;

        protected SerializedProperty audio;
        protected SerializedProperty advancedMode;

        protected abstract GUIContent audioDesc { get; }
        protected abstract List<T> audioLibrary { get; }
        protected abstract List<AudioLibrary.CategoryToList> ctl { get; }

        protected void OnEnable()
        {
            if (target == null)
            {
                OnEnable();
                return;
            }

            Setup();

            PopulateAudioList();
        }

        protected virtual void Setup()
        {
            audio = serializedObject.FindProperty(nameof(audio));
            advancedMode = serializedObject.FindProperty(nameof(advancedMode));
        }

        protected void PopulateAudioList()
        {
            if (!AudioManager.Instance) return;
            if (AudioManager.Instance.Library == null) return;

            for (int i = 0; i < audioLibrary.Count; i++)
            {
                options.Add(audioLibrary[i].SafeName);
            }

            if (audio.objectReferenceValue == null)
            {
                audio.objectReferenceValue = audioLibrary[0];
                serializedObject.ApplyModifiedProperties();
            }
        }

        protected static int attempts = 0;
        protected static int MAX_ATTEMPTS = 3;

        /// <summary>
        /// Is here to prevent the strange errors the appear the moment you finish compiling
        /// </summary>
        protected void TryPopulateAudioList()
        {
            if (attempts < MAX_ATTEMPTS)
            {
                attempts++;
            }
            PopulateAudioList();
        }

        protected virtual void DrawAudioProperty()
        {
            if (!AudioManager.Instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }
            else if (AudioManager.Instance.Library == null)
            {
                EditorGUILayout.HelpBox("Your Audio Manager is missing an Audio Library! Unless advanced mode is true, " + 
                    "this components relies on an Audio Library to function!", MessageType.Warning);
            }

            if (advancedMode.boolValue)
            {
                EditorGUILayout.PropertyField(audio, audioDesc);
            }
            else
            {
                DrawAudioDropdown();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(advancedMode);
            if (EditorGUI.EndChangeCheck())
            {
                if (advancedMode.boolValue) return;
                if (!AudioManager.Instance) return;
                if (AudioManager.Instance.Library) return;
                if (!audioLibrary.Contains(audio.objectReferenceValue as T)) return;

                if (audio.objectReferenceValue == null)
                {
                    audio.objectReferenceValue = audioLibrary[0];
                }
            }
        }

        private void DrawAudioDropdown()
        {
            T audioObject = (T)audio.objectReferenceValue;
            int selected = 0;
            if (audioObject != null) selected = options.IndexOf(audioObject.SafeName);
            if (selected == -1) selected = 0;

            if (options.Count > 0)
            {
                var n = audioLibrary[selected].SafeName;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(audioDesc);
                if (EditorGUILayout.DropdownButton(new GUIContent(n), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    for (int i = 0; i < ctl.Count; i++)
                    {
                        for (int j = 0; j < ctl[i].files.Count; j++)
                        {
                            var categoryName = ctl[i].name == "" ? "Uncategorized" : ctl[i].name;
                            var c = new GUIContent(categoryName + "/" + ctl[i].files[j].SafeName);
                            menu.AddItem(c, ctl[i].files[j].SafeName == n, SetSelected, ctl[i].files[j].SafeName);
                        }
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(audioDesc, selected, new string[] {"<None>"});
                }
            }
        }

        protected virtual void DrawQuickReferenceGuide()
        {
            EditorGUILayout.Space();
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
            EditorGUILayout.Space();
        }

        protected void SetSelected(object data)
        {
            int i = options.IndexOf((string)data);
            audio.objectReferenceValue = audioLibrary[i];
            if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
        }
    }
}