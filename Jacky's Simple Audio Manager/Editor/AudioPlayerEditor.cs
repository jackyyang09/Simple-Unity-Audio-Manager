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
        AudioPlayer myScript;
        List<string> options = new List<string>();
        System.Type enumType = null;

        SerializedProperty sound;
        SerializedProperty soundFile;
        SerializedProperty loopSound;
        SerializedProperty onStart;
        SerializedProperty onEnable;
        SerializedProperty onDisable;
        SerializedProperty onDestroy;

        static bool showAudioClipSettings = false;
        static bool showHowTo = false;

        private void OnEnable()
        {
            myScript = (AudioPlayer)target;

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

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

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
                    EditorGUILayout.Popup(soundDesc, selected, new string[] {"<None>"});
                }
            }

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

            EditorGUILayout.Space();

            #region Quick Reference Guide
            showHowTo = EditorGUILayout.BeginFoldoutHeaderGroup(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

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

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion  
        }

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