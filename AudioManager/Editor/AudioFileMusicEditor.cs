using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioFileMusic))]
public class AudioFileMusicEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AudioFileMusic myScript = (AudioFileMusic)target;

        if (myScript.GetFile()[0] == null)
        {
            EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
        }
        if (myScript.name.Equals("NEW AUDIO FILE") || myScript.name.Equals("None"))
        {
            EditorGUILayout.HelpBox("Warning! Change the name of the audio file to something different or things will break!", MessageType.Warning);
        }

        DrawDefaultInspector();
    }
}
