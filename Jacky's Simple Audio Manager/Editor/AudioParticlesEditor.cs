using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioParticles))]
    [CanEditMultipleObjects]
    public class AudioParticlesEditor : BaseAudioEditor
    {
        AudioParticles myScript;

        SerializedProperty sound;
        SerializedProperty soundFile;
        SerializedProperty playSoundOn;

        protected override void Setup()
        {
            myScript = (AudioParticles)target;

            sound = serializedObject.FindProperty("sound");
            soundFile = serializedObject.FindProperty("soundFile");
            playSoundOn = serializedObject.FindProperty("playSoundOn");
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawSoundDropdown(sound);

            EditorGUILayout.PropertyField(playSoundOn);

            serializedObject.ApplyModifiedProperties();

            DrawQuickReferenceGuide();
        }

        protected override void DrawQuickReferenceGuide()
        {
            base.DrawQuickReferenceGuide();

            if (!showHowTo) return;

            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This component is meant to be attached to a Particle System." +
                " When a particle is created or is destroyed, AudioParticles will play a sound."
                , MessageType.None);
            EditorGUILayout.HelpBox("This component should be placed on the same GameObject that holds the Particle System."
                , MessageType.None);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Feel free to use multiple different AudioParticles components on the same GameObject so your" +
                " Particle System plays sounds on both instantiation and destruction!"
                , MessageType.None);

            EditorCompatability.EndSpecialFoldoutGroup();
        }
    }
}