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

        bool playingRandom;

        protected override string SHOW_LIBRARY => "JSAM_SFO_SHOWLIBRARY";
        protected override string EXPAND_LIBRARY => "JSAM_SFO_EXPANDLIBRARY";

        protected override string SHOW_FADETOOL => "JSAM_SFO_SHOWFADETOOL";

        SerializedProperty neverRepeat;

        protected override void PlayDebug(BaseAudioFileObject asset, bool dontReset)
        {
            helper.SoundHelper.PlayDebug(asset, dontReset);
        }

        protected override void AssignHelperToFader(AudioClip clip)
        {
            helper.SoundHelper.AudioSource.clip = clip;
            editorFader.AudioSource = helper.SoundHelper.AudioSource;
        }

        new protected void OnEnable()
        {
            base.OnEnable();

            if (target.name.Length > 0) // Creating from right-click dialog throws error here because name is invalid when first selected
            {
                //safeName.stringValue = JSAMEditorHelper.ConvertToAlphanumeric(target.name);
            }
            DesignateSerializedProperties();

            openIcon = EditorGUIUtility.TrIconContent("d_ScaleTool", "Click to open Playback Preview in a standalone window");

#if !UNITY_2020_3_OR_NEWER
            list = new AudioClipList(serializedObject, files);
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();

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
#if UNITY_2020_1_OR_NEWER
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

            if (activeClip == null)
            {
                RedesignateActiveAudioClip();
            }

            if (!isPreset) DrawPlaybackTool();

            DrawLoopPointTools(target as SoundFileObject);

            DrawFadeTools(activeClip);

            DrawAudioEffectTools();

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

        protected override void RenderAuxiliaryPlaybackControls()
        {
            using (new EditorGUI.DisabledScope(files.arraySize < 2))
            {
                if (GUILayout.Button(new GUIContent("R", "Preview settings with a random track from your library. Only usable if this Audio File has \"Use Library\" enabled.")))
                {
                    activeClip = GetRandomAudioClip(asset as SoundFileObject, activeClip);
                    helper.Clip = activeClip;
                    editorFader.StartFading(activeClip, asset);
                    PlayDebug(asset, mouseScrubbed);
                    clipPlaying = true;
                    playingRandom = true;
                }
            }
        }

        /// <summary>
        /// Assigns an AudioClip to the activeClip variable. 
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
                activeClip = theClip;
            }
        }

        public static AudioClip GetRandomAudioClip(SoundFileObject file, AudioClip currentClip)
        {
            AudioClip theClip = currentClip;
            if (file.Files.Count == 1) return currentClip;
            var nonNull = file.Files.FindAll(e => e);
            theClip = nonNull[Random.Range(0, nonNull.Count)];
            return theClip;
        }

        protected override void Update()
        {
            base.Update();

            if (activeClip != null)
            {
                if (!SourcePlaying && playingRandom)
                {
                    RedesignateActiveAudioClip();
                    playingRandom = false;
                }
            }
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

            AudioClip sound = activeClip;
            var helperSource = helper.Source;
            float value = 0;
            if (activeClip) value = helperSource.time / activeClip.length;

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

            if (activeClip != null)
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