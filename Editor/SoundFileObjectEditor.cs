using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(SoundFileObject))]
    [CanEditMultipleObjects]
    public class SoundFileObjectEditor : BaseAudioFileObjectEditor
    {
        Color buttonPressedColor = new Color(0.475f, 0.475f, 0.475f);

        AudioClip playingClip;

        bool playingRandom;

        protected override string SHOW_LIBRARY => "JSAM_SFO_SHOWLIBRARY";
        protected override string EXPAND_LIBRARY => "JSAM_SFO_EXPANDLIBRARY";

        protected override string SHOW_FADETOOL => "JSAM_SFO_SHOWFADETOOL";

        SerializedProperty neverRepeat;

        new protected void OnEnable()
        {
            base.OnEnable();

            Undo.undoRedoPerformed += OnUndoRedo;

            if (target.name.Length > 0) // Creating from right-click dialog throws error here because name is invalid when first selected
            {
                //safeName.stringValue = JSAMEditorHelper.ConvertToAlphanumeric(target.name);
            }
            DesignateSerializedProperties();

            openIcon = EditorGUIUtility.TrIconContent("d_ScaleTool", "Click to open Playback Preview in a standalone window");

            editorFader = new JSAMEditorFader(asset, helper);

#if !UNITY_2020_3_OR_NEWER
            list = new AudioClipList(serializedObject, files);
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (editorFader != null)
            {
                editorFader.Dispose();
                editorFader = null;
            }
        }

        protected override void DesignateSerializedProperties()
        {
            base.DesignateSerializedProperties();

            neverRepeat = FindProp("neverRepeat");

            fadeInOut = FindProp(nameof(fadeInOut));
            excludedProperties.Add(nameof(fadeInOut));

            fadeInDuration = FindProp("fadeInDuration");
            fadeOutDuration = FindProp("fadeOutDuration");
        }

        protected override void OnCreatePreset(string[] input)
        {
            presetDescription.stringValue = input[1];
            serializedObject.ApplyModifiedProperties();
            Preset newPreset = new Preset(asset as SoundFileObject);
#if UNITY_2020_OR_NEWER
            newPreset.excludedProperties = new string[] {
                "files", "UsingLibrary", "category"
            };
#endif
            if (!JSAMEditorHelper.GenerateFolderStructureAt(JSAMPaths.Instance.PresetsPath)) return;

            string path = System.IO.Path.Combine(JSAMPaths.Instance.PresetsPath, input[0] + ".preset");
            JSAMEditorHelper.CreateAssetSafe(newPreset, path);
        }

#if !UNITY_2019_3_OR_NEWER
        static bool filesFoldout;
#endif
        public override void OnInspectorGUI()
        {
            if (asset == null) return;

            serializedObject.UpdateIfRequiredOrScript();

            RenderPresetDescription();

            EditorGUILayout.Space();

            RenderGeneratePresetButton();

            EditorGUILayout.Space();

            RenderFileList();

            bool noFiles = files.arraySize == 0;

            if (!isPreset)
            {
                if (noFiles)
                {
                    EditorGUILayout.HelpBox("Add an audio file before running!", MessageType.Error);
                }
                else if (missingFiles > 0)
                {
                    EditorGUILayout.HelpBox(missingFiles + " AudioClip(s) are missing! " +
                        "This can lead to issues during runtime, such as with randomized playback", MessageType.Warning);
                }
            }
            
            EditorGUILayout.Space();

            blontent = new GUIContent("Never Repeat", "Sometimes, AudioManager will allow the same sound from the Audio " +
            "library to play twice in a row, enabling this option will ensure that this audio file never plays the same " +
            "sound until after it plays a different sound.");
            EditorGUILayout.PropertyField(neverRepeat, blontent);

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

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            if (playingClip == null)
            {
                RedesignateActiveAudioClip();
            }

            if (!isPreset) DrawPlaybackTool();

            DrawLoopPointTools(target as SoundFileObject);

            #region Fade Tools
            EditorGUILayout.PropertyField(fadeInOut);

            showFadeTool = EditorCompatability.SpecialFoldouts(showFadeTool, new GUIContent("Fade Tools", "Show/Hide the Audio Fade previewer"));
            using (new EditorGUI.DisabledScope(!fadeInOut.boolValue))
            {
                if (!noFiles)
                {
                    if (showFadeTool)
                    {
                        float duration = playingClip ? playingClip.length : 0;

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
            }

            #region Quick Reference Guide
            string[] howToText = new string[]
            {
                "Overview",
                "Audio File Objects are containers that hold your sound files to be read by Audio Manager.",
                "Tips",
                "Relative volume only helps to reduce how loud a sound is. To increase how loud an individual sound is, you'll have to " +
                    "edit it using a sound editor.",
            };

            showHowTo = JSAMEditorHelper.RenderQuickReferenceGuide(showHowTo, howToText);
            #endregion
        }

        protected override void DrawPlaybackTool()
        {
            blontent = new GUIContent("Audio Playback Preview",
                "Allows you to preview how your AudioFileObject will sound during runtime right here in the inspector. " +
                "Some effects, like spatialization, will not be available to preview");
            showPlaybackTool = EditorCompatability.SpecialFoldouts(showPlaybackTool, blontent);

            if (showPlaybackTool)
            {
                if (ProgressBar())
                {
                    EditorGUILayout.BeginHorizontal();
                    Color colorbackup = GUI.backgroundColor;
                    if (IsClipPlaying)
                    {
                        GUI.backgroundColor = buttonPressedColor;
                        blontent = new GUIContent("Stop", "Stop playback");
                    }
                    else
                    {
                        blontent = new GUIContent("Play", "Play a preview of the sound with it's current sound settings.");
                    }
                    if (GUILayout.Button(blontent))
                    {
                        if (IsClipPlaying)
                        {
                            helper.Source.Stop();
                            if (playingRandom)
                            {
                                playingRandom = false;
                            }
                        }
                        else
                        {
                            if (playingClip != null)
                            {
                                editorFader.StartFading(playingClip, asset as SoundFileObject);
                            }
                        }
                        helper.Source.time = 0;
                        AudioPlaybackToolEditor.DoForceRepaint(true);
                    }
                    GUI.backgroundColor = colorbackup;
                    using (new EditorGUI.DisabledScope(files.arraySize < 2))
                    {
                        if (GUILayout.Button(new GUIContent("Play Random", "Preview settings with a random track from your library. Only usable if this Audio File has \"Use Library\" enabled.")))
                        {
                            DesignateRandomAudioClip();
                            helper.Source.Stop();
                            editorFader.StartFading(playingClip, asset as SoundFileObject);
                        }
                    }

                    if (GUILayout.Button(openIcon, new GUILayoutOption[] { GUILayout.MaxHeight(19) }))
                    {
                        AudioPlaybackToolEditor.Init();
                    }
                    GUILayout.FlexibleSpace();
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
        /// Assigns an AudioClip to the playingClip variable. 
        /// Will fail if AudioClip as marked as missing
        /// </summary>
        public void RedesignateActiveAudioClip()
        {
            AudioClip theClip = null;
            if (files.arraySize != 0)
            {
                theClip = files.GetArrayElementAtIndex(0).objectReferenceValue as AudioClip;
            }
            if (theClip != null)
            {
                playingClip = theClip;
            }
        }

        public AudioClip DesignateRandomAudioClip()
        {
            AudioClip theClip = playingClip;
            if (files.arraySize > 1)
            {
                var nonNull = asset.Files.FindAll(e => e);
                theClip = nonNull[Random.Range(0, nonNull.Count)];
                AudioPlaybackToolEditor.DoForceRepaint(theClip != playingClip);
                playingClip = theClip;
            }
            playingRandom = true;
            return playingClip;
        }

        protected override void Update()
        {
            if (asset == null) return; // This can happen on the same frame it's deleted
            if (asset.Files.Count == 0) return;
            AudioClip clip = asset.Files[0];
            if (playingClip != null)
            {
                if (!AudioPlaybackToolEditor.WindowOpen)
                {
                    if (clip != cachedClip)
                    {
                        cachedClip = asset.Files[0];
                        playingClip = cachedClip;
                    }

                    if (!IsClipPlaying && playingRandom)
                    {
                        RedesignateActiveAudioClip();
                        playingRandom = false;
                    }
                }

                if (IsClipPlaying)
                {
                    // This doesn't seem to do anything
                    //EditorApplication.QueuePlayerLoopUpdate();
                    Repaint();
                }
            }
        }

        void OnUndoRedo()
        {
            AudioPlaybackToolEditor.DoForceRepaint(true);
        }

        new public UndoPropertyModification[] PostProcessModifications(UndoPropertyModification[] modifications)
        {
            base.PostProcessModifications(modifications);
            if (helper.Source.isPlaying)
            {
                helper.SoundHelper.ApplyEffects();
            }
            return modifications;
        }

        GameObject helperObject;

        /// <summary>
        /// Conveniently draws a progress bar
        /// Referenced from the official Unity documentation
        /// https://docs.unity3d.com/ScriptReference/Editor.html
        /// </summary>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns>True if tools successfully rendered</returns>
        bool ProgressBar()
        {
            string label = GetInfoString();
            // Get a rect for the progress bar using the same margins as a TextField:
            Rect rect = GUILayoutUtility.GetRect(64, 64, "TextField");

            bool hide = false;
            GUIContent content = new GUIContent();

            if (files.arraySize == 0)
            {
                content.text = "Add some AudioClips above to preview them";
                hide = true;
            }

            if (hide)
            {
                GUI.Box(rect, content, GUI.skin.box.ApplyTextAnchor(TextAnchor.MiddleCenter).ApplyBoldText().SetFontSize(20).SetTextColor(GUI.skin.label.normal.textColor));
                return false;
            }

            AudioClip sound = playingClip;
            var helperSource = helper.Source;
            float value = 0;
            if (playingClip) value = helperSource.time / playingClip.length;

            if ((cachedTex == null || AudioPlaybackToolEditor.forceRepaint) && Event.current.type == EventType.Repaint)
            {
                Texture2D waveformTexture = AudioPlaybackToolEditor.RenderStaticPreview(sound, rect, asset.relativeVolume);
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

            return true;
        }

        public static void DrawPropertyOverlay(SoundFileObject sound, int width, int height)
        {
            if (Event.current.type != EventType.Repaint) return;

            if (sound.fadeInOut)
            {
                Rect newRect = new Rect();

                // Draw Loop Start
                newRect.height = height;
                newRect.xMax = sound.fadeInDuration * width;
                float firstLabel = newRect.xMax;

                JSAMEditorHelper.BeginColourChange(Color.magenta);
                GUI.Box(newRect, "", "SelectionRect");
                newRect.xMin = newRect.xMax - 48;
                newRect.x += 48;
                JSAMEditorHelper.EndColourChange();
                GUI.Label(newRect, new GUIContent("Fade In"), EditorStyles.label.ApplyTextAnchor(TextAnchor.UpperRight));
                newRect.xMax = newRect.xMin + 2;
                JSAMEditorHelper.BeginColourChange(Color.black);
                GUI.Box(newRect, "", "SelectionRect");
                JSAMEditorHelper.EndColourChange();

                // Draw Loop End
                newRect.height = height;
                newRect.xMin = (1 - sound.fadeOutDuration) * width;
                float secondLabel = newRect.xMin;
                newRect.xMax = width;
                JSAMEditorHelper.BeginColourChange(Color.magenta);
                GUI.Box(newRect, "", "SelectionRect");
                JSAMEditorHelper.EndColourChange();
                newRect.height = 35;
                if (newRect.width < 60)
                {
                    newRect.width = 100;
                    newRect.x -= 60;
                }
                var style = EditorStyles.label.ApplyTextAnchor(TextAnchor.UpperLeft);
                newRect.x += 5;

                if (secondLabel - firstLabel < 70)
                {
                    style = EditorStyles.label.ApplyTextAnchor(TextAnchor.LowerLeft);
                }
                GUI.Label(newRect, new GUIContent("Fade Out"), style);

                newRect.x = 0;
                newRect.height = height;
                newRect.xMin = (1 - sound.fadeOutDuration) * width;
                newRect.xMax = newRect.xMin + 2;
                JSAMEditorHelper.BeginColourChange(Color.black);
                GUI.Box(newRect, "", "SelectionRect");
                JSAMEditorHelper.EndColourChange();
            }
        }
    }
}