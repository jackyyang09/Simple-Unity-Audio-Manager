using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace JSAM.JSAMEditor
{
    /// <summary>
    /// Thank god to brownboot67 for his advice
    /// https://forum.unity.com/threads/custom-editor-not-saving-changes.424675/
    /// </summary>
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : Editor
    {
        AudioManager myScript;

        static bool showVolumeSettings = true;
        //static bool showAdvancedSettings;

        static bool showHowTo;

        int libraryIndex = 0;

        SerializedProperty masterVolume;
        SerializedProperty musicVolume;
        SerializedProperty soundVolume;
        SerializedProperty listener;
        SerializedProperty sourcePrefab;

        SerializedProperty library;
        SerializedProperty settings;

        private void OnEnable()
        {
            myScript = (AudioManager)target;

            myScript.EstablishSingletonDominance();

            masterVolume = serializedObject.FindProperty("masterVolume");
            musicVolume = serializedObject.FindProperty("musicVolume");
            soundVolume = serializedObject.FindProperty("soundVolume");
            listener = serializedObject.FindProperty("listener");
            sourcePrefab = serializedObject.FindProperty("sourcePrefab");

            library = serializedObject.FindProperty("library");
            settings = serializedObject.FindProperty("settings");

            Application.logMessageReceived += UnityDebugLog;

            LoadLibraries();
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= UnityDebugLog;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
                    
            List<string> excludedProperties = new List<string> { "m_Script" };

            GUIContent blontent;

            //if (!showAdvancedSettings)
            //{
            //    excludedProperties.AddRange(new List<string>
            //    {
            //
            //    });
            //
            //    blontent = new GUIContent("↓ Show Advanced Settings ↓", "Toggle this if you're an experienced Unity user");
            //}
            //else
            //{
            //    blontent = new GUIContent("↑ Hide Advanced Settings ↑", "Toggle this if you're an experienced Unity user");
            //}
            excludedProperties.AddRange(new List<string> { "listener", "sourcePrefab" });

            //if (GUILayout.Button(blontent))
            //{
            //    showAdvancedSettings = !showAdvancedSettings;
            //}

            //EditorGUILayout.Space();

            blontent = new GUIContent("Volume Controls", "Change the volume levels of all AudioManager-controlled audio channels here");
            showVolumeSettings = EditorCompatability.SpecialFoldouts(showVolumeSettings, blontent);
            if (showVolumeSettings)
            {
                DrawAdvancedVolumeControls(myScript);
            }
            EditorCompatability.EndSpecialFoldoutGroup();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            blontent = new GUIContent("Library");
            EditorGUI.BeginChangeCheck();
            libraryIndex = EditorGUILayout.Popup(blontent, libraryIndex, AudioLibraryEditor.projectLibrariesNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                library.objectReferenceValue = AudioLibraryEditor.projectLibraries[libraryIndex];
            }
            blontent = new GUIContent(" Open ");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                AudioLibraryEditor.Init();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            blontent = new GUIContent("Settings");
            EditorGUILayout.PropertyField(settings);
            blontent = new GUIContent(" Open ");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                AudioManagerSettingsEditor.Init();
            }
            EditorGUILayout.EndHorizontal();

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            //if (myScript.GetListenerInternal() == null || showAdvancedSettings)
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
            else if (myScript.SourcePrefabExists()/* && showAdvancedSettings*/)
            {
                EditorGUILayout.PropertyField(sourcePrefab);
            }
            #endregion

            EditorGUILayout.Space();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (myScript.GetMasterVolumeInternal() == 0) EditorGUILayout.HelpBox("Note: Master Volume is set to 0!", MessageType.Info);
            if (myScript.GetSoundVolumeInternal() == 0) EditorGUILayout.HelpBox("Note: Sound is set to 0!", MessageType.Info);
            if (myScript.GetMusicVolumeInternal() == 0) EditorGUILayout.HelpBox("Note: Music is set to 0!", MessageType.Info);

            #region Quick Reference Guide
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("This component is the backbone of the entire JSAM Audio Manager system and ideally should occupy it's own gameobject."
                    , MessageType.None);
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

        static void UnityDebugLog(string message, string stackTrace, LogType logType)
        {
            // Code from this steffen-itterheim
            // https://answers.unity.com/questions/482765/detect-compilation-errors-in-editor-script.html
            // if we receive a Debug.LogError we can assume that compilation failed
            if (logType == LogType.Error)
                EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Just hides the fancy loading bar lmao
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorUtility.ClearProgressBar();
        }

        void LoadLibraries()
        {
            var libraries = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<AudioLibrary>(JSAMSettings.Settings.LibraryPath);

            AudioLibraryEditor.projectLibraries = new List<AudioLibrary>();
            AudioLibraryEditor.projectLibrariesNames = new List<string>();
            for (int i = 0; i < libraries.Count; i++)
            {
                AudioLibraryEditor.projectLibraries.Add(libraries[i]);
                AudioLibraryEditor.projectLibrariesNames.Add(libraries[i].name);
            }

            libraryIndex = AudioLibraryEditor.projectLibraries.IndexOf(library.objectReferenceValue as AudioLibrary);
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
    }
}