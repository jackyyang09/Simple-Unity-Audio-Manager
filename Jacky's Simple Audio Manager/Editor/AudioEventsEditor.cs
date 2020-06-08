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
            showHowTo = EditorGUILayout.BeginFoldoutHeaderGroup(showHowTo, "Quick Reference Guide");
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

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("public void PlaySoundByEnum (string enumName)");
                EditorGUILayout.HelpBox("Takes the name of the Audio enum sound to be played as a string and plays it according to the settings specified in the Audio File Object", MessageType.None);

                EditorGUILayout.LabelField("public void PlayAudioPlayer (AudioPlayer player)");
                EditorGUILayout.HelpBox("Pass the AudioPlayer to play it's contents", MessageType.None);

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
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion  
        }
    }
}


