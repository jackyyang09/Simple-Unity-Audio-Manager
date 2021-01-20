using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class JSAMBaseEditorWindow<T> : EditorWindow where T : class
    {
        public T window;
        //public T Window
        //{
        //    get
        //    {
        //        window = GetWindow<T>();
        //        return window;
        //    }
        //}

        protected void OnEnable()
        {

        }

        public abstract void DesignateSerializedProperties();
    }
}