using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM 
{
    [CustomEditor(typeof(AudioPlayerMusic))]
    [CanEditMultipleObjects]
    public class AudioPlayerMusicEditor : BaseAudioMusicEditor
    {
        AudioPlayerMusic myScript;

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

        //static bool showAudioClipSettings = false;

        protected override void Setup()
        {
            myScript = (AudioPlayerMusic)target;

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

            DrawMusicDropdown(musicProperty);

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

            DrawQuickReferenceGuide();
        }

        #region Quick Reference Guide
        protected override void DrawQuickReferenceGuide()
        {
            base.DrawQuickReferenceGuide();

            if (!showHowTo) return;

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

            EditorCompatability.EndSpecialFoldoutGroup();
            EditorGUILayout.Space();
        }
        #endregion

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