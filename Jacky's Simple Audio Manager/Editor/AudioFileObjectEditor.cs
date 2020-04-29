using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM
{
    [CustomEditor(typeof(AudioFileObject))]
    [CanEditMultipleObjects]
    public class AudioFileObjectEditor : Editor
    {
        static bool showHowTo;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioFileObject myScript = (AudioFileObject)target;

            EditorGUILayout.LabelField("Audio File Object", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Name: " + AudioManagerEditor.ConvertToAlphanumeric(myScript.name));

            EditorGUILayout.HelpBox("The name that AudioManager will use to reference this object with.", MessageType.None);

            if (myScript.GetFile() == null && myScript.IsLibraryEmpty())
            {
                EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
            }
            if (myScript.name.Contains("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
            {
                EditorGUILayout.HelpBox("Warning! Change the name of the gameObject to something different or things will break!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            GUIContent blontent = new GUIContent("Use Library", "If true, the single AudioFile will be changed to a list of AudioFiles. AudioManager will choose a random AudioClip from this list when you play this sound");
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
                else
                {
                    if (myScript.files.Count > 0 && myScript.file == null)
                    {
                        myScript.file = myScript.files[0];
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

            EditorGUILayout.Space();

            #region Quick Reference Guide
            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Audio File Objects are a container that hold your sound files to be read by Audio Manager."
                    , MessageType.None);
                EditorGUILayout.HelpBox("No matter the filename or folder location, this Audio File will be referred to as it's name above" 
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("If your one sound has many different variations available, try enabling the \"Use Library\" option " +
                    "just below the name field. This let's AudioManager play a random different sound whenever you choose to play from this audio file object."
                    , MessageType.None);
            }
            #endregion  
        }
    }
}