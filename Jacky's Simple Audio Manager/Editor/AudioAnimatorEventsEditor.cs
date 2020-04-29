using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioAnimatorEvents))]
    public class AudioAnimatorEventsEditor : Editor
    {
        static bool showHowTo;

        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, new[] { "m_Script" });

            #region Quick Reference Guide
            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "This component is specifically meant to be used with Unity's Animator component and must be attached " +
                    "to the same GameObject that holds the Animator."
                    , MessageType.None);
                EditorGUILayout.HelpBox(
                    "This component contains some helpful methods meant to be called by Unity's animation events."
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("public void PlaySoundByEnum (string enumName)");
                EditorGUILayout.HelpBox("Takes the name of the Audio enum sound to be played as a string and plays it without any spatialization", MessageType.None);

                EditorGUILayout.LabelField("public void PlaySpatializedSoundByEnum (string enumName)");
                EditorGUILayout.HelpBox("Takes the name of the Audio enum sound to be played as a string and plays it in the world", MessageType.None);

                EditorGUILayout.LabelField("public void PlayAudioPlayer (AudioPlayer player)");
                EditorGUILayout.HelpBox("Pass the name of the Audio enum sound to be played as a string", MessageType.None);

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
            #endregion  
        }
    }
}


