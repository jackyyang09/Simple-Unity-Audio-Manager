using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioPlayer))]
    [CanEditMultipleObjects]
    public class AudioPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AudioPlayer myScript = (AudioPlayer)target;

            List<string> options = new List<string>();

            System.Type enumType = AudioManager.instance.GetSceneSoundEnum();
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

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

            int sound = serializedObject.FindProperty("sound").intValue;

            using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() != null))
            {
                serializedObject.FindProperty("sound").intValue = EditorGUILayout.Popup(soundDesc, sound, options.ToArray());
            }

            GUIContent fileText = new GUIContent("Custom AudioClip", "Overrides the \"Sound\" parameter with an AudioClip if not null");
            SerializedProperty customSound = serializedObject.FindProperty("soundFile");
            EditorGUILayout.ObjectField(customSound, fileText);

            DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "soundFile" });

            serializedObject.ApplyModifiedProperties();
        }
    }
}