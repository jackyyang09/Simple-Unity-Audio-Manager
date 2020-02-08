using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace JSAM
{
    /// <summary>
    /// Thank god to brownboot67 for his advice
    /// https://forum.unity.com/threads/custom-editor-not-saving-changes.424675/
    /// </summary>
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : Editor
    {
        static bool showVolumeSettings = true;
        static bool showAdvancedSettings;
        static bool showSoundLibrary;
        static bool showMusicLibrary;

        [MenuItem("Tools/Add New AudioManager")]
        public static void AddAudioManager()
        {
            if (!AudioManager.instance)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("AudioChannel")[0]);
                assetPath = assetPath.Replace("Channel", "Manager");
                GameObject newManager = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)));
                newManager.name = newManager.name.Replace("(Clone)", string.Empty);
            }
            else
            {
                Debug.Log("AudioManager already exists in this scene!");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioManager myScript = (AudioManager)target;

            string editorMessage = myScript.GetEditorMessage();
            if (editorMessage != "")
            {
                EditorGUILayout.HelpBox(editorMessage, MessageType.Info);
            }

            string[] excludedProperties = new string[] { "m_Script" };

            GUIContent content = new GUIContent("Show Advanced Settings", "Toggle this if you're an experienced Unity user");

            showAdvancedSettings = EditorGUILayout.Toggle(content, showAdvancedSettings);

            if (!showAdvancedSettings)
            {
                excludedProperties = new string[9]
                {
                    "m_Script", "audioSources", "spatialSound",
                    "spatializeLateUpdate", "timeScaledSounds",
                    "stopSoundsOnSceneLoad", "dontDestroyOnLoad",
                    "dynamicSourceAllocation", "disableConsoleLogs"
                };
            }

            //content = new GUIContent("Volume Controls", "Volume of all Audio managed by AudioManager controlled here");

            //showVolumeSettings = EditorGUILayout.Foldout(showVolumeSettings, content);
            //if (!showVolumeSettings)
            //{
            //    excludedProperties = new string[4]
            //    {
            //        "m_Script", "masterVolume", "musicVolume", "soundVolume"
            //    };
            //}

            DrawPropertiesExcluding(serializedObject, excludedProperties);

            List<string> options = new List<string>();

            options.Add("None");
            foreach (string s in myScript.GetMusicDictionary().Keys)
            {
                options.Add(s);
            }

            // Potentially Deprecated
            //GUIContent content = new GUIContent("Current Track", "Current music that's playing, will play on start if not \"None\"");
            //
            //string currentTrack = serializedObject.FindProperty("currentTrack").stringValue;
            //
            //if (currentTrack.Equals("") || !options.Contains(currentTrack)) // Default to "None"
            //{
            //    currentTrack = options[EditorGUILayout.Popup(content, 0, options.ToArray())];
            //}
            //else
            //{
            //    currentTrack = options[EditorGUILayout.Popup(content, options.IndexOf(currentTrack), options.ToArray())];
            //}
            //
            //serializedObject.FindProperty("currentTrack").stringValue = currentTrack;

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("Re-Generate Audio Library"))
            {
                myScript.GenerateAudioDictionarys();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Sound File"))
            {
                GameObject newSound = new GameObject("NEW AUDIO FILE (RENAME ME)", typeof(AudioFile));
                newSound.transform.parent = myScript.transform.GetChild(0);

                Selection.activeGameObject = newSound;
                //EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
                EditorApplication.delayCall += () => EngageRenameMode(newSound);

                Undo.RegisterCreatedObjectUndo(newSound, "Added new sound file");
            }

            if (GUILayout.Button("Add New Music File"))
            {
                GameObject newMusic = new GameObject("NEW AUDIO FILE (RENAME ME)", typeof(AudioFileMusic));
                newMusic.transform.parent = myScript.transform.GetChild(1);

                Selection.activeGameObject = newMusic;
                EditorApplication.delayCall += () => EngageRenameMode(newMusic);

                Undo.RegisterCreatedObjectUndo(newMusic, "Added new music file");
            }
            EditorGUILayout.EndHorizontal();

            content = new GUIContent("Sound Library", "Library of all sounds loaded into AudioManager's Sound Dictionary");

            showSoundLibrary = EditorGUILayout.Foldout(showSoundLibrary, content);
            if (showSoundLibrary)
            {
                if (myScript.GetSoundDictionary().Count > 0)
                {
                    string list = "";
                    foreach (string s in myScript.GetSoundDictionary().Keys)
                    {
                        if (list == "") list = s;
                        else list += "\n" + s;
                    }
                    EditorGUILayout.HelpBox(list, MessageType.None);
                }
            }

            content = new GUIContent("Music Library", "Library of all music loaded into AudioManager's Music Dictionary");

            showMusicLibrary = EditorGUILayout.Foldout(showMusicLibrary, content);
            if (showMusicLibrary)
            {
                if (myScript.GetMusicDictionary().Count > 0)
                {
                    string musiks = "";
                    foreach (string m in myScript.GetMusicDictionary().Keys)
                    {
                        if (musiks == "") musiks = m;
                        else musiks += "\n" + m;
                    }
                    EditorGUILayout.HelpBox(musiks, MessageType.None);
                }
            }

            if (myScript.GetMasterVolume() == 0) EditorGUILayout.HelpBox("Note: Master Volume is MUTED!", MessageType.Info);
            if (myScript.GetSoundVolume() == 0) EditorGUILayout.HelpBox("Note: Sound is MUTED!", MessageType.Info);
            if (myScript.GetMusicVolume() == 0) EditorGUILayout.HelpBox("Note: Music is MUTED!", MessageType.Info);
        }

        #region GameObject Rename Code
        /// <summary>
        /// Below code referenced by the lovely Unity Answers user vexe
        /// https://answers.unity.com/questions/644608/sending-a-rename-commandevent-to-the-hiearchy-almo.html
        /// </summary>
        /// <param name="go">The gameObject to rename</param>
        public static void EngageRenameMode(Object go)
        {
            SelectObject(go);
            GetFocusedWindow("Hierarchy").SendEvent(Events.Rename);
        }
        public static void SelectObject(Object obj)
        {
            Selection.objects = new Object[] { obj };
        }
        public static EditorWindow GetFocusedWindow(string window)
        {
            FocusOnWindow(window);
            return EditorWindow.focusedWindow;
        }
        public static void FocusOnWindow(string window)
        {
            EditorApplication.ExecuteMenuItem("Window/General/" + window);
        }
        public static class Events
        {
            public static Event Rename = new Event() { keyCode = KeyCode.F2, type = EventType.KeyDown };
        }
        #endregion
    }
}