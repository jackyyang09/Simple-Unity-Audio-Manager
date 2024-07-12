using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace JSAM.JSAMEditor
{
    public class EditorCompatability : Editor
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
