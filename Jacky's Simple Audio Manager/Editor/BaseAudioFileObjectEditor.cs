using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseAudioFileObjectEditor<EditorType> : Editor 
        where EditorType : Editor
    {
        public static EditorType instance;
        protected BaseAudioFileObject asset;

        protected bool isPreset;

        protected static bool showPlaybackTool;
        protected static bool showHowTo;

        protected GUIContent blontent;

        protected SerializedProperty FindProp(string property)
        {
            return serializedObject.FindProperty(property);
        }

        protected void OnEnable()
        {
            asset = target as BaseAudioFileObject;
            instance = this as EditorType;

            isPreset = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset as UnityEngine.Object));

            safeName = FindProp("safeName");
            category = FindProp(nameof(asset.category));
            presetDescription = FindProp("presetDescription");
            file = FindProp(nameof(asset.file));
            files = FindProp(nameof(asset.files));
            relativeVolume = FindProp(nameof(asset.relativeVolume));
            spatialize = FindProp(nameof(asset.spatialize));
            maxDistance = FindProp(nameof(asset.maxDistance));
        }

        protected SerializedProperty safeName;
        protected SerializedProperty category;
        protected SerializedProperty presetDescription;
        protected SerializedProperty file;
        protected SerializedProperty files;
        protected SerializedProperty relativeVolume;
        protected SerializedProperty spatialize;
        protected SerializedProperty maxDistance;

        protected SerializedProperty bypassEffects;
        protected SerializedProperty bypassListenerEffects;
        protected SerializedProperty bypassReverbZones;

        protected virtual void DesignateSerializedProperties()
        {
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