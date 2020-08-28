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
    public class AudioMusicZoneEditor : BaseAudioMusicEditor
    {
        AudioMusicZone myScript;

        SerializedProperty musicProperty;
        SerializedProperty positionsProperty;
        SerializedProperty maxDistanceProperty;
        SerializedProperty minDistanceProperty;

        AudioMusicZone[] otherZones;
        bool othersExist = false;
        bool isSharing = false;

        static bool hideTransformTool;

        protected override void Setup()
        {
            myScript = (AudioMusicZone)target;

            otherZones = FindObjectsOfType<AudioMusicZone>();
            if (otherZones.Length > 1) othersExist = true;

            musicProperty = serializedObject.FindProperty("music");
            positionsProperty = serializedObject.FindProperty("positions");
            maxDistanceProperty = serializedObject.FindProperty("maxDistance");
            minDistanceProperty = serializedObject.FindProperty("minDistance");

            CheckIfSharing();

            Tools.hidden = hideTransformTool;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            if (isSharing)
            {
                EditorGUILayout.HelpBox("Another Audio Music Zone exists in this scene that plays the same music! You can only " +
                    "have one Audio Music Zone that plays a specific piece of music.", MessageType.Error);
            }

            DrawMusicDropdown(musicProperty);

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
                CheckIfSharing();
            }

            DrawQuickReferenceGuide();
        }

        #region Quick Reference Guide
        protected override void DrawQuickReferenceGuide()
        {
            base.DrawQuickReferenceGuide();

            if (!showHowTo) return;

            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Audio Music Zones are like AudioPlayerMusic components in that they playback music in the scene. " +
                "However, music is only played when the scene's AudioListener enters a \"Music Zone.\""
                , MessageType.None);
            EditorGUILayout.HelpBox("\"Music Zones\" are defined by a position, a min distance, and a max distance. You can " +
                "create a new \"Music Zone\" by clicking either \"Add New Zone at World Center\" or \"Add New Zone at This Position\"."
                , MessageType.None);
            EditorGUILayout.HelpBox("The max distance indicates the distance the AudioListener has to be from the Zone's position to hear the music " +
                "at minimal volume."
                , MessageType.None);
            EditorGUILayout.HelpBox("The min distance indicates the distance the AudioListener has to be from the Zone's position to hear the music " +
                "at maximum volume."
                , MessageType.None);
            EditorGUILayout.HelpBox("If the AudioListener is in-between the min and max distances, the volume of the music will be " +
                " played relative to the player's middle distance."
                , MessageType.None);
            EditorGUILayout.HelpBox("There should only be one Audio Music Zone for each music track in the scene."
                , MessageType.None);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("You can assign multiple zone positions to this one component to cover a large range."
                , MessageType.None);
            EditorGUILayout.HelpBox("Click the \"Hide Transform Tool\" option to hide this GameObject's transform tool and " +
                "make it easier to manipulate the positions of your Music Zones."
                , MessageType.None);
            EditorStyles.helpBox.fontSize = 10;
            EditorCompatability.EndSpecialFoldoutGroup();
        }
        #endregion

        private void OnSceneGUI()
        {
            if (!target || !positionsFoldout)
                return;

            Color tempColor = Handles.color;
            if (myScript.enabled)
                Handles.color = new Color(0.50f, 0.70f, 1.00f, 0.5f);
            else
                Handles.color = new Color(0.30f, 0.40f, 0.60f, 0.5f);

            for (int i = 0; i < myScript.positions.Count; i++)
            {
                Undo.RecordObject(myScript, "Modified Zone properties");
                myScript.positions[i] = Handles.PositionHandle(myScript.positions[i], Quaternion.identity);

                myScript.minDistance[i] = Handles.RadiusHandle(Quaternion.identity, myScript.positions[i], myScript.minDistance[i], false);
                if (myScript.minDistance[i] < 0) myScript.minDistance[i] = 0;
                else if (myScript.minDistance[i] > myScript.maxDistance[i]) myScript.minDistance[i] = myScript.maxDistance[i];
                myScript.maxDistance[i] = Handles.RadiusHandle(Quaternion.identity, myScript.positions[i], myScript.maxDistance[i], false);
                if (myScript.maxDistance[i] < 0) myScript.maxDistance[i] = 0;
                else if (myScript.maxDistance[i] < myScript.minDistance[i]) myScript.maxDistance[i] = myScript.minDistance[i];
            }

            Handles.color = tempColor;
        }

        static bool positionsFoldout = false;
        static List<bool> foldouts = new List<bool>();

        public void DrawPositionsEditor()
        {
            bool hideTool = EditorGUILayout.Toggle("Hide Transform Tool", hideTransformTool);
            if (hideTool != hideTransformTool)
            {
                hideTransformTool = hideTool;
                Tools.hidden = hideTransformTool;
                if (SceneView.sceneViews.Count > 0) SceneView.lastActiveSceneView.Repaint();
            }

            List<int> markedForDeletion = new List<int>();
            GUIContent blontent;

            bool previousFoldout = positionsFoldout;
            positionsFoldout = EditorCompatability.SpecialFoldouts(positionsFoldout, "Music Zone");
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

                foreach (int item in markedForDeletion)
                {
                    foldouts.RemoveAt(item);
                    positionsProperty.DeleteArrayElementAtIndex(item);
                }
            }
            EditorCompatability.EndSpecialFoldoutGroup();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Zone At World Center"))
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

            if (GUILayout.Button("Add New Zone At This Position"))
            {
                int index = positionsProperty.arraySize;
                positionsProperty.InsertArrayElementAtIndex(index);
                positionsProperty.GetArrayElementAtIndex(index).vector3Value = myScript.transform.position;

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
            if (!othersExist) return;
            foreach (AudioMusicZone z in otherZones)
            {
                if (myScript.music == z.music && z != myScript)
                {
                    isSharing = true;
                    return;
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
            Undo.RegisterCreatedObjectUndo(newPlayer, "Added new Audio Music Zone");
        }
    }
}