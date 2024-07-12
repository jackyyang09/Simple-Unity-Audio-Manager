using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;

namespace JSAM.JSAMEditor
{
    /// <summary>
    /// Handles the Playback Tool Editor Window
    /// Can play AudioFileObjects, AudioFileMusicObjects and generic AudioClips
    /// Double click on the former to automatically open the window
    /// </summary>
    public class AudioPlaybackToolEditor : EditorWindow
    {
        public enum SelectedAssetType
        {
            AudioClip,
            Sound,
            Music
        }

        JSAMEditorFader editorFader;
        public JSAMEditorFader EditorFader
        {
            get
            {
                if (editorFader == null)
                {
                    editorFader = new JSAMEditorFader(ActiveSound, Helper);
                }
                return editorFader;
            }
        }

        static Texture2D cachedTex;
        public static bool forceRepaint;

        static Vector2 dragStartPos = Vector2.zero;
        static bool mouseDragging;
        static bool mouseScrubbed;
        static bool clipPaused;
        static bool ClipPlaying => Helper.Source.isPlaying;

        readonly string LOOP_KEY = nameof(AudioPlaybackToolEditor) + "Loop";
        bool LoopClip
        {
            get => EditorPrefs.GetBool(LOOP_KEY);
            set => EditorPrefs.SetBool(LOOP_KEY, value);
        }

        static EditorAudioHelper helper;
        static EditorAudioHelper Helper
        {
            get
            {
                if (helper == null)
                {
                    helper = new EditorAudioHelper(null);
                }
                return helper;
            }
        }

        public static bool HelperSourceActive 
        { 
            get
            {
                if (!Helper.Source) return false;
                if (!Helper.Clip) return false;
                return true;
            } 
        }

        readonly Color buttonPressedColor = new Color(0.475f, 0.475f, 0.475f);
        readonly Color buttonPressedColorLighter = new Color(0.75f, 0.75f, 0.75f);

        static Vector2 lastWindowSize = Vector2.zero;
        static bool resized = false;

        UnityEngine.Object activeAsset;
        AudioClip ActiveClip => activeAsset as AudioClip;
        SoundFileObject ActiveSound => activeAsset as SoundFileObject;
        MusicFileObject ActiveMusic => activeAsset as MusicFileObject;
        SelectedAssetType activeAssetType;

        readonly string LOCKED_ASSET_KEY = nameof(AudioPlaybackToolEditor) + "_Locked_Asset";
        string LockedAssetPath
        {
            get => EditorPrefs.GetString(LOCKED_ASSET_KEY);
            set => EditorPrefs.SetString(LOCKED_ASSET_KEY, value);
        }
        UnityEngine.Object lockedAsset;
        public UnityEngine.Object LockedAsset
        {
            get
            {
                if (lockedAsset == null)
                {
                    lockedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(LockedAssetPath);
                }
                return lockedAsset;
            }
            set
            {
                lockedAsset = value;
                LockedAssetPath = AssetDatabase.GetAssetPath(lockedAsset);
            }
        }

        static bool showHowTo;
        static bool showLibraryView = false;
        static float libraryScroll;

        static PreviewRenderUtility m_PreviewUtility;

        static AudioPlaybackToolEditor window;
        public static AudioPlaybackToolEditor Window
        {
            get
            {
                if (window == null) window = GetWindow<AudioPlaybackToolEditor>();
                return window;
            }
        }

        public static bool WindowOpen
        {
            get
            {
#if UNITY_2019_4_OR_NEWER
                return HasOpenInstances<AudioPlaybackToolEditor>();
#else
                return window != null;
#endif
            }
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/JSAM/JSAM Playback Tool")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            window = GetWindow<AudioPlaybackToolEditor>();
            window.Show();
            window.Focus();
            window.titleContent.text = "JSAM Playback Tool";
            // Refresh window contents
            window.OnSelectionChange();
            lastWindowSize = Window.position.size;
        }

