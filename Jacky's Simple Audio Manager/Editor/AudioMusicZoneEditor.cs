using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// A good portion referenced from the official Unity source
/// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/AudioReverbZoneEditor.cs
/// </summary>
namespace JSAM
{
    [CustomEditor(typeof(AudioMusicZone))]
    [CanEditMultipleObjects]
    public class AudioMusicZoneEditor : Editor
    {
        List<string> options = new List<string>();
        System.Type enumType;

        SerializedProperty musicProperty;
        SerializedProperty positionsProperty;
        SerializedProperty maxDistanceProperty;
        SerializedProperty minDistanceProperty;

        AudioMusicZone[] otherZones;
        bool othersExist = false;
        bool isSharing = false;

        private void OnEnable()
        {
            if (target == null)
            {
                OnEnable();
                return;
            }
            enumType = AudioManager.instance.GetSceneMusicEnum();
            foreach (string s in System.Enum.GetNames(enumType))
            {
                options.Add(s);
            }

            otherZones = FindObjectsOfType<AudioMusicZone>();
            if (otherZones.Length > 1) othersExist = true;

            musicProperty = serializedObject.FindProperty("music");
            positionsProperty = serializedObject.FindProperty("positions");
            maxDistanceProperty = serializedObject.FindProperty("maxDistance");
            minDistanceProperty = serializedObject.FindProperty("minDistance");

            CheckIfSharing();
        }

        public override void OnInspectorGUI()
        {
            AudioMusicZone zone = (AudioMusicZone)target;

            serializedObject.Update();

            if (!AudioManager.instance)
            {
                EditorGUILayout.HelpBox("Could not find Audio Manager in the scene! This component needs AudioManager " +
                    "in order to function!", MessageType.Error);
            }
            else
            {
                if (enumType == null)
                {
                    EditorGUILayout.HelpBox("Could not find Audio File info! Try regenerating Audio Files in AudioManager!", MessageType.Error);
                }
            }

            EditorGUILayout.LabelField("Specify Music to Play", EditorStyles.boldLabel);

            if (isSharing)
            {
                EditorGUILayout.HelpBox("Another Audio Music Zone exists in this scene that plays the same music! You can only " +
                    "have one Audio Music Zone that plays a specific piece of music.", MessageType.Error);
            }

            GUIContent musicDesc = new GUIContent("Music", "Music that will be played");

            string music = musicProperty.stringValue;

            using (new EditorGUI.DisabledScope(zone.GetAttachedFile() != null))
            {
                int selected = options.IndexOf(music);
                if (selected == -1) selected = 0;
                string newValue = options[EditorGUILayout.Popup(musicDesc, selected, options.ToArray())];
                if (musicProperty.stringValue != newValue)
                {
                    CheckIfSharing();
                    musicProperty.stringValue = newValue;
                }
            }

            List<string> excludedProperties = new List<string>
            {
                "m_Script", "musicFile", "playOnStart", "playOnEnable", "spatializeSound", "loopMode",
                        "stopOnDisable", "stopOnDestroy", "keepPlaybackPosition", "restartOnReplay", "musicFadeInTime",
                "musicFadeOutTime", "transitionMode", "positions", "maxDistance", "minDistance"
            };

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());

            DrawPositionsEditor();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnSceneGUI()
        {
            if (!target || !positionsFoldout)
                return;

            AudioMusicZone zone = (AudioMusicZone)target;

            Color tempColor = Handles.color;
            if (zone.enabled)
                Handles.color = new Color(0.50f, 0.70f, 1.00f, 0.5f);
            else
                Handles.color = new Color(0.30f, 0.40f, 0.60f, 0.5f);

            for (int i = 0; i < zone.positions.Count; i++)
            {
                Undo.RecordObject(zone, "Modified Zone properties");
                zone.positions[i] = Handles.PositionHandle(zone.positions[i], Quaternion.identity);

                zone.minDistance[i] = Handles.RadiusHandle(Quaternion.identity, zone.positions[i], zone.minDistance[i], false);
                if (zone.minDistance[i] < 0) zone.minDistance[i] = 0;
                else if (zone.minDistance[i] > zone.maxDistance[i]) zone.minDistance[i] = zone.maxDistance[i];
                zone.maxDistance[i] = Handles.RadiusHandle(Quaternion.identity, zone.positions[i], zone.maxDistance[i], false);
                if (zone.maxDistance[i] < 0) zone.maxDistance[i] = 0;
                else if (zone.maxDistance[i] < zone.minDistance[i]) zone.maxDistance[i] = zone.minDistance[i];
            }

            Handles.color = tempColor;
        }

        static bool positionsFoldout = false;
        static List<bool> foldouts = new List<bool>();

