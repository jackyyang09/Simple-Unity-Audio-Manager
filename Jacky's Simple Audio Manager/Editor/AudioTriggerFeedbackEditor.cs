using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioTriggerFeedback))]
    [CanEditMultipleObjects]
    public class AudioTriggerFeedbackEditor : BaseAudioEditor
    {
        AudioTriggerFeedback myScript;

        SerializedProperty sound;
        SerializedProperty soundFile;
        SerializedProperty triggersWith;
        SerializedProperty triggerEvent;

        protected override void Setup()
        {
            myScript = (AudioTriggerFeedback)target;

            sound = serializedObject.FindProperty("sound");
            soundFile = serializedObject.FindProperty("soundFile");
            triggersWith = serializedObject.FindProperty("triggersWith");
            triggerEvent = serializedObject.FindProperty("triggerEvent");
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawSoundDropdown(sound);
            
            EditorGUILayout.PropertyField(triggersWith);
            EditorGUILayout.PropertyField(triggerEvent);

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
                " When something intersects with that physics-enabled object, this component will play a sound." +
                " You can choose to change the trigger event that plays the sound by using the Trigger Event drop-down." +
                " You can select a sound to play using the drop-down at the top of the component."
                , MessageType.None);
            EditorGUILayout.HelpBox("This component should be placed on the same GameObject that holds the physics object's Rigidbody"
                , MessageType.None);
            EditorGUILayout.HelpBox("AudioTriggerFeedback responds to both 2D and 3D trigger events."
                , MessageType.None);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sometimes you want your object to produce different sounds when intersecting different things" +
                " so you can specify different collision layers for this component to react to under" +
                " the Trigger Settings field. "
                , MessageType.None);
            EditorGUILayout.HelpBox("Feel free to use multiple different AudioTriggerFeedback components on the same GameObject!"
                , MessageType.None);

            EditorCompatability.EndSpecialFoldoutGroup();
        }
    }
}