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

        public static bool IsOpen
        {
            get
            {
                return window != null;
            }
        }
        
        protected abstract void SetWindowTitle();
    }

    public abstract class JSAMSerializedEditorWindow<AssetType, T> : JSAMBaseEditorWindow<T>
        where AssetType : ScriptableObject
        where T : EditorWindow
    {
        protected static SerializedObject serializedObject;
        protected static AssetType asset;
        public static AssetType Asset
        {
            get
            {
                return asset;
            }
        }

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