using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioParticles))]
    [CanEditMultipleObjects]
    public class AudioParticlesEditor : BaseSoundEditor
    {
        AudioParticles myScript;

        SerializedProperty playSoundOn;

        protected override void Setup()
        {
            base.Setup();

            myScript = (AudioParticles)target;

            playSoundOn = serializedObject.FindProperty("playSoundOn");
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawAudioProperty();

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