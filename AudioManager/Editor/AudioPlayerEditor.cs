using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioPlayer))]
public class AudioPlayerEditor : Editor
{
    AudioManager am;

    public override void OnInspectorGUI()
    {
        if (am == null) am = AudioManager.instance;

        AudioPlayer myScript = (AudioPlayer)target;

        List<string> options = new List<string>();

        options.Add("None");
        foreach (string s in am.GetSoundDictionary().Keys)
        {
            options.Add(s);
        }

        string sound = serializedObject.FindProperty("sound").stringValue;

        if (sound == "None" && myScript.GetAttachedSound() == null)
        {
            EditorGUILayout.HelpBox("Choose a sound to play before running!", MessageType.Error);
        }

        DrawDefaultInspector();

        GUIContent soundDesc = new GUIContent("Sound", "Sound that will be played");

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
