using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioCollisionFeedback))]
    [CanEditMultipleObjects]
    public class AudioCollisionFeedbackEditor : BaseSoundEditor
    {
        AudioCollisionFeedback myScript;

        SerializedProperty collidesWithProperty;
        SerializedProperty triggerEventProperty;

        protected override void Setup()
        {
            base.Setup();

            myScript = (AudioCollisionFeedback)target;

            collidesWithProperty = serializedObject.FindProperty("collidesWith");
            triggerEventProperty = serializedObject.FindProperty("triggerEvent");
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawAudioProperty();

            EditorGUILayout.PropertyField(collidesWithProperty);
            EditorGUILayout.PropertyField(triggerEventProperty);

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }

            DrawQuickReferenceGuide();
        }

        protected override void DrawQuickReferenceGuide()
        {
            base.DrawQuickReferenceGuide();

            if (!showHowTo) return;

            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This component is meant to be attached to a physics-enabled object." +
                " When something collides with that physics-enabled object, this component will play a sound." +
                " You can choose to change the collision event that plays the sound by using the Collision Event drop-down." +
                " You can select a sound to play using the drop-down at the top of the component."
                , MessageType.None);
            EditorGUILayout.HelpBox("This component should be placed on the same GameObject that holds the physics object's Rigidbody"
                , MessageType.None);
            EditorGUILayout.HelpBox("AudioCollisionFeedback responds to both 2D and 3D collision events."
                , MessageType.None);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sometimes you want your object to produce different sounds when colliding with different things" +
                " (ie. walking on different surfaces) so you can specify different collision layers for this component to react to under" +
                " the Collision Settings field. "
                , MessageType.None);
            EditorGUILayout.HelpBox("Feel free to use multiple different AudioCollisionFeedback components on the same GameObject!"
                , MessageType.None);

            EditorCompatability.EndSpecialFoldoutGroup();
        }
    }
}