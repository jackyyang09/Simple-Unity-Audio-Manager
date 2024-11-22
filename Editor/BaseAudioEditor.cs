using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseAudioEditor<T> : Editor where T : BaseAudioFileObject
    {
        protected static bool showHowTo;

        protected SerializedProperty audio;
        protected SerializedProperty advancedMode;

        protected abstract GUIContent audioDesc { get; }

        protected AudioLibrary[] Libraries => AudioManager.Instance.PreloadedLibraries;

        protected abstract List<T> GetListFromLibrary(AudioLibrary l);
        protected abstract List<AudioLibrary.CategoryToList> GetCTLFromLibrary(AudioLibrary l);

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
            if (Libraries.Length == 0) return;

            if (audio.objectReferenceValue == null)
            {
                audio.objectReferenceValue = GetFirstAvailableLibrarySound();
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
                if (Libraries.Length == 0) return;

                if (audio.objectReferenceValue == null)
                {
                    audio.objectReferenceValue = GetFirstAvailableLibrarySound();
                }
            }

            if (!AudioManager.Instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }
            else if (Libraries.Length == 0 && !advancedMode.boolValue)
            {
                EditorGUILayout.HelpBox("Your Audio Manager is missing an Audio Library! Unless advanced mode is true, " +
                    "this components relies on an Audio Library to function!", MessageType.Warning);
            }
        }

        private void DrawAudioDropdown()
        {
            T audioObject = (T)audio.objectReferenceValue;

            if (!AudioManager.Instance) return;

            if (Libraries.Length > 0)
            {
                var n = audioObject.SafeName;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(audioDesc);
                if (EditorGUILayout.DropdownButton(new GUIContent(n), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    foreach (var l in Libraries)
                    {
                        var ctl = GetCTLFromLibrary(l);
                        for (int i = 0; i < ctl.Count; i++)
                        {
                            for (int j = 0; j < ctl[i].files.Count; j++)
                            {
                                var categoryName = ctl[i].name == "" ? "Uncategorized" : ctl[i].name;
                                var c = new GUIContent(l.name + "/" + categoryName + "/" + ctl[i].files[j].SafeName);
                                menu.AddItem(c, ctl[i].files[j].SafeName == n, SetSelected, ctl[i].files[j]);
                            }
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
                    EditorGUILayout.Popup(audioDesc, 0, new string[] {"<None>"});
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
            audio.objectReferenceValue = data as T;
            if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
        }

        protected T GetFirstAvailableLibrarySound()
        {
            foreach (var l in Libraries)
            {
                var list = GetListFromLibrary(l);
                foreach (var a in list)
                {
                    if (a) return a;
                }
            }
            return null;
        }
    }
}