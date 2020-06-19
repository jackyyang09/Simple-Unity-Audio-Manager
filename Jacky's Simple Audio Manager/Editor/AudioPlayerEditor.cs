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

            System.Type enumType = null;
            if (!AudioManager.instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }
            else 
            {
                enumType = AudioManager.instance.GetSceneSoundEnum();
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
            }

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

            string sound = serializedObject.FindProperty("sound").stringValue;

            using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() != null))
            {
                int selected = options.IndexOf(sound);
                if (selected == -1) selected = 0;
                serializedObject.FindProperty("sound").stringValue = options[EditorGUILayout.Popup(soundDesc, selected, options.ToArray())];
            }

            GUIContent fileText = new GUIContent("Custom AudioClip", "Overrides the \"Sound\" parameter with an AudioClip if not null");
            SerializedProperty customSound = serializedObject.FindProperty("soundFile");

            EditorGUILayout.Space();

            GUIContent fontent = new GUIContent("Custom AudioClip Settings", "These settings only apply if you input your own custom AudioClip rather than choosing from the generated Audio Library");
            if (myScript.GetAttachedSound() == null)
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent);
            else
                showAudioClipSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showAudioClipSettings, fontent);
            if (showAudioClipSettings)
            {
                EditorGUILayout.ObjectField(customSound, fileText);
                using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() == null))
                {
                    DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "soundFile", "playOnStart", "playOnEnable", "stopOnDisable", "stopOnDestroy", "loopSound" });
                }
            }
            if (myScript.GetAttachedSound() != null)
                EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            GUIContent lontent = new GUIContent("Audio Player Settings", "Modify settings specific to the Audio Player");
            EditorGUILayout.LabelField(lontent, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loopSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDisable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDestroy"));

            serializedObject.ApplyModifiedProperties();

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