        public void DrawPositionsEditor()
        {
            AudioMusicZone zone = (AudioMusicZone)target;

            List<int> markedForDeletion = new List<int>();
            GUIContent blontent;
            bool previousFoldout = positionsFoldout;
            positionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(positionsFoldout, new GUIContent("Music Zones"));
            if (previousFoldout != positionsFoldout) // Toggle handles in scene view
            {
                if (SceneView.sceneViews.Count > 0) SceneView.lastActiveSceneView.Repaint();
            }
            if (positionsFoldout)
            {
                for (int i = 0; i < positionsProperty.arraySize; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    SerializedProperty position = positionsProperty.GetArrayElementAtIndex(i);
                    if (foldouts.Count < i + 1)
                    {
                        foldouts.Add(false);
                    }
                    string arrow = (foldouts[i]) ? "▼" : "▶";
                    EditorGUILayout.BeginHorizontal();
                    foldouts[i] = EditorGUILayout.Foldout(foldouts[i], new GUIContent("    " + arrow + " Zone " + i), true, EditorStyles.boldLabel);
                    if (GUILayout.Button(new GUIContent("x", "Remove this transform"), new GUILayoutOption[] { GUILayout.MaxWidth(20) }))
                    {
                        markedForDeletion.Add(i);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (foldouts[i])
                    {
                        position.vector3Value = EditorGUILayout.Vector3Field(new GUIContent("Position"), position.vector3Value);

                        SerializedProperty minDistance = minDistanceProperty.GetArrayElementAtIndex(i);
                        SerializedProperty maxDistance = maxDistanceProperty.GetArrayElementAtIndex(i);

                        blontent = new GUIContent("Min Distance", "The minimum distance the listener can be at to hear the music " +
                            "in this music zone. The music will be at it's loudest when the listener is equal to or below this distance to this zone.");
                        minDistance.floatValue = EditorGUILayout.FloatField(blontent, minDistance.floatValue);
                        if (minDistance.floatValue > maxDistance.floatValue) minDistance.floatValue = maxDistance.floatValue;
                        else if (minDistance.floatValue < 0) minDistance.floatValue = 0;

                        blontent = new GUIContent("Max Distance", "The maximum distance the listener can be at to hear the music " +
                            "in this music zone. Volume of the music will increase as listener approaches the minimum distance.");
                        maxDistance.floatValue = EditorGUILayout.FloatField(blontent, maxDistance.floatValue);
                        if (maxDistance.floatValue < minDistance.floatValue) maxDistance.floatValue = minDistance.floatValue;
                        else if (maxDistance.floatValue < 0) maxDistance.floatValue = 0;
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                foreach (int item in markedForDeletion)
                {
                    foldouts.RemoveAt(item);
                    positionsProperty.DeleteArrayElementAtIndex(item);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Position At World Origin"))
            {
                int index = positionsProperty.arraySize;
                positionsProperty.InsertArrayElementAtIndex(index);
                positionsProperty.GetArrayElementAtIndex(index).vector3Value = Vector3.zero;

                maxDistanceProperty.InsertArrayElementAtIndex(index);
                maxDistanceProperty.GetArrayElementAtIndex(index).floatValue = 15;

                minDistanceProperty.InsertArrayElementAtIndex(index);
                minDistanceProperty.GetArrayElementAtIndex(index).floatValue = 10;

                positionsFoldout = true;
                foldouts.Add(true);
            }

            if (GUILayout.Button("Add New Position At Local Origin"))
            {
                int index = positionsProperty.arraySize;
                positionsProperty.InsertArrayElementAtIndex(index);
                positionsProperty.GetArrayElementAtIndex(index).vector3Value = zone.transform.position;

                maxDistanceProperty.InsertArrayElementAtIndex(index);
                maxDistanceProperty.GetArrayElementAtIndex(index).floatValue = 15;

                minDistanceProperty.InsertArrayElementAtIndex(index);
                minDistanceProperty.GetArrayElementAtIndex(index).floatValue = 10;

                positionsFoldout = true;
                foldouts.Add(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        public void CheckIfSharing()
        {
            AudioMusicZone zone = (AudioMusicZone)target;

            if (!othersExist) return;
            foreach (AudioMusicZone z in otherZones)
            {
                if (zone.music == z.music)
                {
                    isSharing = true;
                    break;
                }
            }
            isSharing = false;
        }

        [MenuItem("GameObject/Audio/JSAM/Audio Music Zone", false, 1)]
        public static void AddAudioMusicZone()
        {
            GameObject newPlayer = new GameObject("Audio Music Zone");
            newPlayer.AddComponent<AudioMusicZone>();
            if (Selection.activeTransform != null)
            {
                newPlayer.transform.parent = Selection.activeTransform;
                newPlayer.transform.localPosition = Vector3.zero;
            }
            EditorGUIUtility.PingObject(newPlayer);
            Selection.activeGameObject = newPlayer;
            Undo.RegisterCreatedObjectUndo(newPlayer, "Added new Audio Player");
        }
    }
}