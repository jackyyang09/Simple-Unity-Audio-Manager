using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioParticles))]
public class AudioParticlesEditor : Editor
{
    AudioManager am;

    public override void OnInspectorGUI()
    {
        if (am == null) am = AudioManager.GetInstance();

        AudioParticles myScript = (AudioParticles)target;

        List<string> options = new List<string>();

        options.Add("None");
        foreach (string s in am.GetSoundDictionary().Keys)
        {
            options.Add(s);
        }

        string sound = serializedObject.FindProperty("sound").stringValue;

        if (sound == "None")
        {
            EditorGUILayout.HelpBox("Choose a sound to play before running!", MessageType.Error);
        }

        DrawDefaultInspector();

        GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played when particles spawn/die");

        if (sound.Equals("") || !options.Contains(sound)) // Default to "None"
        {
            sound = options[EditorGUILayout.Popup(soundDesc, 0, options.ToArray())];
        }
        else
        {
            sound = options[EditorGUILayout.Popup(soundDesc, options.IndexOf(sound), options.ToArray())];
        }

        serializedObject.FindProperty("sound").stringValue = sound;

        serializedObject.ApplyModifiedProperties();
    }
}
