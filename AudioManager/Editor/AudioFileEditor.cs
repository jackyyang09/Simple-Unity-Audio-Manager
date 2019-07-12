using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioFile))]
public class AudioFileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AudioFile myScript = (AudioFile)target;

        EditorGUILayout.LabelField("The name of this gameObject will be used to refer to audio in script");

        if (myScript.GetFile() == null)
        {
            EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
        }
        if (myScript.name.Equals("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
        {
            EditorGUILayout.HelpBox("Warning! Change the name of the gameObject to something different or things will break!", MessageType.Warning);
        }

        DrawDefaultInspector();
    }
}
