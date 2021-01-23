using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class JSAMBaseEditorWindow<T> : EditorWindow
        where T : EditorWindow
    {
        protected static T window;
        public static T Window
        {
            get
            {
                if (window == null)
                {
                    window = GetWindow<T>();
                }
                return window;
            }
        }
        
        protected abstract void SetWindowTitle();
    }

    public abstract class JSAMSerializedEditorWindow<Asset, T> : JSAMBaseEditorWindow<T>
        where Asset : ScriptableObject
        where T : EditorWindow
    {
        protected static SerializedObject serializedObject;
        protected static Asset asset;

        protected virtual void OnEnable()
        {
            if (serializedObject != null) DesignateSerializedProperties();
        }

        protected SerializedProperty FindProp(string prop)
        {
            return serializedObject.FindProperty(prop);
        }

        protected abstract void DesignateSerializedProperties();
    }
}