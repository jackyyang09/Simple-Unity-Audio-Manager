using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
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
            if (AudioManager.instance)
            {
                foreach (AudioFileObject audio in AudioManager.instance.GetSoundLibrary())
                {
                    options.Add(audio.safeName);
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
            if (!AudioManager.instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

            AudioFileObject audioObject = (AudioFileObject)soundProperty.objectReferenceValue;
            int selected = 0;
            if (audioObject != null) selected = options.IndexOf(audioObject.safeName);
            if (selected == -1) selected = 0;
            if (options.Count > 0)
            {
                selected = EditorGUILayout.Popup(soundDesc, selected, options.ToArray());
                soundProperty.objectReferenceValue = AudioManager.instance.GetSoundLibrary()[selected];
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