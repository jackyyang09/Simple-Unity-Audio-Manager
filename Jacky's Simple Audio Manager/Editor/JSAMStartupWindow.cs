using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [InitializeOnLoad]
    public class JSAMStartupWindow : JSAMBaseEditorWindow<JSAMStartupWindow>
    {
        const string startupGraphicPath = "Assets/Jacky's Simple Audio Manager/Editor/Startup/JSAM card image.png";

        static JSAMStartupWindow()
        {
            EditorApplication.update += RunOnStartup;
        }

        static void RunOnStartup()
        {
            if (!JSAMSettings.Settings.HideStartupMessage)
            {
                // Check if started up this session
                if (!SessionState.GetBool("Startup", false))
                {
                    // Set true and show if not
                    SessionState.SetBool("Startup", true);
                    Init();
                }
            }
            EditorApplication.update -= RunOnStartup;
        }

        [MenuItem("Window/JSAM/Startup Message")]
        public static void Init()
        {
            window = CreateInstance<JSAMStartupWindow>();
            window.ShowUtility();
            window.SetWindowTitle();
            window.maxSize = new Vector2(420, 420);
            window.minSize = window.maxSize;
        }

        Texture2D startupGraphic = null;

        private void OnEnable()
        {
            startupGraphic = AssetDatabase.LoadAssetAtPath<Texture2D>(startupGraphicPath);  
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Thank you for downloading JSAM!");
        }

        private void OnGUI()
        {
            GUILayout.Label(startupGraphic, new GUILayoutOption[] { GUILayout.ExpandHeight(false) });

            EditorGUILayout.LabelField("You are currently using <b>Version 3.0</b>", JSAMEditorHelper.ApplyRichTextToStyle(GUI.skin.label));

            EditorGUILayout.LabelField("To get started with JSAM, visit the links below!");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Getting Started", "Click on me open the wiki in a new browser window"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
            {
                Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/wiki/2.-Getting-Started-with-JSAM");
            }
            if (GUILayout.Button(new GUIContent("Documentation", "Click on me to check out the documentation"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
            {
                Application.OpenURL("https://jackyyang09.github.io/Simple-Unity-Audio-Manager/class_j_s_a_m_1_1_audio_manager.html");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Report a Bug", "Click on me to go to the bug report page in a new browser window"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
            {
                Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/issues");
            }
            if (GUILayout.Button(new GUIContent("Github Releases", "Click on me to check out the latest releases in a new browser window"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
            {
                Application.OpenURL("https://github.com/jackyyang09/Simple-Unity-Audio-Manager/releases");
            }
            if (GUILayout.Button(new GUIContent("Email", "You can find me at jackyyang267@gmail.com"), new GUILayoutOption[] { GUILayout.MinWidth(100) }))
            {
                Application.OpenURL("mailto:jackyyang267@gmail.com");
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            bool hideStartupMessage = JSAMSettings.Settings.HideStartupMessage;
            EditorGUI.BeginChangeCheck();
            GUIContent blontent = new GUIContent("Don't show this message again", "If true, prevents this window from showing up on Unity startup. " +
                "You can find this window under Window -> JSAM -> JSAM Startup");
            hideStartupMessage = EditorGUILayout.ToggleLeft(blontent, hideStartupMessage);
            if (EditorGUI.EndChangeCheck())
            {
                var settings = JSAMSettings.SerializedSettings;
                settings.FindProperty("hideStartupMessage").boolValue = hideStartupMessage;
                settings.ApplyModifiedProperties();
            }
        }
    }
}