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
        protected bool SourcePlaying => helper.Source.isPlaying;
        protected bool clipPlaying;
        protected bool clipPaused;

        protected JSAMEditorFader editorFader;
        protected EditorAudioHelper helper;
        protected abstract void PlayDebug(BaseAudioFileObject asset, bool dontReset);
        protected abstract void AssignHelperToFader(AudioClip clip);

        protected bool mouseDragging;
        protected bool mouseScrubbed;
        protected bool loopClip;

        protected static GUIContent backIcon;
        protected static GUIContent[] playIcons;
        protected static GUIContent[] pauseIcons;
        protected static GUIContent[] loopIcons;
        protected static GUIContent openIcon;

        Color COLOR_BUTTONPRESSED => new Color(0.475f, 0.475f, 0.475f);

        /// <summary>
        /// True when switching to a new track, is set false once starting looping behaviour
        /// </summary>
        public static bool FreePlay = false;
        /// <summary>
        /// True so long as the inspector music player hasn't looped
        /// </summary>
        public static bool FirstPlayback = true;

        protected Texture2D cachedTex;
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

        Color COLOR_BUTTONPRESSED_2 = new Color(0.75f, 0.75f, 0.75f);

#if !UNITY_2020_3_OR_NEWER
        protected EditorCompatability.AudioClipList list;
