using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ATL.AudioData;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioFileMusicObject))]
    [CanEditMultipleObjects]
    public class AudioFileMusicObjectEditor : BaseAudioFileObjectEditor<AudioFileMusicObjectEditor>
    {
        AudioFileMusicObject myScript;

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
        public static bool firstPlayback = true;
        public static bool freePlay = false;

        static bool showLoopPointTool;

        static int loopPointInputMode = 0;

        Texture2D cachedTex;
        AudioClip cachedClip;

        bool mouseScrubbed = false;

        SerializedProperty loopMode;
        SerializedProperty clampToLoopPoints;
        SerializedProperty loopStartProperty;
        SerializedProperty loopEndProperty;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            #region Category Inspector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(category);
            EditorGUILayout.EndHorizontal();
            #endregion

            RenderPresetDescription();

            List<string> propertiesToExclude = new List<string>() { "spatialSound", "loopSound", "priority",
                "pitchShift", "loopMode", "fadeMode", "playReversed", "safeName", "maxDistance" };
            if (myScript.GetFile() == null)
            {
                propertiesToExclude.AddRange(new List<string>() { "m_Script", "useLibrary", "files", "relativeVolume",
                "fadeMode", "clampBetweenLoopPoints", "startingPitch", "playReversed", "spatialize", "ignoreTimeScale",
                "delay", });
            }
            else
            {
                propertiesToExclude.AddRange(new List<string>() { "m_Script", "useLibrary", "files", "maxDistance" });
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
                float loopStart = myScript.loopStart;
                float loopEnd = myScript.loopEnd;

                AudioClip music = myScript.GetFile();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(loopMode);
                if (EditorGUI.EndChangeCheck())
                {
                    // This won't do, reset loop point positions
                    if (myScript.loopStart >= myScript.loopEnd)
                    {
                        loopStartProperty.floatValue = 0;
                        loopEndProperty.floatValue = music.length;
                        loopStart = myScript.loopStart;
                        loopEnd = myScript.loopEnd;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                using (new EditorGUI.DisabledScope(myScript.loopMode != LoopMode.LoopWithLoopPoints))
                    EditorGUILayout.PropertyField(clampToLoopPoints);

                if (!AudioPlaybackToolEditor.WindowOpen) DrawPlaybackTool(myScript);

                using (new EditorGUI.DisabledScope(myScript.loopMode != LoopMode.LoopWithLoopPoints))
                {
                    blontent = new GUIContent("Loop Point Tools", "Customize where music will loop between. " +
                        "Loops may not appear to be seamless in the inspector but rest assured, they will be seamless in-game!");
                    showLoopPointTool = EditorCompatability.SpecialFoldouts(showLoopPointTool, blontent);
                    if (showLoopPointTool && myScript.loopMode == LoopMode.LoopWithLoopPoints)
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

                        switch ((LoopPointTool)loopPointInputMode)
                        {
                            case LoopPointTool.Slider:
                                GUILayout.Label("Song Duration Samples: " + music.samples);
                                EditorGUILayout.MinMaxSlider(ref loopStart, ref loopEnd, 0, music.length);

                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Loop Point Start: " + AudioPlaybackToolEditor.TimeToString(loopStart), new GUILayoutOption[] { GUILayout.Width(180) });
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Loop Point Start (Samples): " + myScript.loopStart * music.frequency);
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Loop Point End:   " + AudioPlaybackToolEditor.TimeToString(loopEnd), new GUILayoutOption[] { GUILayout.Width(180) });
                                GUILayout.FlexibleSpace();
                                GUILayout.Label("Loop Point End (Samples): " + myScript.loopEnd * music.frequency);
                                GUILayout.EndHorizontal();
                                break;
                            case LoopPointTool.TimeInput:
                                EditorGUILayout.Space();

                                // int casts are used instead of Mathf.RoundToInt so that we truncate the floats instead of rounding up
                                GUILayout.BeginHorizontal();
                                float theTime = loopStart * 1000f;
                                GUILayout.Label("Loop Point Start:");
                                int minutes = EditorGUILayout.IntField((int)(theTime / 60000f));
                                GUILayout.Label(":");
                                int seconds = Mathf.Clamp(EditorGUILayout.IntField((int)((theTime % 60000) / 1000f)), 0, 59);
                                GUILayout.Label(":");
                                float milliseconds = Mathf.Clamp(EditorGUILayout.IntField(Mathf.RoundToInt(theTime % 1000f)), 0, 999.0f);
                                milliseconds /= 1000.0f; // Ensures that our milliseconds never leave their decimal place
                                loopStart = (float)minutes * 60f + (float)seconds + milliseconds;
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                theTime = loopEnd * 1000f;
                                GUILayout.Label("Loop Point End:  ");
                                minutes = EditorGUILayout.IntField((int)(theTime / 60000f));
                                GUILayout.Label(":");
                                seconds = Mathf.Clamp(EditorGUILayout.IntField((int)((theTime % 60000) / 1000f)), 0, 59);
                                GUILayout.Label(":");
                                milliseconds = Mathf.Clamp(EditorGUILayout.IntField(Mathf.RoundToInt(theTime % 1000f)), 0, 999.0f);
                                milliseconds /= 1000.0f; // Ensures that our milliseconds never leave their decimal place
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
                                float samplesEnd = JSAMExtensions.Clamp(EditorGUILayout.FloatField("Loop Point End:", myScript.loopEnd * music.frequency), 0, music.samples);
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
                                buttonText = new GUIContent("Embed Loop Points to File", "Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                            }
                            else
                            {
                                buttonText = new GUIContent("Embed Loop Points to File", "This option is exclusive to .WAV files. Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
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
                            loopStartProperty.floatValue = Mathf.Clamp(loopStart, 0, music.length);
                            loopEndProperty.floatValue = Mathf.Clamp(loopEnd, 0, Mathf.Ceil(music.length));
                            EditorUtility.SetDirty(myScript);
                            AudioPlaybackToolEditor.DoForceRepaint(true);
                        }
                    }
                    EditorCompatability.EndSpecialFoldoutGroup();
                }
            }
            #endregion

            DrawAudioEffectTools();

            if (serializedObject.hasModifiedProperties)
            {
                AudioPlaybackToolEditor.DoForceRepaint(true);
                serializedObject.ApplyModifiedProperties();

                // Manually fix variables
                if (myScript.delay < 0)
                {
                    myScript.delay = 0;
                    Undo.RecordObject(myScript, "Fixed negative delay");
                }
            }

            #region Quick Reference Guide 
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
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
                EditorGUILayout.HelpBox("You can always check what audio file music objects you have loaded in AudioManager's library by selecting the AudioManager " +
                    "in the inspector and clicking on the drop-down near the bottom."
                    , MessageType.None);
                EditorGUILayout.HelpBox("If you want to better organize your audio file music objects in AudioManager's library, you can assign a " +
                    "category to this audio file music object. Use the \"Hidden\" category to hide your audio file music object from the library list completely."
                    , MessageType.None);
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
                buttonC = new GUIContent("How to use Goldwave", "Click here to learn how to set Loop Points in GoldWave!");
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
            EditorCompatability.EndSpecialFoldoutGroup();
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
            showPlaybackTool = EditorCompatability.SpecialFoldouts(showPlaybackTool, blontent);
            if (showPlaybackTool)
            {
                AudioClip music = myScript.GetFile();
                Rect progressRect = ProgressBar((float)AudioPlaybackToolEditor.helperSource.timeSamples / (float)AudioPlaybackToolEditor.helperSource.clip.samples, GetInfoString());

                EditorGUILayout.BeginHorizontal();

                Event evt = Event.current;

                if (evt.isMouse)
                {
                    switch (evt.type)
                    {
                        case EventType.MouseUp:
                            mouseDragging = false;
                            break;
                        case EventType.MouseDown:
                        case EventType.MouseDrag:
                            if (evt.type == EventType.MouseDown)
                            {
                                if (evt.mousePosition.y > progressRect.yMin && evt.mousePosition.y < progressRect.yMax)
                                {
                                    mouseDragging = true;
                                    mouseScrubbed = true;
                                }
                                else mouseDragging = false;
                            }
                            if (!mouseDragging) break;
                            float newProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, evt.mousePosition.x);
                            AudioPlaybackToolEditor.helperSource.time = Mathf.Clamp((newProgress * music.length), 0, music.length - AudioManager.EPSILON);
                            if (myScript.loopMode == LoopMode.LoopWithLoopPoints && myScript.clampToLoopPoints)
                            {
                                float start = myScript.loopStart * myScript.GetFile().frequency;
                                float end = myScript.loopEnd * myScript.GetFile().frequency;
                                AudioPlaybackToolEditor.helperSource.timeSamples = (int)Mathf.Clamp(AudioPlaybackToolEditor.helperSource.timeSamples, start, end - AudioManager.EPSILON);
                            }
                            break;
                    }
                }

                if (GUILayout.Button(s_BackIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    if (myScript.loopMode == LoopMode.LoopWithLoopPoints && myScript.clampToLoopPoints)
                    {
                        AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt((myScript.loopStart * music.frequency));
                    }
                    else
                    {
                        AudioPlaybackToolEditor.helperSource.timeSamples = 0;
                    }
                    AudioPlaybackToolEditor.helperSource.Stop();
                    mouseScrubbed = false;
                    clipPaused = false;
                    clipPlaying = false;
                }

                Color colorbackup = GUI.backgroundColor;
                GUIContent buttonIcon = (clipPlaying) ? s_PlayIcons[1] : s_PlayIcons[0];
                if (clipPlaying) GUI.backgroundColor = buttonPressedColor;
                if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    clipPlaying = !clipPlaying;
                    if (clipPlaying)
                    {
                        // Note: For some reason, reading from AudioPlaybackToolEditor.helperSource.time returns 0 even if timeSamples is not 0
                        // However, writing a value to AudioPlaybackToolEditor.helperSource.time changes timeSamples to the appropriate value just fine
                        AudioPlaybackToolEditor.helperHelper.PlayDebug(myScript, mouseScrubbed);
                        if (clipPaused) AudioPlaybackToolEditor.helperSource.Pause();
                        firstPlayback = true;
                        freePlay = false;
                    }
                    else
                    {
                        AudioPlaybackToolEditor.helperSource.Stop();
                        if (!mouseScrubbed)
                        {
                            AudioPlaybackToolEditor.helperSource.time = 0;
                        }
                        clipPaused = false;
                    }
                }

                GUI.backgroundColor = colorbackup;
                GUIContent theText = (clipPaused) ? s_PauseIcons[1] : s_PauseIcons[0];
                if (clipPaused) GUI.backgroundColor = buttonPressedColor;
                if (GUILayout.Button(theText, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    clipPaused = !clipPaused;
                    if (clipPaused)
                    {
                        AudioPlaybackToolEditor.helperSource.Pause();
                    }
                    else
                    {
                        AudioPlaybackToolEditor.helperSource.UnPause();
                    }
                }

                GUI.backgroundColor = colorbackup;
                buttonIcon = (loopClip) ? s_LoopIcons[1] : s_LoopIcons[0];
                if (loopClip) GUI.backgroundColor = buttonPressedColor;
                if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    loopClip = !loopClip;
                    // AudioPlaybackToolEditor.helperSource.loop = true;
                }
                GUI.backgroundColor = colorbackup;

                if (GUILayout.Button(openIcon, new GUILayoutOption[] { GUILayout.MaxHeight(19) }))
                {
                    AudioPlaybackToolEditor.Init();
                }

                // Reset loop point input mode if not using loop points so the duration shows up as time by default
                if (myScript.loopMode != LoopMode.LoopWithLoopPoints) loopPointInputMode = 0;

                switch ((LoopPointTool)loopPointInputMode)
                {
                    case LoopPointTool.Slider:
                    case LoopPointTool.TimeInput:
                        blontent = new GUIContent(AudioPlaybackToolEditor.TimeToString((float)AudioPlaybackToolEditor.helperSource.timeSamples / music.frequency) + " / " + (AudioPlaybackToolEditor.TimeToString(music.length)),
                            "The playback time in seconds");
                        break;
                    case LoopPointTool.TimeSamplesInput:
                        blontent = new GUIContent(AudioPlaybackToolEditor.helperSource.timeSamples + " / " + music.samples, "The playback time in samples");
                        break;
                    case LoopPointTool.BPMInput:
                        blontent = new GUIContent(string.Format("{0:0}", AudioPlaybackToolEditor.helperSource.time / (60f / myScript.bpm)) + " / " + music.length / (60f / myScript.bpm),
                            "The playback time in beats");
                        break;
                }
                GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                rightJustified.alignment = TextAnchor.UpperRight;
                EditorGUILayout.LabelField(blontent, rightJustified);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }
            EditorCompatability.EndSpecialFoldoutGroup();
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

            if (cachedTex == null || AudioPlaybackToolEditor.forceRepaint)
            {
                Texture2D waveformTexture = PaintWaveformSpectrum(music, (int)rect.width, (int)rect.height, new Color(1, 0.5f, 0));
                cachedTex = waveformTexture;
                if (waveformTexture != null)
                    GUI.DrawTexture(rect, waveformTexture);
                AudioPlaybackToolEditor.forceRepaint = false;
            }
            else
            {
                GUI.DrawTexture(rect, cachedTex);
            }

            Rect progressRect = new Rect(rect);
            progressRect.width *= value;
            progressRect.xMin = progressRect.xMax - 1;
            GUI.Box(progressRect, "", "SelectionRect");

            EditorGUILayout.Space();

            return rect;
        }

        void Update()
        {
            AudioFileMusicObject myScript = (AudioFileMusicObject)target;
            if (myScript == null) return;
            AudioClip music = myScript.GetFile();
            if (music != cachedClip)
            {
                AudioPlaybackToolEditor.DoForceRepaint(true);
                cachedClip = music;
                AudioPlaybackToolEditor.helperSource.clip = cachedClip;
            }

            if (!AudioPlaybackToolEditor.helperSource.isPlaying && mouseDragging)
            {
                Repaint();
            }

            if ((clipPlaying && !clipPaused) || (mouseDragging && clipPlaying))
            {
                float clipPos = AudioPlaybackToolEditor.helperSource.timeSamples / (float)music.frequency;
                AudioPlaybackToolEditor.helperSource.volume = myScript.relativeVolume;
                AudioPlaybackToolEditor.helperSource.pitch = myScript.startingPitch;

                Repaint();

                if (loopClip)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (myScript.loopMode == LoopMode.LoopWithLoopPoints)
                    {
                        if (!AudioPlaybackToolEditor.helperSource.isPlaying && clipPlaying && !clipPaused)
                        {
                            if (freePlay)
                            {
                                AudioPlaybackToolEditor.helperSource.Play();
                            }
                            else
                            {
                                AudioPlaybackToolEditor.helperSource.Play();
                                AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                            }
                            freePlay = false;
                        }
                        else if (myScript.clampToLoopPoints || !firstPlayback)
                        {
                            if (clipPos < myScript.loopStart || clipPos > myScript.loopEnd)
                            {
                                // CeilToInt to guarantee clip position stays within loop bounds
                                AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                                firstPlayback = false;
                            }
                        }
                        else if (clipPos >= myScript.loopEnd)
                        {
                            AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                            firstPlayback = false;
                        }
                    }
                }
                else if (!loopClip && myScript.loopMode == LoopMode.LoopWithLoopPoints)
                {
                    if ((!AudioPlaybackToolEditor.helperSource.isPlaying && !clipPaused) || clipPos > myScript.loopEnd)
                    {
                        clipPlaying = false;
                        AudioPlaybackToolEditor.helperSource.Stop();
                    }
                    else if (myScript.clampToLoopPoints && clipPos < myScript.loopStart)
                    {
                        AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                    }
                }
            }

            if (myScript.loopMode != LoopMode.LoopWithLoopPoints)
            {
                if (!AudioPlaybackToolEditor.helperSource.isPlaying && !clipPaused && clipPlaying)
                {
                    AudioPlaybackToolEditor.helperSource.time = 0;
                    if (loopClip)
                    {
                        AudioPlaybackToolEditor.helperSource.Play();
                    }
                    else
                    {
                        clipPlaying = false;
                    }
                }
            }
        }

        new protected void OnEnable()
        {
            base.OnEnable();
            myScript = target as AudioFileMusicObject;

            SetupIcons();
            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
            AudioPlaybackToolEditor.CreateAudioHelper(myScript.GetFile(), true);
            Undo.postprocessModifications += ApplyHelperEffects;

            loopMode = FindProp("loopMode");
            clampToLoopPoints = FindProp("clampToLoopPoints");
            loopStartProperty = FindProp("loopStart");
            loopEndProperty = FindProp("loopEnd");
        }

        public UndoPropertyModification[] ApplyHelperEffects(UndoPropertyModification[] modifications)
        {
            if (AudioPlaybackToolEditor.helperSource.isPlaying)
            {
                AudioPlaybackToolEditor.helperHelper.ApplyEffects();
            }
            return modifications;
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            AudioPlaybackToolEditor.DestroyAudioHelper();
            Undo.postprocessModifications -= ApplyHelperEffects;
        }

        void OnUndoRedo()
        {
            AudioPlaybackToolEditor.DoForceRepaint(true);
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
            // Copy sample data to array
            audio.GetData(samples, 0);

            Color lightShade = new Color(0.3f, 0.3f, 0.3f);
            int halfHeight = height / 2;

            float leftValue = AudioPlaybackToolEditor.CalculateZoomedLeftValue();
            float rightValue = AudioPlaybackToolEditor.CalculateZoomedRightValue();

            int leftSide = Mathf.RoundToInt(leftValue * samples.Length);
            int rightSide = Mathf.RoundToInt(rightValue * samples.Length);

            float zoomLevel = AudioPlaybackToolEditor.scrollZoom / AudioPlaybackToolEditor.MAX_SCROLL_ZOOM;
            int packSize = Mathf.RoundToInt((int)samples.Length / (int)width * (float)zoomLevel) + 1;

            int s = 0;
            int limit = Mathf.Min(rightSide, samples.Length);

            // Build waveform data
            float[] waveform = new float[limit];
            for (int i = leftSide; i < limit; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }

            if (myScript.loopMode == LoopMode.LoopWithLoopPoints)
            {
                float beginning = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, myScript.loopStart / audio.length);
                float ending = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, myScript.loopEnd / audio.length);
                float loopStart = beginning * width;
                float loopEnd = ending * width;

                for (int x = 0; x < width; x++)
                {
                    // Here we limit the scope of the area based on loop points
                    if (x < loopStart || x > loopEnd)
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
                            tex.SetPixel(x, y, lightShade);
                        }
                    }
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        tex.SetPixel(x, y, lightShade);
                    }
                }
            }
            
            for (int x = 0; x < Mathf.Clamp(rightSide, 0, width); x++)
            {
                // Scale the wave vertically relative to half the rect height and the relative volume
                float heightLimit = waveform[x] * halfHeight * myScript.relativeVolume;

                for (int y = (int)heightLimit; y >= 0; y--)
                {
                    Color currentPixelColour = tex.GetPixel(x, halfHeight + y);
            
                    tex.SetPixel(x, halfHeight + y, currentPixelColour + col * 0.75f);
            
                    // Get data from upper half offset by 1 unit due to int truncation
                    currentPixelColour = tex.GetPixel(x, halfHeight - (y + 1));
                    // Draw bottom half with data from upper half
                    tex.SetPixel(x, halfHeight - (y + 1), currentPixelColour + col * 0.75f);
                }
            }
            tex.Apply();

            return tex;
        }

        static GUIContent s_BackIcon = null;
        static GUIContent[] s_PlayIcons = { null, null };
        static GUIContent[] s_PauseIcons = { null, null };
        static GUIContent[] s_LoopIcons = { null, null };
        static GUIContent openIcon;

        /// <summary>
        /// Why does Unity keep all this stuff secret?
        /// https://unitylist.com/p/5c3/Unity-editor-icons
        /// </summary>
        static void SetupIcons()
        {
            s_BackIcon = EditorGUIUtility.TrIconContent("beginButton", "Click to Reset Playback Position");
#if UNITY_2019_4_OR_NEWER
            s_PlayIcons[0] = EditorGUIUtility.TrIconContent("d_PlayButton", "Click to Play");
            s_PlayIcons[1] = EditorGUIUtility.TrIconContent("d_PlayButton On", "Click to Stop");
#else
            s_PlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioPlayOff", "Click to Play");
            s_PlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioPlayOn", "Click to Stop");
#endif
            s_PauseIcons[0] = EditorGUIUtility.TrIconContent("PauseButton", "Click to Pause");
            s_PauseIcons[1] = EditorGUIUtility.TrIconContent("PauseButton On", "Click to Unpause");
#if UNITY_2019_4_OR_NEWER
            s_LoopIcons[0] = EditorGUIUtility.TrIconContent("d_preAudioLoopOff", "Click to enable looping");
            s_LoopIcons[1] = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Click to disable looping");
#else
            s_LoopIcons[0] = EditorGUIUtility.TrIconContent("playLoopOff", "Click to enable looping");
            s_LoopIcons[1] = EditorGUIUtility.TrIconContent("playLoopOn", "Click to disable looping");
#endif
            openIcon = EditorGUIUtility.TrIconContent("d_ScaleTool", "Click to open Playback Preview in a standalone window");
        }
    }
}