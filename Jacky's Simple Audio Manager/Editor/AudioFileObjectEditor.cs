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
        AudioFileObject myScript;

        Color buttonPressedColor = new Color(0.475f, 0.475f, 0.475f);

        AudioClip playingClip;

        bool clipPlaying;
        bool playingRandom;

        Texture2D cachedTex;
        bool forceRepaint;
        AudioClip cachedClip;

        bool unregistered = false;
        bool relevant = false;
        string myName = "";
        string cachedName = "";
        bool nameChanged = false;

        SerializedProperty file;
        SerializedProperty files;
        SerializedProperty relativeVolume;
        SerializedProperty spatialize;
        SerializedProperty maxDistance;

        SerializedProperty neverRepeat;
        SerializedProperty fadeInDuration;
        SerializedProperty fadeOutDuration;

        static bool showFadeTool;
        static bool showPlaybackTool;
        static bool showHowTo;

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            EditorGUILayout.LabelField("Audio File Object", EditorStyles.boldLabel);

            EditorGUILayout.LabelField(new GUIContent("Name: ", "This is the name that AudioManager will use to reference this object with."), new GUIContent(myName));

            #region Category Inspector
            EditorGUILayout.BeginHorizontal();
            GUIContent blontent = new GUIContent("Category", "An optional field that lets you further sort your AudioFileObjects for better organization in AudioManager's library view.");
            string newCategory = EditorGUILayout.DelayedTextField(blontent, myScript.category);
            List<string> categories = new List<string>();
            if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
            {
                GUI.FocusControl(null);
                // Check if we're modifying this AudioFileObject in a valid scene
                if (AudioManager.instance != null)
                {
                    //categories.AddRange(AudioManager.instance.GetCategories());
                    categories.AddRange(AudioFileObject.GetCategories());
                }
                GenericMenu newMenu = new GenericMenu();
                int i = 0;
                // To reduce the number of boolean comparisons we do as we iterate
                for (; i < categories.Count; i++)
                {
                    if (myScript.category == categories[i])
                    {
                        newMenu.AddItem(new GUIContent(categories[i]), true, SetCategory, categories[i]);
                        break;
                    }
                    else
                    {
                        newMenu.AddItem(new GUIContent(categories[i]), false, SetCategory, categories[i]);
                    }
                }   
                for (; i < categories.Count; i++)
                {
                    newMenu.AddItem(new GUIContent(categories[i]), false, SetCategory, categories[i]);
                }
                newMenu.AddSeparator("");
                newMenu.AddItem(new GUIContent("Hidden"), myScript.category == "Hidden", SetCategory, "Hidden");
                newMenu.ShowAsContext();
            }
            if (newCategory != myScript.category)
            {
                SetCategory(newCategory);
            }
            EditorGUILayout.EndHorizontal();
            #endregion

            if (unregistered)
            {
                EditorGUILayout.HelpBox("This Audio File Object has yet to be added to AudioManager's library. Do make sure to " +
                    "click on \"Re-generate Audio Library\" in AudioManager before playing!", MessageType.Warning);
            }
            else if (relevant)
            {
                if (cachedName != target.name)
                {
                    CheckIfNameChanged();
                    cachedName = target.name;
                }

                if (nameChanged)
                {
                    EditorGUILayout.HelpBox("This Audio File Object's name differs from it's corresponding enum name! " +
                        "No error will come of this, but you may want to regenerate AudioManager's audio libraries again for clarity.", MessageType.Info);
                }
            }

            List<string> excludedProperties = new List<string>() { "m_Script", "file", "files", "safeName",
                "relativeVolume", "spatialize", "maxDistance" };

            if (myScript.UsingLibrary()) // Swap file with files
            {
                EditorGUILayout.PropertyField(files);
            }
            else
            {
                EditorGUILayout.PropertyField(file);
            }

            blontent = new GUIContent("Use Library", "If true, the single AudioFile will be changed to a list of AudioFiles. AudioManager will choose a random AudioClip from this list when you play this sound");
            bool oldValue = myScript.useLibrary;
            bool newValue = EditorGUILayout.Toggle(blontent, oldValue);
            if (newValue != oldValue) // If you clicked the toggle
            {
                if (newValue)
                {
                    if (myScript.files.Count == 0)
                    {
                        if (myScript.file != null)
                        {
                            myScript.files.Add(myScript.file);
                        }
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

            if (myScript.useLibrary)
            {
                blontent = new GUIContent("Never Repeat", "Sometimes, AudioManager will allow the same sound from the Audio " +
                "library to play twice in a row, enabling this option will ensure that this audio file never plays the same " +
                "sound until after it plays a different sound.");
                EditorGUILayout.PropertyField(neverRepeat, blontent);
            }

            bool noFiles = myScript.GetFile() == null && myScript.IsLibraryEmpty();

            if (noFiles)
            {
                excludedProperties.AddRange(new List<string>() { "loopSound",
                    "priority", "startingPitch", "pitchShift", "playReversed", "delay", "ignoreTimeScale", "fadeMode",
                    "safeName"
                });
            }
            else
            {
                EditorGUILayout.PropertyField(relativeVolume);
                EditorGUILayout.PropertyField(spatialize);
                using (new EditorGUI.DisabledScope(!spatialize.boolValue))
                {
                    EditorGUILayout.PropertyField(maxDistance);
                }
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            if (noFiles)
            {
                EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
            }
            if (myScript.name.Contains("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
            {
                EditorGUILayout.HelpBox("Warning! Change the name of the gameObject to something different or things will break!", MessageType.Warning);
            }

            if (!noFiles) DrawPlaybackTool(myScript);

            #region Fade Tools
            using (new EditorGUI.DisabledScope(myScript.fadeMode == FadeMode.None))
            {
                if (!myScript.IsLibraryEmpty())
                {
                    showFadeTool = EditorGUILayout.BeginFoldoutHeaderGroup(showFadeTool, new GUIContent("Fade Tools", "Show/Hide the Audio Fade previewer"));
                    if (showFadeTool && myScript.fadeMode != FadeMode.None)
                    {
                        GUIContent fContent = new GUIContent();
                        GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                        rightJustified.alignment = TextAnchor.UpperRight;
                        rightJustified.padding = new RectOffset(0, 15, 0, 0);
                        switch (myScript.fadeMode)
                        {
                            case FadeMode.FadeIn:
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(new GUIContent("Fade In Time:    " + TimeToString(fadeInDuration.floatValue * playingClip.length), "Fade in time for this AudioClip in seconds"));
                                EditorGUILayout.LabelField(new GUIContent("Sound Length: " + TimeToString(playingClip.length), "Length of the preview clip in seconds"), rightJustified);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.Slider(fadeInDuration, 0, 1);
                                break;
                            case FadeMode.FadeOut:
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(new GUIContent("Fade Out Time: " + TimeToString(fadeOutDuration.floatValue * playingClip.length), "Fade out time for this AudioClip in seconds"));
                                EditorGUILayout.LabelField(new GUIContent("Sound Length: " + TimeToString(playingClip.length), "Length of the preview clip in seconds"), rightJustified);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.Slider(fadeOutDuration, 0, 1);
                                break;
                            case FadeMode.FadeInAndOut:
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(new GUIContent("Fade In Time:    " + TimeToString(fadeInDuration.floatValue * playingClip.length), "Fade in time for this AudioClip in seconds"));
                                EditorGUILayout.LabelField(new GUIContent("Sound Length: " + TimeToString(playingClip.length), "Length of the preview clip in seconds"), rightJustified);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.LabelField(new GUIContent("Fade Out Time: " + TimeToString(fadeOutDuration.floatValue * playingClip.length), "Fade out time for this AudioClip in seconds"));
                                float fid = fadeInDuration.floatValue;
                                float fod = fadeOutDuration.floatValue;
                                fContent = new GUIContent("Fade In Percentage", "The percentage of time the sound takes to fade-in relative to it's total length.");
                                fid = Mathf.Clamp(EditorGUILayout.Slider(fContent, fid, 0, 1), 0, 1 - fod);
                                fContent = new GUIContent("Fade Out Percentage", "The percentage of time the sound takes to fade-out relative to it's total length.");
                                fod = Mathf.Clamp(EditorGUILayout.Slider(fContent, fod, 0, 1), 0, 1 - fid);
                                fadeInDuration.floatValue = fid;
                                fadeOutDuration.floatValue = fod;
                                EditorGUILayout.HelpBox("Note: The sum of your Fade-In and Fade-Out durations cannot exceed 1 (the length of the sound).", MessageType.None);
                                break;
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
            #endregion

            if (!noFiles) DrawAudioEffectTools(myScript);

            if (serializedObject.hasModifiedProperties)
            {
                forceRepaint = true;
                serializedObject.ApplyModifiedProperties();

                // Manually fix variables
                if (myScript.delay < 0)
                {
                    myScript.delay = 0;
                    Undo.RecordObject(myScript, "Fixed negative delay");
                }
                if (myScript.maxDistance < 0)
                {
                    myScript.maxDistance = 0;
                    Undo.RecordObject(myScript, "Fixed negative distance");
                }
            }

            #region Quick Reference Guide
            showHowTo = EditorGUILayout.BeginFoldoutHeaderGroup(showHowTo, "Quick Reference Guide");
            if (showHowTo)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Audio File Objects are containers that hold your sound files to be read by Audio Manager."
                    , MessageType.None);
                EditorGUILayout.HelpBox("No matter the filename or folder location, this Audio File will be referred to as it's name above"
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("If your one sound has many different variations available, try enabling the \"Use Library\" option " +
                    "just below the name field. This let's AudioManager play a random different sound whenever you choose to play from this audio file object."
                    , MessageType.None);
                EditorGUILayout.HelpBox("Relative volume only helps to reduce how loud a sound is. To increase how loud an individual sound is, you'll have to " +
                    "edit it using a sound editor."
                    , MessageType.None);
                EditorGUILayout.HelpBox("You can always check what audio file objects you have loaded in AudioManager's library by selecting the AudioManager " +
                    "in the inspector and clicking on the drop-down near the bottom."
                    , MessageType.None);
                EditorGUILayout.HelpBox("If you want to better organize your audio file objects in AudioManager's library, you can assign a " +
                    "category to this audio file object. Use the \"Hidden\" category to hide your audio file object from the library list completely."
                    , MessageType.None);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            #endregion  
        }

        void DrawPlaybackTool(AudioFileObject myScript)
        {
            GUIContent fContent = new GUIContent("Audio Playback Preview", 
                "Allows you to preview how your AudioFileObject will sound during runtime right here in the inspector. " +
                "Some effects, like spatialization and delay, will not be available to preview");
            showPlaybackTool = EditorGUILayout.BeginFoldoutHeaderGroup(showPlaybackTool, fContent);

            if (playingClip == null)
            {
                DesignateActiveAudioClip(myScript);
            }

            if (showPlaybackTool)
            {
                if (helperSource == null) CreateAudioHelper();
                ProgressBar(helperSource.time / playingClip.length, GetInfoString());

                EditorGUILayout.BeginHorizontal();
                Color colorbackup = GUI.backgroundColor;
                if (clipPlaying)
                {
                    GUI.backgroundColor = buttonPressedColor;
                    fContent = new GUIContent("Stop", "Stop playback");
                }
                else
                {
                    fContent = new GUIContent("Play", "Play a preview of the sound with it's current sound settings.");
                }
                if (GUILayout.Button(fContent))
                {
                    helperSource.Stop();
                    if (playingClip != null && !clipPlaying)
                    {
                        StartFading(myScript);
                    }
                    else
                    {
                        clipPlaying = false;
                    }
                }
                GUI.backgroundColor = colorbackup;
                using (new EditorGUI.DisabledScope(myScript.GetFileCount() < 2))
                {
                    if (GUILayout.Button(new GUIContent("Play Random", "Preview settings with a random track from your library. Only usable if this Audio File has \"Use Library\" enabled.")))
                    {
                        DesignateRandomAudioClip(myScript);
                        helperSource.Stop();
                        StartFading(myScript);
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public void DesignateActiveAudioClip(AudioFileObject myScript)
        {
            AudioClip theClip = null;
            if (!myScript.IsLibraryEmpty())
            {
                theClip = myScript.GetFirstAvailableFile();
            }
            if (theClip != null)
            {
                playingClip = theClip;
            }
        }

        public void DesignateRandomAudioClip(AudioFileObject myScript)
        {
            AudioClip theClip = playingClip;
            if (!myScript.IsLibraryEmpty())
            {
                List<AudioClip> files = myScript.GetFiles();
                while (theClip == null || theClip == playingClip)
                {
                    theClip = files[Random.Range(0, files.Count)];
                }
            }
            playingClip = theClip;
            playingRandom = true;
            forceRepaint = true;
        }

        void Update()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            if (myScript == null) return; // This can happen on the same frame it's deleted
            AudioClip clip = myScript.GetFirstAvailableFile();
            if (playingClip != null && clip != null)
            {
                if (clip != cachedClip)
                {
                    forceRepaint = true;
                    cachedClip = myScript.GetFirstAvailableFile();
                    playingClip = cachedClip;
                }

                if (!clipPlaying && playingRandom)
                {
                    DesignateActiveAudioClip(myScript);
                }

                if (clipPlaying)
                {
                    Repaint();
                }

                if (myScript.fadeMode != FadeMode.None)
                {
                    HandleFading(myScript);
                }
                else
                {
                    helperSource.volume = myScript.relativeVolume;
                }
            }
            clipPlaying = (playingClip != null && helperSource.isPlaying);
        }

        void OnEnable()
        {
            myScript = (AudioFileObject)target;

            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
            CreateAudioHelper();
            Undo.postprocessModifications += ApplyHelperEffects;
            CheckIfRegistered();
            myName = AudioManagerEditor.ConvertToAlphanumeric(target.name);

            file = serializedObject.FindProperty("file");
            files = serializedObject.FindProperty("files");

            neverRepeat = serializedObject.FindProperty("neverRepeat");

            fadeInDuration = serializedObject.FindProperty("fadeInDuration");
            fadeOutDuration = serializedObject.FindProperty("fadeOutDuration");
            relativeVolume = serializedObject.FindProperty("relativeVolume");
            spatialize = serializedObject.FindProperty("spatialize");
            maxDistance = serializedObject.FindProperty("maxDistance");

            bypassEffects = serializedObject.FindProperty("bypassEffects");
            bypassListenerEffects = serializedObject.FindProperty("bypassListenerEffects");
            bypassReverbZones = serializedObject.FindProperty("bypassReverbZones");
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyAudioHelper();
            Undo.postprocessModifications -= ApplyHelperEffects;
        }

        void OnUndoRedo()
        {
            forceRepaint = true;
        }

        public UndoPropertyModification[] ApplyHelperEffects(UndoPropertyModification[] modifications)
        {
            if (helperSource.isPlaying)
            {
                helperHelper.ApplyEffects();
            }
            return modifications;
        }

        public void CheckIfRegistered()
        {
            if (AudioManager.instance)
            {
                // Check if this file is actually relevant to the AudioManager
                if (AssetDatabase.GetAssetPath(target).Contains(AudioManager.instance.GetAudioFolderLocation()))
                {
                    relevant = true;
                    if (!AudioManager.instance.GetSoundLibrary().Contains((AudioFileObject)target))
                    {
                        unregistered = true;
                    }
                }
            }
        }

        public void CheckIfNameChanged()
        {
            myName = AudioManagerEditor.ConvertToAlphanumeric(target.name);

            List<string> names = new List<string>();
            names.AddRange(AudioManager.instance.GetSceneSoundEnum().GetEnumNames());
            if (!names.Contains(myName))
            {
                nameChanged = true;
            }
            else nameChanged = false;
        }

        FadeMode fadeMode;
        GameObject helperObject;
        float fadeInTime, fadeOutTime;
        AudioSource helperSource;
        AudioChannelHelper helperHelper;

        void CreateAudioHelper()
        {
            if (helperObject == null)
            {
                helperObject = GameObject.Find("JSAM Audio Helper");
                if (helperObject == null)
                    helperObject = new GameObject("JSAM Audio Helper");
                helperHelper = helperObject.AddComponent<AudioChannelHelper>();
                helperObject.hideFlags = HideFlags.HideAndDontSave;
            }

            if (helperSource == null)
            {
                helperSource = helperObject.AddComponent<AudioSource>();
            }
            helperHelper.Init();
        }

        void DestroyAudioHelper()
        {
            helperSource.Stop();
            DestroyImmediate(helperObject);
        }

        void HandleFading(AudioFileObject myScript)
        {
            if (helperSource.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                switch (fadeMode)
                {
                    case FadeMode.FadeIn:
                        if (helperSource.time < fadeInTime)
                        {
                            if (fadeInTime == float.Epsilon)
                            {
                                helperSource.volume = myScript.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, myScript.relativeVolume, helperSource.time / fadeInTime);
                            }
                        }
                        else helperSource.volume = myScript.relativeVolume;
                        break;
                    case FadeMode.FadeOut:
                        if (helperSource.time >= playingClip.length - fadeOutTime)
                        {
                            if (fadeOutTime == float.Epsilon)
                            {
                                helperSource.volume = myScript.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, myScript.relativeVolume, (playingClip.length - helperSource.time) / fadeOutTime);
                            }
                        }
                        break;
                    case FadeMode.FadeInAndOut:
                        if (helperSource.time < playingClip.length - fadeOutTime)
                        {
                            if (fadeInTime == float.Epsilon)
                            {
                                helperSource.volume = myScript.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, myScript.relativeVolume, helperSource.time / fadeInTime);
                            }
                        }
                        else
                        {
                            if (fadeOutTime == float.Epsilon)
                            {
                                helperSource.volume = myScript.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, myScript.relativeVolume, (playingClip.length - helperSource.time) / fadeOutTime);
                            }
                        }
                        break;
                }
            }
        }

        void StartFading(AudioFileObject myScript)
        {
            CreateAudioHelper();
            fadeMode = myScript.fadeMode;
            fadeInTime = myScript.fadeInDuration * playingClip.length;
            fadeOutTime = myScript.fadeOutDuration * playingClip.length;
            // To prevent divisions by 0
            if (fadeInTime == 0) fadeInTime = float.Epsilon;
            if (fadeOutTime == 0) fadeOutTime = float.Epsilon;
            helperSource.clip = playingClip;
            helperHelper.PlayDebug(myScript);
        }

        /// <summary>
        /// Conveniently draws a progress bar
        /// Referenced from the official Unity documentation
        /// https://docs.unity3d.com/ScriptReference/Editor.html
        /// </summary>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        Rect ProgressBar(float value, string label)
        {
            // Get a rect for the progress bar using the same margins as a TextField:
            Rect rect = GUILayoutUtility.GetRect(64, 64, "TextField");

            AudioClip sound = playingClip;

            if (cachedTex == null || forceRepaint)
            {
                Texture2D waveformTexture = PaintWaveformSpectrum(sound, (int)rect.width, (int)rect.height, new Color(1, 0.5f, 0));
                cachedTex = waveformTexture;
                if (waveformTexture != null)
                    GUI.DrawTexture(rect, waveformTexture);
                forceRepaint = false;
            }
            else
            {
                GUI.DrawTexture(rect, cachedTex);
            }

            if (playingClip != null)
            {
                Rect progressRect = new Rect(rect);
                progressRect.width *= value;
                progressRect.xMin = progressRect.xMax - 1;
                GUI.Box(progressRect, "", "SelectionRect");
            }

            EditorGUILayout.Space();

            return rect;
        }

        /// <summary>
        /// Code from these gents
        /// https://answers.unity.com/questions/189886/displaying-an-audio-waveform-in-the-editor.html
        /// </summary>
        public Texture2D PaintWaveformSpectrum(AudioClip audio, int width, int height, Color col)
        {
            if (Event.current.type != EventType.Repaint) return null;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float[] samples = new float[audio.samples * audio.channels];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);

            int packSize = (samples.Length / width) + 1;
            int s = 0;
            for (int i = 0; i < samples.Length; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }

            float fadeInDuration = myScript.fadeInDuration;
            float fadeOutDuration = myScript.fadeOutDuration;

            Color lightShade = new Color(0.3f, 0.3f, 0.3f);
            int halfHeight = (int)(height / 2);
            switch (myScript.fadeMode)
            {
                case FadeMode.None:
                    {
                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                tex.SetPixel(x, y, lightShade);
                            }
                        }
                    }
                    break;
                case FadeMode.FadeIn:
                    {
                        for (int x = 0; x < (int)(fadeInDuration * width); x++)
                        {
                            int amountToPaint = (int)Mathf.Lerp(0, halfHeight, ((float)x / (float)width) / fadeInDuration);
                            for (int y = halfHeight; y >= 0; y--)
                            {
                                switch (amountToPaint)
                                {
                                    case 0:
                                        tex.SetPixel(x, y, Color.black);
                                        break;
                                    default:
                                        tex.SetPixel(x, y, lightShade);
                                        break;
                                }
                                amountToPaint = Mathf.Clamp(amountToPaint - 1, 0, height);
                            }
                            for (int y = halfHeight; y < height; y++)
                            {
                                tex.SetPixel(x, halfHeight - y, tex.GetPixel(x, y - halfHeight));
                            }
                        }
                        for (int x = (int)(fadeInDuration * width); x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                tex.SetPixel(x, y, lightShade);
                            }
                        }
                    }
                    break;
                case FadeMode.FadeOut:
                    for (int x = 0; x < width - (int)(fadeOutDuration * width); x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            tex.SetPixel(x, y, lightShade);
                        }
                    }

                    for (int x = width - (int)(fadeOutDuration * width); x < width; x++)
                    {
                        int amountToPaint = (int)Mathf.Lerp(0, halfHeight, ((width - (float)x) / (float)width) / fadeOutDuration);
                        for (int y = halfHeight; y >= 0; y--)
                        {
                            switch (amountToPaint)
                            {
                                case 0:
                                    tex.SetPixel(x, y, Color.black);
                                    break;
                                default:
                                    tex.SetPixel(x, y, lightShade);
                                    break;
                            }
                            amountToPaint = Mathf.Clamp(amountToPaint - 1, 0, height);
                        }
                        for (int y = halfHeight; y < height; y++)
                        {
                            tex.SetPixel(x, halfHeight - y, tex.GetPixel(x, y - halfHeight));
                        }
                    }
                    break;
                case FadeMode.FadeInAndOut:
                    {
                        for (int x = 0; x < (int)(fadeInDuration * width); x++)
                        {
                            int amountToPaint = (int)Mathf.Lerp(0, halfHeight, ((float)x / (float)width) / fadeInDuration);
                            for (int y = halfHeight; y >= 0; y--)
                            {
                                switch (amountToPaint)
                                {
                                    case 0:
                                        tex.SetPixel(x, y, Color.black);
                                        break;
                                    default:
                                        tex.SetPixel(x, y, lightShade);
                                        break;
                                }
                                amountToPaint = Mathf.Clamp(amountToPaint - 1, 0, height);
                            }
                            for (int y = halfHeight; y < height; y++)
                            {
                                tex.SetPixel(x, halfHeight - y, tex.GetPixel(x, y - halfHeight));
                            }
                        }

                        for (int x = (int)(fadeInDuration * width); x < width - (int)(fadeOutDuration * width); x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                tex.SetPixel(x, y, lightShade);
                            }
                        }

                        for (int x = width - (int)(fadeOutDuration * width); x < width; x++)
                        {
                            int amountToPaint = (int)Mathf.Lerp(0, halfHeight, ((width - (float)x) / (float)width) / fadeOutDuration);
                            for (int y = halfHeight; y >= 0; y--)
                            {
                                switch (amountToPaint)
                                {
                                    case 0:
                                        tex.SetPixel(x, y, Color.black);
                                        break;
                                    default:
                                        tex.SetPixel(x, y, lightShade);
                                        break;
                                }
                                amountToPaint = Mathf.Clamp(amountToPaint - 1, 0, height);
                            }
                            for (int y = halfHeight; y < height; y++)
                            {
                                tex.SetPixel(x, halfHeight - y, tex.GetPixel(x, y - halfHeight));
                            }
                        }
                    }
                    break;
            }
            
            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * myScript.relativeVolume); y++)
                {
                    Color currentPixelColour = tex.GetPixel(x, (height / 2) + y);
                    if (currentPixelColour == Color.black) continue;

                    tex.SetPixel(x, (height / 2) + y, currentPixelColour + col * 0.75f);

                    currentPixelColour = tex.GetPixel(x, (height / 2) - y);
                    tex.SetPixel(x, (height / 2) - y, currentPixelColour + col * 0.75f);
                }
            }
            tex.Apply();

            return tex;
        }

        public static string TimeToString(float time)
        {
            time *= 1000;
            int minutes = (int)time / 60000;
            int seconds = (int)time / 1000 - 60 * minutes;
            int milliseconds = (int)time - minutes * 60000 - 1000 * seconds;
            return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        }

        /// <summary>
        /// Allows for multi-editing of categories
        /// </summary>
        /// <param name="category"></param>
        void SetCategory(object category)
        {
            string c = category.ToString();
            Undo.RecordObjects(Selection.objects, "Modified Category");
            foreach (var g in Selection.objects)
            {
                AudioFileObject obj = (AudioFileObject)g;
                if (obj != null){
                    obj.category = c;
                }
            }
            AudioManager.instance.UpdateAudioFileObjectCategories();
        }

        #region Audio Effect Rendering
        static bool showAudioEffects;
        static bool chorusFoldout;
        static bool distortionFoldout;
        static bool echoFoldout;
        static bool highPassFoldout;
        static bool lowPassFoldout;
        static bool reverbFoldout;

        SerializedProperty bypassEffects;
        SerializedProperty bypassListenerEffects;
        SerializedProperty bypassReverbZones;

        void DrawAudioEffectTools(AudioFileObject myScript)
        {
            GUIContent blontent = new GUIContent("Audio Effects Stack", "");
            showAudioEffects = EditorGUILayout.BeginFoldoutHeaderGroup(showAudioEffects, blontent);
            if (showAudioEffects)
            {
                EditorGUILayout.PropertyField(bypassEffects);
                EditorGUILayout.PropertyField(bypassListenerEffects);
                EditorGUILayout.PropertyField(bypassReverbZones);
                if (myScript.chorusFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (chorusFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Chorus Filter", "Applies a Chorus Filter to the sound when its played. " +
                        "The Audio Chorus Filter takes an Audio Clip and processes it creating a chorus effect. " +
                        "The output sounds like there are multiple sources emitting the same sound with slight variations (resembling a choir).");
                    EditorGUILayout.BeginHorizontal();
                    chorusFoldout = EditorGUILayout.Foldout(chorusFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Chorus Filter");
                        myScript.chorusFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (chorusFoldout)
                    {
                        Undo.RecordObject(myScript, "Modified Distortion Filter");
                        blontent = new GUIContent("Dry Mix", "Volume of original signal to pass to output");
                        myScript.chorusFilter.dryMix = 
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.dryMix, 0, 1);
                        blontent = new GUIContent("Wet Mix 1", "Volume of 1st chorus tap");
                        myScript.chorusFilter.wetMix1 =
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.wetMix1, 0, 1);
                        blontent = new GUIContent("Wet Mix 2", "Volume of 2nd chorus tap");
                        myScript.chorusFilter.wetMix2 =
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.wetMix2, 0, 1);
                        blontent = new GUIContent("Wet Mix 3", "Volume of 2nd chorus tap");
                        myScript.chorusFilter.wetMix3 =
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.wetMix3, 0, 1);
                        blontent = new GUIContent("Delay", "Chorus delay in ms");
                        myScript.chorusFilter.delay =
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.delay, 0, 100);
                        blontent = new GUIContent("Rate", "Chorus modulation rate in hertz");
                        myScript.chorusFilter.rate =
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.rate, 0, 20);
                        blontent = new GUIContent("Depth", "Chorus modulation depth");
                        myScript.chorusFilter.depth =
                            EditorGUILayout.Slider(blontent, myScript.chorusFilter.depth, 0, 1);
                    }
                    EditorGUILayout.EndVertical();
                }
                if (myScript.distortionFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (distortionFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Distortion Filter", "Distorts the sound when its played.");
                    EditorGUILayout.BeginHorizontal();
                    distortionFoldout = EditorGUILayout.Foldout(distortionFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Distortion Filter");
                        myScript.distortionFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (distortionFoldout)
                    {
                        blontent = new GUIContent("Distortion Level", "Amount of distortion to apply");
                        float cf = myScript.highPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, myScript.distortionFilter.distortionLevel, 0, 1);

                        if (cf != myScript.distortionFilter.distortionLevel)
                        {
                            Undo.RecordObject(myScript, "Modified Distortion Filter");
                            myScript.distortionFilter.distortionLevel = cf;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (myScript.echoFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (echoFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Echo Filter", "Repeats a sound after a given Delay, attenuating the repetitions based on the Decay Ratio");
                    EditorGUILayout.BeginHorizontal();
                    echoFoldout = EditorGUILayout.Foldout(echoFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Echo Filter");
                        myScript.echoFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (echoFoldout)
                    {
                        Undo.RecordObject(myScript, "Modified Echo Filter");
                        blontent = new GUIContent("Delay", "Echo delay in ms");
                        myScript.echoFilter.delay =
                            EditorGUILayout.Slider(blontent, myScript.echoFilter.delay, 10, 5000);

                        blontent = new GUIContent("Decay Ratio", "Echo decay per delay");
                        myScript.echoFilter.decayRatio =
                            EditorGUILayout.Slider(blontent, myScript.echoFilter.decayRatio, 0, 1);

                        blontent = new GUIContent("Wet Mix", "Volume of echo signal to pass to output");
                        myScript.echoFilter.wetMix =
                            EditorGUILayout.Slider(blontent, myScript.echoFilter.wetMix, 0, 1);

                        blontent = new GUIContent("Dry Mix", "Volume of original signal to pass to output");
                        myScript.echoFilter.dryMix =
                            EditorGUILayout.Slider(blontent, myScript.echoFilter.dryMix, 0, 1);

                        EditorGUILayout.HelpBox("Note: Echoes are best tested during runtime as they do not behave properly in-editor.", MessageType.None);
                    }
                    EditorGUILayout.EndVertical();
                }
                if (myScript.lowPassFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (lowPassFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Low Pass Filter", "Filters the audio to let lower frequencies pass while removing frequencies higher than the cutoff");
                    EditorGUILayout.BeginHorizontal();
                    lowPassFoldout = EditorGUILayout.Foldout(lowPassFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Low Pass Filter");
                        myScript.lowPassFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (lowPassFoldout)
                    {
                        blontent = new GUIContent("Cutoff Frequency", "Low-pass cutoff frequency in hertz");
                        float cf = myScript.lowPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, myScript.lowPassFilter.cutoffFrequency, 10, 22000);

                        blontent = new GUIContent("Low Pass Resonance Q", "Determines how much the filter's self-resonance is dampened");
                        float q = myScript.lowPassFilter.lowpassResonanceQ;
                        q = EditorGUILayout.Slider(
                            blontent, myScript.lowPassFilter.lowpassResonanceQ, 1, 10);

                        if (cf != myScript.lowPassFilter.cutoffFrequency || q != myScript.lowPassFilter.lowpassResonanceQ)
                        {
                            Undo.RecordObject(myScript, "Modified Low Pass Filter");
                            myScript.lowPassFilter.cutoffFrequency = cf;
                            myScript.lowPassFilter.lowpassResonanceQ = q;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (myScript.highPassFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (highPassFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " High Pass Filter", "Filters the audio to let higher frequencies pass while removing frequencies lower than the cutoff");
                    EditorGUILayout.BeginHorizontal();
                    highPassFoldout = EditorGUILayout.Foldout(highPassFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed High Pass Filter");
                        myScript.highPassFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (highPassFoldout)
                    {
                        blontent = new GUIContent("Cutoff Frequency", "High-pass cutoff frequency in hertz");
                        float cf = myScript.highPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, myScript.highPassFilter.cutoffFrequency, 10, 22000);

                        blontent = new GUIContent("High Pass Resonance Q", "Determines how much the filter's self-resonance is dampened");
                        float q = myScript.highPassFilter.highpassResonanceQ;
                        q = EditorGUILayout.Slider(
                            blontent, myScript.highPassFilter.highpassResonanceQ, 1, 10);

                        if (cf != myScript.highPassFilter.cutoffFrequency || q != myScript.highPassFilter.highpassResonanceQ)
                        {
                            Undo.RecordObject(myScript, "Modified High Pass Filter");
                            myScript.highPassFilter.cutoffFrequency = cf;
                            myScript.highPassFilter.highpassResonanceQ = q;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (myScript.reverbFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (reverbFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Reverb Filter", "Modifies the sound to make it feel like it's reverberating around a room");
                    EditorGUILayout.BeginHorizontal();
                    reverbFoldout = EditorGUILayout.Foldout(reverbFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Reverb Filter");
                        myScript.reverbFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (reverbFoldout)
                    {
                        Undo.RecordObject(myScript, "Modified Reverb Filter");
                        blontent = new GUIContent("Reverb Preset", "Custom reverb presets, select \"User\" to create your own customized reverb effects. You are highly recommended to use a preset.");
                        myScript.reverbFilter.reverbPreset = (AudioReverbPreset)EditorGUILayout.EnumPopup(
                            blontent, myScript.reverbFilter.reverbPreset);

                        using (new EditorGUI.DisabledScope(myScript.reverbFilter.reverbPreset != AudioReverbPreset.User))
                        {
                            blontent = new GUIContent("Dry Level", "Mix level of dry signal in output in mB");
                            myScript.reverbFilter.dryLevel = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.dryLevel, -10000, 0);
                            blontent = new GUIContent("Room", "Room effect level at low frequencies in mB");
                            myScript.reverbFilter.room = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.room, -10000, 0);
                            blontent = new GUIContent("Room HF", "Room effect high-frequency level in mB");
                            myScript.reverbFilter.roomHF = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.roomHF, -10000, 0);
                            blontent = new GUIContent("Room LF", "Room effect low-frequency level in mB");
                            myScript.reverbFilter.roomLF = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.roomLF, -10000, 0);
                            blontent = new GUIContent("Decay Time", "Reverberation decay time at low-frequencies in seconds");
                            myScript.reverbFilter.decayTime = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.decayTime, 0.1f, 20);
                            blontent = new GUIContent("Decay HFRatio", "Decay HF Ratio : High-frequency to low-frequency decay time ratio");
                            myScript.reverbFilter.decayHFRatio = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.decayHFRatio, 0.1f, 20);
                            blontent = new GUIContent("Reflections Level", "Early reflections level relative to room effect in mB");
                            myScript.reverbFilter.reflectionsLevel = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.reflectionsLevel, -10000, 1000);
                            blontent = new GUIContent("Reflections Delay", "Early reflections delay time relative to room effect in mB");
                            myScript.reverbFilter.reflectionsDelay = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.reflectionsDelay, 0, 0.3f);
                            blontent = new GUIContent("Reverb Level", "Late reverberation level relative to room effect in mB");
                            myScript.reverbFilter.reverbLevel = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.reverbLevel, -10000, 2000);
                            blontent = new GUIContent("Reverb Delay", "Late reverberation delay time relative to first reflection in seconds");
                            myScript.reverbFilter.reverbDelay = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.reverbDelay, 0, 0.1f);
                            blontent = new GUIContent("HFReference", "Reference high frequency in Hz");
                            myScript.reverbFilter.hFReference = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.hFReference, 1000, 20000);
                            blontent = new GUIContent("LFReference", "Reference low frequency in Hz");
                            myScript.reverbFilter.lFReference = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.lFReference, 20, 1000);
                            blontent = new GUIContent("Diffusion", "Reverberation diffusion (echo density) in percent");
                            myScript.reverbFilter.diffusion = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.diffusion, 0, 100);
                            blontent = new GUIContent("Density", "Reverberation density (modal density) in percent");
                            myScript.reverbFilter.density = EditorGUILayout.Slider(
                                blontent, myScript.reverbFilter.density, 0, 100);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                #region Add New Effect Button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add New Effect", new GUILayoutOption[] { GUILayout.MaxWidth(200) }))
                {
                    GenericMenu menu = new GenericMenu();
                    blontent = new GUIContent("Chorus Filter");
                    if (myScript.chorusFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableChorus);
                    blontent = new GUIContent("Distortion Filter");
                    if (myScript.distortionFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableDistortion);
                    blontent = new GUIContent("Echo Filter");
                    if (myScript.echoFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableEcho);
                    blontent = new GUIContent("High Pass Filter");
                    if (myScript.highPassFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableHighPass);
                    blontent = new GUIContent("Low Pass Filter");
                    if (myScript.lowPassFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableLowPass);
                    blontent = new GUIContent("Reverb Filter");
                    if (myScript.reverbFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableReverb);
                    menu.ShowAsContext();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                #endregion
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void EnableChorus()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            Undo.RecordObject(myScript, "Added Effect");
            myScript.chorusFilter.enabled = true;
            myScript.chorusFilter.dryMix = 0.5f;
            myScript.chorusFilter.wetMix1 = 0.5f;
            myScript.chorusFilter.wetMix2 = 0.5f;
            myScript.chorusFilter.wetMix3 = 0.5f;
            myScript.chorusFilter.delay = 40;
            myScript.chorusFilter.rate = 0.8f;
            myScript.chorusFilter.depth = 0.03f;
        }

        void EnableDistortion()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            Undo.RecordObject(myScript, "Added Effect");
            myScript.distortionFilter.enabled = true;
            myScript.distortionFilter.distortionLevel = 0.5f;
        }

        void EnableEcho()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            Undo.RecordObject(myScript, "Added Effect");
            myScript.echoFilter.enabled = true;
            myScript.echoFilter.delay = 500;
            myScript.echoFilter.decayRatio = 0.5f;
            myScript.echoFilter.wetMix = 1;
            myScript.echoFilter.dryMix = 1;
        }

        void EnableHighPass()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            Undo.RecordObject(myScript, "Added Effect");
            myScript.highPassFilter.enabled = true;
            myScript.highPassFilter.cutoffFrequency = 5000;
            myScript.highPassFilter.highpassResonanceQ = 1;
        }

        void EnableLowPass()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            Undo.RecordObject(myScript, "Added Effect");
            myScript.lowPassFilter.enabled = true;
            myScript.lowPassFilter.cutoffFrequency = 5000;
            myScript.lowPassFilter.lowpassResonanceQ = 1;
        }

        void EnableReverb()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            Undo.RecordObject(myScript, "Added Effect");
            myScript.reverbFilter.enabled = true;
            myScript.reverbFilter.reverbPreset = AudioReverbPreset.Generic;
            myScript.reverbFilter.dryLevel = 0;
            myScript.reverbFilter.room = 0;
            myScript.reverbFilter.roomHF = 0;
            myScript.reverbFilter.roomLF = 0;
            myScript.reverbFilter.decayTime = 1;
            myScript.reverbFilter.decayHFRatio = 0.5f;
            myScript.reverbFilter.reflectionsLevel = -10000.0f;
            myScript.reverbFilter.reflectionsDelay = 0;
            myScript.reverbFilter.reverbLevel = 0;
            myScript.reverbFilter.reverbDelay = 0.04f;
            myScript.reverbFilter.hFReference = 5000;
            myScript.reverbFilter.lFReference = 250;
            myScript.reverbFilter.diffusion = 100;
            myScript.reverbFilter.density = 100;
        }
        #endregion
    }
}