#endif

        protected GUIContent blontent;

        protected Vector2 scroll;

        protected AudioClipList list;

        protected bool NoFiles => files.arraySize == 0;

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

            helper = new EditorAudioHelper(asset.Files.Count > 0 ? asset.Files[0] : null);

            isPreset = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset as UnityEngine.Object));

            SetupIcons();

            editorFader = new JSAMEditorFader(asset);

            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
            Undo.postprocessModifications += PostProcessModifications;
        }

        protected virtual void OnDisable()
        {
            helper.Dispose();

            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.postprocessModifications -= PostProcessModifications;
        }

        protected SerializedProperty safeName;
        protected SerializedProperty presetDescription;
        protected SerializedProperty files;
        protected SerializedProperty relativeVolume;
        protected SerializedProperty spatialize;
        protected SerializedProperty maxDistance;
        protected SerializedProperty priority;
        protected SerializedProperty startingPitch;
        protected SerializedProperty pitchShift;
        protected SerializedProperty delay;
        protected SerializedProperty ignoreTimeScale;
        protected SerializedProperty maxPlayingInstances;
        protected SerializedProperty channelOverride;
        protected SerializedProperty mixerGroupOverride;

        protected SerializedProperty fadeInOut;
        protected SerializedProperty fadeInDuration;
        protected SerializedProperty fadeOutDuration;
        protected SerializedProperty loopMode;
        protected SerializedProperty loopStart;
        protected SerializedProperty loopEnd;

        protected SerializedProperty bypassEffects;
        protected SerializedProperty bypassListenerEffects;
        protected SerializedProperty bypassReverbZones;

        protected virtual void DesignateSerializedProperties()
        {
            safeName = FindProp(nameof(safeName));
            presetDescription = FindProp(nameof(presetDescription));
            files = FindProp(nameof(files));

            relativeVolume = FindProp(nameof(relativeVolume));
            spatialize = FindProp(nameof(spatialize));
            maxDistance = FindProp(nameof(maxDistance));
            priority = FindProp(nameof(priority));
            startingPitch = FindProp(nameof(startingPitch));
            pitchShift = FindProp(nameof(pitchShift));
            delay = FindProp(nameof(delay));
            ignoreTimeScale = FindProp(nameof(ignoreTimeScale));
            maxPlayingInstances = FindProp(nameof(maxPlayingInstances));
            channelOverride = FindProp(nameof(channelOverride));
            mixerGroupOverride = FindProp(nameof(mixerGroupOverride));

            fadeInOut = FindProp(nameof(fadeInOut));
            fadeInDuration = FindProp(nameof(fadeInDuration));
            fadeOutDuration = FindProp(nameof(fadeOutDuration));
            loopMode = FindProp(nameof(loopMode));
            loopStart = FindProp(nameof(loopStart));
            loopEnd = FindProp(nameof(loopEnd));

            bypassEffects = FindProp(nameof(asset.bypassEffects));
            bypassListenerEffects = serializedObject.FindProperty(nameof(asset.bypassListenerEffects));
            bypassReverbZones = serializedObject.FindProperty(nameof(asset.bypassReverbZones));

            RedesignateActiveAudioClip();
            CheckFiles();
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

        protected void RenderBasicProperties()
        {
            EditorGUILayout.PropertyField(relativeVolume);
            EditorGUILayout.PropertyField(spatialize);
            using (new EditorGUI.DisabledScope(!spatialize.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(maxDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    if (maxDistance.floatValue < 0)
                    {
                        maxDistance.floatValue = 0;
                    }
                }
            }
            EditorGUILayout.PropertyField(priority);
            EditorGUILayout.PropertyField(startingPitch);
            EditorGUILayout.PropertyField(pitchShift);
            EditorGUILayout.PropertyField(delay);
            EditorGUILayout.PropertyField(ignoreTimeScale);
            EditorGUILayout.PropertyField(maxPlayingInstances);
            EditorGUILayout.PropertyField(channelOverride);
            EditorGUILayout.PropertyField(mixerGroupOverride);
        }

        protected void RenderSpecialProperties()
        {
            if (!isPreset) DrawPlaybackTool();

            DrawLoopPointTools(asset);

            DrawFadeTools(activeClip);

            DrawAudioEffectTools();
        }

        protected void PostFixAndSave()
        {
            if (serializedObject.hasModifiedProperties)
            {
                AudioPlaybackToolEditor.DoForceRepaint(true);
                serializedObject.ApplyModifiedProperties();

                // Manually fix variables
                if (asset.delay < 0)
                {
                    asset.delay = 0;
                    Undo.RecordObject(asset, "Fixed negative delay");
                }
            }
        }

        protected virtual void Update()
        {
            if (asset == null) return; // This can happen on the same frame it's deleted
            if (asset.Files.Count == 0) return;

            AudioClip clip = asset.Files[0];

            // Why did we enforce the activeClip being the first clip again?
            //if (clip != activeClip)
            //{
            //    activeClip = clip;
            //    helper.Source.clip = activeClip;
            //}

            if ((clipPlaying && !clipPaused) || (mouseDragging && SourcePlaying))
            {
                float clipPos = helper.Source.timeSamples / (float)activeClip.frequency;

                if (loopClip)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (asset.loopMode == LoopMode.LoopWithLoopPoints)
                    {
                        // We've probably reached the end of the track before officially hitting the loop point marker
                        if (!helper.Source.isPlaying && clipPlaying && !clipPaused)
                        {
                            helper.Source.Play();
                            helper.Source.timeSamples = Mathf.CeilToInt(asset.loopStart * activeClip.frequency);
                        }
                        else if (clipPos >= asset.loopEnd)
                        {
                            helper.Source.timeSamples = Mathf.CeilToInt(asset.loopStart * activeClip.frequency);
                            FirstPlayback = false;
                        }
                    }
                    else if (asset.loopMode == LoopMode.ClampedLoopPoints)
                    {
                        if (clipPos < asset.loopStart || clipPos > asset.loopEnd)
                        {
                            // CeilToInt to guarantee clip position stays within loop bounds
                            helper.Source.timeSamples = Mathf.CeilToInt(asset.loopStart * activeClip.frequency);
                            FirstPlayback = false;
                        }
                    }
                }
                else if (!loopClip)
                {
                    if (asset.loopMode == LoopMode.LoopWithLoopPoints)
                    {
                        if ((!helper.Source.isPlaying && !clipPaused) || clipPos > asset.loopEnd)
                        {
                            helper.Source.Stop();
                        }
                    }
                    else if (asset.loopMode == LoopMode.ClampedLoopPoints && clipPos < asset.loopStart)
                    {
                        helper.Source.timeSamples = Mathf.CeilToInt(asset.loopStart * activeClip.frequency);
                    }
                    else if (!SourcePlaying) clipPlaying = false;
                }
            }

            if (asset.loopMode != LoopMode.LoopWithLoopPoints)
            {
                if (!helper.Source.isPlaying && !clipPaused && clipPlaying)
                {
                    helper.Source.time = 0;
                    if (loopClip)
                    {
                        helper.Source.Play();
                    }
                }
            }
        }

        public override bool RequiresConstantRepaint() => clipPlaying || mouseDragging;

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

        protected int missingFiles;
        void CheckFiles()
        {
            missingFiles = 0;
            for (int i = 0; i < files.arraySize; i++)
            {
                var f = files.GetArrayElementAtIndex(i);
                if (f.objectReferenceValue == null) missingFiles++;
            }
        }

        /// <summary>
        /// Assigns an AudioClip to the activeClip variable. 
        /// Will fail if AudioClip as marked as missing
        /// </summary>
        protected void RedesignateActiveAudioClip()
        {
            AudioClip theClip = null;
            if (files.arraySize != 0)
            {
                for (int i = 0; i < files.arraySize; i++)
                {
                    theClip = files.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
                    if (theClip) break;
                }
            }
            if (theClip != null)
            {
                activeClip = theClip;
                helper.Clip = activeClip;
                AudioPlaybackToolEditor.forceRepaint = true;
            }
        }

        protected UndoPropertyModification[] PostProcessModifications(UndoPropertyModification[] modifications)
        {
            if (!asset.Files.Contains(activeClip))
            {
                RedesignateActiveAudioClip();
            }

            CheckFiles();
            return modifications;
        }

        void OnUndoRedo()
        {
            if (AudioPlaybackToolEditor.WindowOpen)
            {
                AudioPlaybackToolEditor.DoForceRepaint(true);
            }
            Repaint();
        }

        protected void RenderFileList()
        {
            if (isPreset) return;
#if UNITY_2020_3_OR_NEWER
            if (JSAMSettings.Settings.UseBuiltInAudioListRenderer)
#else
            if (false)
#endif
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(files);
                if (EditorGUI.EndChangeCheck())
                {
                    //files = files.RemoveNullElementsFromArray();
                    if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                int magicNumber = 5; // Maybe this should be set in JSAMSettings?

                #region Library Region
                Rect overlay = new Rect();
                EditorGUILayout.BeginHorizontal();
                showLibrary = EditorCompatability.SpecialFoldouts(showLibrary, "Library");
                EditorGUILayout.EndHorizontal();
                if (showLibrary)
                {
                    GUILayoutOption[] layoutOptions;
                    layoutOptions = expandLibrary && files.arraySize > magicNumber ? new GUILayoutOption[0] : new GUILayoutOption[] { GUILayout.MinHeight(150) };
                    overlay = EditorGUILayout.BeginVertical(GUI.skin.box, layoutOptions);

                    scroll = EditorGUILayout.BeginScrollView(scroll);

                    if (files.arraySize > magicNumber)
                    {
                        string label = expandLibrary ? "Retract Library" : "Expand Library";
                        if (JSAMEditorHelper.CondensedButton(label))
                        {
                            expandLibrary = !expandLibrary;
                        }
                    }

                    if (files.arraySize > 0)
                    {
                        EditorGUI.BeginChangeCheck();
                        list.Draw();
                        if (EditorGUI.EndChangeCheck())
                        {
                            CheckFiles();
                        }
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
                #endregion
            }

            string fileLabel = "File Count: " + files.arraySize;
            if (missingFiles > 0) fileLabel += " | Missing Files: " + missingFiles;
            EditorGUILayout.LabelField(fileLabel);

            RenderNoClipWarning();
        }

        protected void RenderNoClipWarning()
        {
            if (!isPreset)
            {
                if (NoFiles)
                {
                    EditorGUILayout.HelpBox("Add an audio file before running!", MessageType.Error);
                }
                else if (missingFiles > 0)
                {
                    EditorGUILayout.HelpBox(missingFiles + " AudioClip(s) are missing!", MessageType.Warning);
                }
            }
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

            if (files.arraySize == 0 || !activeClip)
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

            AudioClip music = activeClip;
            var value = (float)helper.Source.timeSamples / (float)music.samples;

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
        protected virtual void DrawPlaybackTool()
        {
            GUIContent blontent = new GUIContent("Audio Playback Preview",
                "Allows you to preview how your AudioFileMusicObject will sound during runtime right here in the inspector. " +
                "Some effects, like spatialization, will not be available to preview");
            showPlaybackTool = EditorCompatability.SpecialFoldouts(showPlaybackTool, blontent);

            if (showPlaybackTool)
            {
                Rect progressRect;
                if (ProgressBar(out progressRect))
                {
                    AudioClip music = files.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip;

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
                                if (!music) break;
                                float newProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, evt.mousePosition.x);
                                helper.Source.time = Mathf.Clamp((newProgress * music.length), 0, music.length - AudioManagerInternal.EPSILON);
                                if (asset.loopMode == LoopMode.ClampedLoopPoints)
                                {
                                    float start = asset.loopStart * asset.Files[0].frequency;
                                    float end = asset.loopEnd * asset.Files[0].frequency;
                                    helper.Source.timeSamples = (int)Mathf.Clamp(helper.Source.timeSamples, start, end - AudioManagerInternal.EPSILON);
                                }
                                break;
                        }
                    }

                    if (GUILayout.Button(backIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        if (asset.loopMode == LoopMode.ClampedLoopPoints)
                        {
                            helper.Source.timeSamples = Mathf.CeilToInt((asset.loopStart * music.frequency));
                        }
                        else
                        {
                            helper.Source.timeSamples = 0;
                        }
                        helper.Source.Stop();
                        mouseScrubbed = false;
                        clipPaused = false;
                    }

                    Color colorbackup = GUI.backgroundColor;
                    GUIContent buttonIcon = SourcePlaying ? playIcons[1] : playIcons[0];
                    if (clipPlaying) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        clipPlaying = !clipPlaying;
                        if (clipPlaying)
                        {
                            // Note: For some reason, reading from helper.Source.time returns 0 even if timeSamples is not 0
                            // However, writing a value to helper.Source.time changes timeSamples to the appropriate value just fine
                            PlayDebug(asset, mouseScrubbed);
                            editorFader.StartFading(activeClip, asset);
                            AssignHelperToFader(activeClip);
                            if (clipPaused) helper.Source.Pause();
                            FirstPlayback = true;
                            FreePlay = true;
                        }
                        else
                        {
                            helper.Source.Stop();
                            if (!mouseScrubbed)
                            {
                                helper.Source.time = 0;
                            }
                            clipPaused = false;
                        }
                    }

                    GUI.backgroundColor = colorbackup;
                    GUIContent theText = clipPaused ? pauseIcons[1] : pauseIcons[0];
                    if (clipPaused) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(theText, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        clipPaused = !clipPaused;
                        if (clipPaused)
                        {
                            helper.Source.Pause();
                        }
                        else
                        {
                            helper.Source.UnPause();
                        }
                    }

                    GUI.backgroundColor = colorbackup;
                    buttonIcon = loopClip ? loopIcons[1] : loopIcons[0];
                    if (loopClip) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        loopClip = !loopClip;
                        // helper.Source.loop = true;
                    }
                    GUI.backgroundColor = colorbackup;

                    RenderAuxiliaryPlaybackControls();

                    if (GUILayout.Button(openIcon, new GUILayoutOption[] { GUILayout.MaxHeight(19) }))
                    {
                        AudioPlaybackToolEditor.Init();
                    }

                    // Reset loop point input mode if not using loop points so the duration shows up as time by default
                    if (asset.loopMode != LoopMode.LoopWithLoopPoints && asset.loopMode != LoopMode.ClampedLoopPoints) loopPointInputMode = 0;

                    blontent = new GUIContent("-", "The playback time");
                    if (activeClip)
                    {
                        switch ((LoopPointTool)loopPointInputMode)
                        {
                            case LoopPointTool.Slider:
                            case LoopPointTool.TimeInput:
                                var time = (float)helper.Source.timeSamples / activeClip.frequency;
                                blontent = new GUIContent(time.TimeToString() + " / " + (activeClip.length.TimeToString()),
                                    "The playback time in seconds");
                                break;
                            case LoopPointTool.TimeSamplesInput:
                                blontent = new GUIContent(helper.Source.timeSamples + " / " + activeClip.samples, "The playback time in samples");
                                break;
                            case LoopPointTool.BPMInput:
                                blontent = new GUIContent(string.Format("{0:0}", helper.Source.time / (60f / asset.bpm)) + " / " + activeClip.length / (60f / asset.bpm),
                                    "The playback time in beats");
                                break;
                        }
                    }

                    GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                    rightJustified.alignment = TextAnchor.UpperRight;
                    EditorGUILayout.LabelField(blontent, rightJustified);
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

        protected virtual void RenderAuxiliaryPlaybackControls()
        {

        }

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
                                helper.Source.time = Mathf.Clamp((newProgress * audio.length), 0, audio.length - AudioManagerInternal.EPSILON);
                                if (asset.loopMode == LoopMode.ClampedLoopPoints)
                                {
                                    float start = asset.loopStart * asset.Files[0].frequency;
                                    float end = asset.loopEnd * asset.Files[0].frequency;
                                    helper.Source.timeSamples = (int)Mathf.Clamp(helper.Source.timeSamples, start, end - AudioManagerInternal.EPSILON);
                                }
                                break;
                        }
                    }

                    if (GUILayout.Button(backIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        if (asset.loopMode == LoopMode.ClampedLoopPoints)
                        {
                            helper.Source.timeSamples = Mathf.CeilToInt(asset.loopStart * audio.frequency);
                        }
                        else
                        {
                            helper.Source.timeSamples = 0;
                        }
                        helper.Source.Stop();
                        mouseScrubbed = false;
                        clipPaused = false;
                    }

                    Color colorbackup = GUI.backgroundColor;
                    GUIContent buttonIcon = clipPlaying ? playIcons[1] : playIcons[0];
                    if (clipPlaying) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        clipPlaying = !clipPlaying;
                        if (!clipPlaying)
                        {
                            // Note: For some reason, reading from helper.Source.time returns 0 even if timeSamples is not 0
                            // However, writing a value to helper.Source.time changes timeSamples to the appropriate value just fine

                            PlayDebug(asset, mouseScrubbed);

                            editorFader.StartFading(activeClip, asset);
                            AssignHelperToFader(activeClip);

                            if (clipPaused) helper.Source.Pause();
                            FirstPlayback = true;
                            FreePlay = false;
                        }
                        else
                        {
                            helper.Source.Stop();
                            if (!mouseScrubbed)
                            {
                                helper.Source.time = 0;
                            }
                            clipPaused = false;
                        }
                    }

                    GUI.backgroundColor = colorbackup;
                    GUIContent theText = (clipPaused) ? pauseIcons[1] : pauseIcons[0];
                    if (clipPaused) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(theText, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        clipPaused = !clipPaused;
                        if (clipPaused)
                        {
                            helper.Source.Pause();
                        }
                        else
                        {
                            helper.Source.UnPause();
                        }
                    }

                    GUI.backgroundColor = colorbackup;
                    buttonIcon = (loopClip) ? loopIcons[1] : loopIcons[0];
                    if (loopClip) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        loopClip = !loopClip;
                        // helper.Source.loop = true;
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
                                ((float)helper.Source.timeSamples / helper.Source.clip.frequency).TimeToString() +
                                " / " +
                                helper.Source.clip.length.TimeToString(),
                                "The playback time in seconds");
                            break;
                        case LoopPointTool.TimeSamplesInput:
                            blontent = new GUIContent(helper.Source.timeSamples + " / " + audio.samples, "The playback time in samples");
                            break;
                        case LoopPointTool.BPMInput:
                            blontent = new GUIContent(string.Format("{0:0}", helper.Source.time / (60f / asset.bpm)) + " / " + audio.length / (60f / asset.bpm),
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

        protected void DrawFadeTools(AudioClip focusedClip)
        {
            EditorGUILayout.PropertyField(fadeInOut);

            showFadeTool = EditorCompatability.SpecialFoldouts(showFadeTool, new GUIContent("Fade Tools", "Show/Hide the Audio Fade previewer"));
            using (new EditorGUI.DisabledScope(!fadeInOut.boolValue))
            {
                if (showFadeTool)
                {
                    float duration = focusedClip ? focusedClip.length : 0;

                    GUIContent fContent = new GUIContent();
                    GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                    rightJustified.alignment = TextAnchor.UpperRight;
                    rightJustified.padding = new RectOffset(0, 15, 0, 0);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Fade In Time:    " + (fadeInDuration.floatValue * duration).TimeToString(), "Fade in time for this AudioClip in seconds"));
                    EditorGUILayout.LabelField(new GUIContent("Sound Length: " + (duration).TimeToString(), "Length of the preview clip in seconds"), rightJustified);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Fade Out Time: " + (fadeOutDuration.floatValue * duration).TimeToString(), "Fade out time for this AudioClip in seconds"));
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
            }
            EditorCompatability.EndSpecialFoldoutGroup();
        }

        protected void DrawLoopPointTools(BaseAudioFileObject asset)
        {
            if (asset.Files.Count == 0) return;

            float start = asset.loopStart;
            float end = asset.loopEnd;

            float duration = 0;
            int frequency = 0;
            int samples = 0;

            if (activeClip)
            {
                duration = activeClip.length;
                frequency = activeClip.frequency;
                samples = activeClip.samples;
            }

            EditorGUI.BeginDisabledGroup(activeClip == null);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(loopMode);
            if (EditorGUI.EndChangeCheck())
            {
                // This won't do, reset loop point positions
                if (asset.loopStart >= asset.loopEnd)
                {
                    loopStart.floatValue = 0;
                    loopEnd.floatValue = duration;
                    start = asset.loopStart;
                    end = asset.loopEnd;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUI.EndDisabledGroup();

            blontent = new GUIContent("Loop Point Tools", "Customize where music will loop between. " +
                    "Loops may not appear to be seamless in the inspector but rest assured, they will be seamless in-game!");
            showLoopPointTool = EditorCompatability.SpecialFoldouts(showLoopPointTool, blontent);
            using (new EditorGUI.DisabledScope(asset.loopMode < LoopMode.LoopWithLoopPoints))
            {
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
                            EditorGUILayout.MinMaxSlider(ref start, ref end, 0, duration);

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Loop Point Start: " + start.TimeToString(), new GUILayoutOption[] { GUILayout.Width(180) });
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Loop Point Start (Samples): " + asset.loopStart * frequency);
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Loop Point End:   " + end.TimeToString(), new GUILayoutOption[] { GUILayout.Width(180) });
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Loop Point End (Samples): " + asset.loopEnd * frequency);
                            GUILayout.EndHorizontal();
                            break;
                        case LoopPointTool.TimeInput:
                            EditorGUILayout.Space();

                            // int casts are used instead of Mathf.RoundToInt so that we truncate the floats instead of rounding up
                            GUILayout.BeginHorizontal();
                            float theTime = start * 1000f;
                            GUILayout.Label("Loop Point Start:");
                            int minutes = EditorGUILayout.IntField((int)(theTime / 60000f));
                            GUILayout.Label(":");
                            int seconds = Mathf.Clamp(EditorGUILayout.IntField((int)((theTime % 60000) / 1000f)), 0, 59);
                            GUILayout.Label(":");
                            float milliseconds = Mathf.Clamp(EditorGUILayout.IntField(Mathf.RoundToInt(theTime % 1000f)), 0, 999.0f);
                            milliseconds /= 1000.0f; // Ensures that our milliseconds never leave their decimal place
                            start = (float)minutes * 60f + (float)seconds + milliseconds;
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            theTime = end * 1000f;
                            GUILayout.Label("Loop Point End:  ");
                            minutes = EditorGUILayout.IntField((int)(theTime / 60000f));
                            GUILayout.Label(":");
                            seconds = Mathf.Clamp(EditorGUILayout.IntField((int)((theTime % 60000) / 1000f)), 0, 59);
                            GUILayout.Label(":");
                            milliseconds = Mathf.Clamp(EditorGUILayout.IntField(Mathf.RoundToInt(theTime % 1000f)), 0, 999.0f);
                            milliseconds /= 1000.0f; // Ensures that our milliseconds never leave their decimal place
                            end = (float)minutes * 60f + (float)seconds + milliseconds;
                            GUILayout.EndHorizontal();
                            break;
                        case LoopPointTool.TimeSamplesInput:
                            GUILayout.Label("Song Duration (Samples): " + samples);
                            EditorGUILayout.Space();

                            GUILayout.BeginHorizontal();
                            float samplesStart = EditorGUILayout.FloatField("Loop Point Start:", asset.loopStart * frequency);
                            GUILayout.EndHorizontal();
                            start = samplesStart / frequency;

                            GUILayout.BeginHorizontal();
                            float samplesEnd = JSAMExtensions.Clamp(EditorGUILayout.FloatField("Loop Point End:", asset.loopEnd * frequency), 0, samples);
                            GUILayout.EndHorizontal();
                            end = samplesEnd / frequency;
                            break;
                        case LoopPointTool.BPMInput/*WithBeats*/:
                            Undo.RecordObject(asset, "Modified song BPM");
                            asset.bpm = EditorGUILayout.IntField("Song BPM: ", asset.bpm/*, new GUILayoutOption[] { GUILayout.MaxWidth(30)}*/);

                            EditorGUILayout.Space();

                            float startBeat = start / (60f / (float)asset.bpm);
                            startBeat = EditorGUILayout.FloatField("Starting Beat:", startBeat);

                            float endBeat = end / (60f / (float)asset.bpm);
                            endBeat = Mathf.Clamp(EditorGUILayout.FloatField("Ending Beat:", endBeat), 0, duration / (60f / asset.bpm));

                            start = (float)startBeat * 60f / (float)asset.bpm;
                            end = (float)endBeat * 60f / (float)asset.bpm;
                            break;
                    }

                    GUIContent buttonText = new GUIContent("Reset Loop Points", "Click to set loop points to the start and end of the track.");
                    if (GUILayout.Button(buttonText))
                    {
                        start = 0;
                        end = duration;
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
                                start = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].Start"]) / frequency;
                                end = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].End"]) / frequency;
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

                    if (activeClip)
                    {
                        if (asset.loopStart != start || asset.loopEnd != end)
                        {
                            Undo.RecordObject(asset, "Modified loop point properties");
                            loopStart.floatValue = Mathf.Clamp(start, 0, duration);
                            loopEnd.floatValue = Mathf.Clamp(end, 0, Mathf.Ceil(duration));
                            EditorUtility.SetDirty(asset);
                            AudioPlaybackToolEditor.DoForceRepaint(true);
                        }
                    }
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();
        }

        static AudioSource reference;
        protected void DrawSpatialSoundSettingProperty()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField("Reference", reference, typeof(AudioSource), true);

            if (GUILayout.Button("Copy"))
            {

            }

            EditorGUILayout.EndHorizontal();
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