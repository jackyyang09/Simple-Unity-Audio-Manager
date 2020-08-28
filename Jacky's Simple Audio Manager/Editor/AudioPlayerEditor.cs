using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioPlayer))]
    [CanEditMultipleObjects]
    public class AudioPlayerEditor : BaseAudioEditor
    {
        AudioPlayer myScript;

        SerializedProperty sound;
        SerializedProperty soundFile;
        SerializedProperty loopSound;
        SerializedProperty onStart;
        SerializedProperty onEnable;
        SerializedProperty onDisable;
        SerializedProperty onDestroy;

        protected override void Setup()
        {
            myScript = (AudioPlayer)target;

            sound = serializedObject.FindProperty("sound");
            soundFile = serializedObject.FindProperty("soundFile");
            loopSound = serializedObject.FindProperty("loopSound");
            onStart = serializedObject.FindProperty("onStart");
            onEnable = serializedObject.FindProperty("onEnable");
            onDisable = serializedObject.FindProperty("onDisable");
            onDestroy = serializedObject.FindProperty("onDestroy");
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawSoundDropdown(sound);

            EditorGUILayout.Space();

            GUIContent lontent = new GUIContent("Audio Player Settings", "Modify settings specific to the Audio Player");
            EditorGUILayout.LabelField(lontent, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(loopSound);
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
                "This component allows you to easily play sounds anywhere in the scene."
                , MessageType.None);
            EditorGUILayout.HelpBox(
                "To get started, choose your sound to play from the drop-down at the top. " +
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

            EditorCompatability.EndSpecialFoldoutGroup();
        }
        #endregion

        [MenuItem("GameObject/Audio/JSAM/Audio Player", false, 1)]
        public static void AddAudioPlayer()
        {
            GameObject newPlayer = new GameObject("Audio Player");
            newPlayer.AddComponent<AudioPlayer>();
            if (Selection.activeTransform != null)
            {
                newPlayer.transform.parent = Selection.activeTransform;
                newPlayer.transform.localPosition = Vector3.zero;
            }
            EditorGUIUtility.PingObject(newPlayer);
            Selection.activeGameObject = newPlayer;
            Undo.RegisterCreatedObjectUndo(newPlayer, "Added new Audio Player");
        }
    }
}