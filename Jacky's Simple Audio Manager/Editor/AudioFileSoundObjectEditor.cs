using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioFileSoundObject))]
    [CanEditMultipleObjects]
    public class AudioFileSoundObjectEditor : BaseAudioFileObjectEditor<AudioFileSoundObjectEditor>
    {
        Color buttonPressedColor = new Color(0.475f, 0.475f, 0.475f);

        AudioClip playingClip;

        bool clipPlaying;
        bool playingRandom;

        Texture2D cachedTex;
        AudioClip cachedClip;

        GUIContent openIcon;

        static bool showFadeTool;

        SerializedProperty neverRepeat;
        SerializedProperty fadeInDuration;
        SerializedProperty fadeOutDuration;

        new protected void OnEnable()
        {
            base.OnEnable();

            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
            Undo.postprocessModifications += ApplyHelperEffects;

            if (target.name.Length > 0) // Creating from right-click dialog throws error here because name is invalid when first selected
            {
                //safeName.stringValue = JSAMEditorHelper.ConvertToAlphanumeric(target.name);
            }

            neverRepeat = serializedObject.FindProperty("neverRepeat");

            fadeInDuration = serializedObject.FindProperty("fadeInDuration");
            fadeOutDuration = serializedObject.FindProperty("fadeOutDuration");

            bypassEffects = serializedObject.FindProperty("bypassEffects");
            bypassListenerEffects = serializedObject.FindProperty("bypassListenerEffects");
            bypassReverbZones = serializedObject.FindProperty("bypassReverbZones");

            openIcon = EditorGUIUtility.TrIconContent("d_ScaleTool", "Click to open Playback Preview in a standalone window");

            AudioPlaybackToolEditor.CreateAudioHelper(asset.GetFirstAvailableFile());
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            AudioPlaybackToolEditor.DestroyAudioHelper();
            Undo.postprocessModifications -= ApplyHelperEffects;
        }

#if !UNITY_2019_3_OR_NEWER
        static bool filesFoldout;
#endif
        public override void OnInspectorGUI()
        {
            if (asset == null) return;

            serializedObject.Update();

#region Category Inspector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(category);
            EditorGUILayout.EndHorizontal();
#endregion

            List<string> excludedProperties = new List<string>() { "m_Script", "file", "files", "safeName",
                "relativeVolume", "spatialize", "maxDistance" };

            if (asset.UsingLibrary()) // Swap file with files
            {
#if UNITY_2019_3_OR_NEWER
                EditorGUILayout.PropertyField(files);
#else           // Property field on an array doesn't seem to work before 2019.3, so we have to make it ourselves
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attach audio files here to use", EditorStyles.boldLabel);
                filesFoldout = EditorGUILayout.Foldout(filesFoldout, new GUIContent("Files"), true);
                if (filesFoldout)
                {
                    EditorGUI.indentLevel++;
                    files.arraySize = EditorGUILayout.IntField("Size", files.arraySize);
                    for (int i = 0; i < files.arraySize; i++)
                    {
                        EditorGUILayout.PropertyField(files.GetArrayElementAtIndex(i));
                    }
                    EditorGUI.indentLevel--;
                }
#endif
            }
            else
            {
                if (file != null)
                {
                    EditorGUILayout.PropertyField(file);
                }
            }

            GUIContent blontent = new GUIContent("Use Library", "If true, the single AudioFile will be changed to a list of AudioFiles. AudioManager will choose a random AudioClip from this list when you play this sound");
            bool oldValue = asset.useLibrary;
            bool newValue = EditorGUILayout.Toggle(blontent, oldValue);
            if (newValue != oldValue) // If you clicked the toggle
            {
                if (newValue)
                {
                    if (asset.files.Count == 0)
                    {
                        if (asset.file != null)
                        {
                            asset.files.Add(asset.file);
                        }
                    }
                    else if (asset.files.Count == 1)
                    {
                        if (asset.files[0] == null)
                        {
                            asset.files[0] = asset.file;
                        }
                    }
                }
                else
                {
                    if (asset.files.Count > 0 && asset.file == null)
                    {
                        asset.file = asset.files[0];
                    }
                }
                asset.useLibrary = newValue;
            }

            if (asset.useLibrary)
            {
                blontent = new GUIContent("Never Repeat", "Sometimes, AudioManager will allow the same sound from the Audio " +
                "library to play twice in a row, enabling this option will ensure that this audio file never plays the same " +
                "sound until after it plays a different sound.");
                EditorGUILayout.PropertyField(neverRepeat, blontent);
            }

            bool noFiles = asset.GetFile() == null && asset.IsLibraryEmpty();

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
            if (asset.name.Contains("NEW AUDIO FILE") || asset.name.Equals("None") || asset.name.Equals("GameObject"))
            {
                EditorGUILayout.HelpBox("Warning! Change the name of the gameObject to something different or things will break!", MessageType.Warning);
            }

            if (playingClip == null)
            {
                DesignateActiveAudioClip(asset);
            }
            if (!noFiles && !AudioPlaybackToolEditor.WindowOpen) DrawPlaybackTool();

#region Fade Tools
            using (new EditorGUI.DisabledScope(asset.fadeMode == FadeMode.None))
            {
                if (!asset.IsLibraryEmpty())
                {
                    showFadeTool = EditorCompatability.SpecialFoldouts(showFadeTool, new GUIContent("Fade Tools", "Show/Hide the Audio Fade previewer"));
                    if (showFadeTool && asset.fadeMode != FadeMode.None)
                    {
                        GUIContent fContent = new GUIContent();
                        GUIStyle rightJustified = new GUIStyle(EditorStyles.label);
                        rightJustified.alignment = TextAnchor.UpperRight;
                        rightJustified.padding = new RectOffset(0, 15, 0, 0);
                        switch (asset.fadeMode)
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
                    EditorCompatability.EndSpecialFoldoutGroup();
                }
            }
#endregion

            if (!noFiles) DrawAudioEffectTools();

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
                if (asset.maxDistance < 0)
                {
                    asset.maxDistance = 0;
                    Undo.RecordObject(asset, "Fixed negative distance");
                }
            }

#region Quick Reference Guide
            showHowTo = EditorCompatability.SpecialFoldouts(showHowTo, "Quick Reference Guide");
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
            EditorCompatability.EndSpecialFoldoutGroup();
#endregion
        }

        void DrawPlaybackTool()
        {
            GUIContent fContent = new GUIContent("Audio Playback Preview", 
                "Allows you to preview how your AudioFileObject will sound during runtime right here in the inspector. " +
                "Some effects, like spatialization and delay, will not be available to preview");
            showPlaybackTool = EditorCompatability.SpecialFoldouts(showPlaybackTool, fContent);

            if (showPlaybackTool)
            {
                var helperSource = AudioPlaybackToolEditor.helperSource;
                ProgressBar(AudioPlaybackToolEditor.helperSource.time / playingClip.length, GetInfoString());

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
                    AudioPlaybackToolEditor.helperSource.Stop();
                    if (playingClip != null && !clipPlaying)
                    {
                        if (playingRandom)
                        {
                            AudioPlaybackToolEditor.DoForceRepaint(true);
                            playingRandom = false;
                        }
                        StartFading();
                    }
                    else
                    {
                        clipPlaying = false;
                    }
                }
                GUI.backgroundColor = colorbackup;
                using (new EditorGUI.DisabledScope(asset.GetFileCount() < 2))
                {
                    if (GUILayout.Button(new GUIContent("Play Random", "Preview settings with a random track from your library. Only usable if this Audio File has \"Use Library\" enabled.")))
                    {
                        DesignateRandomAudioClip();
                        helperSource.Stop();
                        StartFading();
                    }
                }

                if (GUILayout.Button(openIcon, new GUILayoutOption[] { GUILayout.MaxHeight(19) }))
                {
                    AudioPlaybackToolEditor.Init();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            EditorCompatability.EndSpecialFoldoutGroup();
        }

        public void DesignateActiveAudioClip(BaseAudioFileObject asset)
        {
            AudioClip theClip = null;
            if (!asset.IsLibraryEmpty())
            {
                theClip = asset.GetFirstAvailableFile();
            }
            if (theClip != null)
            {
                playingClip = theClip;
            }
        }

        public AudioClip DesignateRandomAudioClip()
        {
            AudioClip theClip = playingClip;
            if (!asset.IsLibraryEmpty())
            {
                List<AudioClip> files = asset.GetFiles();
                while (theClip == null || theClip == playingClip)
                {
                    theClip = files[Random.Range(0, files.Count)];
                }
            }
            playingClip = theClip;
            playingRandom = true;
            AudioPlaybackToolEditor.DoForceRepaint(true);
            return playingClip;
        }

        void Update()
        {
            if (asset == null) return; // This can happen on the same frame it's deleted
            AudioClip clip = asset.GetFirstAvailableFile();
            if (playingClip != null && clip != null)
            {
                if (!AudioPlaybackToolEditor.WindowOpen)
                {
                    if (clip != cachedClip)
                    {
                        AudioPlaybackToolEditor.DoForceRepaint(true);
                        cachedClip = asset.GetFirstAvailableFile();
                        playingClip = cachedClip;
                    }

                    if (!clipPlaying && playingRandom)
                    {
                        DesignateActiveAudioClip(asset);
                    }
                }

                if (clipPlaying)
                {
                    Repaint();
                }

                if (asset.fadeMode != FadeMode.None)
                {
                    HandleFading(asset);
                }
                else
                {
                    AudioPlaybackToolEditor.helperSource.volume = asset.relativeVolume;
                }
            }
            clipPlaying = (playingClip != null && AudioPlaybackToolEditor.helperSource.isPlaying);
        }

        void OnUndoRedo()
        {
            AudioPlaybackToolEditor.DoForceRepaint(true);
        }

        public UndoPropertyModification[] ApplyHelperEffects(UndoPropertyModification[] modifications)
        {
            if (AudioPlaybackToolEditor.helperSource.isPlaying)
            {
                AudioPlaybackToolEditor.helperHelper.ApplyEffects();
            }
            return modifications;
        }

        FadeMode fadeMode;
        GameObject helperObject;
        float fadeInTime, fadeOutTime;

        public void HandleFading(BaseAudioFileObject asset)
        {
            var helperSource = AudioPlaybackToolEditor.helperSource;
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
                                helperSource.volume = asset.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, asset.relativeVolume, helperSource.time / fadeInTime);
                            }
                        }
                        else helperSource.volume = asset.relativeVolume;
                        break;
                    case FadeMode.FadeOut:
                        if (helperSource.time >= playingClip.length - fadeOutTime)
                        {
                            if (fadeOutTime == float.Epsilon)
                            {
                                helperSource.volume = asset.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, asset.relativeVolume, (playingClip.length - helperSource.time) / fadeOutTime);
                            }
                        }
                        break;
                    case FadeMode.FadeInAndOut:
                        if (helperSource.time < playingClip.length - fadeOutTime)
                        {
                            if (fadeInTime == float.Epsilon)
                            {
                                helperSource.volume = asset.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, asset.relativeVolume, helperSource.time / fadeInTime);
                            }
                        }
                        else
                        {
                            if (fadeOutTime == float.Epsilon)
                            {
                                helperSource.volume = asset.relativeVolume;
                            }
                            else
                            {
                                helperSource.volume = Mathf.Lerp(0, asset.relativeVolume, (playingClip.length - helperSource.time) / fadeOutTime);
                            }
                        }
                        break;
                }
            }
        }

        public void StartFading(AudioClip overrideClip = null)
        {
            fadeMode = asset.fadeMode;
            if (!overrideClip)
                AudioPlaybackToolEditor.helperSource.clip = playingClip;
            else
                AudioPlaybackToolEditor.helperSource.clip = overrideClip;
            fadeInTime = asset.fadeInDuration * AudioPlaybackToolEditor.helperSource.clip.length;
            fadeOutTime = asset.fadeOutDuration * AudioPlaybackToolEditor.helperSource.clip.length;
            // To prevent divisions by 0
            if (fadeInTime == 0) fadeInTime = float.Epsilon;
            if (fadeOutTime == 0) fadeOutTime = float.Epsilon;
            
            AudioPlaybackToolEditor.helperHelper.PlayDebug(asset, false);
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

            if (cachedTex == null || AudioPlaybackToolEditor.forceRepaint)
            {
                Texture2D waveformTexture = PaintWaveformSpectrum(sound, (int)rect.width, (int)rect.height, new Color(1, 0.5f, 0));
                cachedTex = waveformTexture;
                if (waveformTexture != null)
                    GUI.DrawTexture(rect, waveformTexture);
                AudioPlaybackToolEditor.forceRepaint = false;
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
            // Copy sample data to array
            audio.GetData(samples, 0);

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

            float fadeInDuration = asset.fadeInDuration;
            float fadeOutDuration = asset.fadeOutDuration;

            Color lightShade = new Color(0.3f, 0.3f, 0.3f);
            int halfHeight = height / 2;

            // The halved height limit of the wave at the left/right extremes
            float fadeStart = leftValue * halfHeight;
            float fadeEnd = rightValue * halfHeight;

            switch (asset.fadeMode)
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
                case FadeMode.FadeIn: // Paint the fade-in area
                    {
                        // Scope lol
                        {
                            // Scale our fadeIn value by the current zoom
                            float fadeInRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, fadeInDuration);
                            float fadeStartRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, 0);

                            // Get the length of the whole fade shape from the scaled fade duration relative to the rect width
                            float fadeWidth = width * fadeInRelative;
                            // Offset the lerp value by how much the left side bar is obscuring the start of the fade
                            int offset = Mathf.RoundToInt(width * Mathf.Abs(fadeStartRelative));

                            // Clamp the limit in case fadeWidth exceeds the right side of the bar
                            for (int x = 0; x + offset < Mathf.Clamp(fadeWidth, 0, width) + offset; x++)
                            {
                                // Paint amount of vertical black depending on progress
                                // Lerp from 0 to half height as those are the extremes we're working with
                                // amountToPaint is the amount of 
                                float lerpValue = (float)(x + offset) / (fadeWidth + offset);
                                int amountToPaint = (int)Mathf.Lerp(0, halfHeight, lerpValue);
                                for (int y = halfHeight; y >= 0; y--)
                                {
                                    switch (amountToPaint)
                                    {
                                        case 0:
                                            tex.SetPixel(x, y, Color.black);
                                            break;
                                        default:
                                            tex.SetPixel(x, y, lightShade);
                                            amountToPaint--;
                                            break;
                                    }
                                }
                                // Paint the same on the lower half
                                for (int y = halfHeight; y < height; y++)
                                {
                                    tex.SetPixel(x, halfHeight - y, tex.GetPixel(x, y - halfHeight));
                                }
                            }

                            for (int x = (int)Mathf.Clamp(fadeWidth, 0, width); x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    tex.SetPixel(x, y, lightShade);
                                }
                            }
                        }
                    }
                    break;
                case FadeMode.FadeOut:
                    {
                        float fadeStartRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, 1 - fadeOutDuration);
                        int fadeStartX = Mathf.RoundToInt(width * fadeStartRelative);

                        for (int x = 0; x < fadeStartX; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                tex.SetPixel(x, y, lightShade);
                            }
                        }

                        float fadeEndRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, 1);

                        float fadeWidth = width * (fadeEndRelative - fadeStartRelative);

                        for (int x = fadeStartX; x < width; x++)
                        {
                            float lerpValue = (float)(x - fadeStartX) / (fadeWidth);
                            int amountToPaint = (int)Mathf.Lerp(halfHeight, 0, lerpValue);
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
                case FadeMode.FadeInAndOut:
                    {
                        // Scale our fadeIn value by the current zoom
                        float fadeInRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, fadeInDuration);
                        float fadeStartRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, 0);

                        // Get the length of the whole fade shape from the scaled fade duration relative to the rect width
                        float fadeWidth = width * fadeInRelative;
                        // Offset the lerp value by how much the left side bar is obscuring the start of the fade
                        int offset = Mathf.RoundToInt(width * Mathf.Abs(fadeStartRelative));

                        for (int x = 0; x + offset < Mathf.Clamp(fadeWidth, 0, width) + offset; x++)
                        {
                            float lerpValue = (float)(x + offset) / (fadeWidth + offset);
                            int amountToPaint = (int)Mathf.Lerp(0, halfHeight, lerpValue);
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

                        fadeStartRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, 1 - fadeOutDuration);
                        float fadeStartX = width * fadeStartRelative;
                        // Paint the middle rectangle
                        for (int x = (int)fadeWidth; x < Mathf.Clamp(fadeStartX, 0, width); x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                tex.SetPixel(x, y, lightShade);
                            }
                        }

                        float fadeEndRelative = JSAMExtensions.InverseLerpUnclamped(leftValue, rightValue, 1);

                        fadeWidth = width * (fadeEndRelative - fadeStartRelative);
                        // Paint the right-side triangle
                        for (int x = (int)fadeStartX; x < width; x++)
                        {
                            float lerpValue = (float)(x - fadeStartX) / (fadeWidth);
                            int amountToPaint = (int)Mathf.Lerp(halfHeight, 0, lerpValue);
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
            
            for (int x = 0; x < Mathf.Clamp(rightSide, 0, width); x++)
            {
                // Scale the wave vertically relative to half the rect height and the relative volume
                float heightLimit = waveform[x] * halfHeight * asset.relativeVolume;

                for (int y = (int)heightLimit; y >= 0; y--)
                {
                    Color currentPixelColour = tex.GetPixel(x, halfHeight + y);
                    if (currentPixelColour == Color.black) continue;

                    tex.SetPixel(x, halfHeight + y, lightShade + col * 0.75f);

                    // Get data from upper half offset by 1 unit due to int truncation
                    tex.SetPixel(x, halfHeight - (y + 1), lightShade + col * 0.75f);
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

#region Audio Effect Rendering
        static bool showAudioEffects;
        static bool chorusFoldout;
        static bool distortionFoldout;
        static bool echoFoldout;
        static bool highPassFoldout;
        static bool lowPassFoldout;
        static bool reverbFoldout;

        void DrawAudioEffectTools()
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