using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public class JSAMUtilityWindow : JSAMBaseEditorWindow<JSAMUtilityWindow>
    {
        public class CustomField
        {
            public GUIContent content;
            public string text;
            public bool useTextArea;
        }

        string windowName;
        bool allowEnterKey;
        bool allowEscapeKey;
        bool focused = false;
        static bool quickReferenceGuide = true;

        List<CustomField> fields = new List<CustomField>();

        public static System.Action<string[]> onSubmitField;

        public static JSAMUtilityWindow Init(string _windowName, bool _allowEnterKey, bool _allowEscapeKey)
        {
            window = CreateInstance<JSAMUtilityWindow>();
            window.ShowUtility();
            window.titleContent = new GUIContent(_windowName);
            window.allowEnterKey = _allowEnterKey;
            window.allowEscapeKey = _allowEscapeKey;
            return window;
        }

        private void OnDisable()
        {
            onSubmitField = null;
        }

        public void AddField(GUIContent content, string startingText = "", bool useTextArea = false)
        {
            var newField = new CustomField();
            newField.content = content;
            newField.text = startingText;
            newField.useTextArea = useTextArea;
            fields.Add(newField);
        }

        private void OnGUI()
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].content != GUIContent.none)
                {
                    EditorGUILayout.LabelField(fields[i].content);
                }
                if (!focused) GUI.SetNextControlName("TextBox");
                if (!fields[i].useTextArea)
                {
                    fields[i].text = EditorGUILayout.TextField(fields[i].text);
                }
                else
                {
                    fields[i].text = EditorGUILayout.TextArea(fields[i].text);
                }
                if (!focused)
                {
                    GUI.FocusControl("TextBox");
                    focused = true;
                }
                EditorGUILayout.Space();
                
            }
            List<string> tipText = new List<string>();
            if (allowEscapeKey)
            {
                tipText.Add("Tip: You can double-press ESC to quickly close this window!");
            }
            if (allowEnterKey)
            {
                tipText.Add("Tip: You can double-press ENTER to quickly submit your text!");
            }
            quickReferenceGuide = JSAMEditorHelper.RenderQuickReferenceGuide(quickReferenceGuide, tipText.ToArray());

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    if (allowEscapeKey)
                    {
                        window.Close();
                    }
                }

                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (allowEnterKey)
                    {
                        SubmitText();
                    }
                }
            }
            
            if (GUILayout.Button("Submit"))
            {
                SubmitText();
            }
        }

        void SubmitText()
        {
            string[] text = new string[fields.Count];
            for (int i = 0; i < fields.Count; i++)
            {
                text[i] = fields[i].text;
            }
            onSubmitField?.Invoke(text);
            window.Close();
        }

        protected override void SetWindowTitle()
        {
        }
    }
}