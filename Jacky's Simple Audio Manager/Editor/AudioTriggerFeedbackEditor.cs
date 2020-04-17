using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioTriggerFeedback))]
    [CanEditMultipleObjects]
    public class AudioTriggerFeedbackEditor : Editor
    {
        static bool showHowTo;

        public override void OnInspectorGUI()
        {
            AudioTriggerFeedback myScript = (AudioTriggerFeedback)target;

            List<string> options = new List<string>();

            System.Type enumType = AudioManager.instance.GetSceneSoundEnum();
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

            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            int sound = serializedObject.FindProperty("sound").intValue;

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played on intersection with another collider");

            using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() != null))
            {
                serializedObject.FindProperty("sound").intValue = EditorGUILayout.Popup(soundDesc, sound, options.ToArray());
            }

            GUIContent fileText = new GUIContent("Custom AudioClip", "Overrides the \"Sound\" parameter with an AudioClip if not null");
            SerializedProperty customSound = serializedObject.FindProperty("soundFile");
            EditorGUILayout.ObjectField(customSound, fileText);

            DrawPropertiesExcluding(serializedObject, new[] { "m_Script" });

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            GUIStyle boldFoldout = EditorStyles.foldout;
            boldFoldout.fontStyle = FontStyle.Bold;
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This component is meant to be attached to a physics-enabled object." +
                    " When something intersects with that physics-enabled object, this component will play a sound." +
                    " You can choose to change the trigger event that plays the sound by using the Trigger Event dropdown." +
                    " You can select a sound to play using the dropdown at the top of the component."
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
            }
        }
    }
}