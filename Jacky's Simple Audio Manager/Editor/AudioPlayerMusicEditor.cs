using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM 
{
    [CustomEditor(typeof(AudioPlayerMusic))]
    [CanEditMultipleObjects]
    public class AudioPlayerMusicEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AudioPlayerMusic myScript = (AudioPlayerMusic)target;

            List<string> options = new List<string>();

            System.Type enumType = AudioManager.instance.GetSceneMusicEnum();
            if (enumType == null)
            {
                EditorGUILayout.HelpBox("Could not find Audio File info! Try regenerating Audio Files in AudioManager!", MessageType.Error);
            }
            else
            {
                foreach (string s in System.Enum.GetNames(enumType))
                {
                    options.Add(s);
                }
            }

            EditorGUILayout.LabelField("Specify Music to Play", EditorStyles.boldLabel);

            GUIContent musicDesc = new GUIContent("Music", "Music that will be played");

            int music = serializedObject.FindProperty("music").intValue;

            using (new EditorGUI.DisabledScope(myScript.GetAttachedFile() != null))
            {
                serializedObject.FindProperty("music").intValue = EditorGUILayout.Popup(musicDesc, music, options.ToArray());
            }

            GUIContent fileText = new GUIContent("Custom AudioClip", "Overrides the \"Music\" parameter with an AudioClip if not null");
            SerializedProperty customSound = serializedObject.FindProperty("musicFile");
            EditorGUILayout.PropertyField(customSound, fileText);

            DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "musicFile" });

            serializedObject.ApplyModifiedProperties();
        }
    }
}