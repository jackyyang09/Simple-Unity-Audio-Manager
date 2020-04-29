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
        static bool showAudioClipSettings = false;
        static bool showHowTo = false;

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
            EditorGUILayout.ObjectField(customSound, fileText);

            EditorGUILayout.Space();

            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;

            GUIContent fontent = new GUIContent("Custom AudioClip Settings", "These settings only apply if you input your own custom AudioClip rather than choosing from the generated Audio Library");
            if (myScript.GetAttachedFile() == null)
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent);
            else
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent, boldFoldout);
            if (showAudioClipSettings)
            {
                using (new EditorGUI.DisabledScope(myScript.GetAttachedFile() == null))
                {
                    DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "musicFile", "playOnStart", "playOnEnable",
                        "stopOnDisable", "stopOnDestroy", "keepPlaybackPosition", "restartOnReplay", "musicFadeTime", "transitionMode" });
                }
            }

            EditorGUILayout.Space();

            GUIContent lontent = new GUIContent("Music Player Settings", "Modify settings specific to Audio Player Music");
            EditorGUILayout.LabelField(lontent, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionMode"));
            if (myScript.GetTransitionMode() != TransitionMode.None)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("keepPlaybackPosition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("musicFadeTime"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restartOnReplay"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDisable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDestroy"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            #region Quick Reference Guide
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "This component allows you to easily play music anywhere in the scene."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "To get started, choose your music to play from the dropdown at the top. " +
                    "Make sure you've generated your Audio Libraries in your Audio Manager. "
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Alternatively, you can specify to use your own AudioClip by filling in the AudioClip. " +
                    "You can then fill out the Custom AudioClip settings so the AudioPlayer plays your music to your liking."
                    , MessageType.None);
                //EditorGUILayout.HelpBox(
                //    "AudioPlayer includes a public function Play() that lets you play the sound in AudioPlayer on your own. " +
                //    "AudioPlayer's Play() function also returns the AudioSource to let you further modify the audio being played."
                //    , MessageType.None);

            }
            #endregion  
        }
    }
}