        [OnOpenAsset]
        public static bool OnDoubleClickAssets(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            var file = AssetDatabase.LoadAssetAtPath<BaseAudioFileObject>(assetPath);
            if (file)
            {
                SoundFileObject soundFile = file as SoundFileObject;
                if (soundFile)
                {
                    Init();
                    cachedTex = null;
                    return true;
                }

                MusicFileObject musicFile = file as MusicFileObject;
                if (musicFile)
                {
                    Init();
                    cachedTex = null;
                    return true;
                }
            }
            
            return false;
        }

        private void OnEnable()
        {
            UpdateActiveAsset(LockedAsset);
            //m_HandleLinesMaterial = EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat") as Material;
        }

        private void OnDisable()
        {
            if (EditorFader != null)
            {
                EditorFader.Dispose();
                editorFader = null;
            }

            //m_HandleLinesMaterial = null;

            window = null;

            // When the window is closed, the active selection changes to the Previewed asset
            // Why?
            //Selection.activeObject = null;
            //EditorApplication.delayCall += () => Selection.activeObject = activeAsset;

            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }

            Helper.Dispose();
            helper = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            DrawPlaybackTool();
            EditorGUILayout.BeginHorizontal();
            if (HelperSourceActive)
            {
                EditorGUILayout.LabelField("Now Playing - " + Helper.Clip.name);
            }
            else
            {
                EditorGUILayout.LabelField("-");
            }
            EditorGUILayout.EndHorizontal();

