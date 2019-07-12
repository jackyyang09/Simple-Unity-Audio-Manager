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

        EditorGUILayout.LabelField("The name of this gameObject will be used to refer to audio in script");

        if (myScript.GetFile()[0] == null)
        {
            EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
        }
        if (myScript.name.Equals("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
        {
            EditorGUILayout.HelpBox("Warning! Change the name of the audio file to something different or things will break!", MessageType.Warning);
        }

        DrawDefaultInspector();
    }
}
