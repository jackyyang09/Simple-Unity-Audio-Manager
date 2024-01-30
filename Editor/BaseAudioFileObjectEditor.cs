using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseAudioFileObjectEditor : Editor
    {
        public enum LoopPointTool
        {
            Slider,
            TimeInput,
            TimeSamplesInput,
            BPMInput//WithBeats,
                    //BPMInputWithBars
        }

        protected BaseAudioFileObject asset;

        protected bool isPreset;

        protected static bool showPlaybackTool;
        protected static bool showHowTo;

        protected static bool showLoopPointTool;
        protected static int loopPointInputMode = 0;

        protected AudioClip activeClip;
        protected bool clipPlaying;
        protected bool IsClipPlaying
        {
            get
            {
                return AudioPlaybackToolEditor.helperSource.clip == activeClip && 
                    AudioPlaybackToolEditor.helperSource.isPlaying;
            }
        }
        protected bool clipPaused;

        protected JSAMEditorFader editorFader;

        protected bool mouseDragging;
        protected bool mouseScrubbed;
        protected bool loopClip;

        protected static GUIContent backIcon;
        protected static GUIContent[] playIcons;
        protected static GUIContent[] pauseIcons;
        protected static GUIContent[] loopIcons;
        protected static GUIContent openIcon;

        Color COLOR_BUTTONPRESSED => new Color(0.475f, 0.475f, 0.475f);

        public static bool FreePlay = false;

        protected Texture2D cachedTex;
        protected AudioClip cachedClip;
        protected virtual string SHOW_FADETOOL { get; }
        protected bool showFadeTool
        {
            get
            {
                if (!EditorPrefs.HasKey(SHOW_FADETOOL))
                {
                    EditorPrefs.SetBool(SHOW_FADETOOL, false);
                }
                return EditorPrefs.GetBool(SHOW_FADETOOL);
            }
            set
            {
                EditorPrefs.SetBool(SHOW_FADETOOL, value);
            }
        }

        protected virtual string SHOW_LIBRARY { get; }
        protected bool showLibrary
        {
            get
            {
                if (!EditorPrefs.HasKey(SHOW_LIBRARY))
                {
                    EditorPrefs.SetBool(SHOW_LIBRARY, false);
                }
                return EditorPrefs.GetBool(SHOW_LIBRARY);
            }
            set { EditorPrefs.SetBool(SHOW_LIBRARY, value); }
        }

        protected virtual string EXPAND_LIBRARY { get; }
        protected bool expandLibrary
        {
            get
            {
                if (!EditorPrefs.HasKey(EXPAND_LIBRARY))
                {
                    EditorPrefs.SetBool(EXPAND_LIBRARY, false);
                }
                return EditorPrefs.GetBool(EXPAND_LIBRARY);
            }
            set { EditorPrefs.SetBool(EXPAND_LIBRARY, value); }
        }

        protected List<string> excludedProperties = new List<string>() { "m_Script" };

        Color COLOR_BUTTONPRESSED_2 = new Color(0.75f, 0.75f, 0.75f);

#if !UNITY_2020_3_OR_NEWER
        protected EditorCompatability.AudioClipList list;
#endif

        protected GUIContent blontent;

        protected Vector2 scroll;

        static BaseAudioFileObjectEditor instance;
        /// <summary>
        /// Multiple inspector windows are a thing. 
        /// Find a way to leave this paradigm
        /// </summary>
        public static BaseAudioFileObjectEditor Instance => instance;

        protected SerializedProperty FindProp(string property)
        {
            return serializedObject.FindProperty(property);
        }

        protected virtual void OnEnable()
        {
            instance = this;

            asset = target as BaseAudioFileObject;

            isPreset = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset as UnityEngine.Object));

            SetupIcons();

            EditorApplication.update += Update;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        protected SerializedProperty safeName;
        protected SerializedProperty presetDescription;
        protected SerializedProperty files;
        protected SerializedProperty relativeVolume;
        protected SerializedProperty spatialize;
        protected SerializedProperty maxDistance;
        protected SerializedProperty fadeInOut;
        protected SerializedProperty fadeInDuration;
        protected SerializedProperty fadeOutDuration;
        protected SerializedProperty loopMode;
        protected SerializedProperty loopStartProperty;
        protected SerializedProperty loopEndProperty;

        protected SerializedProperty bypassEffects;
        protected SerializedProperty bypassListenerEffects;
        protected SerializedProperty bypassReverbZones;

        protected virtual void DesignateSerializedProperties()
        {
            presetDescription = FindProp("presetDescription");

            files = FindProp("files");
            excludedProperties.Add("files");

            relativeVolume = FindProp(nameof(asset.relativeVolume));
            excludedProperties.Add(nameof(asset.relativeVolume));

            spatialize = FindProp(nameof(asset.spatialize));
            excludedProperties.Add(nameof(asset.spatialize));

            maxDistance = FindProp(nameof(asset.maxDistance));
            excludedProperties.Add(nameof(asset.maxDistance));

            fadeInOut = FindProp(nameof(fadeInOut));
            excludedProperties.Add(nameof(fadeInOut));

            fadeInDuration = FindProp("fadeInDuration");
            fadeOutDuration = FindProp("fadeOutDuration");

            loopMode = FindProp("loopMode");
            excludedProperties.Add("loopMode");

            loopStartProperty = FindProp("loopStart");
            excludedProperties.Add("loopStart");

            loopEndProperty = FindProp("loopEnd");
            excludedProperties.Add("loopEnd");

            bypassEffects = FindProp(nameof(asset.bypassEffects));
            excludedProperties.Add(nameof(asset.bypassEffects));
            bypassListenerEffects = serializedObject.FindProperty(nameof(asset.bypassListenerEffects));
            excludedProperties.Add(nameof(asset.bypassListenerEffects));
            bypassReverbZones = serializedObject.FindProperty(nameof(asset.bypassReverbZones));
            excludedProperties.Add(nameof(asset.bypassReverbZones));
        }

        /// <summary>
        /// Why does Unity keep all this stuff secret?
        /// https://unitylist.com/p/5c3/Unity-editor-icons
        /// </summary>
        protected void SetupIcons()
        {
            backIcon = EditorGUIUtility.TrIconContent("beginButton", "Click to Reset Playback Position");

            playIcons = new GUIContent[2];
#if UNITY_2019_4_OR_NEWER
            playIcons[0] = EditorGUIUtility.TrIconContent("d_PlayButton", "Click to Play");
            playIcons[1] = EditorGUIUtility.TrIconContent("d_PlayButton On", "Click to Stop");
#else
            playIcons[0] = EditorGUIUtility.TrIconContent("preAudioPlayOff", "Click to Play");
            playIcons[1] = EditorGUIUtility.TrIconContent("preAudioPlayOn", "Click to Stop");
#endif

            pauseIcons = new GUIContent[2];
            pauseIcons[0] = EditorGUIUtility.TrIconContent("PauseButton", "Click to Pause");
            pauseIcons[1] = EditorGUIUtility.TrIconContent("PauseButton On", "Click to Unpause");

            loopIcons = new GUIContent[2];
#if UNITY_2019_4_OR_NEWER
            loopIcons[0] = EditorGUIUtility.TrIconContent("d_preAudioLoopOff", "Click to enable looping");
            loopIcons[1] = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Click to disable looping");
#else
            loopIcons[0] = EditorGUIUtility.TrIconContent("playLoopOff", "Click to enable looping");
            loopIcons[1] = EditorGUIUtility.TrIconContent("playLoopOn", "Click to disable looping");
#endif
            openIcon = EditorGUIUtility.TrIconContent("d_ScaleTool", "Click to open Playback Preview in a standalone window");
        }

        protected abstract void Update();

        protected void RenderPresetDescription()
        {
            if (isPreset)
            {
                EditorGUILayout.LabelField("Preset Description");
                presetDescription.stringValue = EditorGUILayout.TextArea(presetDescription.stringValue);
            }
        }

        protected void RenderGeneratePresetButton()
        {
            if (!isPreset)
            {
                blontent = new GUIContent("Create Preset from this Audio File",
                    "Create a new preset using this Audio File object as a base. " +
                    "You are highly recommended to create presets through this interface rather than Unity's built-in interface.");
                if (GUILayout.Button(blontent))
                {
                    var window = JSAMUtilityWindow.Init("JSAM Preset Wizard", false, true);
                    window.AddField(new GUIContent("Preset Name"), "New Preset", true);
                    window.AddField(new GUIContent("Preset Description"), "", true);
                    JSAMUtilityWindow.onSubmitField += OnCreatePreset;
                }
            }
        }

        protected void RenderFileList()
        {
#if UNITY_2020_3_OR_NEWER
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(files);
            if (EditorGUI.EndChangeCheck())
            {
                files = files.RemoveNullElementsFromArray();
                if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
            }
#else
            #region Library Region
            if (!isPreset)
            {
                Rect overlay = new Rect();
                EditorGUILayout.BeginHorizontal();
                showLibrary = EditorCompatability.SpecialFoldouts(showLibrary, "Library");
                EditorGUILayout.EndHorizontal();
                if (showLibrary)
                {
                    GUILayoutOption[] layoutOptions;
                    layoutOptions = expandLibrary && files.arraySize > 5 ? new GUILayoutOption[0] : new GUILayoutOption[] { GUILayout.MinHeight(150) };
                    overlay = EditorGUILayout.BeginVertical(GUI.skin.box, layoutOptions);

                    scroll = EditorGUILayout.BeginScrollView(scroll);

                    if (files.arraySize > 5) // Magic number haha
                    {
                        string label = expandLibrary ? "Retract Library" : "Expand Library";
                        if (JSAMEditorHelper.CondensedButton(label))
                        {
                            expandLibrary = !expandLibrary;
                        }
                    }

                    if (files.arraySize > 0)
                    {
                        list.Draw();
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }

                EditorCompatability.EndSpecialFoldoutGroup();

                if (showLibrary)
                {
                    string normalLabel = files.arraySize == 0 ? "Drag AudioClips here" : string.Empty;
                    if (JSAMEditorHelper.DragAndDropRegion(overlay, normalLabel, "Release to add AudioClips"))
                    {
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            string filePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[i]);
                            var clips = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<AudioClip>(filePath);

                            for (int j = 0; j < clips.Count; j++)
                            {
                                files.AddAndReturnNewArrayElement().objectReferenceValue = clips[j];
                            }
                        }
                    }
                }
                EditorGUILayout.LabelField("File Count: " + files.arraySize);
            }
            #endregion
