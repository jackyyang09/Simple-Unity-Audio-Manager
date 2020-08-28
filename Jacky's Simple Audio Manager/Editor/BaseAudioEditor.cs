using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    public abstract class BaseAudioEditor : Editor
    {
        protected List<string> options = new List<string>();
        protected static System.Type enumType;
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
                enumType = AudioManager.instance.GetSceneSoundEnum();
                if (enumType != null)
                {
                    foreach (string s in System.Enum.GetNames(enumType))
                    {
                        options.Add(s);
                    }
                }
            }
        }

        protected static int attempts = 0;
        protected static int MAX_ATTEMPTS = 3;

        /// <summary>
        /// Is here to prevent the strange errors the moment you finish compiling
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
            else 
            {
                if (enumType == null)
                {
                    EditorGUILayout.HelpBox("Could not find Audio File info! Try regenerating Audio Files in AudioManager!", MessageType.Error);
                    TryPopulateSoundList();
                }
            }

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

            int selected = options.IndexOf(soundProperty.stringValue);
            if (selected == -1) selected = 0;
            if (options.Count > 0)
            {
                soundProperty.stringValue = options[EditorGUILayout.Popup(soundDesc, selected, options.ToArray())];
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