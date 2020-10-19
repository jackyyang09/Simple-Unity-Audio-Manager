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
        struct SPandName
        {
            public SerializedProperty sp;
            public string name;
        }

        AudioManager myScript;

        SerializedProperty audioFolderLocation;
        SerializedProperty masterVolume;
        SerializedProperty musicVolume;
        SerializedProperty soundVolume;
        SerializedProperty instancedEnums;
        SerializedProperty wasInstancedBefore;
        SerializedProperty listener;
        SerializedProperty sourcePrefab;
        SerializedProperty sceneSoundEnumName;
        SerializedProperty sceneMusicEnumName;
        SerializedProperty audioFileProperty;
        SerializedProperty audioFileMusicProperty;

        static bool showVolumeSettings = true;
        static bool showAdvancedSettings;
        static bool showSoundLibrary;
        static bool showMusicLibrary;

        static bool showHowTo;

        static Dictionary<string, bool> categories = new Dictionary<string, bool>();
        static Dictionary<string, bool> categoriesMusic = new Dictionary<string, bool>();

        static GUIContent copyIcon;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

                    
            List<string> excludedProperties = new List<string> { "m_Script", "audioFileObjects", "audioFileMusicObjects" };

            GUIContent content;

            if (!showAdvancedSettings)
            {
                excludedProperties.AddRange(new List<string>
                {
                    "m_Script", "audioSources",
                    "spatializeLateUpdate", "timeScaledSounds",
                    "stopOnSceneLoad", "dontDestroyOnLoad",
                    "dynamicSourceAllocation", "disableConsoleLogs"
                });

                content = new GUIContent("↓ Show Advanced Settings ↓", "Toggle this if you're an experienced Unity user");
            }
            else
            {
                content = new GUIContent("↑ Hide Advanced Settings ↑", "Toggle this if you're an experienced Unity user");
            }
            excludedProperties.AddRange(new List<string> { "listener", "sourcePrefab" });

            if (GUILayout.Button(content))
            {
                showAdvancedSettings = !showAdvancedSettings;
            }

            EditorGUILayout.Space();

            content = new GUIContent("Volume Controls", "Change the volume levels of all AudioManager-controlled audio channels here");
            showVolumeSettings = EditorCompatability.SpecialFoldouts(showVolumeSettings, content);
            if (showVolumeSettings)
            {
                DrawAdvancedVolumeControls(myScript);
            }
            EditorCompatability.EndSpecialFoldoutGroup();

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            #region Folder Browser
            EditorGUILayout.BeginHorizontal();

            GUIContent pathContent = new GUIContent("Audio Assets Folder", "This folder and all sub-folders will be searched for Audio File Objects, AudioManager-generated files will be stored in this location as well");
            string filePath = audioFolderLocation.stringValue;
            string prevPath = filePath;

            filePath = EditorGUILayout.TextField(pathContent, filePath);

            GUIContent buttonContent = new GUIContent("Browse", "Designate a new folder to store JSAM's audio files");
            if (GUILayout.Button(buttonContent, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(55) }))
            {
                filePath = EditorUtility.OpenFolderPanel("Specify folder to store JSAM's audio files", Application.dataPath, "Audio Files");

                // If the user presses "cancel"
                if (filePath == "")
                {
                    filePath = prevPath;
                }
                // or specifies something outside of this folder, reset filePath and don't proceed
                else if (!filePath.Contains("Assets/"))
                {
                    EditorUtility.DisplayDialog("Folder Browsing Error!", "AudioManager is a Unity editor tool and can only " +
                        "function inside the project's Assets folder. Please choose a different folder.", "OK");
                    filePath = prevPath;
                }
                else
                {
                    // Fix path to be usable for AssetDatabase.FindAssets
                    filePath = filePath.Remove(0, filePath.IndexOf("Assets/"));
                    if (filePath[filePath.Length - 1] == '/') filePath = filePath.Remove(filePath.Length - 1, 1);
                }
            }
            audioFolderLocation.stringValue = filePath;

            EditorGUILayout.EndHorizontal();
            if (filePath == "" || filePath == "Assets")
            {
                EditorGUILayout.HelpBox("You may want to consider setting your Audio Assets Folder to a more narrow path and " +
                    "saving your Audio File Objects there. Otherwise, AudioManager will search your entire project for Audio " +
                    "Files and enums and it can lead to false positives. Consider putting your files in \"Assets/Audio Files\"",
                    MessageType.Warning);
            }
            #endregion

            bool usingInstancedEnums = instancedEnums.boolValue;

            if (showAdvancedSettings)
            {
                GUIContent longTent = new GUIContent("Instanced Audio Enums", "By default, AudioManager assumes that your project will share all sounds and will have them all ready to be called using a single Enum list. " +
                    "However, you may also choose to have different instances of AudioManager per-scene with different Audio Files loaded in each. " +
                    "In that case, enabling this option will make AudioManager generate Enums specific to your scene and will allow it to differentiate between them.");
                EditorGUILayout.PropertyField(instancedEnums, longTent);
            }

            if (myScript.GetListenerInternal() == null || showAdvancedSettings)
            {
                EditorGUILayout.PropertyField(listener);
            }

            #region Source Prefab Helper
            if (!myScript.SourcePrefabExists())
            {
                EditorGUILayout.PropertyField(sourcePrefab);

                EditorGUILayout.HelpBox("Reference to Source Prefab is missing! This prefab is required to make " +
                        "AudioManager function. Click the button below to have AudioManager reapply the default reference.", MessageType.Warning);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reapply Default AudioSource Prefab"))
                {
                    string[] GUIDs = AssetDatabase.FindAssets("Audio Channel t:GameObject");

                    GameObject fallback = null;

                    foreach (string s in GUIDs)
                    {
                        GameObject theObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(s), typeof(GameObject)) as GameObject;
                        if (theObject.GetComponent<AudioSource>())
                        {
                            fallback = theObject;
                            break;
                        }
                    }
                    if (fallback != null) // Check has succeeded in finding the default reference
                    {
                        sourcePrefab.objectReferenceValue = fallback;
                    }
                    else // Check has failed to turn up results
                    {
                        GameObject newPrefab = new GameObject("Audio Channel");
                        AudioSource theSource = newPrefab.AddComponent<AudioSource>();
                        theSource.rolloffMode = AudioRolloffMode.Logarithmic;
                        theSource.minDistance = 0.5f;
                        theSource.maxDistance = 7;
                        newPrefab.AddComponent<AudioChannelHelper>();

                        // Look for AudioManager so we can put the new prefab next to it
                        string assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Audio Manager t:GameObject")[0]);
                        assetPath = assetPath.Substring(0, assetPath.LastIndexOf("/") + 1);
                        assetPath += "Audio Channel.prefab";
                        bool success = false;
                        PrefabUtility.SaveAsPrefabAsset(newPrefab, assetPath, out success);
                        if (success)
                        {
                            sourcePrefab.objectReferenceValue = newPrefab;
                            EditorUtility.DisplayDialog("Success", "AudioManager's default source prefab was missing. So a new one was recreated in it's place. " +
                                "If AudioManager doesn't immediately update with the Audio Source prefab in place, click the button again or recompile your code.", "OK");
                        }
                        DestroyImmediate(newPrefab);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else if (myScript.SourcePrefabExists() && showAdvancedSettings)
            {
                EditorGUILayout.PropertyField(sourcePrefab);
            }
            #endregion

            EditorGUILayout.Space();

            #region New Sound/Music Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add New Sound File"))
            {
                GenerateFolderStructure(filePath);
                if (AssetDatabase.IsValidFolder(filePath))
                {
                    // Many thanks to mstevenson for having a reference on creating scriptable objects from code
                    // https://gist.github.com/mstevenson/4726563
                    var asset = CreateInstance<AudioFileObject>();
                    string savePath = EditorUtility.SaveFilePanel("Create new Audio File Object", filePath, "New Audio File Object", "asset");
                    if (savePath != "") // Make sure user didn't press "Cancel"
                    {
                        savePath = savePath.Remove(0, savePath.IndexOf("Assets/"));
                        AssetDatabase.CreateAsset(asset, savePath);
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = asset;
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                    }
                }
            }

            if (GUILayout.Button("Add New Music File"))
            {
                GenerateFolderStructure(filePath);
                if (AssetDatabase.IsValidFolder(filePath))
                {
                    var asset = CreateInstance<AudioFileMusicObject>();
                    string savePath = EditorUtility.SaveFilePanel("Create new Audio File Music Object", filePath, "New Audio File Music Object", "asset");
                    if (savePath != "") // Make sure user didn't press "Cancel"
                    {
                        savePath = savePath.Remove(0, savePath.IndexOf("Assets/"));
                        AssetDatabase.CreateAsset(asset, savePath);
                        EditorUtility.FocusProjectWindow();
                        Selection.activeObject = asset;
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            GUIContent blontent = new GUIContent("Re-Generate Audio Library", "Click this whenever you add new Audio Files, change the scene name, or encounter any issues");
            if (GUILayout.Button(blontent))
            {
                GenerateFolderStructure(filePath);
                if (AssetDatabase.IsValidFolder(filePath))
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

                        theObject.safeName = ConvertToAlphanumeric(theObject.name);

                        // Is this actually a music object?
                        if (!theObject.GetType().IsAssignableFrom(typeof(AudioFileObject)))
                        {
                            musicFiles.Add((AudioFileMusicObject)theObject);
                        }
                        else audioFiles.Add(theObject);
                    }

                    AudioFileComparer afc = new AudioFileComparer();
                    audioFiles.Sort(afc);
                    musicFiles.Sort(afc);
                    if (myScript.GenerateAudioDictionarys(audioFiles, musicFiles) || wasInstancedBefore.boolValue == usingInstancedEnums)
                    {
                        EditorUtility.DisplayProgressBar("Re-Generating Audio Library", "Generating audio enum file...", 0.5f);
                        string safeSceneName = GenerateEnumFile(filePath, usingInstancedEnums);
                        if (safeSceneName == "") // File generation has failed, abort
                        {
                            EditorUtility.ClearProgressBar();
                        }
                        else // Generation successful, proceed as usual
                        {
                            if (instancedEnums.boolValue)
                            {
                                sceneSoundEnumName.stringValue = "JSAM.Sounds" + safeSceneName;
                                sceneMusicEnumName.stringValue = "JSAM.Music" + safeSceneName;
                            }
                            else
                            {
                                sceneSoundEnumName.stringValue = "JSAM.Sounds";
                                sceneMusicEnumName.stringValue = "JSAM.Music";
                            }

                            EditorUtility.DisplayProgressBar("Re-Generating Audio Library", "Done! Recompiling...", 0.95f);
                        }
                    }
                    else
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
            }

            wasInstancedBefore.boolValue = usingInstancedEnums;
            #endregion

            if (serializedObject.hasModifiedProperties)
            {
                // Final check on the file path
                bool fix = false;
                if (filePath.Length < 6) fix = true;
                else if (filePath.Substring(0, 6) != "Assets") fix = true;
                if (fix) audioFolderLocation.stringValue = prevPath;
                if (filePath[filePath.Length - 1] == '/')
                    audioFolderLocation.stringValue = filePath.Remove(filePath.Length - 1);

                serializedObject.ApplyModifiedProperties();
            }

            if (usingInstancedEnums)
            {
                string sceneNameSpecial = sceneSoundEnumName.stringValue;
                string sceneNameSpecialMusic = sceneMusicEnumName.stringValue;
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
                    EditorGUILayout.HelpBox("When using AudioManager in code, refer to the enums in " + sceneNameSpecial + " and " + sceneNameSpecialMusic + " for your list of available Audio Files", MessageType.None);
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

            #region Library Foldouts and Categories
            content = new GUIContent("Sound Library", "Library of all sounds loaded into AudioManager's Sound Dictionary. Mouse over each entry to see the full name in script.");

            showSoundLibrary = EditorCompatability.SpecialFoldouts(showSoundLibrary, content);
            if (showSoundLibrary)
            {
                string enumName = sceneSoundEnumName.stringValue;

                System.Type soundType = myScript.GetSceneSoundEnum();
                string[] soundNames = new string[0];
                if (soundType != null)
                {
                    soundNames = System.Enum.GetNames(soundType);
                }

                if (audioFileProperty.arraySize > 0)
                {
                    EditorGUILayout.LabelField("You can right click a field to copy the enum to your clipboard");
                }

                // Make sure AudioManager didn't break during regeneration
                if (soundNames.Length != audioFileProperty.arraySize)
                {
                    EditorGUILayout.HelpBox("Something may have interrupted AudioManager while it was generating the Audio Library. " +
                        "Try regenerating the library one more time by clicking the button above.", MessageType.Info);
                }
                else
                {
                    // Fill the dictionary of categories
                    foreach (string c in myScript.GetCategories())
                    {
                        // Instantiate a new category entry if not found
                        if (!categories.ContainsKey(c))
                        {
                            categories[c] = false;
                        }
                    }
                    if (!categories.ContainsKey("Uncategorized")) categories["Uncategorized"] = false;

                    Dictionary<string, List<SPandName>> audioSPs = new Dictionary<string, List<SPandName>>();
                    List<AudioFileObject> audioRef = myScript.GetSoundLibrary();
                    // If there are new AudioFiles being added and the SoundLibrary foldout is open
                    if (audioRef.Count == audioFileProperty.arraySize)
                    {
                        for (int i = 0; i < audioRef.Count; i++)
                        {
                            SerializedProperty ao = audioFileProperty.GetArrayElementAtIndex(i);
                            SPandName newPair = new SPandName();
                            newPair.name = soundNames[i];
                            newPair.sp = ao;
                            if (audioRef[i].category == "")
                            {
                                if (!audioSPs.ContainsKey("Uncategorized"))
                                {
                                    audioSPs["Uncategorized"] = new List<SPandName>();
                                }
                                audioSPs["Uncategorized"].Add(newPair);
                            }
                            else
                            {
                                if (!audioSPs.ContainsKey(audioRef[i].category))
                                {
                                    audioSPs[audioRef[i].category] = new List<SPandName>();
                                }
                                audioSPs[audioRef[i].category].Add(newPair);
                            }
                        }

                        GUIStyle foldoutGroup = EditorCompatability.GetFoldoutHeaderStyle();
                        foldoutGroup.fontStyle = FontStyle.Normal;
                        string[] keys = new string[categories.Keys.Count];
                        categories.Keys.CopyTo(keys, 0);
                        List<string> keysList = new List<string>();
                        keysList.AddRange(keys);
                        foreach (string k in keys)
                        {
                            if (k == "Uncategorized") continue;
                            categories[k] = EditorGUILayout.Foldout(categories[k], k, true, foldoutGroup);
                            EditorGUI.indentLevel++;
                            if (categories[k])
                            {
                                for (int i = 0; i < audioSPs[k].Count; i++)
                                {
                                    RenderAudioFileListing(audioSPs[k][i].sp, audioSPs[k][i].name, enumName);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }

                        if (audioSPs.ContainsKey("Uncategorized"))
                        {
                            for (int i = 0; i < audioSPs["Uncategorized"].Count; i++)
                            {
                                RenderAudioFileListing(audioSPs["Uncategorized"][i].sp, audioSPs["Uncategorized"][i].name, enumName);
                            }
                        }
                    }
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();

            content = new GUIContent("Music Library", "Library of all music loaded into AudioManager's Music Dictionary. Mouse over each entry to see the full name in script.");

            showMusicLibrary = EditorCompatability.SpecialFoldouts(showMusicLibrary, content);
            if (showMusicLibrary)
            {
                string enumName = sceneMusicEnumName.stringValue;

                System.Type musicType = myScript.GetSceneMusicEnum();
                string[] musicNames = new string[0];
                if (musicType != null)
                {
                    musicNames = System.Enum.GetNames(musicType);
                }

                if (audioFileMusicProperty.arraySize > 0)
                {
                    EditorGUILayout.LabelField("You can right click a field to copy the enum to your clipboard");
                }

                if (musicNames.Length != audioFileMusicProperty.arraySize)
                {
                    EditorGUILayout.HelpBox("Something may have interrupted AudioManager while it was generating the Audio Library. " +
                        "Try regenerating the library one more time by clicking the button above.", MessageType.Info);
                }
                else
                {
                    // Fill the dictionary of categories
                    foreach (string c in myScript.GetMusicCategories())
                    {
                        // Instantiate a new category entry if not found
                        if (!categoriesMusic.ContainsKey(c))
                        {
                            categoriesMusic[c] = false;
                        }
                    }
                    if (!categoriesMusic.ContainsKey("Uncategorized")) categoriesMusic["Uncategorized"] = false;

                    Dictionary<string, List<SPandName>> audioSPs = new Dictionary<string, List<SPandName>>();
                    List<AudioFileMusicObject> audioRef = myScript.GetMusicLibrary();
                    // If there are new audioFileMusicProperty being added and the SoundLibrary foldout is open
                    if (audioRef.Count == audioFileMusicProperty.arraySize)
                    {
                        for (int i = 0; i < audioRef.Count; i++)
                        {
                            SerializedProperty ao = audioFileMusicProperty.GetArrayElementAtIndex(i);
                            SPandName newPair = new SPandName();
                            newPair.name = musicNames[i];
                            newPair.sp = ao;
                            if (audioRef[i].category == "")
                            {
                                if (!audioSPs.ContainsKey("Uncategorized"))
                                {
                                    audioSPs["Uncategorized"] = new List<SPandName>();
                                }
                                audioSPs["Uncategorized"].Add(newPair);
                            }
                            else
                            {
                                if (!audioSPs.ContainsKey(audioRef[i].category))
                                {
                                    audioSPs[audioRef[i].category] = new List<SPandName>();
                                }
                                audioSPs[audioRef[i].category].Add(newPair);
                            }
                        }

                        GUIStyle foldoutGroup = EditorCompatability.GetFoldoutHeaderStyle();
                        foldoutGroup.fontStyle = FontStyle.Normal;
                        string[] keys = new string[categoriesMusic.Keys.Count];
                        categoriesMusic.Keys.CopyTo(keys, 0);
                        List<string> keysList = new List<string>();
                        keysList.AddRange(keys);
                        foreach (string k in keys)
                        {
                            if (k == "Uncategorized") continue;
                            categoriesMusic[k] = EditorGUILayout.Foldout(categoriesMusic[k], k, true, foldoutGroup);
                            EditorGUI.indentLevel++;
                            if (categoriesMusic[k])
                            {
                                for (int i = 0; i < audioSPs[k].Count; i++)
                                {
                                    RenderAudioFileListing(audioSPs[k][i].sp, audioSPs[k][i].name, enumName);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }

                        if (audioSPs.ContainsKey("Uncategorized"))
                        {
                            for (int i = 0; i < audioSPs["Uncategorized"].Count; i++)
                            {
                                RenderAudioFileListing(audioSPs["Uncategorized"][i].sp, audioSPs["Uncategorized"][i].name, enumName);
                            }
                        }
                    }
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();
#endregion

            if (myScript.GetMasterVolumeInternal() == 0) EditorGUILayout.HelpBox("Note: Master Volume is set to 0!", MessageType.Info);
            if (myScript.GetSoundVolumeInternal() == 0) EditorGUILayout.HelpBox("Note: Sound is set to 0!", MessageType.Info);
            if (myScript.GetMusicVolumeInternal() == 0) EditorGUILayout.HelpBox("Note: Music is set to 0!", MessageType.Info);

            EditorGUILayout.Space();

            #region Quick Reference Guide
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This component is the backbone of the entire JSAM Audio Manager system and ideally should occupy it's own gameobject."
                    , MessageType.None);
                EditorGUILayout.HelpBox("Click on these links here for more info on getting started and for full documentation!"
                    , MessageType.None);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Getting Started", "Click on me open the wiki in a new browser window"), new GUILayoutOption[]{ GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/wiki/2.-Getting-Started-with-JSAM");
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Documentation", "Click on me to check out the documentation"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://jackyyang09.github.io/Simple-Unity-Audio-Manager/class_j_s_a_m_1_1_audio_manager.html");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Remember to mouse over the settings in this and other windows to learn more about them!", MessageType.None);
                EditorGUILayout.HelpBox("Please ensure that you don't have multiple AudioManagers in one scene."
                    , MessageType.None);
                EditorGUILayout.HelpBox("If you have any questions, suggestions or bug reports, feel free to open a new issue " +
                    "on Github repository's Issues page or send me an email directly!", MessageType.None);
                EditorGUILayout.HelpBox("The Github Repository is usually more up to date with bug fixes than what's shown on the Unity Asset Store, so give it a look just in case!", MessageType.None);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Report a Bug", "Click on me to go to the bug report page in a new browser window"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/issues");
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Github Releases", "Click on me to check out the latest releases in a new browser window"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/releases");
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Email", "You can find me at jackyyang267@gmail.com"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
                {
                    Application.OpenURL("mailto:jackyyang267@gmail.com");
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox("AudioManager uses Unity's AudioSources as a basis for all audio playing. As such, spatialized 3D sound " +
                    "will play within a distance of 7 units from the listener before fading completely. If you want to change the way sounds are " +
                    "spatialized, you can either locate the Audio Channel prefab and modify the settings there " +
                    "or replace the existing prefab with your own."
                    , MessageType.None);

                EditorGUILayout.HelpBox("AudioManager works best as a global system where each scene's AudioManager draws from the same AudioFiles." +
                    " However, if you want scenes to draw from separate groups of Audio Files, you can select the option to enable instanced Audio Enums under" +
                    " AudioManager's advanced settings and regenerate the Audio Library. This let's AudioManager use it's own designated Audio Files separate to ones" +
                    " used in other scenes. It's with this system that allows AudioManager to hold many different example projects as sub folders!"
                    , MessageType.None);
            }
            EditorCompatability.EndSpecialFoldoutGroup();
            #endregion  
        }

        private void OnEnable()
        {
            myScript = (AudioManager)target;

            myScript.EstablishSingletonDominance();

            audioFolderLocation = serializedObject.FindProperty("audioFolderLocation");
            masterVolume = serializedObject.FindProperty("masterVolume");
            musicVolume = serializedObject.FindProperty("musicVolume");
            soundVolume = serializedObject.FindProperty("soundVolume");
            instancedEnums = serializedObject.FindProperty("instancedEnums");
            wasInstancedBefore = serializedObject.FindProperty("wasInstancedBefore");
            listener = serializedObject.FindProperty("listener");
            sourcePrefab = serializedObject.FindProperty("sourcePrefab");
            sceneSoundEnumName = serializedObject.FindProperty("sceneSoundEnumName");
            sceneMusicEnumName = serializedObject.FindProperty("sceneMusicEnumName");
            audioFileProperty = serializedObject.FindProperty("audioFileObjects");
            audioFileMusicProperty = serializedObject.FindProperty("audioFileMusicObjects");

            //copyIcon = EditorGUIUtility.TrIconContent("winbtn_win_restore_h", "Click to Copy Enum Name to Clipboard");
            copyIcon = new GUIContent("Copy", "Click to Copy Enum Name to Clipboard");

            categories = new Dictionary<string, bool>();
            if (AudioManager.instance)
            {
                AudioManager.instance.UpdateAudioFileMusicObjectCategories();
                AudioManager.instance.UpdateAudioFileObjectCategories();
            }
            Application.logMessageReceived += UnityDebugLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= UnityDebugLog;
        }

        static void UnityDebugLog(string message, string stackTrace, LogType logType)
        {
            // Code from this steffen-itterheim
            // https://answers.unity.com/questions/482765/detect-compilation-errors-in-editor-script.html
            // if we receive a Debug.LogError we can assume that compilation failed
            if (logType == LogType.Error)
                EditorUtility.ClearProgressBar();
        }

        void RenderAudioFileListing(SerializedProperty ao, string soundName, string enumName)
        {
            GUIContent sName = new GUIContent(soundName, enumName + "." + soundName);
            Rect clickArea = EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.ObjectField(ao, sName);
            GUI.enabled = true;
            if (GUILayout.Button(copyIcon, new GUILayoutOption[] { GUILayout.MaxWidth(40) }))
            {
                CopyToClipboard(sName.tooltip);
            }
            // Thank you JJCrawley https://answers.unity.com/questions/1326881/right-click-in-custom-editor.html
            // Deprecated as of Unity 2020
            //if (clickArea.Contains(Event.current.mousePosition) && Event.current.type == EventType.ContextClick)
            //{
            //    Debug.Log("TEST");
            //    GenericMenu menu = new GenericMenu();
            //
            //    menu.AddItem(new GUIContent("Copy \"" + sName.tooltip + "\" to Clipboard"), false, CopyToClipboard, sName.tooltip);
            //    menu.ShowAsContext();
            //}
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Just hides the fancy loading bar lmao
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Copies the given string to your clipboard
        /// </summary>
        /// <param name="text"></param>
        void CopyToClipboard(object text)
        {
            string s = text.ToString();
            EditorGUIUtility.systemCopyBuffer = s;
            AudioManager.instance.DebugLog("Copied " + s + " to clipboard!");
        }

        [MenuItem("GameObject/Audio/JSAM/Audio Manager", false, 1)]
        public static void AddAudioManager()
        {
            AudioManager existingAudioManager = FindObjectOfType<AudioManager>();
            if (!existingAudioManager)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Audio Manager t:GameObject")[0]);
                GameObject newManager = (GameObject)Instantiate(AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)));
                if (Selection.activeTransform != null)
                {
                    newManager.transform.parent = Selection.activeTransform;
                    newManager.transform.localPosition = Vector3.zero;
                }
                newManager.name = newManager.name.Replace("(Clone)", string.Empty);
                EditorGUIUtility.PingObject(newManager);
                Selection.activeGameObject = newManager;
                Undo.RegisterCreatedObjectUndo(newManager, "Added new AudioManager");
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
            // Evaluate potential enum names before writing to file
            List<AudioFileObject> soundLibrary = AudioManager.instance.GetSoundLibrary();

            List<string> soundNames = new List<string>();
            foreach (var s in soundLibrary)
            {
                string newName = s.safeName;

                if (!soundNames.Contains(newName))
                {
                    soundNames.Add(newName);
                }
                else
                {
                    string problemString = soundLibrary[soundNames.IndexOf(newName)].name;
                    EditorUtility.DisplayDialog("Audio Library Generation Error!", "\"" + s.name + "\" shares the same name with \"" + problemString + "\"! " +
                        "When converting to Audio Enums, AudioManager strips away non Alphanumeric characters to ensure proper C# syntax and " +
                        "in the process they've ended with the same name! Please make \"" + s.name + "\" more different and try again!", "OK");
                    return "";
                }
            }

            List<AudioFileMusicObject> musicLibrary = AudioManager.instance.GetMusicLibrary();

            List<string> musicNames = new List<string>();
            foreach (var m in musicLibrary)
            {
                string newName = m.safeName;

                if (!musicNames.Contains(newName))
                {
                    musicNames.Add(newName);
                }
                else
                {
                    string problemString = musicLibrary[musicNames.IndexOf(newName)].name;
                    EditorUtility.DisplayDialog("Audio Library Generation Error!", "\"" + m.name + "\" shares the same name with \"" + problemString + "\"! " +
                        "When converting to Audio Enums, AudioManager strips away non Alphanumeric characters to ensure proper C# syntax and " +
                        "in the process they've ended with the same name! Please make \"" + m.name + "\" more different and try again!", "OK");
                    return "";
                }
            }

            // Now that we've gotten that over with, check for duplicate AudioEnums in this folder, because we can't trust the user to be organized
            string sceneName = AudioManager.instance.gameObject.scene.name;
            // User is working in a completely new, unsaved scene, better remind them what's up
            if (sceneName == "" || sceneName == null)
            {
                EditorUtility.DisplayDialog("Audio Library Generation Error!", "It seems like you're working in a completely  " +
                    "new scene! AudioManager requires that your scene is saved to file in order to keep a proper Audio Library. " +
                    "It's also good to ensure all your work isn't lost forever!", "OK");
                return "";
            }
            string safeSceneName = ConvertToAlphanumeric(sceneName);

            string fileName = (usingInstancedEnums) ? "\\AudioEnums - " + sceneName + ".cs" : "\\AudioEnums.cs";

            bool overwriting = false;
            // Looking for AudioEnums
            string[] GUIDs = AssetDatabase.FindAssets("AudioEnums", new[] { filePath });
            foreach (var p in GUIDs)
            {
                // Make the detected file match the format of expected filenames up above
                string assetPath = AssetDatabase.GUIDToAssetPath(p);
                string assetName = "\\" + assetPath.Remove(0, assetPath.LastIndexOf('/') + 1);
                if (assetName == fileName)
                {
                    overwriting = true;
                    continue; // We're overwriting this anyway
                }
                if (EditorUtility.DisplayDialog("Duplicate AudioEnums file Found!", "Another instance of AudioEnums.cs was found in this folder. " +
                    "Although they don't conflict, having both in the same place may create problems in the future. " +
                    "Would you like to delete the existing AudioEnums file?)", "Yes", "No"))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            // Check for existing Enums of the same name
            if (!overwriting)
            {
                if (usingInstancedEnums)
                {
                    if (GetEnumType("JSAM.Sounds" + safeSceneName) != null)
                    {
                        if (EditorUtility.DisplayDialog("Duplicate Enum Type Found!", "An existing script in the project already " +
                            "contains an Enum called \"JSAM.Sounds" + safeSceneName + "\"! AudioManager cannot regenerate the library " +
                            "until the scene name is changed to something different or the existing enum name is modified!", "OK"))
                        {
                            return "";
                        }
                    }
                    else if (GetEnumType("JSAM.Music" + safeSceneName) != null)
                    {
                        if (EditorUtility.DisplayDialog("Duplicate Enum Type Found!", "An existing script in the project already " +
                            "contains an Enum called \"JSAM.Music" + safeSceneName + "\"! AudioManager cannot regenerate the library " +
                            "until the scene name is changed to something different or the existing enum name is modified!", "OK"))
                        {
                            return "";
                        }
                    }
                }
                else if (GetEnumType("JSAM.Sounds") != null)
                {
                    if (EditorUtility.DisplayDialog("Duplicate Enum Type Found!", "An existing script in the project already " +
                        "contains an Enum called \"JSAM.Sounds\"! Is there an existing AudioEnums script in your project? Check to " +
                        "make sure your Audio Assets folder isn't just in the root Assets folder. Otherwise, you may " +
                        "want to consider using Instanced Enums in AudioManager's advanced settings.", "OK"))
                    {
                        return "";
                    }
                }
            }

            filePath += fileName;
            File.WriteAllText(filePath, string.Empty);
            StreamWriter writer = new StreamWriter(filePath, true);
            writer.WriteLine("namespace JSAM {");

            if (!usingInstancedEnums)
            writer.WriteLine("    public enum Sounds {");
            else
            writer.WriteLine("    public enum Sounds" + safeSceneName + "{");

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
        public static string ConvertToAlphanumeric(string input)
        {
            char[] arr = input.ToCharArray();

            arr = System.Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c) 
            || c == '_')));

            // If the first index is a number
            while (char.IsDigit(arr[0]))
            {
                List<char> newArray = new List<char>();
                newArray = new List<char>(arr);
                newArray.RemoveAt(0);
                arr = newArray.ToArray();
                if (arr.Length == 0) break; // No valid characters to use, returning empty
            }

            return new string(arr);
        }
        #endregion

        #region GameObject Rename Code (Deprecated)
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
                GUIContent bContent = new GUIContent(myScript.IsMasterMutedInternal() ? "OFF" : "ON", "Turns the channel's audio output ON/OFF");
                if (GUI.Button(buttonRect, bContent))
                {
                    Undo.RecordObject(myScript, "Toggled master volume mute state");
                    myScript.SetMasterChannelMuteInternal(!myScript.IsMasterMutedInternal());
                }
                using (new EditorGUI.DisabledScope(myScript.IsMasterMutedInternal()))
                {
                    Rect sliderRect = new Rect(rect);
                    sliderRect.xMin = buttonRect.xMax + 7.5f;
                    Rect textRect = new Rect(rect);
                    textRect.width = 50;
                    textRect.x = Mathf.Abs(textRect.width - (rect.width + 17.5f));
                    sliderRect.xMax = textRect.xMin - 5;

                    int sliderVolume = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, myScript.GetMasterVolumeAsIntInternal(), 0f, 100f));
                    if (sliderVolume != myScript.GetMasterVolumeAsIntInternal())
                    {
                        Undo.RecordObject(myScript, "Changed master channel volume");
                        masterVolume.floatValue = (float)sliderVolume / 100;
                        myScript.SetMasterVolumeInternal(sliderVolume);
                    }

                    GUI.SetNextControlName("textMaster");
                    masterText = GUI.TextField(textRect, masterText);
                    if (!GUI.GetNameOfFocusedControl().Equals("textMaster"))
                    {
                        masterText = myScript.GetMasterVolumeAsIntInternal().ToString();
                    }
                    float resultF = 0;
                    if (!float.TryParse(masterText, out resultF))
                    {
                        resultF = (float)sliderVolume / 100;
                    }

                    Undo.RecordObject(myScript, "Changed master channel volume");
                    masterVolume.floatValue = resultF / 100f;
                    myScript.SetMasterVolumeInternal((int)resultF);

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
                GUIContent bContent = new GUIContent(myScript.IsSoundMutedInternal() ? "OFF" : "ON", "Turns the channel's audio output ON/OFF");
                if (GUI.Button(buttonRect, bContent))
                {
                    Undo.RecordObject(myScript, "Toggled sound volume mute state");
                    myScript.SetSoundChannelMuteInternal(!myScript.IsSoundMutedInternal());
                }
                using (new EditorGUI.DisabledScope(myScript.IsSoundMutedInternal()))
                {
                    Rect sliderRect = new Rect(rect);
                    sliderRect.xMin = buttonRect.xMax + 7.5f;
                    Rect textRect = new Rect(rect);
                    textRect.width = 50;
                    textRect.x = Mathf.Abs(textRect.width - (rect.width + 17.5f));
                    sliderRect.xMax = textRect.xMin - 5;

                    int sliderVolume = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, myScript.GetSoundVolumeAsIntInternal(), 0f, 100f));
                    if (sliderVolume != myScript.GetSoundVolumeAsIntInternal())
                    {
                        Undo.RecordObject(myScript, "Changed sound channel volume");
                        soundVolume.floatValue = (float)sliderVolume / 100;
                        myScript.SetSoundVolumeInternal(sliderVolume);
                    }

                    GUI.SetNextControlName("textSound");
                    soundText = GUI.TextField(textRect, soundText);
                    if (!GUI.GetNameOfFocusedControl().Equals("textSound"))
                    {
                        soundText = myScript.GetSoundVolumeAsIntInternal().ToString();
                    }
                    float resultF = 0;
                    if (!float.TryParse(soundText, out resultF))
                    {
                        resultF = (float)sliderVolume / 100;
                    }

                    Undo.RecordObject(myScript, "Changed sound channel volume");
                    soundVolume.floatValue = resultF / 100f;
                    myScript.SetSoundVolumeInternal((int)resultF);

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
                GUIContent bContent = new GUIContent(myScript.IsMusicMutedInternal() ? "OFF" : "ON", "Turns the channel's audio output ON/OFF");
                if (GUI.Button(buttonRect, bContent))
                {
                    Undo.RecordObject(myScript, "Toggled music volume mute state");
                    myScript.SetMusicChannelMuteInternal(!myScript.IsMusicMutedInternal());
                }
                using (new EditorGUI.DisabledScope(myScript.IsMusicMutedInternal()))
                {
                    Rect sliderRect = new Rect(rect);
                    sliderRect.xMin = buttonRect.xMax + 7.5f;
                    Rect textRect = new Rect(rect);
                    textRect.width = 50;
                    textRect.x = Mathf.Abs(textRect.width - (rect.width + 17.5f));
                    sliderRect.xMax = textRect.xMin - 5;

                    int sliderVolume = Mathf.RoundToInt(GUI.HorizontalSlider(sliderRect, myScript.GetMusicVolumeAsIntInternal(), 0f, 100f));
                    if (sliderVolume != myScript.GetMusicVolumeAsIntInternal())
                    {
                        Undo.RecordObject(myScript, "Changed music channel volume");
                        musicVolume.floatValue = (float)sliderVolume / 100;
                        myScript.SetMusicVolumeInternal(sliderVolume);
                    }

                    GUI.SetNextControlName("textMusic");
                    musicText = GUI.TextField(textRect, musicText);
                    if (!GUI.GetNameOfFocusedControl().Equals("textMusic"))
                    {
                        musicText = myScript.GetMusicVolumeAsIntInternal().ToString();
                    }
                    float resultF = 0;
                    if (!float.TryParse(musicText, out resultF))
                    {
                        resultF = (float)sliderVolume / 100;
                    }

                    Undo.RecordObject(myScript, "Changed music channel volume");
                    musicVolume.floatValue = resultF / 100f;
                    myScript.SetMusicVolumeInternal((int)resultF);

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        #endregion

        public static void GenerateFolderStructure(string filePath)
        {
            if (!AssetDatabase.IsValidFolder(filePath))
            {
                string existingPath = "Assets";
                string unknownPath = filePath.Remove(0, existingPath.Length + 1); // Remove the Assets/ at the start of the path name
                string folderName = (unknownPath.Contains("/")) ? unknownPath.Substring(0, (unknownPath.IndexOf("/"))) : unknownPath;
                do
                {
                    // Begin checking down the file path to see if it's valid
                    if (EditorUtility.DisplayDialog("Path does not exist!", "The Audio Assets Folder \"" + folderName +
                        "\" at \"" + existingPath +
                        "\" does not exist! Would you like to create this folder?", "Yes", "No"))
                    {
                        AssetDatabase.CreateFolder(existingPath, folderName);
                    }
                    else break;
                    existingPath += "/" + folderName;
                    // Full path still doesn't exist
                    if (existingPath != filePath)
                    {
                        unknownPath = unknownPath.Remove(0, folderName.Length + 1);
                        folderName = (unknownPath.Contains("/")) ? unknownPath.Substring(0, (unknownPath.IndexOf("/"))) : unknownPath;
                    }
                }
                while (!AssetDatabase.IsValidFolder(filePath));
            }
        }

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

    class AudioFileComparer : IComparer<AudioFileObject>
    {
        public int Compare(AudioFileObject x, AudioFileObject y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            return x.Compare(x, y);
        }
    }
}