            if (!resized)
            {
                if (lastWindowSize != Window.position.size)
                {
                    lastWindowSize = Window.position.size;
                    resized = true;
                    cachedTex = null;
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            if (showLibraryView)
            {
                if (activeAssetType != SelectedAssetType.Sound)
                {
                    EditorGUILayout.LabelField("Library view is only available for Sound File Objects!",
                        EditorStyles.label.ApplyWordWrap().SetFontSize(JSAMSettings.Settings.QuickReferenceFontSize),
                        GUILayout.Width(140));
                }
                else
                {
                    libraryScroll = EditorGUILayout.BeginScrollView(new Vector2(0, libraryScroll)).y;
                    var selectedSound = activeAsset as SoundFileObject;
                    for (int i = 0; i < selectedSound.Files.Count; i++)
                    {
                        AudioClip sound = selectedSound.Files[i];
                        Color colorbackup = GUI.backgroundColor;
                        //EditorGUILayout.BeginHorizontal();
                        if (Helper.Clip == sound) GUI.backgroundColor = buttonPressedColor;
                        var buttonName = sound.name;
                        if (buttonName.Length > 15) buttonName = sound.name.Substring(0, 15) + "...";
                        GUIContent bContent = new GUIContent(buttonName, "Click to change AudioClip being played back to " + sound.name);
                        if (GUILayout.Button(bContent))
                        {
                            // Play the sound
                            Helper.Clip = sound;
                            EditorFader.StartFading(Helper.Clip, selectedSound);
                        }
                        //EditorGUILayout.EndHorizontal();
                        GUI.backgroundColor = colorbackup;
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            #region Quick Reference Guide
            JSAMEditorHelper.StartMeasureLastGuideSize();
            showHowTo = JSAMEditorHelper.RenderQuickReferenceGuide(showHowTo, new string[]
            {
                "Overview",
                "This EditorWindow serves as a high-fidelity alternative to the small playback preview in the" +
                "inspector window used when inspecting Audio File Objects.",
                "Tips",
                "The active playing clip can be changed by selecting different assets in the Project window.",
                "You can open the JSAM Playback Tool by double-clicking on Audio File assets in the Project window.",
                "The JSAM Playback Tool can also play standard AudioClips!"
            });
            if (!showHowTo)
            {
                // Dirty hack
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            #endregion
        }

        private void OnSelectionChange()
        {
            if (LockedAsset) return;

            UpdateActiveAsset(Selection.activeObject);
            
            DoForceRepaint(true);
        }

        void UpdateActiveAsset(UnityEngine.Object obj)
        {
            if (obj == null) return;
            var file = obj as BaseAudioFileObject;
            if (file)
            {
                var sound = file as SoundFileObject;
                if (sound)
                {
                    activeAsset = file;

                    activeAssetType = SelectedAssetType.Sound;

                    if (sound.Files.Count > 0) Helper.Clip = sound.Files[0];
                }
                else
                {
                    var music = file as MusicFileObject;
                    if (music)
                    {
                        activeAsset = file;
                        activeAssetType = SelectedAssetType.Music;

                        if (music.Files.Count > 0) Helper.Clip = music.Files[0];
                    }
                }
            }
            else
            {
                var clip = obj as AudioClip;
                if (clip)
                {
                    activeAsset = file;
                    activeAssetType = SelectedAssetType.AudioClip;
                    Helper.Clip = clip;
                }
            }
        }

        /// <summary>
        /// Draws a playback 
        /// </summary>
        /// <param name="music"></param>
        public void DrawPlaybackTool()
        {
            float progress = 0;
            if (HelperSourceActive)
            {
                progress = (float)Helper.Source.timeSamples / (float)Helper.Clip.samples;
            }
            Rect progressRect = ProgressBar(progress);
            EditorGUI.BeginChangeCheck();
            if (EditorGUI.EndChangeCheck())
            {
                DoForceRepaint(true);
            }

            EditorGUILayout.BeginHorizontal();

            PollMouseEvents(progressRect);

            using (new EditorGUI.DisabledScope(!HelperSourceActive))
            {
                if (GUILayout.Button(BackIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    Helper.Source.timeSamples = 0;
                    Helper.Source.Stop();
                    mouseScrubbed = false;
                    clipPaused = false;
                }

                // Draw Play Button
                GUIContent buttonIcon = ClipPlaying ? PlayIcons[1] : PlayIcons[0];
                if (ClipPlaying) JSAMEditorHelper.BeginBackgroundColourChange(buttonPressedColor);
                if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    if (!ClipPlaying)
                    {
                        switch (activeAssetType)
                        {
                            case SelectedAssetType.AudioClip:
                                Helper.SoundHelper.PlayDebug(mouseScrubbed);
                                break;
                            case SelectedAssetType.Sound:
                                EditorFader.StartFading(Helper.Clip, ActiveSound);
                                break;
                            case SelectedAssetType.Music:
                                Helper.MusicHelper.PlayDebug(ActiveMusic, mouseScrubbed);
                                MusicFileObjectEditor.firstPlayback = true;
                                BaseAudioFileObjectEditor.FreePlay = false;
                                break;
                        }

                        if (clipPaused) Helper.Source.Pause();
                    }
                    else
                    {
                        Helper.Source.Stop();
                        if (!mouseScrubbed)
                        {
                            // Note: For some reason, reading from helper.Source.time returns 0 even if timeSamples is not 0
                            // However, writing a value to helper.Source.time changes timeSamples to the appropriate value just fine
                            Helper.Source.time = 0;
                        }
                        clipPaused = false;
                    }
                }
                if (ClipPlaying) JSAMEditorHelper.EndBackgroundColourChange();

                // Pause button
                GUIContent theText = clipPaused ? PauseIcons[1] : PauseIcons[0];
                if (clipPaused) JSAMEditorHelper.BeginBackgroundColourChange(buttonPressedColor);
                if (GUILayout.Button(theText, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    clipPaused = !clipPaused;
                    if (clipPaused)
                    {
                        Helper.Source.Pause();
                    }
                    else
                    {
                        Helper.Source.UnPause();
                    }
                }
                if (clipPaused) JSAMEditorHelper.EndBackgroundColourChange();

                // Loop button
                buttonIcon = LoopClip ? LoopIcons[1] : LoopIcons[0];
                if (LoopClip) JSAMEditorHelper.BeginBackgroundColourChange(buttonPressedColor);
                if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                {
                    LoopClip = !LoopClip;
                    helper.Source.loop = LoopClip;
                }
                if (LoopClip) JSAMEditorHelper.EndBackgroundColourChange();

                var randomButton = new GUIContent("R", "Play a random track from your Sound File Object's file library. Only usable for Sound Files with more than 2 AudioClips.");
                if (activeAssetType == SelectedAssetType.Sound)
                {
                    var selectedSound = ActiveSound;
                    using (new EditorGUI.DisabledScope(selectedSound.Files.Count == 0))
                    {
                        if (GUILayout.Button(randomButton))
                        {
                            var instance = BaseAudioFileObjectEditor.Instance as SoundFileObjectEditor;
                            if (instance != null)
                            {
                                Helper.Clip = instance.DesignateRandomAudioClip();
                                DoForceRepaint(true);
                                Helper.Source.Stop();
                                EditorFader.StartFading(Helper.Clip, selectedSound);
                            }
                        }
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true)) GUILayout.Button(randomButton);
                }

                var libraryButton = new GUIContent("Toggle Library", "Show/Hide the Sound File List");
                if (GUILayout.Button(libraryButton))
                {
                    showLibraryView = !showLibraryView;
                    var r = window.position;
                    if (showLibraryView) r.xMax += 150;
                    else r.xMax -= 150;

                    window.position = r;
                }

                if (GUILayout.Button(LockIcons[Convert.ToInt32(LockedAsset)], new GUILayoutOption[] { GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true), GUILayout.MaxHeight(19) }))
                {
                    if (LockedAsset) LockedAsset = null;
                    else LockedAsset = activeAsset;
                }

                int loopPointInputMode = 0;
                // Reset loop point input mode if not using loop points so the duration shows up as time by default
                if (activeAssetType == SelectedAssetType.Music)
                {
                    if (ActiveMusic.loopMode != LoopMode.LoopWithLoopPoints) loopPointInputMode = 0;
                }

                GUIContent blontent = new GUIContent();
                if (HelperSourceActive)
                {
                    switch ((BaseAudioFileObjectEditor.LoopPointTool)loopPointInputMode)
                    {
                        case BaseAudioFileObjectEditor.LoopPointTool.Slider:
                        case BaseAudioFileObjectEditor.LoopPointTool.TimeInput:
                            blontent = new GUIContent(
                                ((float)Helper.Source.timeSamples / Helper.Clip.frequency).TimeToString() + 
                                " / " + 
                                Helper.Clip.length.TimeToString(),
                                "The playback time in seconds");
                            break;
                        case BaseAudioFileObjectEditor.LoopPointTool.TimeSamplesInput:
                            blontent = new GUIContent(Helper.Source.timeSamples + " / " + Helper.Clip.samples, "The playback time in samples");
                            break;
                        case BaseAudioFileObjectEditor.LoopPointTool.BPMInput:
                            var m = ActiveMusic;
                            blontent = new GUIContent(string.Format("{0:0}", Helper.Source.time / (60f / m.bpm)) + " / " + Helper.Clip.length / (60f / m.bpm),
                                "The playback time in beats");
                            break;
                    }
                }
                else
                {
                    blontent = new GUIContent("00:00:000 / 00:00:000");
                }

                EditorGUILayout.LabelField(blontent, JSAMStyles.TimeStyle);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        void PollMouseEvents(Rect progressRect)
        {
            Event evt = Event.current;

            if (evt.isMouse)
            {
                switch (evt.type)
                {
                    case EventType.MouseUp:
                        switch (evt.button)
                        {
                            case 0:
                                mouseDragging = false;
                                break;
                            case 2:
                                //mouseGrabbed = false;
                                break;
                        }
                        break;
                    case EventType.MouseDown:
                    case EventType.MouseDrag:
                        if (!HelperSourceActive) return;
                        if (evt.button == 0) // Left Mouse Events
                        {
                            if (evt.type == EventType.MouseDown)
                            {
                                // Only begin dragging if mouse is in the waveform window
                                if (evt.mousePosition.y > progressRect.yMin && evt.mousePosition.y < progressRect.yMax)
                                {
                                    mouseDragging = true;
                                    mouseScrubbed = true;
                                }
                                else mouseDragging = false;
                            }
                            if (!mouseDragging) break;
                            float newProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, evt.mousePosition.x);
                            Helper.Source.time = Mathf.Clamp((newProgress * Helper.Clip.length), 0, Helper.Clip.length - AudioManagerInternal.EPSILON);

                            if (activeAssetType == SelectedAssetType.Music)
                            {
                                var m = ActiveMusic;
                                if (m.loopMode == LoopMode.ClampedLoopPoints)
                                {
                                    float start = m.loopStart * m.Files[0].frequency;
                                    float end = m.loopEnd * m.Files[0].frequency;
                                    Helper.Source.timeSamples = (int)Mathf.Clamp(Helper.Source.timeSamples, start, end - AudioManagerInternal.EPSILON);
                                }
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Conveniently draws a progress bar
        /// Referenced from the official Unity documentation
        /// https://docs.unity3d.com/ScriptReference/Editor.html
        /// </summary>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public Rect ProgressBar(float value)
        {
            //float maxHeight = (showHowTo) ? playbackPreviewClamped : 4000;
            //float minHeight = (showHowTo) ? playbackPreviewClamped : 64;
            Rect rect = GUILayoutUtility.GetRect(64, 4000, 64, 4000);

            if (Helper.Clip)
            {
                if (cachedTex == null)
                {
                    switch (activeAssetType)
                    {
                        case SelectedAssetType.AudioClip:
                            cachedTex = RenderStaticPreview(Helper.Clip, rect, 1);
                            break;
                        case SelectedAssetType.Sound:
                            cachedTex = RenderStaticPreview(Helper.Clip, rect, ActiveSound.relativeVolume);
                            break;
                        case SelectedAssetType.Music:
                            cachedTex = RenderStaticPreview(Helper.Clip, rect, ActiveMusic.relativeVolume);
                            break;
                    }
                }

                if (cachedTex != null)
                {
                    GUI.DrawTexture(rect, cachedTex);
                }
            }
            else
            {
                GUIStyle style = new GUIStyle(GUI.skin.box.ApplyTextAnchor(TextAnchor.MiddleCenter)
                                .SetFontSize(30)
                                .SetTextColor(Color.white)
                                .ApplyBoldText());
                GUI.Box(rect, "Select an Audio File to preview it here", style);
            }
            
            forceRepaint = false;

            switch (activeAssetType)
            {
                case SelectedAssetType.Sound:
                    SoundFileObjectEditor.DrawPropertyOverlay(ActiveSound, (int)rect.width, (int)rect.height);
                    break;
                case SelectedAssetType.Music:
                    MusicFileObjectEditor.DrawPropertyOverlay(ActiveMusic, (int)rect.width, (int)rect.height);
                    break;
            }

            Rect progressRect = new Rect(rect);
            progressRect.width = value * rect.width;
            progressRect.xMin = progressRect.xMax - 1;
            GUI.Box(progressRect, "", "SelectionRect");

            EditorGUILayout.Space();

            return rect;
        }

        private void Update()
        {
            if (!Helper.Clip) return;

            if (!Helper.Source.isPlaying && mouseDragging || resized)
            {
                DoForceRepaint(resized);
            }

            #region Sound Update
            switch (activeAssetType)
            {
                case SelectedAssetType.AudioClip:
                case SelectedAssetType.Sound:
                    if ((ClipPlaying && !clipPaused) || (mouseDragging && ClipPlaying))
                    {
                        float clipPos = Helper.Source.timeSamples / (float)Helper.Clip.frequency;

                        Repaint();

                        if (!ClipPlaying && !clipPaused)
                        {
                            Helper.Source.time = 0;
                            if (LoopClip)
                            {
                                Helper.Source.Play();
                            }
                        }
                    }
                    break;
            #endregion
            #region Music Update
                case SelectedAssetType.Music:
                    var selectedMusic = ActiveMusic;
                    if ((ClipPlaying && !clipPaused) || (mouseDragging && ClipPlaying))
                    {
                        float clipPos = Helper.Source.timeSamples / (float)Helper.Clip.frequency;

                        Helper.Source.volume = selectedMusic.relativeVolume;
                        Helper.Source.pitch = selectedMusic.startingPitch;

                        Repaint();

                        if (LoopClip)
                        {
                            if (selectedMusic.loopMode == LoopMode.LoopWithLoopPoints || selectedMusic.loopMode == LoopMode.ClampedLoopPoints)
                            {
                                if (!Helper.Source.isPlaying && !clipPaused)
                                {
                                    if (MusicFileObjectEditor.freePlay)
                                    {
                                        Helper.Source.Play();
                                    }
                                    else
                                    {
                                        Helper.Source.Play();
                                        Helper.Source.timeSamples = Mathf.CeilToInt(selectedMusic.loopStart * Helper.Clip.frequency);
                                    }
                                    MusicFileObjectEditor.freePlay = false;
                                }
                                else if (selectedMusic.loopMode == LoopMode.ClampedLoopPoints || !MusicFileObjectEditor.firstPlayback)
                                {
                                    if (clipPos < selectedMusic.loopStart || clipPos > selectedMusic.loopEnd)
                                    {
                                        // CeilToInt to guarantee clip position stays within loop bounds
                                        Helper.Source.timeSamples = Mathf.CeilToInt(selectedMusic.loopStart * Helper.Clip.frequency);
                                        MusicFileObjectEditor.firstPlayback = false;
                                    }
                                }
                                else if (clipPos >= selectedMusic.loopEnd)
                                {
                                    Helper.Source.timeSamples = Mathf.CeilToInt(selectedMusic.loopStart * Helper.Clip.frequency);
                                    MusicFileObjectEditor.firstPlayback = false;
                                }
                            }
                        }
                        else if (!LoopClip)
                        {
                            if (selectedMusic.loopMode == LoopMode.LoopWithLoopPoints)
                            {
                                if ((!Helper.Source.isPlaying && !clipPaused) || clipPos > selectedMusic.loopEnd)
                                {
                                    Helper.Source.Stop();
                                }
                            }
                            else if (selectedMusic.loopMode == LoopMode.ClampedLoopPoints && clipPos < selectedMusic.loopStart)
                            {
                                Helper.Source.timeSamples = Mathf.CeilToInt(selectedMusic.loopStart * Helper.Clip.frequency);
                            }
                        }
                    }

                    if (selectedMusic.loopMode != LoopMode.LoopWithLoopPoints)
                    {
                        if (!Helper.Source.isPlaying && !clipPaused && ClipPlaying)
                        {
                            Helper.Source.time = 0;
                            if (LoopClip)
                            {
                                Helper.Source.Play();
                            }
                        }
                    }
                    break;
            }
            #endregion
        }

        #region Unity's Asset Preview render code
        ///static Material m_HandleLinesMaterial;
        /// <summary>
        /// Borrowed from Unity
        /// <para>https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/AudioClipInspector.cs</para>
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        //public static Texture2D RenderUnityStaticPreview(AudioClip clip, Rect rect, float relativeVolume)
        //{
        //    AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
        //    AudioImporter audioImporter = importer as AudioImporter;
        //
        //    if (audioImporter == null || !ShaderUtil.hardwareSupportsRectRenderTexture)
        //        return null;
        //
        //    if (m_PreviewUtility == null)
        //        m_PreviewUtility = new PreviewRenderUtility();
        //
        //    m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, rect.width * 0.5f, rect.height * 0.5f));
        //    //m_HandleLinesMaterial.SetPass(0);
        //
        //    // We're drawing into an offscreen here which will have a resolution defined by EditorGUIUtility.pixelsPerPoint. This is different from the DoRenderPreview call below where we draw directly to the screen, so we need to take
        //    // the higher resolution into account when drawing into the offscreen, otherwise only the upper-left quarter of the preview texture will be drawn.
        //    //DoRenderPreview(clip, audioImporter, new Rect(0, 0, rect.width, rect.height), relativeVolume);
        //    //var newRect = new Rect(0, 0, rect.width * 3, rect.height * 3);
        //    //var newRect = new Rect(
        //    //    0.05f * rect.width * EditorGUIUtility.pixelsPerPoint,
        //    //    0.05f * rect.width * EditorGUIUtility.pixelsPerPoint,
        //    //    1.9f * rect.width * EditorGUIUtility.pixelsPerPoint,
        //    //    1.9f * rect.height * EditorGUIUtility.pixelsPerPoint);
        //    DoRenderPreview(clip, audioImporter, rect, 0.5f);
        //    return m_PreviewUtility.EndStaticPreview();
        //}
        //
        //// Passing in clip and importer separately as we're not completely done with the asset setup at the time we're asked to generate the preview.
        //private static void DoRenderPreview(AudioClip clip, AudioImporter audioImporter, Rect wantedRect, float scaleFactor)
        //{
        //    scaleFactor *= 0.95f; // Reduce amplitude slightly to make highly compressed signals fit.
        //    float[] minMaxData = (audioImporter == null) ? null : AudioUtil.GetMinMaxData(audioImporter);
        //    int numChannels = clip.channels;
        //    int numSamples = (minMaxData == null) ? 0 : (minMaxData.Length / (2 * numChannels));
        //    float h = (float)wantedRect.height / (float)numChannels;
        //    for (int channel = 0; channel < numChannels; channel++)
        //    {
        //        Rect channelRect = new Rect(wantedRect.x, wantedRect.y + h * channel, wantedRect.width, h);
        //        Color curveColor = new Color(1.0f, 140.0f / 255.0f, 0.0f, 1.0f);
        //
        //        AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator dlg = delegate (float x, out Color col, out float minValue, out float maxValue)
        //        {
        //            col = curveColor;
        //            if (numSamples <= 0)
        //            {
        //                minValue = 0.0f;
        //                maxValue = 0.0f;
        //            }
        //            else
        //            {
        //                float p = Mathf.Clamp(x * (numSamples - 2), 0.0f, numSamples - 2);
        //                int i = (int)Mathf.Floor(p);
        //                int offset1 = (i * numChannels + channel) * 2;
        //                int offset2 = offset1 + numChannels * 2;
        //                minValue = Mathf.Min(minMaxData[offset1 + 1], minMaxData[offset2 + 1]) * scaleFactor;
        //                maxValue = Mathf.Max(minMaxData[offset1 + 0], minMaxData[offset2 + 0]) * scaleFactor;
        //                if (minValue > maxValue) { float tmp = minValue; minValue = maxValue; maxValue = tmp; }
        //            }
        //        };
        //
        //        AudioCurveRendering.DrawMinMaxFilledCurve(wantedRect, dlg);
        //    }
        //}
        #endregion

        public static Texture2D RenderStaticPreview(AudioClip clip, Rect rect, float relativeVolume)
        {
            return RenderStaticPreview(clip, rect, relativeVolume, new Color(1.0f, 140.0f / 255.0f, 0.0f, 1.0f), Color.clear);
        }

        public static Texture2D RenderStaticPreview(AudioClip clip, Rect rect, float relativeVolume, Color mainColor, Color clearColor)
        {
            if (Event.current.type != EventType.Repaint) return null;
            float[] samples = new float[0];
            float[] waveform = new float[0];
            int halfHeight = 0;
            if (clip)
            {
                //GUI.Box(rect, "");
                samples = new float[clip.samples];
                waveform = new float[(int)rect.width];
                clip.GetData(samples, 0);
                int packSize = clip.samples / (int)rect.width + 1;
                int s = 0;

                for (int i = 0; i < clip.samples; i += packSize)
                {
                    waveform[s] = samples[i];
                    s++;
                }

                halfHeight = (int)rect.height / 2;
            }
            
            Texture2D tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            for (int x = 0; x < rect.width; x++)
            {
                for (int y = 0; y < rect.height; y++)
                {
                    tex.SetPixel(x, y, clearColor);
                }
            }

            if (clip)
            {
                for (int x = 0; x < waveform.Length; x++)
                {
                    // Scale the wave vertically relative to half the rect height and the relative volume
                    float heightLimit = waveform[x] * halfHeight;

                    for (int y = (int)heightLimit; y >= 0; y--)
                    {
                        //Color currentPixelColour = tex.GetPixel(x, halfHeight + y);
                        //if (currentPixelColour == Color.black) continue;

                        tex.SetPixel(x, halfHeight + y, mainColor);

                        // Get data from upper half offset by 1 unit due to int truncation
                        tex.SetPixel(x, halfHeight - (y + 1), mainColor);
                    }
                }
            }
           
            tex.Apply();
            return tex;
        }

        // Why does Unity keep all this stuff secret?
        // https://unitylist.com/p/5c3/Unity-editor-icons
        static GUIContent backIcon;
        static GUIContent BackIcon
        {
            get
            {
                if (backIcon == null)
                {
                    backIcon = EditorGUIUtility.TrIconContent("beginButton", "Click to Reset Playback Position");
                }
                return backIcon;
            }
        }
        static GUIContent[] playIcons;
        static GUIContent[] PlayIcons
        {
            get
            {
                if (playIcons == null)
                {
                    playIcons = new GUIContent[2];
#if UNITY_2019_4_OR_NEWER
                    playIcons[0] = EditorGUIUtility.TrIconContent("d_PlayButton", "Click to Play");
                    playIcons[1] = EditorGUIUtility.TrIconContent("d_PlayButton On", "Click to Stop");
#else
                    playIcons[0] = EditorGUIUtility.TrIconContent("preAudioPlayOff", "Click to Play");
                    playIcons[1] = EditorGUIUtility.TrIconContent("preAudioPlayOn", "Click to Stop");
#endif
                }
                return playIcons;
            }
        }
        static GUIContent[] pauseIcons;
        static GUIContent[] PauseIcons
        {
            get
            {
                if (pauseIcons == null)
                {
                    pauseIcons = new GUIContent[2];
                    pauseIcons[0] = EditorGUIUtility.TrIconContent("PauseButton", "Click to Pause");
                    pauseIcons[1] = EditorGUIUtility.TrIconContent("PauseButton On", "Click to Unpause");
                }
                return pauseIcons;
            }
        }
        static GUIContent[] loopIcons;
        static GUIContent[] LoopIcons
        {
            get
            {
                if (loopIcons == null)
                {
                    loopIcons = new GUIContent[2];
#if UNITY_2019_4_OR_NEWER
                    LoopIcons[0] = EditorGUIUtility.TrIconContent("d_preAudioLoopOff", "Click to enable looping");
                    LoopIcons[1] = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Click to disable looping");
#else
                    loopIcons[0] = EditorGUIUtility.TrIconContent("playLoopOff", "Click to enable looping");
                    loopIcons[1] = EditorGUIUtility.TrIconContent("playLoopOn", "Click to disable looping");
#endif
                }
                return loopIcons;
            }
        }
        static GUIContent[] lockIcons;
        static GUIContent[] LockIcons
        {
            get
            {
                if (lockIcons == null)
                {
                    lockIcons = new GUIContent[2];
                    lockIcons[0] = EditorGUIUtility.TrIconContent("IN LockButton", "Toggles the changing of audio when selecting inspector objects");
                    lockIcons[1] = EditorGUIUtility.TrIconContent("IN LockButton on", "Toggles the changing of audio when selecting inspector objects");
                }
                return lockIcons;
            }
        }
        static GUIContent openIcon;
        static GUIContent OpenIcon
        {
            get
            {
                if (openIcon == null)
                {
                    openIcon = EditorGUIUtility.TrIconContent("d_ScaleTool", "Click to open Playback Preview in a standalone window");
                }
                return openIcon;
            }
        }

        public static void DoForceRepaint(bool fullRepaint = false)
        {
            forceRepaint = fullRepaint;
            if (forceRepaint) cachedTex = null;
            if (WindowOpen)
            {
                resized = false;
                Window.Repaint();
            }
        }
    }
}