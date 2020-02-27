using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioFile))]
    [CanEditMultipleObjects]
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

            EditorGUILayout.Space();

            GUIContent blontent = new GUIContent("Use Library", "If true, the single AudioFile will be changed to a list of AudioFiles. AudioManager will choose a random AudioClip from this list when you playback this sound");
            bool oldValue = myScript.useLibrary;
            bool newValue = EditorGUILayout.Toggle(blontent, oldValue);
            if (newValue != oldValue) // If you clicked the toggle
            {
                if (newValue)
                {
                    if (myScript.files.Count == 0)
                    {
                        myScript.files.Add(myScript.file);
                    }
                    else if (myScript.files.Count == 1)
                    {
                        if (myScript.files[0] == null)
                        {
                            myScript.files[0] = myScript.file;
                        }
                    }
                }
                myScript.useLibrary = newValue;
            }

            string[] excludedProperties = new string[] { "m_Script", "files" };

            if (myScript.UsingLibrary()) // Swap file with files
            {
                excludedProperties[1] = "file";
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties);

            serializedObject.ApplyModifiedProperties();
        }
    }
}