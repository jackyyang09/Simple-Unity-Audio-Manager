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
        AudioTriggerFeedback myScript;
        List<string> options = new List<string>();
        System.Type enumType = null;

        SerializedProperty sound;
        SerializedProperty soundFile;
        SerializedProperty triggersWith;
        SerializedProperty triggerEvent;

        static bool showHowTo;

        private void OnEnable()
        {
            myScript = (AudioTriggerFeedback)target;

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
            triggersWith = serializedObject.FindProperty("triggersWith");
            triggerEvent = serializedObject.FindProperty("triggerEvent");
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

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played on intersection with another collider");

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
                    EditorGUILayout.Popup(soundDesc, selected, new string[] { "<None>" });
                }
            }
            
            EditorGUILayout.PropertyField(triggersWith);
            EditorGUILayout.PropertyField(triggerEvent);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

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
            }
            EditorCompatability.EndSpecialFoldoutGroup();
        }
    }
}