#endif
        }

        protected abstract void OnCreatePreset(string[] input);

        /// <summary>
        /// Conveniently draws a progress bar
        /// Referenced from the official Unity documentation
        /// https://docs.unity3d.com/ScriptReference/Editor.html
        /// </summary>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns>True if tools successfully rendered</returns>
        bool ProgressBar(out Rect rect)
        {
            string label = GetInfoString();

            // Get a rect for the progress bar using the same margins as a text field
            rect = GUILayoutUtility.GetRect(64, 64, "TextField");

            bool hide = false;
            GUIContent content = new GUIContent();

            if (files.arraySize == 0)
            {
                content.text = "Add some AudioClips above to preview them";
                hide = true;
            }

            if (AudioPlaybackToolEditor.WindowOpen)
            {
                content.text = "JSAM Playback Tool is Open";
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Re-focus Playback Tool"))
                {
                    EditorWindow.FocusWindowIfItsOpen<AudioPlaybackToolEditor>();
                }
                if (GUILayout.Button("Close Playback Tool"))
                {
                    AudioPlaybackToolEditor.Window.Close();
                }
                EditorGUILayout.EndHorizontal();
                hide = true;
            }

            if (hide)
            {
                GUI.Box(rect, content, GUI.skin.box.ApplyTextAnchor(TextAnchor.MiddleCenter).ApplyBoldText().SetFontSize(20).SetTextColor(GUI.skin.label.normal.textColor));
                return false;
            }

            AudioClip music = files.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip;
            var helperSource = AudioPlaybackToolEditor.helperSource;
            var value = (float)helperSource.timeSamples / (float)music.samples;

            if (cachedTex == null || AudioPlaybackToolEditor.forceRepaint)
            {
                Texture2D waveformTexture = AudioPlaybackToolEditor.RenderStaticPreview(music, rect, asset.relativeVolume);
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

            return true;
        }

        /// <summary>
        /// TODO: Playback drawing should have its own class. 
        /// Should support having multiple active playback tools in the inspector, 
        /// even of the same editor window type (except the dedicated playback tool). 
        /// </summary>
        protected abstract void DrawPlaybackTool();

        /// <summary>
        /// Embeds a playback preview in the inspector
        /// </summary>
        /// <param name="music"></param>
        protected void DrawInternalPlaybackTool()
        {
            if (asset.Files.Count == 0) return;
            GUIContent blontent = new GUIContent("Audio Playback Preview",
                "Allows you to preview how your Audio File will sound during runtime in the inspector. " +
                "Some effects, like spatialization, will not be available to preview");
            showPlaybackTool = EditorCompatability.SpecialFoldouts(showPlaybackTool, blontent);

            float loopStart = asset.loopStart;
            float loopEnd = asset.loopEnd;
            if (showPlaybackTool)
            {
                Rect progressRect;
                if (ProgressBar(out progressRect))
                {
                    AudioClip audio = files.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip;

                    AudioClip music = asset.Files[0];
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
                                AudioPlaybackToolEditor.helperSource.time = Mathf.Clamp((newProgress * audio.length), 0, audio.length - AudioManagerInternal.EPSILON);
                                if (asset.loopMode == LoopMode.ClampedLoopPoints)
                                {
                                    float start = asset.loopStart * asset.Files[0].frequency;
                                    float end = asset.loopEnd * asset.Files[0].frequency;
                                    AudioPlaybackToolEditor.helperSource.timeSamples = (int)Mathf.Clamp(AudioPlaybackToolEditor.helperSource.timeSamples, start, end - AudioManagerInternal.EPSILON);
                                }
                                break;
                        }
                    }

                    if (GUILayout.Button(backIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        if (asset.loopMode == LoopMode.ClampedLoopPoints)
                        {
                            AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(asset.loopStart * audio.frequency);
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
                    GUIContent buttonIcon = (clipPlaying) ? playIcons[1] : playIcons[0];
                    if (clipPlaying) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        if (!clipPlaying)
                        {
                            // Note: For some reason, reading from AudioPlaybackToolEditor.helperSource.time returns 0 even if timeSamples is not 0
                            // However, writing a value to AudioPlaybackToolEditor.helperSource.time changes timeSamples to the appropriate value just fine
                            if (asset as MusicFileObject)
                            {
                                AudioPlaybackToolEditor.musicHelper.PlayDebug(asset as MusicFileObject, mouseScrubbed);
                            }
                            else
                            {
                                AudioPlaybackToolEditor.soundHelper.PlayDebug(asset as SoundFileObject, mouseScrubbed);
                                editorFader.StartFading(activeClip, asset as SoundFileObject);
                            }
                            if (clipPaused) AudioPlaybackToolEditor.helperSource.Pause();
                            MusicFileObjectEditor.firstPlayback = true;
                            FreePlay = false;
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
                        clipPlaying = !clipPlaying;
                    }

                    GUI.backgroundColor = colorbackup;
                    GUIContent theText = (clipPaused) ? pauseIcons[1] : pauseIcons[0];
                    if (clipPaused) GUI.backgroundColor = COLOR_BUTTONPRESSED;
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
                    buttonIcon = (loopClip) ? loopIcons[1] : loopIcons[0];
                    if (loopClip) GUI.backgroundColor = COLOR_BUTTONPRESSED;
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
                    if (asset.loopMode != LoopMode.LoopWithLoopPoints && asset.loopMode != LoopMode.ClampedLoopPoints) loopPointInputMode = 0;

                    // Render time label
                    switch ((LoopPointTool)loopPointInputMode)
                    {
                        case LoopPointTool.Slider:
                        case LoopPointTool.TimeInput:
                            blontent = new GUIContent(
                                ((float)AudioPlaybackToolEditor.helperSource.timeSamples / AudioPlaybackToolEditor.helperSource.clip.frequency).TimeToString() +
                                " / " +
                                AudioPlaybackToolEditor.helperSource.clip.length.TimeToString(),
                                "The playback time in seconds");
                            break;
                        case LoopPointTool.TimeSamplesInput:
                            blontent = new GUIContent(AudioPlaybackToolEditor.helperSource.timeSamples + " / " + audio.samples, "The playback time in samples");
                            break;
                        case LoopPointTool.BPMInput:
                            blontent = new GUIContent(string.Format("{0:0}", AudioPlaybackToolEditor.helperSource.time / (60f / asset.bpm)) + " / " + audio.length / (60f / asset.bpm),
                                "The playback time in beats");
                            break;
                    }
                    EditorGUILayout.LabelField(blontent, JSAMStyles.TimeStyle);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }

                if (EditorUtility.audioMasterMute)
                {
                    JSAMEditorHelper.RenderHelpbox("Audio is muted in the game view, which also mutes audio " +
                         "playback here. Please un-mute it to hear your audio.");
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();
        }

        protected void DrawFadeTools()
        {
            EditorGUILayout.PropertyField(fadeInOut);

            using (new EditorGUI.DisabledScope(!fadeInOut.boolValue))
            {
                showFadeTool = EditorCompatability.SpecialFoldouts(showFadeTool, new GUIContent("Fade Tools", "Show/Hide the Audio Fade previewer"));
                if (showFadeTool)
                {
                    GUIContent fContent = new GUIContent();
                    GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                    rightJustified.alignment = TextAnchor.UpperRight;
                    rightJustified.padding = new RectOffset(0, 15, 0, 0);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Fade In Time:    " + (fadeInDuration.floatValue * activeClip.length).TimeToString(), "Fade in time for this AudioClip in seconds"));
                    EditorGUILayout.LabelField(new GUIContent("Sound Length: " + (activeClip.length).TimeToString(), "Length of the preview clip in seconds"), rightJustified);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Fade Out Time: " + (fadeOutDuration.floatValue * activeClip.length).TimeToString(), "Fade out time for this AudioClip in seconds"));
                    float fid = fadeInDuration.floatValue;
                    float fod = fadeOutDuration.floatValue;
                    fContent = new GUIContent("Fade In Percentage", "The percentage of time the sound takes to fade-in relative to it's total length.");
                    fid = Mathf.Clamp(EditorGUILayout.Slider(fContent, fid, 0, 1), 0, 1 - fod);
                    fContent = new GUIContent("Fade Out Percentage", "The percentage of time the sound takes to fade-out relative to it's total length.");
                    fod = Mathf.Clamp(EditorGUILayout.Slider(fContent, fod, 0, 1), 0, 1 - fid);
                    fadeInDuration.floatValue = fid;
                    fadeOutDuration.floatValue = fod;
                    EditorGUILayout.HelpBox("Note: The sum of your Fade-In and Fade-Out durations cannot exceed 1 (the length of the sound).", MessageType.None);

                }
                EditorCompatability.EndSpecialFoldoutGroup();
            }
        }

        protected void DrawLoopPointTools(BaseAudioFileObject asset)
        {
            if (asset.Files.Count == 0) return;

            float loopStart = asset.loopStart;
            float loopEnd = asset.loopEnd;

            AudioClip music = asset.Files[0];
            float duration = 0;
            int frequency = 0;
            int samples = 0;

            if (music)
            {
                duration = music.length;
                frequency = music.frequency;
                samples = music.samples;
            }

            EditorGUI.BeginDisabledGroup(music == null);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(loopMode);
            if (EditorGUI.EndChangeCheck())
            {
                // This won't do, reset loop point positions
                if (asset.loopStart >= asset.loopEnd)
                {
                    loopStartProperty.floatValue = 0;
                    loopEndProperty.floatValue = duration;
                    loopStart = asset.loopStart;
                    loopEnd = asset.loopEnd;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUI.EndDisabledGroup();

            using (new EditorGUI.DisabledScope(asset.loopMode < LoopMode.LoopWithLoopPoints))
            {
                blontent = new GUIContent("Loop Point Tools", "Customize where music will loop between. " +
                    "Loops may not appear to be seamless in the inspector but rest assured, they will be seamless in-game!");
                showLoopPointTool = EditorCompatability.SpecialFoldouts(showLoopPointTool, blontent);
                if (showLoopPointTool)
                {
                    GUIContent[] contents = new GUIContent[] { new GUIContent("Slider"), new GUIContent("Time"), new GUIContent("Samples"), new GUIContent("BPM") };
                    EditorGUILayout.BeginHorizontal();
                    for (int i = 0; i < contents.Length; i++)
                    {
                        if (i == loopPointInputMode) JSAMEditorHelper.BeginColourChange(COLOR_BUTTONPRESSED_2);
                        if (GUILayout.Button(contents[i], EditorStyles.miniButtonMid)) loopPointInputMode = i;
                        JSAMEditorHelper.EndColourChange();
                    }
                    EditorGUILayout.EndHorizontal();

                    switch ((LoopPointTool)loopPointInputMode)
                    {
                        case LoopPointTool.Slider:
                            GUILayout.Label("Song Duration Samples: " + samples);
                            EditorGUILayout.MinMaxSlider(ref loopStart, ref loopEnd, 0, duration);

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Loop Point Start: " + loopStart.TimeToString(), new GUILayoutOption[] { GUILayout.Width(180) });
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Loop Point Start (Samples): " + asset.loopStart * frequency);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Loop Point End:   " + loopEnd.TimeToString(), new GUILayoutOption[] { GUILayout.Width(180) });
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Loop Point End (Samples): " + asset.loopEnd * frequency);
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
                            GUILayout.Label("Song Duration (Samples): " + samples);
                            EditorGUILayout.Space();

                            GUILayout.BeginHorizontal();
                            float samplesStart = EditorGUILayout.FloatField("Loop Point Start:", asset.loopStart * frequency);
                            GUILayout.EndHorizontal();
                            loopStart = samplesStart / frequency;

                            GUILayout.BeginHorizontal();
                            float samplesEnd = JSAMExtensions.Clamp(EditorGUILayout.FloatField("Loop Point End:", asset.loopEnd * frequency), 0, samples);
                            GUILayout.EndHorizontal();
                            loopEnd = samplesEnd / frequency;
                            break;
                        case LoopPointTool.BPMInput/*WithBeats*/:
                            Undo.RecordObject(asset, "Modified song BPM");
                            asset.bpm = EditorGUILayout.IntField("Song BPM: ", asset.bpm/*, new GUILayoutOption[] { GUILayout.MaxWidth(30)}*/);

                            EditorGUILayout.Space();

                            float startBeat = loopStart / (60f / (float)asset.bpm);
                            startBeat = EditorGUILayout.FloatField("Starting Beat:", startBeat);

                            float endBeat = loopEnd / (60f / (float)asset.bpm);
                            endBeat = Mathf.Clamp(EditorGUILayout.FloatField("Ending Beat:", endBeat), 0, duration / (60f / asset.bpm));

                            loopStart = (float)startBeat * 60f / (float)asset.bpm;
                            loopEnd = (float)endBeat * 60f / (float)asset.bpm;
                            break;
                    }

                    GUIContent buttonText = new GUIContent("Reset Loop Points", "Click to set loop points to the start and end of the track.");
                    if (GUILayout.Button(buttonText))
                    {
                        loopStart = 0;
                        loopEnd = duration;
                    }
                    using (new EditorGUI.DisabledScope(!asset.Files[0].IsWavFile()))
                    {
                        if (asset.Files[0].IsWavFile())
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
                            string filePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(asset.Files[0].name)[0]);
                            string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;

                            ATL.Track theTrack = new ATL.Track(trueFilePath);

                            if (theTrack.AdditionalFields.ContainsKey("sample.SampleLoop[0].Start") && theTrack.AdditionalFields.ContainsKey("sample.SampleLoop[0].End"))
                            {
                                loopStart = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].Start"]) / frequency;
                                loopEnd = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].End"]) / frequency;
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error Reading Metadata", "Could not find any loop point data in " + asset.Files[0].name + ".wav!/n" +
                                    "Are you sure you wrote loop points in this file?", "OK");
                            }

                        }
                        if (asset.Files[0].IsWavFile())
                        {
                            buttonText = new GUIContent("Embed Loop Points to File", "Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                        }
                        else
                        {
                            buttonText = new GUIContent("Embed Loop Points to File", "This option is exclusive to .WAV files. Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                        }
                        if (GUILayout.Button(buttonText))
                        {
                            string filePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(asset.Files[0].name)[0]);
                            string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;

                            ATL.Track theTrack = new ATL.Track(trueFilePath);

                            if (EditorUtility.DisplayDialog("Confirm Loop Point saving", "This will overwrite loop point Start/End loop point markers saved in this .WAV file, are you sure you want to continue?", "Yes", "Cancel"))
                            {
                                theTrack.AdditionalFields["sample.MIDIUnityNote"] = "60";
                                theTrack.AdditionalFields["sample.NumSampleLoops"] = "1";
                                theTrack.AdditionalFields["sample.SampleLoop[0].Type"] = "0";
                                theTrack.AdditionalFields["sample.SampleLoop[0].Start"] = (Mathf.RoundToInt(asset.loopStart * frequency)).ToString();
                                theTrack.AdditionalFields["sample.SampleLoop[0].End"] = (Mathf.RoundToInt(asset.loopEnd * frequency)).ToString();
                                theTrack.Save();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (music)
                    {
                        if (asset.loopStart != loopStart || asset.loopEnd != loopEnd)
                        {
                            Undo.RecordObject(asset, "Modified loop point properties");
                            loopStartProperty.floatValue = Mathf.Clamp(loopStart, 0, duration);
                            loopEndProperty.floatValue = Mathf.Clamp(loopEnd, 0, Mathf.Ceil(duration));
                            EditorUtility.SetDirty(asset);
                            AudioPlaybackToolEditor.DoForceRepaint(true);
                        }
                    }
                }
                EditorCompatability.EndSpecialFoldoutGroup();
            }

        }

        #region Audio Effect Rendering
        static bool showAudioEffects;
        static bool chorusFoldout;
        static bool distortionFoldout;
        static bool echoFoldout;
        static bool highPassFoldout;
        static bool lowPassFoldout;
        static bool reverbFoldout;

        protected void DrawAudioEffectTools()
        {
            GUIContent blontent = new GUIContent("Audio Effects Stack", "");
            showAudioEffects = EditorCompatability.SpecialFoldouts(showAudioEffects, blontent);
            if (showAudioEffects)
            {
                EditorGUILayout.PropertyField(bypassEffects);
                EditorGUILayout.PropertyField(bypassListenerEffects);
                EditorGUILayout.PropertyField(bypassReverbZones);
                if (asset.chorusFilter.enabled)
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
                        Undo.RecordObject(asset, "Removed Chorus Filter");
                        asset.chorusFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (chorusFoldout)
                    {
                        Undo.RecordObject(asset, "Modified Distortion Filter");
                        blontent = new GUIContent("Dry Mix", "Volume of original signal to pass to output");
                        asset.chorusFilter.dryMix =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.dryMix, 0, 1);
                        blontent = new GUIContent("Wet Mix 1", "Volume of 1st chorus tap");
                        asset.chorusFilter.wetMix1 =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.wetMix1, 0, 1);
                        blontent = new GUIContent("Wet Mix 2", "Volume of 2nd chorus tap");
                        asset.chorusFilter.wetMix2 =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.wetMix2, 0, 1);
                        blontent = new GUIContent("Wet Mix 3", "Volume of 2nd chorus tap");
                        asset.chorusFilter.wetMix3 =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.wetMix3, 0, 1);
                        blontent = new GUIContent("Delay", "Chorus delay in ms");
                        asset.chorusFilter.delay =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.delay, 0, 100);
                        blontent = new GUIContent("Rate", "Chorus modulation rate in hertz");
                        asset.chorusFilter.rate =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.rate, 0, 20);
                        blontent = new GUIContent("Depth", "Chorus modulation depth");
                        asset.chorusFilter.depth =
                            EditorGUILayout.Slider(blontent, asset.chorusFilter.depth, 0, 1);
                    }
                    EditorGUILayout.EndVertical();
                }
                if (asset.distortionFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (distortionFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Distortion Filter", "Distorts the sound when its played.");
                    EditorGUILayout.BeginHorizontal();
                    distortionFoldout = EditorGUILayout.Foldout(distortionFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(asset, "Removed Distortion Filter");
                        asset.distortionFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (distortionFoldout)
                    {
                        blontent = new GUIContent("Distortion Level", "Amount of distortion to apply");
                        float cf = asset.highPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, asset.distortionFilter.distortionLevel, 0, 1);

                        if (cf != asset.distortionFilter.distortionLevel)
                        {
                            Undo.RecordObject(asset, "Modified Distortion Filter");
                            asset.distortionFilter.distortionLevel = cf;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (asset.echoFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (echoFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Echo Filter", "Repeats a sound after a given Delay, attenuating the repetitions based on the Decay Ratio");
                    EditorGUILayout.BeginHorizontal();
                    echoFoldout = EditorGUILayout.Foldout(echoFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(asset, "Removed Echo Filter");
                        asset.echoFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (echoFoldout)
                    {
                        Undo.RecordObject(asset, "Modified Echo Filter");
                        blontent = new GUIContent("Delay", "Echo delay in ms");
                        asset.echoFilter.delay =
                            EditorGUILayout.Slider(blontent, asset.echoFilter.delay, 10, 5000);

                        blontent = new GUIContent("Decay Ratio", "Echo decay per delay");
                        asset.echoFilter.decayRatio =
                            EditorGUILayout.Slider(blontent, asset.echoFilter.decayRatio, 0, 1);

                        blontent = new GUIContent("Wet Mix", "Volume of echo signal to pass to output");
                        asset.echoFilter.wetMix =
                            EditorGUILayout.Slider(blontent, asset.echoFilter.wetMix, 0, 1);

                        blontent = new GUIContent("Dry Mix", "Volume of original signal to pass to output");
                        asset.echoFilter.dryMix =
                            EditorGUILayout.Slider(blontent, asset.echoFilter.dryMix, 0, 1);

                        EditorGUILayout.HelpBox("Note: Echoes are best tested during runtime as they do not behave properly in-editor.", MessageType.None);
                    }
                    EditorGUILayout.EndVertical();
                }
                if (asset.lowPassFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (lowPassFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Low Pass Filter", "Filters the audio to let lower frequencies pass while removing frequencies higher than the cutoff");
                    EditorGUILayout.BeginHorizontal();
                    lowPassFoldout = EditorGUILayout.Foldout(lowPassFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(asset, "Removed Low Pass Filter");
                        asset.lowPassFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (lowPassFoldout)
                    {
                        blontent = new GUIContent("Cutoff Frequency", "Low-pass cutoff frequency in hertz");
                        float cf = asset.lowPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, asset.lowPassFilter.cutoffFrequency, 10, 22000);

                        blontent = new GUIContent("Low Pass Resonance Q", "Determines how much the filter's self-resonance is dampened");
                        float q = asset.lowPassFilter.lowpassResonanceQ;
                        q = EditorGUILayout.Slider(
                            blontent, asset.lowPassFilter.lowpassResonanceQ, 1, 10);

                        if (cf != asset.lowPassFilter.cutoffFrequency || q != asset.lowPassFilter.lowpassResonanceQ)
                        {
                            Undo.RecordObject(asset, "Modified Low Pass Filter");
                            asset.lowPassFilter.cutoffFrequency = cf;
                            asset.lowPassFilter.lowpassResonanceQ = q;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (asset.highPassFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (highPassFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " High Pass Filter", "Filters the audio to let higher frequencies pass while removing frequencies lower than the cutoff");
                    EditorGUILayout.BeginHorizontal();
                    highPassFoldout = EditorGUILayout.Foldout(highPassFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(asset, "Removed High Pass Filter");
                        asset.highPassFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (highPassFoldout)
                    {
                        blontent = new GUIContent("Cutoff Frequency", "High-pass cutoff frequency in hertz");
                        float cf = asset.highPassFilter.cutoffFrequency;
                        cf = EditorGUILayout.Slider(
                            blontent, asset.highPassFilter.cutoffFrequency, 10, 22000);

                        blontent = new GUIContent("High Pass Resonance Q", "Determines how much the filter's self-resonance is dampened");
                        float q = asset.highPassFilter.highpassResonanceQ;
                        q = EditorGUILayout.Slider(
                            blontent, asset.highPassFilter.highpassResonanceQ, 1, 10);

                        if (cf != asset.highPassFilter.cutoffFrequency || q != asset.highPassFilter.highpassResonanceQ)
                        {
                            Undo.RecordObject(asset, "Modified High Pass Filter");
                            asset.highPassFilter.cutoffFrequency = cf;
                            asset.highPassFilter.highpassResonanceQ = q;
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                if (asset.reverbFilter.enabled)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string arrow = (reverbFoldout) ? "▼" : "▶";
                    blontent = new GUIContent("    " + arrow + " Reverb Filter", "Modifies the sound to make it feel like it's reverberating around a room");
                    EditorGUILayout.BeginHorizontal();
                    reverbFoldout = EditorGUILayout.Foldout(reverbFoldout, blontent, true, EditorStyles.boldLabel);
                    blontent = new GUIContent("x", "Remove this filter");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        Undo.RecordObject(asset, "Removed Reverb Filter");
                        asset.reverbFilter.enabled = false;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (reverbFoldout)
                    {
                        Undo.RecordObject(asset, "Modified Reverb Filter");
                        blontent = new GUIContent("Reverb Preset", "Custom reverb presets, select \"User\" to create your own customized reverb effects. You are highly recommended to use a preset.");
                        asset.reverbFilter.reverbPreset = (AudioReverbPreset)EditorGUILayout.EnumPopup(
                            blontent, asset.reverbFilter.reverbPreset);

                        using (new EditorGUI.DisabledScope(asset.reverbFilter.reverbPreset != AudioReverbPreset.User))
                        {
                            blontent = new GUIContent("Dry Level", "Mix level of dry signal in output in mB");
                            asset.reverbFilter.dryLevel = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.dryLevel, -10000, 0);
                            blontent = new GUIContent("Room", "Room effect level at low frequencies in mB");
                            asset.reverbFilter.room = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.room, -10000, 0);
                            blontent = new GUIContent("Room HF", "Room effect high-frequency level in mB");
                            asset.reverbFilter.roomHF = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.roomHF, -10000, 0);
                            blontent = new GUIContent("Room LF", "Room effect low-frequency level in mB");
                            asset.reverbFilter.roomLF = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.roomLF, -10000, 0);
                            blontent = new GUIContent("Decay Time", "Reverberation decay time at low-frequencies in seconds");
                            asset.reverbFilter.decayTime = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.decayTime, 0.1f, 20);
                            blontent = new GUIContent("Decay HFRatio", "Decay HF Ratio : High-frequency to low-frequency decay time ratio");
                            asset.reverbFilter.decayHFRatio = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.decayHFRatio, 0.1f, 20);
                            blontent = new GUIContent("Reflections Level", "Early reflections level relative to room effect in mB");
                            asset.reverbFilter.reflectionsLevel = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.reflectionsLevel, -10000, 1000);
                            blontent = new GUIContent("Reflections Delay", "Early reflections delay time relative to room effect in mB");
                            asset.reverbFilter.reflectionsDelay = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.reflectionsDelay, 0, 0.3f);
                            blontent = new GUIContent("Reverb Level", "Late reverberation level relative to room effect in mB");
                            asset.reverbFilter.reverbLevel = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.reverbLevel, -10000, 2000);
                            blontent = new GUIContent("Reverb Delay", "Late reverberation delay time relative to first reflection in seconds");
                            asset.reverbFilter.reverbDelay = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.reverbDelay, 0, 0.1f);
                            blontent = new GUIContent("HFReference", "Reference high frequency in Hz");
                            asset.reverbFilter.hFReference = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.hFReference, 1000, 20000);
                            blontent = new GUIContent("LFReference", "Reference low frequency in Hz");
                            asset.reverbFilter.lFReference = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.lFReference, 20, 1000);
                            blontent = new GUIContent("Diffusion", "Reverberation diffusion (echo density) in percent");
                            asset.reverbFilter.diffusion = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.diffusion, 0, 100);
                            blontent = new GUIContent("Density", "Reverberation density (modal density) in percent");
                            asset.reverbFilter.density = EditorGUILayout.Slider(
                                blontent, asset.reverbFilter.density, 0, 100);
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
                    if (asset.chorusFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableChorus);
                    blontent = new GUIContent("Distortion Filter");
                    if (asset.distortionFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableDistortion);
                    blontent = new GUIContent("Echo Filter");
                    if (asset.echoFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableEcho);
                    blontent = new GUIContent("High Pass Filter");
                    if (asset.highPassFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableHighPass);
                    blontent = new GUIContent("Low Pass Filter");
                    if (asset.lowPassFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableLowPass);
                    blontent = new GUIContent("Reverb Filter");
                    if (asset.reverbFilter.enabled) menu.AddDisabledItem(blontent);
                    else menu.AddItem(blontent, false, EnableReverb);
                    menu.ShowAsContext();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                #endregion
            }
            EditorCompatability.EndSpecialFoldoutGroup();
        }

        void EnableChorus()
        {
            Undo.RecordObject(asset, "Added Effect");
            asset.chorusFilter.enabled = true;
            asset.chorusFilter.dryMix = 0.5f;
            asset.chorusFilter.wetMix1 = 0.5f;
            asset.chorusFilter.wetMix2 = 0.5f;
            asset.chorusFilter.wetMix3 = 0.5f;
            asset.chorusFilter.delay = 40;
            asset.chorusFilter.rate = 0.8f;
            asset.chorusFilter.depth = 0.03f;
        }

        void EnableDistortion()
        {
            Undo.RecordObject(asset, "Added Effect");
            asset.distortionFilter.enabled = true;
            asset.distortionFilter.distortionLevel = 0.5f;
        }

        void EnableEcho()
        {
            Undo.RecordObject(asset, "Added Effect");
            asset.echoFilter.enabled = true;
            asset.echoFilter.delay = 500;
            asset.echoFilter.decayRatio = 0.5f;
            asset.echoFilter.wetMix = 1;
            asset.echoFilter.dryMix = 1;
        }

        void EnableHighPass()
        {
            Undo.RecordObject(asset, "Added Effect");
            asset.highPassFilter.enabled = true;
            asset.highPassFilter.cutoffFrequency = 5000;
            asset.highPassFilter.highpassResonanceQ = 1;
        }

        void EnableLowPass()
        {
            Undo.RecordObject(asset, "Added Effect");
            asset.lowPassFilter.enabled = true;
            asset.lowPassFilter.cutoffFrequency = 5000;
            asset.lowPassFilter.lowpassResonanceQ = 1;
        }

        void EnableReverb()
        {
            Undo.RecordObject(asset, "Added Effect");
            asset.reverbFilter.enabled = true;
            asset.reverbFilter.reverbPreset = AudioReverbPreset.Generic;
            asset.reverbFilter.dryLevel = 0;
            asset.reverbFilter.room = 0;
            asset.reverbFilter.roomHF = 0;
            asset.reverbFilter.roomLF = 0;
            asset.reverbFilter.decayTime = 1;
            asset.reverbFilter.decayHFRatio = 0.5f;
            asset.reverbFilter.reflectionsLevel = -10000.0f;
            asset.reverbFilter.reflectionsDelay = 0;
            asset.reverbFilter.reverbLevel = 0;
            asset.reverbFilter.reverbDelay = 0.04f;
            asset.reverbFilter.hFReference = 5000;
            asset.reverbFilter.lFReference = 250;
            asset.reverbFilter.diffusion = 100;
            asset.reverbFilter.density = 100;
        }
        #endregion
    }
}