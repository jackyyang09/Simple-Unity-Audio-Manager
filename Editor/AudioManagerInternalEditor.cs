using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioManagerInternal))]
    public class AudioManagerInternalEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var style = new GUIStyle(EditorStyles.label);
            style.ApplyWordWrap();
            style.ApplyTextAnchor(TextAnchor.MiddleCenter);
            EditorGUILayout.LabelField("Don't touch me! AudioManager needs me!", style);
        }
    }
}