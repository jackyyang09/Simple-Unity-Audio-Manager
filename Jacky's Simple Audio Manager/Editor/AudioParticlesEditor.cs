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
        AudioParticles myScript;
        List<string> options = new List<string>();
        System.Type enumType = null;

        SerializedProperty sound;
        SerializedProperty soundFile;
        SerializedProperty playSoundOn;

        static bool showHowTo;

        private void OnEnable()
        {
            myScript = (AudioParticles)target;

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

            sound = serializedObject.FindProperty("sound");
            soundFile = serializedObject.FindProperty("soundFile");
            playSoundOn = serializedObject.FindProperty("playSoundOn");
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

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
                }
            }

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played when particles spawn/die");

            int selected = options.IndexOf(sound.stringValue);
            if (selected == -1) selected = 0;
            if (options.Count > 0)
            {
                sound.stringValue = options[EditorGUILayout.Popup(soundDesc, selected, options.ToArray())];
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Popup(soundDesc, selected, new string[] { "<None>" });
                }
            }

            EditorGUILayout.PropertyField(playSoundOn);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            showHowTo = EditorGUILayout.BeginFoldoutHeaderGroup(showHowTo, "Quick Reference Guide");
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
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}