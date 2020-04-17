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

        static bool showHowTo;

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

                content = new GUIContent("Show Advanced Settings", "Toggle this if you're an experienced Unity user");
            }
            else
            {
                content = new GUIContent("Hide Advanced Settings", "Toggle this if you're an experienced Unity user");
            }
            excludedProperties.AddRange(new List<string> { "listener", "sourcePrefab" });

            if (GUILayout.Button(content))
            {
                showAdvancedSettings = !showAdvancedSettings;
            }

            EditorGUILayout.Space();

            GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
            boldFoldout.fontStyle = FontStyle.Bold;
            content = new GUIContent("Volume Controls", "Change the volume levels of all AudioManager-controlled audio channels here");
            showVolumeSettings = EditorGUILayout.Foldout(showVolumeSettings, content, boldFoldout);
            if (showVolumeSettings)
            {
                DrawAdvancedVolumeControls(myScript);
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            EditorGUILayout.BeginHorizontal();

            GUIContent pathContent = new GUIContent("Audio Assets Folder", "This folder and all sub-folders will be searched for Audio File Objects, AudioManager-generated files will be stored in this location as well");
            string filePath = serializedObject.FindProperty("audioFolderLocation").stringValue;
            filePath = EditorGUILayout.TextField(pathContent, filePath);

            GUIContent buttonContent = new GUIContent("Browse", "Designate a new folder to store JSAM's audio files");
            if (GUILayout.Button(buttonContent, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(55) }))
            {
                string prevPath = filePath;
                filePath = EditorUtility.OpenFolderPanel("Specify folder to store JSAM's audio files", Application.dataPath, "Audio Files");
            
                if (filePath == "") filePath = prevPath; // If the user presses "cancel"
            
                // Fix path to be useable for AssetDatabase.FindAssets
                filePath = filePath.Remove(0, filePath.IndexOf("Assets/"));
                if (filePath[filePath.Length - 1] == '/') filePath = filePath.Remove(filePath.Length - 1, 1);
            }
            serializedObject.FindProperty("audioFolderLocation").stringValue = filePath;

            EditorGUILayout.EndHorizontal();

            SerializedProperty instancedEnums = serializedObject.FindProperty("instancedEnums");
            SerializedProperty wasInstancedBefore = serializedObject.FindProperty("wasInstancedBefore");
            bool usingInstancedEnums = instancedEnums.boolValue;

            if (showAdvancedSettings)
            {
                GUIContent longTent = new GUIContent("Enable Instanced Audio Enums", "By default, AudioManager assumes that your project will share all sounds and will have them all ready to be called using a single Enum list. " +
                    "However, you may also choose to have different instances of AudioManager per-scene with different Audio Files loaded in each. " +
                    "In that case, AudioManager will generate Enums specific to your scene and will be able to differentiate between them.");
                EditorGUILayout.PropertyField(instancedEnums, longTent);
            }

            if (myScript.GetListener() == null || showAdvancedSettings)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("listener"));
            }
            if (!myScript.SourcePrefabExists() || showAdvancedSettings)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourcePrefab"));
            }

            EditorGUILayout.Space();

            GUIContent blontent = new GUIContent("Re-Generate Audio Library", "Click this whenever you add new Audio Files, change the scene name, or encounter any issues");
            if (GUILayout.Button(blontent))
            {
                EditorUtility.DisplayProgressBar("Re-Generating Audio Library", "Finding audio files...", 0);
                string[] paths = new string[] { filePath };

                // Search for AudioFileObjects, music included
                string[] GUIDs = AssetDatabase.FindAssets("t:JSAM.AudioFileObject", paths);

                List<AudioFileObject> audioFiles = new List<AudioFileObject>();
                List<AudioFileMusicObject> musicFiles = new List<AudioFileMusicObject>();
                foreach (var s in GUIDs)
                {
                    AudioFileObject theObject = (AudioFileObject)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(s), typeof(AudioFileObject));

                    // Is this actually a music object?
                    if (!theObject.GetType().IsAssignableFrom(typeof(AudioFileObject)))
                    {
                        musicFiles.Add((AudioFileMusicObject)theObject);
                    } 
                    else audioFiles.Add(theObject);
                }

                if (myScript.GenerateAudioDictionarys(audioFiles, musicFiles) || wasInstancedBefore.boolValue == usingInstancedEnums)
                {
                    EditorUtility.DisplayProgressBar("Re-Generating Audio Library", "Generating audio enum file...", 0.5f);
                    string safeSceneName = GenerateEnumFile(filePath, usingInstancedEnums);
                    serializedObject.FindProperty("sceneSoundEnumName").stringValue = "JSAM.Sounds" + safeSceneName;
                    serializedObject.FindProperty("sceneMusicEnumName").stringValue = "JSAM.Music" + safeSceneName;
                    EditorUtility.DisplayProgressBar("Re-Generating Audio Library", "Done! Recompiling...", 0.95f);
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            wasInstancedBefore.boolValue = usingInstancedEnums;

            serializedObject.ApplyModifiedProperties();

            if (usingInstancedEnums)
            {
                string sceneNameSpecial = serializedObject.FindProperty("sceneSoundEnumName").stringValue;
                string sceneNameSpecialMusic = serializedObject.FindProperty("sceneMusicEnumName").stringValue;
                if (GetEnumType(sceneNameSpecial) == null)
                {
                    EditorGUILayout.HelpBox("Could not find Audio Files, did you make sure to designate the correct Audio Assets Folder? Try generating the audio library again!", MessageType.Error);
                }
                else if (ArrayUtility.IndexOf(myScript.GetSoundLibrary().ToArray(), null) > -1 || ArrayUtility.IndexOf(myScript.GetMusicLibrary().ToArray(), null) > -1)
                {
                    EditorGUILayout.HelpBox("Mismatch between Audio Files found and Audio Files registered in Audio Manager! Did you move your Audio Files? Try generating the audio library again!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("When using AudioManager in code, refer to the enums in " + sceneNameSpecial + " and " + sceneNameSpecialMusic + " for your list of available Audio Files", MessageType.Info);
                }
            }
            else
            {
                if (GetEnumType("JSAM.Sounds") != null || GetEnumType("JSAM.Music") != null)
                {
                    EditorGUILayout.HelpBox("When using AudioManager in code, refer to the enums in JSAM.Sounds and JSAM.Music for your list of available Audio Files", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Could not find Audio Files, did you make sure to designate the correct Audio Assets Folder? Try regenerating the audio library again!", MessageType.Warning);
                }
            }

            EditorGUILayout.Space();

            content = new GUIContent("Sound Library", "Library of all sounds loaded into AudioManager's Sound Dictionary. Mouse over each entry to see the full name in script.");

            showSoundLibrary = EditorGUILayout.Foldout(showSoundLibrary, content, boldFoldout);
            if (showSoundLibrary)
            {
                string enumName = serializedObject.FindProperty("sceneSoundEnumName").stringValue;

                string[] soundNames = System.Enum.GetNames(myScript.GetSceneSoundEnum());

                SerializedProperty audioFiles = serializedObject.FindProperty("audioFileObjects");

                for (int i = 0; i < audioFiles.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty ao = audioFiles.GetArrayElementAtIndex(i);
                    GUIContent sName;
                    if (soundNames.Length == audioFiles.arraySize) // Additional check to prevent editor from breaking during regeneration
                    {
                        sName = new GUIContent(soundNames[i], enumName + "." + soundNames[i]);
                    }
                    else
                    {
                        sName = new GUIContent("");
                    }
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(ao, sName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            content = new GUIContent("Music Library", "Library of all music loaded into AudioManager's Music Dictionary. Mouse over each entry to see the full name in script.");

            showMusicLibrary = EditorGUILayout.Foldout(showMusicLibrary, content, boldFoldout);
            if (showMusicLibrary)
            {
                string enumName = serializedObject.FindProperty("sceneMusicEnumName").stringValue;

                string[] musicNames = System.Enum.GetNames(myScript.GetSceneMusicEnum());

                SerializedProperty audioFiles = serializedObject.FindProperty("audioFileMusicObjects");

                for (int i = 0; i < audioFiles.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    SerializedProperty ao = audioFiles.GetArrayElementAtIndex(i);
                    GUIContent mName;
                    if (musicNames.Length == audioFiles.arraySize) // Additional check to prevent editor from breaking during regeneration
                    {
                        mName = new GUIContent(musicNames[i], enumName + "." + musicNames[i]);
                    }
                    else
                    {
                        mName = new GUIContent("");
                    }
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(ao, mName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            if (myScript.GetMasterVolume() == 0) EditorGUILayout.HelpBox("Note: Master Volume is MUTED!", MessageType.Info);
            if (myScript.GetSoundVolume() == 0) EditorGUILayout.HelpBox("Note: Sound is MUTED!", MessageType.Info);
            if (myScript.GetMusicVolume() == 0) EditorGUILayout.HelpBox("Note: Music is MUTED!", MessageType.Info);

            EditorGUILayout.Space();

            #region Quick Reference Guide
            boldFoldout.fontStyle = FontStyle.Bold;
            showHowTo = EditorGUILayout.Foldout(showHowTo, "Quick Reference Guide", boldFoldout);
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This component is the backbone of the entire JSAM Audio Manager system and ideally should occupy it's own gameobject."
                    , MessageType.None);
                EditorGUILayout.HelpBox("For detailed instructions on how to get started, check out JSAM's Github page here!"
                    , MessageType.None);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Github", new GUILayoutOption[]{ GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Remember to mouse over the settings in this and other windows to learn more about them!", MessageType.None);
                EditorGUILayout.HelpBox("Please ensure that you don't have multiple AudioManagers in one scene."
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("AudioManager works best as a global system where each scene's AudioManager draws from the same AudioFiles." +
                    " However, if you want scenes to draw from separate groups of Audio Files, you can select the option to enable instanced Audio Enums under" +
                    " AudioManager's advanced settings and regenerate the Audio Library. This let's AudioManager use it's own designated Audio Files separate to ones" +
                    " used in other scenes."
                    , MessageType.None);
            }
            #endregion  
        }

        /// <summary>
        /// Just hide the fancy loading bar
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("GameObject/Audio/Audio Manager", false, 0)]
        public static void AddAudioManager()
        {
            AudioManager existingAudioManager = FindObjectOfType<AudioManager>();
            if (!existingAudioManager)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Manager t:GameObject")[0]);
                GameObject newManager = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)));
                newManager.name = newManager.name.Replace("(Clone)", string.Empty);
                EditorGUIUtility.PingObject(newManager);
                Selection.activeGameObject = newManager;
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
        public static string GenerateEnumFile(string filePath, bool usingInstancedEnums)
        {
            string sceneName = AudioManager.instance.gameObject.scene.name;
            string safeSceneName = ConvertToAlphanumeric(sceneName);
            
            // Looking for AudioEnums
            if (usingInstancedEnums) filePath += "\\AudioEnums - " + sceneName + ".cs";
            else filePath += "\\AudioEnums.cs";

            File.WriteAllText(filePath, string.Empty);
            StreamWriter writer = new StreamWriter(filePath, true);
            writer.WriteLine("namespace JSAM {");

            if (!usingInstancedEnums)
            writer.WriteLine("    public enum Sounds {");
            else
            writer.WriteLine("    public enum Sounds" + safeSceneName + "{");

            List<AudioFileObject> soundLibrary = AudioManager.instance.GetSoundLibrary();
            if (soundLibrary != null)
            {
                if (soundLibrary.Count > 0)
                {
                    for (int i = 0; i < soundLibrary.Count - 1; i++)
                    {
                        writer.WriteLine("        " + ConvertToAlphanumeric(soundLibrary[i].name) + ",");
                    }
                    writer.WriteLine("        " + ConvertToAlphanumeric(soundLibrary[soundLibrary.Count - 1].name));
                }
            }

            writer.WriteLine("    }");

            if (!usingInstancedEnums)
                writer.WriteLine("    public enum Music {");
            else
                writer.WriteLine("    public enum Music" + safeSceneName + "{");

            List<AudioFileMusicObject> musicLibrary = AudioManager.instance.GetMusicLibrary();

            if (musicLibrary != null)
            {
                if (musicLibrary.Count > 0)
                {
                    for (int i = 0; i < musicLibrary.Count - 1; i++)
                    {
                        writer.WriteLine("        " + ConvertToAlphanumeric(musicLibrary[i].name) + ",");
                    }
                    writer.WriteLine("        " + ConvertToAlphanumeric(musicLibrary[musicLibrary.Count - 1].name));
                }
            }
            
            writer.WriteLine("    }");
            writer.WriteLine("}");
            writer.Close();

            //Re-import the file to update the reference in the editor
            AssetDatabase.ImportAsset(filePath);
            AssetDatabase.Refresh();

            return safeSceneName;
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

        #region Volume Sliders
        string masterText;
        string soundText;
        string musicText;

        /// <summary>
        /// Features many complex operations to format the UI
        /// </summary>
        /// <param name="myScript"></param>
        void DrawAdvancedVolumeControls(AudioManager myScript)
        {
            // Master Volume
            {
                Rect rect = EditorGUILayout.BeginHorizontal();
                Rect buttonRect = new Rect(rect);
                GUIContent blontent = new GUIContent("Master Volume", "All volume is set relative to the Master Volume");
                GUILayout.Label(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MaxWidth(90) });
                buttonRect.x += 95;
                buttonRect.width = 50;
                GUIContent bContent = new GUIContent(myScript.IsMasterVolumeMuted() ? "OFF" : "ON", "Turns the channel's audio output ON/OFF");
                if (GUI.Button(buttonRect, bContent))
                {
                    Undo.RecordObject(myScript, "Toggled master volume mute state");
                    myScript.MuteMasterVolume(!myScript.IsMasterVolumeMuted());
                }
                using (new EditorGUI.DisabledScope(myScript.IsMasterVolumeMuted()))
                {
                    Rect sliderRect = new Rect(rect);
                    sliderRect.xMin = buttonRect.xMax + 7.5f;
                    Rect textRect = new Rect(rect);
                    textRect.width = 50;
                    textRect.x = Mathf.Abs(textRect.width - (rect.width + 17.5f));
                    sliderRect.xMax = textRect.xMin - 5;

                    int sliderVolume = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, myScript.GetMasterVolumeAsInt(), 0f, 100f));
                    if (sliderVolume != myScript.GetMasterVolumeAsInt())
                    {
                        Undo.RecordObject(myScript, "Changed master channel volume");
                        serializedObject.FindProperty("masterVolume").floatValue = (float)sliderVolume / 100;
                        myScript.SetMasterVolume(sliderVolume);
                    }

                    GUI.SetNextControlName("textMaster");
                    masterText = GUI.TextField(textRect, masterText);
                    if (!GUI.GetNameOfFocusedControl().Equals("textMaster"))
                    {
                        masterText = myScript.GetMasterVolumeAsInt().ToString();
                    }
                    float resultF = 0;
                    if (!float.TryParse(masterText, out resultF))
                    {
                        resultF = (float)sliderVolume / 100;
                    }

                    Undo.RecordObject(myScript, "Changed master channel volume");
                    serializedObject.FindProperty("masterVolume").floatValue = resultF / 100f;
                    myScript.SetMasterVolume((int)resultF);

                    EditorGUILayout.EndHorizontal();
                }
            }

            // Sound Volume
            {
                Rect rect = EditorGUILayout.BeginHorizontal();
                Rect buttonRect = new Rect(rect);
                GUIContent blontent = new GUIContent("Sound Volume", "Controls volume of all sound channels");
                GUILayout.Label(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MaxWidth(90) });
                buttonRect.x += 95;
                buttonRect.width = 50;
                GUIContent bContent = new GUIContent(myScript.IsSoundVolumeMuted() ? "OFF" : "ON", "Turns the channel's audio output ON/OFF");
                if (GUI.Button(buttonRect, bContent))
                {
                    Undo.RecordObject(myScript, "Toggled sound volume mute state");
                    myScript.MuteSoundVolume(!myScript.IsSoundVolumeMuted());
                }
                using (new EditorGUI.DisabledScope(myScript.IsSoundVolumeMuted()))
                {
                    Rect sliderRect = new Rect(rect);
                    sliderRect.xMin = buttonRect.xMax + 7.5f;
                    Rect textRect = new Rect(rect);
                    textRect.width = 50;
                    textRect.x = Mathf.Abs(textRect.width - (rect.width + 17.5f));
                    sliderRect.xMax = textRect.xMin - 5;

                    int sliderVolume = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, myScript.GetSoundVolumeAsInt(), 0f, 100f));
                    if (sliderVolume != myScript.GetSoundVolumeAsInt())
                    {
                        Undo.RecordObject(myScript, "Changed sound channel volume");
                        serializedObject.FindProperty("soundVolume").floatValue = (float)sliderVolume / 100;
                        myScript.SetSoundVolume(sliderVolume);
                    }

                    GUI.SetNextControlName("textSound");
                    soundText = GUI.TextField(textRect, soundText);
                    if (!GUI.GetNameOfFocusedControl().Equals("textSound"))
                    {
                        soundText = myScript.GetSoundVolumeAsInt().ToString();
                    }
                    float resultF = 0;
                    if (!float.TryParse(soundText, out resultF))
                    {
                        resultF = (float)sliderVolume / 100;
                    }

                    Undo.RecordObject(myScript, "Changed sound channel volume");
                    serializedObject.FindProperty("soundVolume").floatValue = resultF / 100f;
                    myScript.SetSoundVolume((int)resultF);

                    EditorGUILayout.EndHorizontal();
                }
            }

            // Music Volume
            {
                Rect rect = EditorGUILayout.BeginHorizontal();
                Rect buttonRect = new Rect(rect);
                GUIContent blontent = new GUIContent("Music Volume", "Controls volume of music channels");
                GUILayout.Label(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.MaxWidth(90) });
                buttonRect.x += 95;
                buttonRect.width = 50;
                GUIContent bContent = new GUIContent(myScript.IsMusicVolumeMuted() ? "OFF" : "ON", "Turns the channel's audio output ON/OFF");
                if (GUI.Button(buttonRect, bContent))
                {
                    Undo.RecordObject(myScript, "Toggled music volume mute state");
                    myScript.MuteMusicVolume(!myScript.IsMusicVolumeMuted());
                }
                using (new EditorGUI.DisabledScope(myScript.IsMusicVolumeMuted()))
                {
                    Rect sliderRect = new Rect(rect);
                    sliderRect.xMin = buttonRect.xMax + 7.5f;
                    Rect textRect = new Rect(rect);
                    textRect.width = 50;
                    textRect.x = Mathf.Abs(textRect.width - (rect.width + 17.5f));
                    sliderRect.xMax = textRect.xMin - 5;

                    int sliderVolume = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, myScript.GetMusicVolumeAsInt(), 0f, 100f));
                    if (sliderVolume != myScript.GetMusicVolumeAsInt())
                    {
                        Undo.RecordObject(myScript, "Changed music channel volume");
                        serializedObject.FindProperty("musicVolume").floatValue = (float)sliderVolume / 100;
                        myScript.SetMusicVolume(sliderVolume);
                    }

                    GUI.SetNextControlName("textMusic");
                    musicText = GUI.TextField(textRect, musicText);
                    if (!GUI.GetNameOfFocusedControl().Equals("textMusic"))
                    {
                        musicText = myScript.GetMusicVolumeAsInt().ToString();
                    }
                    float resultF = 0;
                    if (!float.TryParse(musicText, out resultF))
                    {
                        resultF = (float)sliderVolume / 100;
                    }

                    Undo.RecordObject(myScript, "Changed music channel volume");
                    serializedObject.FindProperty("musicVolume").floatValue = resultF / 100f;
                    myScript.SetMusicVolume((int)resultF);

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        //
        //bool EnterKeyPressed()
        //{
        //    return (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
        //}
        #endregion

        /// <summary>
        /// Returns an enum type given it's name as a string
        /// https://stackoverflow.com/questions/25404237/how-to-get-enum-type-by-specifying-its-name-in-string
        /// </summary>
        /// <param name="enumName"></param>
        /// <returns></returns>
        public static System.Type GetEnumType(string enumName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(enumName);
                if (type == null)
                    continue;
                if (type.IsEnum)
                    return type;
            }
            return null;
        }
    }
}