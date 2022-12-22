using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseAudioEditor : Editor
    {
        protected List<string> options = new List<string>();
        protected List<List<string>> moreOptions = new List<List<string>>();
        protected static bool showHowTo;

        protected SerializedProperty audio;

        protected void OnEnable()
        {
            if (target == null)
            {
                OnEnable();
                return;
            }

            PopulateSoundList();

            Setup();
        }

        protected virtual void Setup()
        {
            audio = serializedObject.FindProperty("sound");
        }

        protected void PopulateSoundList()
        {
            if (AudioManager.Instance)
            {
                if (AudioManager.Instance.Library == null) return;
                var sounds = AudioManager.Instance.Library.Sounds;
                for (int i = 0; i < sounds.Count; i++)
                {
                    options.Add(sounds[i].SafeName);
                }
            }
        }

        protected static int attempts = 0;
        protected static int MAX_ATTEMPTS = 3;

        /// <summary>
        /// Is here to prevent the strange errors the appear the moment you finish compiling
        /// </summary>
        protected void TryPopulateSoundList()
        {
            if (attempts < MAX_ATTEMPTS)
            {
                attempts++;
            }
            PopulateSoundList();
        }

        /// <summary>
        /// </summary>
        /// <param name="soundProperty">This is passed by reference, thanks Unity!</param>
        protected void DrawSoundDropdown()
        {
            if (!AudioManager.Instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }
            else if (AudioManager.Instance.Library == null)
            {
                EditorGUILayout.HelpBox("Your Audio Manager is missing an Audio Library! This components relies on an " +
                    "Audio Library to function!", MessageType.Error);
            }

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

            JSAMSoundFileObject audioObject = (JSAMSoundFileObject)audio.objectReferenceValue;
            int selected = 0;
            if (audioObject != null) selected = options.IndexOf(audioObject.SafeName);
            if (selected == -1) selected = 0;

            if (options.Count > 0)
            {
                var n = AudioManager.Instance.Library.Sounds[selected].SafeName;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(soundDesc);
                if (EditorGUILayout.DropdownButton(new GUIContent(n), FocusType.Keyboard))
                {
                    var ctl = AudioManager.Instance.Library.soundCategoriesToList;
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
                    EditorGUILayout.Popup(soundDesc, selected, new string[] {"<None>"});
                }
            }
        }

        protected virtual void DrawQuickReferenceGuide()
        {
            EditorGUILayout.Space();
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
            EditorGUILayout.Space();
        }

        void SetSelected(object data)
        {
            int i = options.IndexOf((string)data);
            audio.objectReferenceValue = AudioManager.Instance.Library.Sounds[i];
            if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
        }
    }
}