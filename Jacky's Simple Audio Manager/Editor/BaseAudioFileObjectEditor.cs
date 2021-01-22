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
    }
}