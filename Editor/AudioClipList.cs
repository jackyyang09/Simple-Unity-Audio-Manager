using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

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

            list.footerHeight = 20;
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
}
