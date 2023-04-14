using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace JSAM.JSAMEditor
{
    public class AudioClipList
    {
        ReorderableList list;
        SerializedObject serializedObject;
        SerializedProperty property;

        public int Selected
        {
            get
            {
                return list.index;
            }
            set
            {
                list.index = value;
            }
        }

        public AudioClipList(SerializedObject obj, SerializedProperty prop)
        {
            list = new ReorderableList(obj, prop, true, false, true, true);

            list.onRemoveCallback += OnRemoveElement;
            list.drawElementCallback += DrawElement;

            list.headerHeight = 1;
            list.footerHeight = 0;
            serializedObject = obj;
            property = prop;
        }

        private void OnRemoveElement(ReorderableList list)
        {
            int listSize = property.arraySize;
            property.DeleteArrayElementAtIndex(list.index);
            if (listSize == property.arraySize)
            {
                property.DeleteArrayElementAtIndex(list.index);
            }

            if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(index);

            var file = element.objectReferenceValue as AudioClip;

            Rect prevRect = new Rect(rect);
            Rect currentRect = new Rect(prevRect);

            string name = "Element " + index;
            if (file) name = file.name;

            GUIContent blontent = new GUIContent(name);

            currentRect.xMax = rect.width * 0.6f;
            // Force a normal-colored label in a disabled scope
            JSAMEditorHelper.BeginColourChange(Color.clear);
            Rect decoyRect = EditorGUI.PrefixLabel(currentRect, blontent);
            JSAMEditorHelper.EndColourChange();

            EditorGUI.LabelField(currentRect, blontent);

            decoyRect.xMin = currentRect.xMax + 5;
            decoyRect.xMax = rect.xMax - 2.5f;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(decoyRect, element, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                if (element.objectReferenceValue == null)
                {
                    list.index = index;
                    OnRemoveElement(list);
                }
            }
        }

        public void Draw() => list.DoLayoutList();
    }

    public abstract class BaseAudioFileObjectEditor<EditorType> : Editor
        where EditorType : Editor
    {
        public enum LoopPointTool
        {
            Slider,
            TimeInput,
            TimeSamplesInput,
            BPMInput//WithBeats,
                    //BPMInputWithBars
        }

        public static EditorType instance;
        protected BaseAudioFileObject asset;

        protected bool isPreset;

        protected static bool showPlaybackTool;
        protected static bool showHowTo;

        protected static bool showLoopPointTool;
        protected static int loopPointInputMode = 0;

        protected virtual string SHOW_LIBRARY => "JSAM_BAFO_SHOWLIBRARY";
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

        protected virtual string EXPAND_LIBRARY => "JSAM_BAFO_EXPANDLIBRARY";
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

        protected AudioClipList list;

        protected GUIContent blontent;

        protected Vector2 scroll;

        protected SerializedProperty FindProp(string property)
        {
            return serializedObject.FindProperty(property);
        }

        protected void OnEnable()
        {
            asset = target as BaseAudioFileObject;
            instance = this as EditorType;

            isPreset = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset as UnityEngine.Object));
        }

        protected SerializedProperty safeName;
        protected SerializedProperty presetDescription;
        protected SerializedProperty files;
        protected SerializedProperty relativeVolume;
        protected SerializedProperty spatialize;
        protected SerializedProperty maxDistance;
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

        protected abstract void DrawPlaybackTool();

        protected void DrawLoopPointTools(BaseAudioFileObject myScript)
        {
            if (myScript.Files.Count == 0) return;

            float loopStart = myScript.loopStart;
            float loopEnd = myScript.loopEnd;

            AudioClip music = myScript.Files[0];
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

            using (new EditorGUI.DisabledScope(myScript.loopMode < LoopMode.LoopWithLoopPoints))
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
                    }

                    GUIContent buttonText = new GUIContent("Reset Loop Points", "Click to set loop points to the start and end of the track.");
                    if (GUILayout.Button(buttonText))
                    {
                        loopStart = 0;
                        loopEnd = music.length;
                    }
                    using (new EditorGUI.DisabledScope(!myScript.Files[0].IsWavFile()))
                    {
                        if (myScript.Files[0].IsWavFile())
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
                            string filePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(myScript.Files[0].name)[0]);
                            string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;

                            ATL.Track theTrack = new ATL.Track(trueFilePath);

                            float frequency = myScript.Files[0].frequency;

                            if (theTrack.AdditionalFields.ContainsKey("sample.SampleLoop[0].Start") && theTrack.AdditionalFields.ContainsKey("sample.SampleLoop[0].End"))
                            {
                                loopStart = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].Start"]) / frequency;
                                loopEnd = float.Parse(theTrack.AdditionalFields["sample.SampleLoop[0].End"]) / frequency;
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error Reading Metadata", "Could not find any loop point data in " + myScript.Files[0].name + ".wav!/n" +
                                    "Are you sure you wrote loop points in this file?", "OK");
                            }

                        }
                        if (myScript.Files[0].IsWavFile())
                        {
                            buttonText = new GUIContent("Embed Loop Points to File", "Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                        }
                        else
                        {
                            buttonText = new GUIContent("Embed Loop Points to File", "This option is exclusive to .WAV files. Clicking this will write the above start and end loop points into the actual file itself. Check the quick reference guide for details!");
                        }
                        if (GUILayout.Button(buttonText))
                        {
                            string filePath = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(myScript.Files[0].name)[0]);
                            string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;

                            ATL.Track theTrack = new ATL.Track(trueFilePath);

                            float frequency = myScript.Files[0].frequency;

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