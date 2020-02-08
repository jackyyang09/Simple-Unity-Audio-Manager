using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
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

<<<<<<< HEAD
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("If true, the single AudioFile will be changed to a list of AudioFiles. AudioManager will choose a random AudioClip from this list when you playback this sound", MessageType.None);
        bool oldValue = myScript.useLibrary;
        bool newValue = EditorGUILayout.Toggle("Use Library", oldValue);
        if (newValue != oldValue) // If you clicked the toggle
        {
            if (newValue)
            {
                if (myScript.files.Count == 0)
                {
                    myScript.files.Add(myScript.file);
                }
            }
            myScript.useLibrary = newValue;
        }

        string[] excludedProperties = new string[2] { "m_Script", "files" };
=======
            string[] excludedProperties = new string[2] { "m_Script", "files" };
>>>>>>> ffad8f2e20ce18cefdd768d3cad36e1923868b17

            if (myScript.UsingLibrary()) // Swap file with files
            {
                excludedProperties[1] = "file";
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties);

            EditorGUILayout.HelpBox("Use this option if you want to have this sound correspond to multiple different variant sounds", MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}