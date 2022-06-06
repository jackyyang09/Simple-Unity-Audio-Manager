using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseAudioEditor : Editor
    {
        protected List<string> options = new List<string>();
        protected static bool showHowTo;

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
        protected void DrawSoundDropdown(SerializedProperty soundProperty)
        {
            if (!AudioManager.Instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

            JSAMSoundFileObject audioObject = (JSAMSoundFileObject)soundProperty.objectReferenceValue;
            int selected = 0;
            if (audioObject != null) selected = options.IndexOf(audioObject.SafeName);
            if (selected == -1) selected = 0;
            if (options.Count > 0)
            {
                selected = EditorGUILayout.Popup(soundDesc, selected, options.ToArray());
                soundProperty.objectReferenceValue = AudioManager.Instance.Library.Sounds[selected];
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
    }
}