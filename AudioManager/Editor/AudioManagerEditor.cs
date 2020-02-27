using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioManager myScript = (AudioManager)target;

            string editorMessage = myScript.GetEditorMessage();
            if (editorMessage != "")
            {
                EditorGUILayout.HelpBox(editorMessage, MessageType.Info);
            }

            List<string> excludedProperties = new List<string> { "m_Script" };

            GUIContent content;

            if (!showAdvancedSettings)
            {
                excludedProperties.AddRange(new List<string>
                {
                    "m_Script", "audioSources",
                    "spatializeLateUpdate", "timeScaledSounds",
                    "stopSoundsOnSceneLoad", "dontDestroyOnLoad",
                    "dynamicSourceAllocation", "disableConsoleLogs"
                });
                if (myScript.GetListener() != null) excludedProperties.Add("listener");
                if (myScript.SourcePrefabExists()) excludedProperties.Add("sourcePrefab");

                content = new GUIContent("Show Advanced Settings", "Toggle this if you're an experienced Unity user");
            }
            else
            {
                content = new GUIContent("Hide Advanced Settings", "Toggle this if you're an experienced Unity user");
            }

            //showAdvancedSettings = EditorGUILayout.Toggle(content, showAdvancedSettings);
            if (GUILayout.Button(content))
            {
                showAdvancedSettings = !showAdvancedSettings;
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

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

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


        [MenuItem("Tools/Add New AudioManager")]
        public static void AddAudioManager()
        {
            AudioManager existingAudioManager = FindObjectOfType<AudioManager>();
            if (!existingAudioManager)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("AudioChannel")[0]);
                assetPath = assetPath.Replace("Channel", "Manager");
                GameObject newManager = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)));
                newManager.name = newManager.name.Replace("(Clone)", string.Empty);
            }
            else
            {
                EditorGUIUtility.PingObject(existingAudioManager);
                Debug.Log("AudioManager already exists in this scene!");
            }
        }

        #region Per-Scene Enum Generation
        /// <summary>
        /// With help from Daniel Robledo
        /// https://support.unity3d.com/hc/en-us/articles/115000341143-How-do-I-read-and-write-data-from-a-text-file-
        /// </summary>
        //[MenuItem("Tools/Generate Enum File")] //Only used for debugging
        public static void GenerateEnumFile()
        {
            // Looking for AudioEnums
            string assetPath = Directory.GetDirectories(Directory.GetCurrentDirectory(), "audioe*", SearchOption.AllDirectories)[0];
            assetPath += "\\AudioEnums - " + AudioManager.instance.gameObject.scene.name + ".cs";
            Debug.Log(assetPath);

            File.WriteAllText(assetPath, string.Empty);
            StreamWriter writer = new StreamWriter(assetPath, true);
            writer.WriteLine("namespace JSAM {");
            writer.WriteLine("    public enum Sounds {");
            string[] dict = new string[AudioManager.instance.GetSoundDictionary().Count];
            AudioManager.instance.GetSoundDictionary().Keys.CopyTo(dict, 0);
            if (dict.Length > 0)
            {
                for (int i = 0; i < dict.Length - 1; i++)
                {
                    writer.WriteLine("        " + ConvertToAlphanumeric(dict[i]) + ",");
                }
                writer.WriteLine("        " + ConvertToAlphanumeric(dict[dict.Length - 1]));
            }

            dict = new string[AudioManager.instance.GetMusicDictionary().Count];
            AudioManager.instance.GetMusicDictionary().Keys.CopyTo(dict, 0);
            if (dict.Length > 0)
            {
                for (int i = 0; i < dict.Length - 1; i++)
                {
                    writer.WriteLine("        " + ConvertToAlphanumeric(dict[i]) + ",");
                }
                writer.WriteLine("        " + ConvertToAlphanumeric(dict[dict.Length - 1]));
            }

            writer.WriteLine("    }");
            writer.WriteLine("    public enum Music {");
            writer.WriteLine("    }");
            writer.WriteLine("}");
            writer.Close();

            //Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Helpful method by Stack Overflow user ata
        /// https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        static string ConvertToAlphanumeric(string input)
        {
            char[] arr = input.ToCharArray();

            arr = System.Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c)
                                              || char.IsWhiteSpace(c)
                                              || c == '-')));
            return new string(arr);
        }
        #endregion

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