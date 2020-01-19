using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioFile))]
public class AudioFileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        AudioFile myScript = (AudioFile)target;

        EditorGUILayout.HelpBox("The name of this gameObject will be used to refer to audio in script", MessageType.None);

        if (myScript.GetFile() == null)
        {
            EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
        }
        if (myScript.name.Contains("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
        {
            EditorGUILayout.HelpBox("Warning! Change the name of the gameObject to something different or things will break!", MessageType.Warning);
        }

        string[] excludedProperties = new string[2] { "m_Script", "files" };

        if (myScript.UsingLibrary()) // Swap file with files
        {
            excludedProperties[1] = "file";
        }

        DrawPropertiesExcluding(serializedObject, excludedProperties);

        EditorGUILayout.HelpBox("Use this option if you want to have this sound correspond to multiple different variant sounds", MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }
}
