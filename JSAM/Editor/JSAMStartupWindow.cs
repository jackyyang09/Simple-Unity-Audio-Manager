using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    [InitializeOnLoad]
    public class JSAMStartupWindow : JSAMBaseEditorWindow<JSAMStartupWindow>
    {
        [Tooltip("If true, prevents this window from showing up on Unity startup. You can find this window under Window -> JSAM -> JSAM Startup")]
        const string HIDE_STARTUP_MESSAGE_KEY = "JSAMSTARTUP_HIDEMSG";
        static bool HideStartupMessage
        {
            get { return EditorPrefs.GetBool(HIDE_STARTUP_MESSAGE_KEY, false); }
            set { EditorPrefs.SetBool(HIDE_STARTUP_MESSAGE_KEY, value); }
        }

        const string STARTUP_GRAPHIC_FILENAME = "JSAM card image.png";
        string startupGraphicPath
        {
            get
            {
                return System.IO.Path.Combine(new string[] 
                {
                    JSAMPaths.Instance.PackagePath,
                    "Editor",
                    "Startup",
                    STARTUP_GRAPHIC_FILENAME
                });
            }
        }

        static JSAMStartupWindow()
        {
            EditorApplication.update += RunOnStartup;
        }

        static void RunOnStartup()
        {
            if (!HideStartupMessage)
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

        const string STARTUP_MSG_MENU_PATH = "Window/JSAM/Startup Message";
        [MenuItem(STARTUP_MSG_MENU_PATH)]
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

        private void OnDisable()
        {
            if (HideStartupMessage)
            {
                EditorUtility.DisplayDialog("We won't show it to you again",
                    "But if you want to find it again, navigate to \"" + STARTUP_MSG_MENU_PATH + 
                    "\" in the toolbar", "OK");
            }
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Thank you for downloading JSAM!");
        }

        private void OnGUI()
        {
            GUILayout.Label(startupGraphic, new GUILayoutOption[] { GUILayout.ExpandHeight(false) });

            EditorGUILayout.LabelField("You are currently using <b>Version 3.0</b>", GUI.skin.label.ApplyRichText());

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

            EditorGUI.BeginChangeCheck();
            GUIContent blontent = new GUIContent("Don't show this message again", "If true, prevents this window from showing up on Unity startup. " +
                "You can find this window under \"" + STARTUP_MSG_MENU_PATH + "\"");
            HideStartupMessage = EditorGUILayout.ToggleLeft(blontent, HideStartupMessage);
        }
    }
}