using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ATL.AudioData;

namespace JSAM
{
    [CustomEditor(typeof(AudioFileMusicObject))]
    [CanEditMultipleObjects]
    public class AudioFileMusicObjectEditor : Editor
    {
        public enum LoopPointTool
        {
            Slider,
            TimeInput,
            TimeSamplesInput,
            BPMInput//WithBeats,
                    //BPMInputWithBars
        }

        Color buttonPressedColor = new Color(0.475f, 0.475f, 0.475f);
        Color buttonPressedColorLighter = new Color(0.75f, 0.75f, 0.75f);

        bool clipPlaying = false;
        bool clipPaused = false;

        bool mouseDragging = false;
        bool loopClip = false;

        /// <summary>
        /// True so long as the inspector music player hasn't looped
        /// </summary>
        bool firstPlayback = true;
        bool freePlay = false;

        static bool showLoopPointTool;
        static bool showPlaybackTool;
        static bool showHowTo;

        static int loopPointInputMode = 0;

        Texture2D cachedTex;
        bool forceRepaint;
        AudioClip cachedClip;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioFileMusicObject myScript = (AudioFileMusicObject)target;

            EditorGUILayout.LabelField("Audio File Music Object", EditorStyles.boldLabel);

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
                categories.AddRange(AudioManager.instance.GetMusicCategories());
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

            List<string> propertiesToExclude = new List<string>() { "spatialSound", "loopSound", "priority", "pitchShift", "delay", "loopMode", "fadeMode" };
            if (myScript.GetFile() == null)
            {
                propertiesToExclude.AddRange(new List<string>() { "m_Script", "useLibrary", "files",
                "fadeMode", "clampBetweenLoopPoints", "spatialize", "ignoreTimeScale", "relativeVolume" });
            }
            else
            {
                propertiesToExclude.AddRange(new List<string>() { "m_Script", "useLibrary", "files" });
            }

            DrawPropertiesExcluding(serializedObject, propertiesToExclude.ToArray());

