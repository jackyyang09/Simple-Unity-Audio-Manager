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
        AudioPlayerMusic myScript;
        List<string> options = new List<string>();
        System.Type enumType = null;

        SerializedProperty musicProperty;
        SerializedProperty transitionMode;

        SerializedProperty keepPlaybackPosition;
        SerializedProperty musicFadeInTime;
        SerializedProperty musicFadeOutTime;
        SerializedProperty restartOnReplay;

        SerializedProperty onStart;
        SerializedProperty onEnable;
        SerializedProperty onDisable;
        SerializedProperty onDestroy;

        static bool showAudioClipSettings = false;
        static bool showHowTo = false;

        private void OnEnable()
        {
            myScript = (AudioPlayerMusic)target;

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

            musicProperty = serializedObject.FindProperty("music");
            transitionMode = serializedObject.FindProperty("transitionMode");
            keepPlaybackPosition = serializedObject.FindProperty("keepPlaybackPosition");
            musicFadeInTime = serializedObject.FindProperty("musicFadeInTime");
            musicFadeOutTime = serializedObject.FindProperty("musicFadeOutTime");
            restartOnReplay = serializedObject.FindProperty("restartOnReplay");

            onStart = serializedObject.FindProperty("onStart");
            onEnable = serializedObject.FindProperty("onEnable");
            onDisable = serializedObject.FindProperty("onDisable");
            onDestroy = serializedObject.FindProperty("onDestroy");
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

            EditorGUILayout.Space();
            
            GUIContent lontent = new GUIContent("Music Player Settings", "Modify settings specific to Audio Player Music");
            EditorGUILayout.LabelField(lontent, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(transitionMode);
            if (myScript.GetTransitionMode() != TransitionMode.None)
            {
                EditorGUILayout.PropertyField(keepPlaybackPosition);
                EditorGUILayout.PropertyField(musicFadeInTime);
                EditorGUILayout.PropertyField(musicFadeOutTime);
            }
            EditorGUILayout.PropertyField(restartOnReplay);
            EditorGUILayout.PropertyField(onStart);
            EditorGUILayout.PropertyField(onEnable);
            EditorGUILayout.PropertyField(onDisable);
            EditorGUILayout.PropertyField(onDestroy);

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            #region Quick Reference Guide
            showHowTo = EditorGUILayout.BeginFoldoutHeaderGroup(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "This component allows you to easily play music anywhere in the scene."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "To get started, choose your music to play from the drop-down at the top. " +
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
            EditorGUILayout.Space();
            #endregion  
        }

        [MenuItem("GameObject/Audio/JSAM/Audio Player Music", false, 1)]
        public static void AddAudioPlayerMusic()
        {
            GameObject newPlayer = new GameObject("Audio Player Music");
            newPlayer.AddComponent<AudioPlayerMusic>();
            if (Selection.activeTransform != null)
            {
                newPlayer.transform.parent = Selection.activeTransform;
                newPlayer.transform.localPosition = Vector3.zero;
            }
            EditorGUIUtility.PingObject(newPlayer);
            Selection.activeGameObject = newPlayer;
            Undo.RegisterCreatedObjectUndo(newPlayer, "Added new AudioPlayerMusic");
        }
    }
}