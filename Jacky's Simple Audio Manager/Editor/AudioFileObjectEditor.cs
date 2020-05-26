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
        Color buttonPressedColor = new Color(0.475f, 0.475f, 0.475f);

        static bool showFadeTool;
        static bool showPlaybackTool;

        static bool showHowTo;

        AudioClip playingClip;

        bool clipPlaying;
        bool playingRandom;

        Texture2D cachedTex;
        bool forceRepaint;
        AudioClip cachedClip;

        static bool testBool;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioFileObject myScript = (AudioFileObject)target;

            EditorGUILayout.LabelField("Audio File Object", EditorStyles.boldLabel);

            string theName = AudioManagerEditor.ConvertToAlphanumeric(myScript.name);
            EditorGUILayout.LabelField(new GUIContent("Name: ", "This is the name that AudioManager will use to reference this object with."), new GUIContent(theName));

            #region Category Inspector
            EditorGUILayout.BeginHorizontal();
            GUIContent blontent = new GUIContent("Category", "An optional field that lets you further sort your AudioFileObjects for better organization in AudioManager's library view.");
            string newCategory = EditorGUILayout.DelayedTextField(blontent, myScript.category);
            List<string> categories = new List<string>();
            // Check if we're modifying this AudioFileObject in a valid scene
            if (AudioManager.instance != null)
            {
                categories.AddRange(AudioManager.instance.GetCategories());
            }
            if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
            {
                AudioManager.instance.InitializeCategories();
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

            List<string> excludedProperties = new List<string>() { "m_Script", "file", "files" };

            if (myScript.UsingLibrary()) // Swap file with files
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("files"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("file"));
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

            bool noFiles = myScript.GetFile() == null && myScript.IsLibraryEmpty();

            if (noFiles)
            {
                excludedProperties.AddRange(new List<string>() { "relativeVolume", "spatialize", "loopSound", "priority", "pitchShift", "delay", "ignoreTimeScale", "fadeMode" });
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
                    if (showFadeTool)
                    {
                        GUIContent fContent = new GUIContent();
                        SerializedProperty fadeInDuration = serializedObject.FindProperty("fadeInDuration");
                        SerializedProperty fadeOutDuration = serializedObject.FindProperty("fadeOutDuration");
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

            DrawAudioEffectTools(myScript);

            if (serializedObject.hasModifiedProperties)
            {
                forceRepaint = true;
                serializedObject.ApplyModifiedProperties();
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
        }

        void Update()
        {
            if (playingClip != null)
            {
                AudioFileObject myScript = (AudioFileObject)target;
                if (playingClip != cachedClip)
                {
                    forceRepaint = true;
                    cachedClip = myScript.GetFirstAvailableFile();
                }

                if (!clipPlaying && playingRandom)
                {
                    DesignateActiveAudioClip(myScript);
                }

                if (clipPlaying)
                {
                    Repaint();
                }
                HandleFading(myScript);
            }
            clipPlaying = (playingClip != null && helperSource.isPlaying);
        }

        void OnEnable()
        {
            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
            CreateAudioHelper();
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            DestroyAudioHelper();
        }

        void OnUndoRedo()
        {
            forceRepaint = true;
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
                helperSource = helperObject.AddComponent<AudioSource>();
                helperHelper = helperObject.AddComponent<AudioChannelHelper>();
                helperHelper.Init();
                helperObject.hideFlags = HideFlags.HideAndDontSave;
            }
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
            helperSource.pitch = 1 + Random.Range(-myScript.pitchShift, myScript.pitchShift);
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
            float[] samples = new float[audio.samples];
            float[] waveform = new float[width];
            audio.GetData(samples, 0);

            int packSize = (audio.samples / width) + 1;
            int s = 0;
            for (int i = 0; i < audio.samples; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }

            AudioFileObject myScript = (AudioFileObject)target;

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
        static bool lowPassFoldout;
        static bool highPassFoldout;
        static bool distortionFoldout;

        void DrawAudioEffectTools(AudioFileObject myScript)
        {
            GUIContent blontent = new GUIContent("Audio Effects Stack", "");
            showAudioEffects = EditorGUILayout.BeginFoldoutHeaderGroup(showAudioEffects, blontent);
            if (showAudioEffects)
            {
                if (myScript.lowPassFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (lowPassFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Low Pass Filter", "Applies a Low Pass Filter to the sound when its played.");
                    EditorGUILayout.BeginHorizontal();
                    lowPassFoldout = EditorGUILayout.Foldout(lowPassFoldout, blontent, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Low Pass Filter");
                        myScript.lowPassFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (lowPassFoldout)
                    {
                        blontent = new GUIContent("Cutoff Frequency");
                        float cf = myScript.lowPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, myScript.lowPassFilter.cutoffFrequency, 10, 22000);

                        blontent = new GUIContent("Low Pass Resonance Q");
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
                    blontent = new GUIContent("    " + arrow + " High Pass Filter", "Applies a High Pass Filter to the sound when its played.");
                    EditorGUILayout.BeginHorizontal();
                    highPassFoldout = EditorGUILayout.Foldout(highPassFoldout, blontent, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed High Pass Filter");
                        myScript.highPassFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (highPassFoldout)
                    {
                        blontent = new GUIContent("Cutoff Frequency");
                        float cf = myScript.highPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, myScript.highPassFilter.cutoffFrequency, 10, 22000);

                        blontent = new GUIContent("High Pass Resonance Q");
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
                if (myScript.distortionFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (distortionFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Distortion Filter", "Applies a Distortion Filter to the sound when its played.");
                    EditorGUILayout.BeginHorizontal();
                    distortionFoldout = EditorGUILayout.Foldout(distortionFoldout, blontent, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(myScript, "Removed Distortion Filter");
                        myScript.distortionFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (distortionFoldout)
                    {
                        blontent = new GUIContent("Distortion Level");
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
                #region Add New Effect Button
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add New Effect", new GUILayoutOption[] { GUILayout.MaxWidth(200) }))
                {
                    GenericMenu menu = new GenericMenu();
                    blontent = new GUIContent("Low Pass Filter");
                    if (myScript.lowPassFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableLowPass);
                    blontent = new GUIContent("High Pass Filter");
                    if (myScript.highPassFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableHighPass);
                    blontent = new GUIContent("Distortion Filter");
                    if (myScript.distortionFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableDistortion);
                    menu.ShowAsContext();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                #endregion
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void EnableLowPass()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            myScript.lowPassFilter.enabled = true;
            myScript.lowPassFilter.cutoffFrequency = 5000;
            myScript.lowPassFilter.lowpassResonanceQ = 1;
        }

        void EnableHighPass()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            myScript.highPassFilter.enabled = true;
            myScript.highPassFilter.cutoffFrequency = 5000;
            myScript.highPassFilter.highpassResonanceQ = 1;
        }

        void EnableDistortion()
        {
            AudioFileObject myScript = (AudioFileObject)target;
            myScript.distortionFilter.enabled = true;
            myScript.distortionFilter.distortionLevel = 0.5f;
        }
        #endregion
    }
}