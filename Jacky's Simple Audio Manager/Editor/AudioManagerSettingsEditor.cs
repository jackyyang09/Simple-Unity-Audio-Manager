using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace JSAM
{
    public class AudioManagerSettingsEditor : EditorWindow
    {
        private void OnEnable()
        {

        }

        [OnOpenAsset]
        public static bool Test(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            AudioManagerSettings scriptableObject = AssetDatabase.LoadAssetAtPath<AudioManagerSettings>(assetPath);
            //if ((Selection.activeObject.GetType()).Equals(typeof(AudioManagerSettings)))
            if (scriptableObject)
            {
                Init();
            }
            //if ((AudioManagerSettings)Selection.activeObject)
            //{
            //    return true;
            //}
            return false;
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/AudioManager Settings")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            AudioManagerSettingsEditor window = (AudioManagerSettingsEditor)GetWindow(typeof(AudioManagerSettingsEditor));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Benis", EditorStyles.boldLabel);
        }
    }
}