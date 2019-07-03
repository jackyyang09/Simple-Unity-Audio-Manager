using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioTriggerFeedback))]
public class AudioTriggerFeedbackEditor : Editor
{
    AudioManager am;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (am == null) am = AudioManager.GetInstance();

        AudioTriggerFeedback myScript = (AudioTriggerFeedback)target;
        
        List<string> options = new List<string>();

        options.Add("None");
        foreach (string s in am.GetSoundDictionary().Keys)
        {
            options.Add(s);
        }

        GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played on collision");

        if (serializedObject.FindProperty("sound").stringValue.Equals("")) // Default to "None"
        {
            serializedObject.FindProperty("sound").stringValue = options[EditorGUILayout.Popup(soundDesc, 0, options.ToArray())];
        }
        else
        {
            serializedObject.FindProperty("sound").stringValue = options[EditorGUILayout.Popup(soundDesc, options.IndexOf(serializedObject.FindProperty("sound").stringValue), options.ToArray())];
        }

        serializedObject.ApplyModifiedProperties();
    }
}
