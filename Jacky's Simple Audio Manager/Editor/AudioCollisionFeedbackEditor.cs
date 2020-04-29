using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioCollisionFeedback))]
    [CanEditMultipleObjects]
    public class AudioCollisionFeedbackEditor : Editor
    {
        static bool showAudioClipSettings = false;
        static bool showHowTo;

        public override void OnInspectorGUI()
        {
            AudioCollisionFeedback myScript = (AudioCollisionFeedback)target;

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

            GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played on collision");

            using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() != null))
            {
                serializedObject.FindProperty("sound").intValue = EditorGUILayout.Popup(soundDesc, sound, options.ToArray());
            }

            GUIContent fileText = new GUIContent("Custom AudioClip", "Overrides the \"Sound\" parameter with an AudioClip if not null");
            SerializedProperty customSound = serializedObject.FindProperty("soundFile");
            EditorGUILayout.ObjectField(customSound, fileText);

            EditorGUILayout.Space();

            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;

            GUIContent fontent = new GUIContent("Custom AudioClip Settings", "These settings only apply if you input your own custom AudioClip rather than choosing from the generated Audio Library");
            if (myScript.GetAttachedSound() == null)
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent);
            else
                showAudioClipSettings = EditorGUILayout.Foldout(showAudioClipSettings, fontent, boldFoldout);
            if (showAudioClipSettings)
            {
                using (new EditorGUI.DisabledScope(myScript.GetAttachedSound() == null))
                {
                    DrawPropertiesExcluding(serializedObject, new[] { "m_Script", "soundFile", "playOnStart", "playOnEnable",
                        "stopOnDisable", "stopOnDestroy", "collidesWith", "triggerEvent"});
                }
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collidesWith"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerEvent"));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This component is meant to be attached to a physics-enabled object." +
                    " When something collides with that physics-enabled object, this component will play a sound." +
                    " You can choose to change the collision event that plays the sound by using the Collision Event dropdown." +
                    " You can select a sound to play using the dropdown at the top of the component."
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
            }
        }
    }
}