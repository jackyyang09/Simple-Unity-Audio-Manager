using UnityEditor;
using UnityEngine;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(VideoPlayerVolume))]
    public class VideoPlayerVolumeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });

            if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
        }
    }
}