using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// https://www.reddit.com/r/Unity3D/comments/5n6ddx/audioclip_propertydrawer/
/// </summary>
[CustomPropertyDrawer(typeof(AudioClip))]
public class AudioClipPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(prop);
    }

    private Dictionary<ButtonState, Action<SerializedProperty, AudioClip>> _audioButtonStates = new Dictionary<ButtonState, Action<SerializedProperty, AudioClip>>
    {
        { ButtonState.Play, Play },
        { ButtonState.Stop, Stop },
    };

    private enum ButtonState
    {
        Play,
        Stop
    }

    private static string CurrentClip;


    public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, prop);

        if (prop.objectReferenceValue != null)
        {
            float totalWidth = position.width;
            position.width = totalWidth - (totalWidth / 4);
            EditorGUI.PropertyField(position, prop, true);

            position.x += position.width;
            position.width = totalWidth / 4;
            DrawButton(position, prop);
        }
        else
        {
            EditorGUI.PropertyField(position, prop, true);
        }

        EditorGUI.EndProperty();
    }

    private void DrawButton(Rect position, SerializedProperty prop)
    {
        if (prop.objectReferenceValue != null)
        {
            position.x += 4;
            position.width -= 5;

            AudioClip clip = prop.objectReferenceValue as AudioClip;

            Rect buttonRect = new Rect(position);
            buttonRect.width = 20;

            Rect waveformRect = new Rect(position);
            waveformRect.x += 22;
            waveformRect.width -= 22;
            Texture2D waveformTexture = AssetPreview.GetAssetPreview(prop.objectReferenceValue);
            if (waveformTexture != null)
                GUI.DrawTexture(waveformRect, waveformTexture);

            bool isPlaying = AudioUtil.IsClipPlaying(clip) && (CurrentClip == prop.propertyPath);
            string buttonText = "";
            Action<SerializedProperty, AudioClip> buttonAction;
            if (isPlaying)
            {
                EditorUtility.SetDirty(prop.serializedObject.targetObject);
                buttonAction = GetStateInfo(ButtonState.Stop, out buttonText);

                Rect progressRect = new Rect(waveformRect);
                float percentage = (float)AudioUtil.GetClipSamplePosition(clip) / AudioUtil.GetSampleCount(clip);
                float width = progressRect.width * percentage;
                progressRect.width = Mathf.Clamp(width, 6, width);
                GUI.Box(progressRect, "", "SelectionRect");
            }
            else
            {
                buttonAction = GetStateInfo(ButtonState.Play, out buttonText);
            }

            if (GUI.Button(buttonRect, buttonText))
            {
                AudioUtil.StopAllClips();
                buttonAction(prop, clip);
            }
        }
    }

    private static void Play(SerializedProperty prop, AudioClip clip)
    {
        CurrentClip = prop.propertyPath;
        AudioUtil.PlayClip(clip, 0, false);
    }

    private static void Stop(SerializedProperty prop, AudioClip clip)
    {
        CurrentClip = "";
        AudioUtil.StopClip(clip);
    }

    private Action<SerializedProperty, AudioClip> GetStateInfo(ButtonState state, out string buttonText)
    {
        buttonText = state == ButtonState.Play ? "►" : "■";
        return _audioButtonStates[state];
    }
}