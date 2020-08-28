using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    public abstract class BaseAudioMusicEditor : BaseAudioEditor
    {
        protected new void OnEnable()
        {
            if (target == null)
            {
                OnEnable();
                return;
            }

            PopulateMusicList();

            Setup();
        }

        protected void PopulateMusicList()
        {
            if (AudioManager.instance)
            {
                enumType = AudioManager.instance.GetSceneMusicEnum();
                if (enumType != null)
                {
                    foreach (string s in System.Enum.GetNames(enumType))
                    {
                        options.Add(s);
                    }
                }
            }
        }

        /// <summary>
        /// Is here to prevent the strange errors the moment you finish compiling
        /// </summary>
        protected void TryPopulateMusicList()
        {
            if (attempts < MAX_ATTEMPTS)
            {
                attempts++;
            }
            PopulateMusicList();
        }

        /// <summary>
        /// </summary>
        /// <param name="musicProperty">This is passed by reference, thanks Unity!</param>
        protected void DrawMusicDropdown(SerializedProperty musicProperty)
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
                    TryPopulateMusicList();
                }
            }

            EditorGUILayout.LabelField("Specify Music to Play", EditorStyles.boldLabel);

            GUIContent musicDesc = new GUIContent("Music", "Music that will be played");

            int selected = options.IndexOf(musicProperty.stringValue);
            if (selected == -1) selected = 0;
            if (options.Count > 0)
            {
                musicProperty.stringValue = options[EditorGUILayout.Popup(musicDesc, selected, options.ToArray())];
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(musicDesc, selected, new string[] { "<None>" });
                }
            }
        }
    }
}