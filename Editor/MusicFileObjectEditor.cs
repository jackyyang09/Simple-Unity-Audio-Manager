using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using ATL.AudioData;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(MusicFileObject))]
    [CanEditMultipleObjects]
    public class MusicFileObjectEditor : BaseAudioFileObjectEditor<MusicFileObjectEditor>
    {
        public class MusicEditorFader : System.IDisposable
        {
            public MusicEditorFader()
            {
                EditorApplication.update += Update;
            }

            public void Dispose()
            {
                EditorApplication.update -= Update;
            }

            void Update()
            {

            }
        }

        MusicFileObject myScript;

        Color COLOR_BUTTONPRESSED = new Color(0.475f, 0.475f, 0.475f);

        bool clipPlaying = false;
        bool clipPaused = false;

        bool mouseDragging = false;
        bool loopClip = false;

        /// <summary>
        /// True so long as the inspector music player hasn't looped
        /// </summary>
        public static bool firstPlayback = true;
        public static bool freePlay = false;

        Texture2D cachedTex;
        AudioClip cachedClip;

        bool mouseScrubbed = false;

        new protected void OnEnable()
        {
            base.OnEnable();
            myScript = target as MusicFileObject;

            SetupIcons();
            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
            Undo.postprocessModifications += ApplyHelperEffects;

            DesignateSerializedProperties();

            AudioPlaybackToolEditor.CreateAudioHelper(files.arraySize > 0 ? asset.Files[0] : null);
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (!AudioPlaybackToolEditor.WindowOpen)
            {
                AudioPlaybackToolEditor.DestroyAudioHelper();
            }
            Undo.postprocessModifications -= ApplyHelperEffects;
        }

        protected override void DesignateSerializedProperties()
        {
            base.DesignateSerializedProperties();
        }

        protected override void OnCreatePreset(string[] input)
        {
            presetDescription.stringValue = input[1];
            serializedObject.ApplyModifiedProperties();
            Preset newPreset = new Preset(asset as MusicFileObject);
#if UNITY_2020_OR_NEWER
            newPreset.excludedProperties = new string[] {
                "useLibrary", "category"
            };
#endif
            string path = System.IO.Path.Combine(JSAMPaths.Instance.PresetsPath, input[0] + ".preset");
            JSAMEditorHelper.CreateAssetSafe(newPreset, path);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RenderPresetDescription();

            EditorGUILayout.Space();

            RenderGeneratePresetButton();

            GUIContent clipContent = new GUIContent("Music", "AudioClip associated with this Music File Object");
            if (files.arraySize == 0)
            {
                AudioClip clip = null;
                EditorGUI.BeginChangeCheck();
                clip = EditorGUILayout.ObjectField(clipContent, clip, typeof(AudioClip), false) as AudioClip;
                if (EditorGUI.EndChangeCheck())
                {
                    if (clip != null)
                    {
                        files.AddAndReturnNewArrayElement().objectReferenceValue = clip;
                    }
                }
            }
            else
            {
                EditorGUILayout.PropertyField(files.GetArrayElementAtIndex(0), clipContent);
            }
            EditorGUILayout.PropertyField(relativeVolume);

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            if (!isPreset) DrawPlaybackTool();

            DrawLoopPointTools(myScript);

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
                JSAMEditorHelper.RenderHelpbox("Overview");

                JSAMEditorHelper.RenderHelpbox("Audio File Music Objects are containers that hold your music.");
                JSAMEditorHelper.RenderHelpbox("These Audio File objects are to be stored in an Audio Library where it is " +
                    "then read and played through the Audio Manager");

                JSAMEditorHelper.RenderHelpbox("Tips");

                JSAMEditorHelper.RenderHelpbox("Relative volume only helps to reduce how loud a sound is. To increase how loud an individual sound is, you'll have to " +
                    "edit it using a sound editor.");
                JSAMEditorHelper.RenderHelpbox("Before you cut up your music into an intro and looping portion, try using the loop point tools!");
                JSAMEditorHelper.RenderHelpbox("By designating your loop points in the loop point tools and setting your music's loop mode to " +
                    "\"Loop with Loop Points\", you can easily get AudioManager to play your intro portion once and repeat the looping portion forever!");
                JSAMEditorHelper.RenderHelpbox("If your music is saved in the WAV format, you can use external programs to set loop points in the file itself! " +
                    "After that, click the \"Import Loop Points from .WAV Metadata\" button above to have AudioManager to read them in.");
                JSAMEditorHelper.RenderHelpbox("You can designate loop points in your .WAV file using programs like Wavosaur and Goldwave! Click the links " +
                    "below to learn more about how to get these free tools and create loop points with them!");

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
                JSAMEditorHelper.RenderHelpbox("Otherwise, using BPM input to set your loop points is strongly recommended!");
                JSAMEditorHelper.RenderHelpbox("You can also choose to export your loop point data by clicking the \"Save Loop Points to File\" button " +
                    "to use in other programs!");
            }
            EditorCompatability.EndSpecialFoldoutGroup();
#endregion
        }

        /// <summary>
        /// Draws a playback 
        /// </summary>
        /// <param name="music"></param>
        protected override void DrawPlaybackTool()
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
                                float newProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, evt.mousePosition.x);
                                AudioPlaybackToolEditor.helperSource.time = Mathf.Clamp((newProgress * music.length), 0, music.length - AudioManagerInternal.EPSILON);
                                if (myScript.loopMode == LoopMode.ClampedLoopPoints)
                                {
                                    float start = myScript.loopStart * myScript.Files[0].frequency;
                                    float end = myScript.loopEnd * myScript.Files[0].frequency;
                                    AudioPlaybackToolEditor.helperSource.timeSamples = (int)Mathf.Clamp(AudioPlaybackToolEditor.helperSource.timeSamples, start, end - AudioManagerInternal.EPSILON);
                                }
                                break;
                        }
                    }

                    if (GUILayout.Button(s_BackIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        if (myScript.loopMode == LoopMode.ClampedLoopPoints)
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
                    if (clipPlaying) GUI.backgroundColor = COLOR_BUTTONPRESSED;
                    if (GUILayout.Button(buttonIcon, new GUILayoutOption[] { GUILayout.MaxHeight(20) }))
                    {
                        clipPlaying = !clipPlaying;
                        if (clipPlaying)
                        {
                            // Note: For some reason, reading from AudioPlaybackToolEditor.helperSource.time returns 0 even if timeSamples is not 0
                            // However, writing a value to AudioPlaybackToolEditor.helperSource.time changes timeSamples to the appropriate value just fine
                            AudioPlaybackToolEditor.musicHelper.PlayDebug(myScript, mouseScrubbed);
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
                    buttonIcon = (loopClip) ? s_LoopIcons[1] : s_LoopIcons[0];
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
                    if (myScript.loopMode != LoopMode.LoopWithLoopPoints && myScript.loopMode != LoopMode.ClampedLoopPoints) loopPointInputMode = 0;

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

                if (EditorUtility.audioMasterMute)
                {
                    JSAMEditorHelper.RenderHelpbox("Audio is muted in the game view, which also mutes audio " +
                         "playback here. Please un-mute it to hear your audio.");
                }
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

        void Update()
        {
            MusicFileObject myScript = (MusicFileObject)target;
            if (myScript == null) return;
            if (asset.Files.Count == 0) return;
            AudioClip music = myScript.Files[0];
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
                        else if (clipPos >= myScript.loopEnd)
                        {
                            AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                            firstPlayback = false;
                        }
                    }
                    else if (myScript.loopMode == LoopMode.ClampedLoopPoints)
                    {
                        if (clipPos < myScript.loopStart || clipPos > myScript.loopEnd)
                        {
                            // CeilToInt to guarantee clip position stays within loop bounds
                            AudioPlaybackToolEditor.helperSource.timeSamples = Mathf.CeilToInt(myScript.loopStart * music.frequency);
                            firstPlayback = false;
                        }
                    }
                }
                else if (!loopClip)
                {
                    if (myScript.loopMode == LoopMode.LoopWithLoopPoints)
                    {
                        if ((!AudioPlaybackToolEditor.helperSource.isPlaying && !clipPaused) || clipPos > myScript.loopEnd)
                        {
                            clipPlaying = false;
                            AudioPlaybackToolEditor.helperSource.Stop();
                        }
                    }
                    else if (myScript.loopMode == LoopMode.ClampedLoopPoints && clipPos < myScript.loopStart)
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

        public UndoPropertyModification[] ApplyHelperEffects(UndoPropertyModification[] modifications)
        {
            if (AudioPlaybackToolEditor.helperSource.isPlaying)
            {
                AudioPlaybackToolEditor.musicHelper.ApplyEffects();
            }
            return modifications;
        }

        void OnUndoRedo()
        {
            AudioPlaybackToolEditor.DoForceRepaint(true);
        }

        public static void DrawPropertyOverlay(MusicFileObject music, int width, int height)
        {
            if (Event.current.type != EventType.Repaint) return;

#region Draw Loop Point Markers
            if (music.loopMode >= LoopMode.LoopWithLoopPoints)
            {
                Rect newRect = new Rect();

                // Draw Loop Start
                newRect.height = height;
                newRect.xMax = (music.loopStart / music.Files[0].length) * width;
                float firstLabel = newRect.xMax;

                GUI.Box(newRect, "");
                newRect.xMin = newRect.xMax - 65;
                newRect.x += 60;
                GUI.Label(newRect, new GUIContent("Loop Start"), EditorStyles.label.ApplyTextAnchor(TextAnchor.UpperRight));

                // Draw Loop End
                newRect.height = height;
                newRect.xMin = (music.loopEnd / music.Files[0].length) * width;
                float secondLabel = newRect.xMin;
                newRect.xMax = width;
                GUI.Box(newRect, "");

                var style = EditorStyles.label.ApplyTextAnchor(TextAnchor.UpperLeft);
                newRect.height = 35;
                if (newRect.width < 60)
                {
                    newRect.width = 100;
                    newRect.x -= 55;
                    if (secondLabel - firstLabel < 140)
                    {
                        style = EditorStyles.label.ApplyTextAnchor(TextAnchor.LowerLeft);
                    }
                }
                else if (secondLabel - firstLabel < 70)
                {
                    style = EditorStyles.label.ApplyTextAnchor(TextAnchor.LowerLeft);
                }

                GUI.Label(newRect, new GUIContent("Loop End"), style);

                newRect.x = 0;
                newRect.height = height;
                newRect.xMin = (music.loopStart / music.Files[0].length) * width;
                newRect.xMax = (music.loopEnd / music.Files[0].length) * width;
                JSAMEditorHelper.BeginColourChange(Color.green);
                GUI.Box(newRect, "", "SelectionRect");
                JSAMEditorHelper.EndColourChange();
            }
#endregion
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