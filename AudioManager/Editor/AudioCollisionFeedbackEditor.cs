using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioCollisionFeedback))]
public class AudioCollisionFeedbackEditor : Editor
{
    AudioManager am;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (am == null) am = AudioManager.GetInstance();

        AudioCollisionFeedback myScript = (AudioCollisionFeedback)target;
        
        List<string> options = new List<string>();

        options.Add("None");
        foreach (string s in am.GetSoundDictionary().Keys)
        {
            options.Add(s);
        }

        GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played on collision");

        string sound = serializedObject.FindProperty("sound").stringValue;

        if (sound.Equals("") || !options.Contains(sound)) // Default to "None"
        {
            sound = options[EditorGUILayout.Popup(soundDesc, 0, options.ToArray())];
        }
        else
        {
            sound = options[EditorGUILayout.Popup(soundDesc, options.IndexOf(sound), options.ToArray())];
        }

        serializedObject.ApplyModifiedProperties();
    }
}
