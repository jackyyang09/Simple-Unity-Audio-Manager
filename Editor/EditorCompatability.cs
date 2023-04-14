using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public class EditorCompatability : Editor
    {
        [System.Serializable]
        public struct AgnosticGUID<T> where T : UnityEngine.Object
        {
#if UNITY_2020_3_OR_NEWER
            public GUID guid;
#else
            public string guid;
#endif
            [SerializeField] T savedObject;
            public T SavedObject
            {
                get
                {
                    return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                }
                set
                {
#if UNITY_2020_3_OR_NEWER
                    guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(value));
#else
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
#endif
                }
            }
        }

        public static bool SpecialFoldouts(bool foldout, string foldoutText)
        {
#if UNITY_2019_4_OR_NEWER
            return EditorGUILayout.BeginFoldoutHeaderGroup(foldout, foldoutText);
#else
            return EditorGUILayout.Foldout(foldout, foldoutText, true);
#endif
        }

        public static bool SpecialFoldouts(bool foldout, GUIContent content)
        {
#if UNITY_2019_4_OR_NEWER
            return EditorGUILayout.BeginFoldoutHeaderGroup(foldout, content);
#else
            return EditorGUILayout.Foldout(foldout, content, true);
#endif
        }

        public static void EndSpecialFoldoutGroup()
        {
#if UNITY_2019_4_OR_NEWER
        EditorGUILayout.EndFoldoutHeaderGroup();
#endif
        }

        public static GUIStyle GetFoldoutHeaderStyle()
        {
#if UNITY_2019_4_OR_NEWER
            return new GUIStyle(EditorStyles.foldoutHeader);
#else
            return new GUIStyle(EditorStyles.foldout);
#endif
        }

        /// <summary>
        /// Not sure if this works
        /// </summary>
        public static void OpenInspectorWindow()
        {
#if UNITY_2019_4_OR_NEWER
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
#else
            EditorApplication.ExecuteMenuItem("Window/Inspector");
#endif
        }
    }
}
