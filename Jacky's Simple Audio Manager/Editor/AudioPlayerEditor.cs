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
        static bool showAudioClipSettings = false;
        static bool showHowTo = false;

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

            EditorGUILayout.Space();

            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;

            GUIContent fontent = new GUIContent("Custom AudioClip Settings", "These settings only apply if you input your own custom AudioClip rather than choosing from the generated Audio Library");
            if (myScript.GetAttachedSound() == null)
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent);
            else
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent, boldFoldout);
            if (showAudioClipSettings)
            {
                using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() == null))
                {
                    DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "soundFile", "playOnStart", "playOnEnable", "stopOnDisable", "stopOnDestroy" });
                }
            }

            EditorGUILayout.Space();

            GUIContent lontent = new GUIContent("Audio Player Settings", "Modify settings specific to the Audio Player");
            EditorGUILayout.LabelField(lontent, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDisable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDestroy"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            #region Quick Reference Guide
            boldFoldout.fontStyle = FontStyle.Bold;
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "This component allows you to easily play sounds anywhere in the scene."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "To get started, choose your sound to play from the dropdown at the top. " +
                    "Make sure you've generated your Audio Libraries in your Audio Manager. "
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Alternatively, you can specify to use your own AudioClip by filling in the AudioClip. " +
                    "You can then fill out the Custom AudioClip settings so the AudioPlayer plays your sound to your liking."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "AudioPlayer includes a public function Play() that lets you play the sound in AudioPlayer on your own. " +
                    "AudioPlayer's Play() function also returns the AudioSource to let you further modify the audio being played."
                    , MessageType.None);

            }
            #endregion  
        }
    }
}