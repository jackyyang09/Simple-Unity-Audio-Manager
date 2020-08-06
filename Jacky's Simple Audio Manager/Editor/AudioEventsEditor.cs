using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioEvents))]
    public class AudioEventsEditor : Editor
    {
        static bool showHowTo;

        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, new[] { "m_Script" });

            #region Quick Reference Guide
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "This component is specifically meant to be used as a companion component to various Event handling components."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "This includes things like UnityEvents and Unity Animation Events, among other things."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "This component contains some helpful methods meant to be called by these Event systems."
                    , MessageType.None);

                if (GUILayout.Button("Guide on using JSAM with Mecanim", new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/wiki/8.-Using-JSAM-with-Mecanim");
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("public void PlayAudioPlayer (AudioPlayer player)");
                EditorGUILayout.HelpBox("Pass the AudioPlayer to play it's contents", MessageType.None);

                EditorGUILayout.LabelField("public void PlaySoundByEnum (string enumName)");
                EditorGUILayout.HelpBox("Takes the Enum name of the Audio File to be played and plays it according to the File's settings", MessageType.None);

                EditorGUILayout.LabelField("public void PlayLoopingSoundByEnum (string enumName)");
                EditorGUILayout.HelpBox("Does the same as PlaySoundByEnum but loops the sound instead", MessageType.None);

                EditorGUILayout.LabelField("public void StopLoopingSoundByEnum (string enumName)");
                EditorGUILayout.HelpBox("Stops an existing looping sound whose Audio File name matches the one specified", MessageType.None);

                EditorGUILayout.LabelField("public void SetMasterVolume (float newVolume)");
                EditorGUILayout.HelpBox("Changes the volume level of the Master channel to the specified value from 0 to 1", MessageType.None);

                EditorGUILayout.LabelField("public void SetMusicVolume (float newVolume)");
                EditorGUILayout.HelpBox("Changes the volume level of the Music channel to the specified value from 0 to 1", MessageType.None);

                EditorGUILayout.LabelField("public void SetSoundVolume (float newVolume)");
                EditorGUILayout.HelpBox("Changes the volume level of the Sound channel to the specified value from 0 to 1", MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "To learn more about how to use Animation Events as well as Unity's Animation System, do check out " +
                    "their online Manual."
                    , MessageType.None);
                if (GUILayout.Button("Using Unity Animation Events", new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/script-AnimationWindowEvent.html");
                }
                if (GUILayout.Button("Animation Events with Imported Animations", new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://docs.unity3d.com/Manual/AnimationEventsOnImportedClips.html");
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();
            #endregion  
        }
    }
}