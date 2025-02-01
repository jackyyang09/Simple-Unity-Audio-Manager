using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(MusicFileObject))]
    [CanEditMultipleObjects]
    public class MusicFileObjectEditor : BaseAudioFileObjectEditor
    {
        MusicFileObject myScript;

        Color COLOR_BUTTONPRESSED = new Color(0.475f, 0.475f, 0.475f);

        protected override void PlayDebug(BaseAudioFileObject asset, bool dontReset)
        {
            helper.MusicHelper.PlayDebug(asset, dontReset);
        }

        protected override void AssignHelperToFader(AudioClip clip)
        {
            helper.MusicHelper.AudioSource.clip = clip;
            editorFader.AudioSource = helper.MusicHelper.AudioSource;
        }

        new protected void OnEnable()
        {
            base.OnEnable();
            myScript = target as MusicFileObject;

            SetupIcons();
            Undo.postprocessModifications += ApplyHelperEffects;

            DesignateSerializedProperties();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
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
#if UNITY_2020_1_OR_NEWER
            newPreset.excludedProperties = new string[] {
                "files", "useLibrary", "category"
            };
#endif
            if (!JSAMEditorHelper.GenerateFolderStructureAt(JSAMPaths.Instance.PresetsPath)) return;

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

            RenderNoClipWarning();

            RenderBasicProperties();

            RenderSpecialProperties();

            PostFixAndSave();

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
            var helperSource = helper.Source;
            float value = 0;
            if (music) value = (float)helperSource.timeSamples / (float)music.samples;

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

        public UndoPropertyModification[] ApplyHelperEffects(UndoPropertyModification[] modifications)
        {
            if (helper.Source.isPlaying)
            {
                helper.MusicHelper.ApplyEffects();
            }
            return modifications;
        }

        public static void DrawLoopPointOverlay(BaseAudioFileObject music, int width, int height)
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
    }
}