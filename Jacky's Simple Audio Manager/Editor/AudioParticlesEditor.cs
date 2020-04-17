using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioParticles))]
    [CanEditMultipleObjects]
    public class AudioParticlesEditor : Editor
    {
        static bool showHowTo;

        public override void OnInspectorGUI()
        {
            AudioParticles myScript = (AudioParticles)target;

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

            int sound = serializedObject.FindProperty("sound").intValue;

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played when particles spawn/die");

            using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() != null))
            {
                serializedObject.FindProperty("sound").intValue = EditorGUILayout.Popup(soundDesc, sound, options.ToArray());
            }

            GUIContent fileText = new GUIContent("Custom AudioClip", "Overrides the \"Sound\" parameter with an AudioClip if not null");
            SerializedProperty customSound = serializedObject.FindProperty("soundFile");
            EditorGUILayout.ObjectField(customSound, fileText);

            DrawPropertiesExcluding(serializedObject, new[] { "m_Script" });

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            GUIStyle boldFoldout = EditorStyles.foldout;
            boldFoldout.fontStyle = FontStyle.Bold;
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This component is meant to be attached to a Particle System." + 
                    " When a particle is created or is destroyed, AudioParticles will play a sound."
                    , MessageType.None);
                EditorGUILayout.HelpBox("This component should be placed on the same GameObject that holds the Particle System."
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Feel free to use multiple different AudioParticles components on the same GameObject so your" +
                    " Particle System plays sounds on both instantiation and destruction!"
                    , MessageType.None);
            }
        }
    }
}