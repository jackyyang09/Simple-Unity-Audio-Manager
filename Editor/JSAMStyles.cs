using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public class JSAMStyles
    {
        static Font timeFont;
        public static Font TimeFont
        {
            get
            {
                if (!timeFont)
                {
                    timeFont = AssetDatabase.LoadAssetAtPath<Font>(JSAMPaths.Instance.FontsPath + "AzeretMono-Regular.ttf");
                }
                return timeFont;
            }
        }

        static GUIStyle timeStyle;
        public static GUIStyle TimeStyle
        {
            get
            {
                if (timeStyle == null)
                {
                    timeStyle = new GUIStyle(EditorStyles.label)
                        .SetFontSize(11)
                        .ApplyTextAnchor(TextAnchor.MiddleRight);
                    timeStyle.font = TimeFont;
                }
                return timeStyle;
            }
        }
    }
}