            if (myScript.GetFile() == null)
            {
                EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
            }
            if (myScript.name.Contains("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
            {
                EditorGUILayout.HelpBox("Warning! Change the name of this file to something different or things will break!", MessageType.Warning);
            }

            #region Loop Point Tools
            if (myScript.GetFile() != null)
            {
                AudioClip music = myScript.GetFile();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loopMode"));
                using (new EditorGUI.DisabledScope(myScript.loopMode != LoopMode.LoopWithLoopPoints))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("clampToLoopPoints"));

                DrawPlaybackTool(myScript);

                using (new EditorGUI.DisabledScope(myScript.loopMode != LoopMode.LoopWithLoopPoints))
                {
                    blontent = new GUIContent("Loop Point Tools", "Customize where music will loop between. " +
                        "Loops may not appear to be seamless in the inspector but rest assured, they will be seamless in-game!");
                    showLoopPointTool = EditorGUILayout.BeginFoldoutHeaderGroup(showLoopPointTool, blontent);
                    if (showLoopPointTool)
                    {
                        GUIContent[] contents = new GUIContent[] { new GUIContent("Slider"), new GUIContent("Time"), new GUIContent("Samples"), new GUIContent("BPM") };
                        EditorGUILayout.BeginHorizontal();
                        Color colorbackup = GUI.backgroundColor;
                        for (int i = 0; i < contents.Length; i++)
                        {
                            if (i == loopPointInputMode) GUI.backgroundColor = buttonPressedColorLighter;
                            if (GUILayout.Button(contents[i], EditorStyles.miniButtonMid)) loopPointInputMode = i;
                            GUI.backgroundColor = colorbackup;
                        }
                        EditorGUILayout.EndHorizontal();

                        float loopStart = myScript.loopStart;
                        float loopEnd = myScript.loopEnd;

                        switch ((LoopPointTool)loopPointInputMode)
                        {
                            case LoopPointTool.Slider:
                                GUILayout.Label("Song Duration Samples: " + music.samples);
                                EditorGUILayout.MinMaxSlider(ref loopStart, ref loopEnd, 0, music.length);

                                //GUIStyle timeStyle = new GUIStyle(EditorStyles.label);
                                //timeStyle.font = 
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Loop Point Start: " + TimeToString(loopStart), new GUILayoutOption[] { GUILayout.Width(180) });
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Loop Point Start (Samples): " + myScript.loopStart * music.frequency);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Loop Point End:   " + TimeToString(loopEnd), new GUILayoutOption[] { GUILayout.Width(180) });
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Loop Point End (Samples): " + myScript.loopEnd * music.frequency);
                                GUILayout.EndHorizontal();
                                break;
                            case LoopPointTool.TimeInput:
                                EditorGUILayout.Space();

                                GUILayout.BeginHorizontal();
                                float theTime = loopStart * 1000f;
                                GUILayout.Label("Loop Point Start:");
                                int minutes = EditorGUILayout.IntField((int)(theTime / 60000f));
                                GUILayout.Label(":");
                                int seconds = Mathf.Clamp(EditorGUILayout.IntField((int)(theTime % 60000) / 1000), 0, 59);
                                GUILayout.Label(":");
                                float milliseconds = EditorGUILayout.IntField((int)(theTime % 60000) % 1000);
                                milliseconds = float.Parse("0." + milliseconds.ToString("0.####")); // Ensures that our milliseconds never leave their decimal place
                                loopStart = (float)minutes * 60f + (float)seconds + milliseconds;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                theTime = loopEnd * 1000f;
                                GUILayout.Label("Loop Point End:  ");
                                minutes = EditorGUILayout.IntField((int)theTime / 60000);
                                GUILayout.Label(":");
                                seconds = Mathf.Clamp(EditorGUILayout.IntField((int)(theTime % 60000) / 1000), 0, 59);
                                GUILayout.Label(":");
                                milliseconds = EditorGUILayout.IntField((int)(theTime % 60000) % 1000);
                                milliseconds = float.Parse("0." + milliseconds.ToString("0.####")); // Ensures that our milliseconds never leave their decimal place
                                loopEnd = (float)minutes * 60f + (float)seconds + milliseconds;
                                GUILayout.EndHorizontal();
                                break;
                            case LoopPointTool.TimeSamplesInput:
                                GUILayout.Label("Song Duration (Samples): " + music.samples);
                                EditorGUILayout.Space();

                                GUILayout.BeginHorizontal();
                                float samplesStart = EditorGUILayout.FloatField("Loop Point Start:", myScript.loopStart * music.frequency);
                                GUILayout.EndHorizontal();
                                loopStart = samplesStart / music.frequency;

                                GUILayout.BeginHorizontal();
                                float samplesEnd = Mathf.Clamp(EditorGUILayout.FloatField("Loop Point End:", myScript.loopEnd * music.frequency), 0, music.samples);
                                GUILayout.EndHorizontal();
                                loopEnd = samplesEnd / music.frequency;
                                break;
                            case LoopPointTool.BPMInput/*WithBeats*/:
                                Undo.RecordObject(myScript, "Modified song BPM");
                                myScript.bpm = EditorGUILayout.IntField("Song BPM: ", myScript.bpm/*, new GUILayoutOption[] { GUILayout.MaxWidth(30)}*/);

                                EditorGUILayout.Space();

                                float startBeat = loopStart / (60f / (float)myScript.bpm);
                                startBeat = EditorGUILayout.FloatField("Starting Beat:", startBeat);

                                float endBeat = loopEnd / (60f / (float)myScript.bpm);
                                endBeat = Mathf.Clamp(EditorGUILayout.FloatField("Ending Beat:", endBeat), 0, music.length / (60f / myScript.bpm));

                                loopStart = (float)startBeat * 60f / (float)myScript.bpm;
                                loopEnd = (float)endBeat * 60f / (float)myScript.bpm;
                                break;
                                //case AudioFileMusic.LoopPointTool.BPMInputWithBars:
                                //    GUILayout.BeginHorizontal();
                                //    GUILayout.Label("Song Duration: " + TimeToString(music.length));
                                //    myScript.bpm = EditorGUILayout.IntField("Song BPM: ", myScript.bpm);
                                //    GUILayout.EndHorizontal();
                                //
                                //    int startBar = (int)(loopStart / (60f / (float)myScript.bpm));
                                //    startBar = EditorGUILayout.IntField("Starting Bar:", startBar);
                                //
                                //    int endBar = (int)(loopEnd / (60f / (float)myScript.bpm));
                                //    endBar = EditorGUILayout.IntField("Ending Bar:", endBar);
                                //
                                //    loopStart = startBar * 60f / myScript.bpm;
                                //    loopEnd = endBar * 60f / myScript.bpm;
                                //    break;
                        }

                        GUIContent buttonText = new GUIContent("Reset Loop Points", "Click to set loop points to the start and end of the track.");
                        if (GUILayout.Button(buttonText))
                        {
                            loopStart = 0;
                            loopEnd = music.length;
                        }
                        using (new EditorGUI.DisabledScope(!myScript.IsWavFile()))
                        {
                            if (myScript.IsWavFile())
                            {
                                buttonText = new GUIContent("Import Loop Points from .WAV Metadata", "Using this option will overwrite existing loop point data. Check the quick reference guide for details!");
                            }
                            else
                            {
                                buttonText = new GUIContent("Import Loop Points from .WAV Metadata", "This option is exclusive to .WAV files. Using this option will overwrite existing loop point data. Check the quick reference guide for details!");
                            }
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button(buttonText))
                            {
                                // Zeugma440 and his Audio Tools Library is a godsend
                                // https://github.com/Zeugma440/atldotnet/
                                string filePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(myScript.file.name)[0]);
                                string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;

                                ATL.Track theTrack = new ATL.Track(trueFilePath);

                                float frequency = myScript.GetFile().frequency;

                                if (theTrack.AdditionalFields.ContainsKey("sample.SampleLoop[0].Start") && theTrack.AdditionalFields.ContainsKey("sample.SampleLoop[0].End"))
                                {
                                    loopStart = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].Start"]) / frequency;
                                    loopEnd = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].End"]) / frequency;
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error Reading Metadata", "Could not find any loop point data in " + myScript.GetFile().name + ".wav!\n" +
                                        "Are you sure you wrote loop points in this file?", "OK");
                                }

                            }
                            if (myScript.IsWavFile())
                            {
                                buttonText = new GUIContent("Save Loop Points to File", "Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                            }
                            else
                            {
                                buttonText = new GUIContent("Save Loop Points to File", "This option is exclusive to .WAV files. Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                            }
                            if (GUILayout.Button(buttonText))
                            {
                                string filePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(myScript.file.name)[0]);
                                string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;

                                ATL.Track theTrack = new ATL.Track(trueFilePath);

                                float frequency = myScript.GetFile().frequency;

                                if (EditorUtility.DisplayDialog("Confirm Loop Point saving", "This will overwrite loop point Start/End loop point markers saved in this .WAV file, are you sure you want to continue?", "Yes", "Cancel"))
                                {
                                    theTrack.AdditionalFields["sample.MIDIUnityNote"] = "60";
                                    theTrack.AdditionalFields["sample.NumSampleLoops"] = "1";
                                    theTrack.AdditionalFields["sample.SampleLoop[0].Type"] = "0";
                                    theTrack.AdditionalFields["sample.SampleLoop[0].Start"] = (Mathf.RoundToInt(myScript.loopStart * frequency)).ToString();
                                    theTrack.AdditionalFields["sample.SampleLoop[0].End"] = (Mathf.RoundToInt(myScript.loopEnd * frequency)).ToString();
                                    theTrack.Save();
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }

                        if (myScript.loopStart != loopStart || myScript.loopEnd != loopEnd)
                        {
                            Undo.RecordObject(myScript, "Modified loop point properties");
                            serializedObject.FindProperty("loopStart").floatValue = Mathf.Clamp(loopStart, 0, music.length);
                            serializedObject.FindProperty("loopEnd").floatValue = Mathf.Clamp(loopEnd, 0, Mathf.Ceil(music.length));
                            EditorUtility.SetDirty(myScript);
                            forceRepaint = true;
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
            #endregion

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
                EditorGUILayout.HelpBox("Audio File Music Objects are containers that hold your music files to be read by Audio Manager."
                    , MessageType.None);
                EditorGUILayout.HelpBox("No matter the filename or folder location, this Audio File will be referred to as it's name above"
                    , MessageType.None);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Relative volume only helps to reduce how loud a sound is. To increase how loud an individual sound is, you'll have to " +
                    "edit it using a sound editor."
                    , MessageType.None);
                EditorGUILayout.HelpBox("Before you cut up your music into an intro and looping portion, try using the loop point tools!"
                    , MessageType.None);
                EditorGUILayout.HelpBox("By designating your loop points in the loop point tools and setting your music's loop mode to " +
                    "\"Loop with Loop Points\", you can easily get AudioManager to play your intro portion once and repeat the looping portion forever!"
                    , MessageType.None);
                EditorGUILayout.HelpBox("If your music is saved in the WAV format, you can use external programs to set loop points in the file itself! " +
                    "After that, click the \"Import Loop Points from .WAV Metadata\" button above to have AudioManager to read them in."
                    , MessageType.None);
                EditorGUILayout.HelpBox("You can designate loop points in your .WAV file using programs like Wavosaur and Goldwave! Click the links " +
                    "below to learn more about how to get these free tools and create loop points with them!"
                    , MessageType.None);

                EditorGUILayout.BeginHorizontal();
                GUIContent buttonC = new GUIContent("Wavosaur", "Click here to download Wavosaur!");
                if (GUILayout.Button(buttonC))
                {
                    Application.OpenURL("https://www.wavosaur.com/");
                }
                buttonC = new GUIContent("GoldWave", "Click here to download GoldWave!");
                if (GUILayout.Button(buttonC))
                {
                    Application.OpenURL("http://www.goldwave.com/release.php");
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                buttonC = new GUIContent("How to use Wavosaur", "Click here to learn how to set Loop Points in Wavosaur!");
                if (GUILayout.Button(buttonC))
                {
                    Application.OpenURL("https://www.wavosaur.com/quick-help/loop-points-edition.php");
                }
                buttonC = new GUIContent("How to use Wavosaur", "Click here to learn how to set Loop Points in GoldWave!");
                if (GUILayout.Button(buttonC))
                {
                    Application.OpenURL("https://developer.valvesoftware.com/wiki/Looping_a_Sound");
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Otherwise, using BPM input to set your loop points is strongly recommended!"
                    , MessageType.None);
                EditorGUILayout.HelpBox("You can also choose to export your loop point data by clicking the \"Save Loop Points to File\" button " +
                    "to use in other programs!"
                    , MessageType.None);
            }
            #endregion  
        }

        /// <summary>
        /// Draws a playback 
        /// </summary>
        /// <param name="music"></param>
        public void DrawPlaybackTool(AudioFileMusicObject myScript)
        {
            GUIContent blontent = new GUIContent("Audio Playback Preview",
                "Allows you to preview how your AudioFileMusicObject will sound during runtime right here in the inspector. " +
                "Some effects, like spatialization, will not be available to preview");
            showPlaybackTool = EditorGUILayout.BeginFoldoutHeaderGroup(showPlaybackTool, blontent);

            if (showPlaybackTool)
            {
                AudioClip music = myScript.GetFile();
                Rect progressRect = ProgressBar(helperSource.time / music.length, GetInfoString());

                EditorGUILayout.BeginHorizontal();

                Event evt = Event.current;

                if (evt.isMouse)
                {
                    switch (evt.type)
                    {
                        case EventType.MouseMove:
                            break;
                        case EventType.MouseUp:
                            mouseDragging = false;
                            break;
                        case EventType.MouseDown:
                        case EventType.MouseDrag:
                            if (evt.type == EventType.MouseDown && !mouseDragging)
                            {
                                if (evt.mousePosition.y > progressRect.yMin && evt.mousePosition.y < progressRect.yMax)
                                {
                                    mouseDragging = true;
                                }
                            }
                            if (!mouseDragging) break;
                            float newProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, evt.mousePosition.x);
                            helperSource.timeSamples = Mathf.Clamp((int)(newProgress * music.length * music.frequency), 0, music.samples);
                            break;
                    }
                }

                Color colorbackup = GUI.backgroundColor;
                GUIContent buttonIcon = (clipPlaying) ? s_PlayIcons[1] : s_PlayIcons[0];
                if (clipPlaying) GUI.backgroundColor = buttonPressedColor;
                if (GUILayout.Button(buttonIcon))
                {
                    clipPlaying = !clipPlaying;
                    if (clipPlaying)
                    {
                        helperSource.volume = myScript.relativeVolume;
                        helperSource.Play();
                        // Perhaps make resetting the position optional?
                        helperSource.timeSamples = 0;
                        if (clipPaused) helperSource.Pause();
                        firstPlayback = true;
                        freePlay = false;
                    }
                    else
                    {
                        helperSource.Stop();
                        clipPaused = false;
                    }
                }

                GUI.backgroundColor = colorbackup;
                GUIContent theText = (clipPaused) ? s_PauseIcons[1] : s_PauseIcons[0];
                if (clipPaused) GUI.backgroundColor = buttonPressedColor;
                if (GUILayout.Button(theText))
                {
                    clipPaused = !clipPaused;
                    if (clipPaused)
                    {
                        helperSource.Pause();
                    }
                    else
                    {
                        helperSource.UnPause();
                    }
                }

                GUI.backgroundColor = colorbackup;
                buttonIcon = (loopClip) ? s_LoopIcons[1] : s_LoopIcons[0];
                if (loopClip) GUI.backgroundColor = buttonPressedColor;
                if (GUILayout.Button(buttonIcon))
                {
                    loopClip = !loopClip;
                    // helperSource.loop = true;
                }
                GUI.backgroundColor = colorbackup;

                // Reset loop point input mode if not using loop points so the duration shows up as time by default
                if (myScript.loopMode != LoopMode.LoopWithLoopPoints) loopPointInputMode = 0;

                switch ((LoopPointTool)loopPointInputMode)
                {
                    case LoopPointTool.Slider:
                    case LoopPointTool.TimeInput:
                        blontent = new GUIContent(TimeToString((float)helperSource.timeSamples / music.frequency) + " / " + (TimeToString(music.length)),
                            "The playback time in samples");
                        break;
                    case LoopPointTool.TimeSamplesInput:
                        blontent = new GUIContent(helperSource.timeSamples + " / " + music.samples, "The playback time in samples");
                        break;
                    case LoopPointTool.BPMInput:
                        blontent = new GUIContent(string.Format("{0:0}", helperSource.time / (60f / myScript.bpm)) + " / " + music.length / (60f / myScript.bpm),
                            "The playback time in beats");
                        break;
                }
                GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                rightJustified.alignment = TextAnchor.UpperRight;
                EditorGUILayout.LabelField(blontent, rightJustified);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
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
            // Get a rect for the progress bar using the same margins as a text field
            Rect rect = GUILayoutUtility.GetRect(64, 64, "TextField");

            AudioClip music = ((AudioFileMusicObject)target).GetFile();

            if (cachedTex == null || forceRepaint)
            {
                Texture2D waveformTexture = PaintWaveformSpectrum(music, (int)rect.width, (int)rect.height, new Color(1, 0.5f, 0));
                cachedTex = waveformTexture;
                if (waveformTexture != null)
                    GUI.DrawTexture(rect, waveformTexture);
                forceRepaint = false;
            }
            else
            {
                GUI.DrawTexture(rect, cachedTex);
            }

            if (clipPlaying)
            {
                Rect progressRect = new Rect(rect);
                progressRect.width *= value;
                progressRect.xMin = progressRect.xMax - 1;
                GUI.Box(progressRect, "", "SelectionRect");
            }

            EditorGUILayout.Space();

            return rect;
        }

        void Update()
        {
            AudioFileMusicObject myScript = (AudioFileMusicObject)target;
            AudioClip music = myScript.GetFile();
            if (music != cachedClip)
            {
                forceRepaint = true;
                cachedClip = music;
                helperSource.clip = cachedClip;
            }

            if ((clipPlaying && !clipPaused) || (mouseDragging && clipPlaying))
            {
                Repaint();

                float clipPos = helperSource.timeSamples / (float)music.frequency;

                if (loopClip)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (!helperSource.isPlaying && clipPlaying)
                    {
                        if (freePlay)
                        {
                            helperSource.Play();
                        }
                        else
                        {
                            helperSource.Play();
                            helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                        }
                        freePlay = false;
                    }
                    else if (myScript.clampToLoopPoints || !firstPlayback)
                    {
                        if (clipPos < myScript.loopStart || clipPos > myScript.loopEnd)
                        {
                            // CeilToInt to guarantee clip position stays within loop bounds
                            helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                            firstPlayback = false;
                        }
                    }
                    else if (clipPos >= myScript.loopEnd)
                    {
                        helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                        firstPlayback = false;
                    }
                }
                else if (!loopClip)
                {
                    if (!helperSource.isPlaying || clipPos > myScript.loopEnd)
                    {
                        clipPlaying = false;
                        helperSource.Stop();
                    }
                    else if (myScript.clampToLoopPoints && clipPos < myScript.loopStart)
                    {
                        helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                    }
                }
            }
        }


        void OnEnable()
        {
            Init();
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

        GameObject helperObject;
        AudioSource helperSource;

        void CreateAudioHelper()
        {
            if (helperObject == null)
            {
                helperObject = GameObject.Find("JSAM Audio Music Helper");
                if (helperObject == null)
                    helperObject = new GameObject("JSAM Audio Music Helper");
                helperSource = helperObject.AddComponent<AudioSource>();
                helperObject.hideFlags = HideFlags.HideAndDontSave;
                helperSource.clip = ((AudioFileMusicObject)target).GetFile();
                helperSource.time = 0;
            }
        }

        void DestroyAudioHelper()
        {
            helperSource.Stop();
            DestroyImmediate(helperObject);
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

            AudioFileMusicObject myScript = (AudioFileMusicObject)target;

            for (int x = 0; x < width; x++)
            {
                // Here we limit the scope of the area based on loop points
                if (x < waveform.Length * (myScript.loopStart / audio.length) || x > waveform.Length * (myScript.loopEnd / audio.length))
                {
                    for (int y = 0; y < height; y++)
                    {
                        tex.SetPixel(x, y, Color.black);
                    }
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        tex.SetPixel(x, y, new Color(0.3f, 0.3f, 0.3f));
                    }
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * myScript.relativeVolume); y++)
                {
                    Color currentPixelColour = tex.GetPixel(x, (height / 2) + y);

                    tex.SetPixel(x, (height / 2) + y, currentPixelColour + col * 0.75f);

                    currentPixelColour = tex.GetPixel(x, (height / 2) - y);
                    tex.SetPixel(x, (height / 2) - y, currentPixelColour + col * 0.75f);
                }
            }
            tex.Apply();

            return tex;
        }

        static GUIContent[] s_PlayIcons = { null, null };
        static GUIContent[] s_AutoPlayIcons = { null, null };
        static GUIContent[] s_PauseIcons = { null, null };
        static GUIContent[] s_LoopIcons = { null, null };

        /// <summary>
        /// Why does Unity keep all this stuff secret?
        /// https://unitylist.com/p/5c3/Unity-editor-icons
        /// </summary>
        static void Init()
        {
            s_AutoPlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioAutoPlayOff", "Turn Auto Play on");
            s_AutoPlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioAutoPlayOn", "Turn Auto Play off");
            s_PlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioPlayOff", "Click to Play");
            s_PlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioPlayOn", "Click to Stop");
            s_PauseIcons[0] = EditorGUIUtility.TrIconContent("PauseButton", "Click to Pause");
            s_PauseIcons[1] = EditorGUIUtility.TrIconContent("PauseButton On", "Click to Unpause");
            s_LoopIcons[0] = EditorGUIUtility.TrIconContent("playLoopOff", "Click to enable looping");
            s_LoopIcons[1] = EditorGUIUtility.TrIconContent("playLoopOn", "Click to disable looping");
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
                AudioFileMusicObject obj = (AudioFileMusicObject)g;
                if (obj != null)
                {
                    obj.category = c;
                }
            }
            AudioManager.instance.UpdateAudioFileMusicObjectCategories();
        }

        public static string TimeToString(float time)
        {
            time *= 1000;
            int minutes = (int)time / 60000;
            int seconds = (int)time / 1000 - 60 * minutes;
            int milliseconds = (int)time - minutes * 60000 - 1000 * seconds;
            return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        }
    }
}