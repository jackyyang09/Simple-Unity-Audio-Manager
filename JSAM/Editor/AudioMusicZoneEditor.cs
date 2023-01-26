using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// A good portion referenced from the official Unity source
/// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/AudioReverbZoneEditor.cs
/// </summary>
namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(AudioMusicZone))]
    [CanEditMultipleObjects]
    public class AudioMusicZoneEditor : BaseMusicEditor
    {
        AudioMusicZone myScript;

        SerializedProperty keepPlayingWhenAway;
        SerializedProperty musicZones;

        protected virtual string HIDE_TRANSFORMHANDLE => "JSAM_AMZ_HIDETRANSFORMHANDLE";
        protected bool hideTransformHandle
        {
            get
            {
                if (!EditorPrefs.HasKey(HIDE_TRANSFORMHANDLE))
                {
                    EditorPrefs.SetBool(HIDE_TRANSFORMHANDLE, false);
                }
                return EditorPrefs.GetBool(HIDE_TRANSFORMHANDLE);
            }
            set { EditorPrefs.SetBool(HIDE_TRANSFORMHANDLE, value); }
        }

        protected override void Setup()
        {
            base.Setup();

            myScript = (AudioMusicZone)target;

            keepPlayingWhenAway = serializedObject.FindProperty(nameof(keepPlayingWhenAway));
            musicZones = serializedObject.FindProperty("MusicZones");

            Tools.hidden = hideTransformHandle;
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawAudioProperty();

            EditorGUILayout.PropertyField(keepPlayingWhenAway);

            DrawPositionsEditor();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
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

            for (int i = 0; i < myScript.MusicZones.Count; i++)
            {
                Undo.RecordObject(myScript, "Modified Zone properties");
                myScript.MusicZones[i].Position = Handles.PositionHandle(myScript.MusicZones[i].Position, Quaternion.identity);

                myScript.MusicZones[i].MinDistance = Handles.RadiusHandle(Quaternion.identity, myScript.MusicZones[i].Position, myScript.MusicZones[i].MinDistance, false);
                if (myScript.MusicZones[i].MinDistance < 0) myScript.MusicZones[i].MinDistance = 0;
                else if (myScript.MusicZones[i].MinDistance > myScript.MusicZones[i].MaxDistance) myScript.MusicZones[i].MinDistance = myScript.MusicZones[i].MaxDistance;
                myScript.MusicZones[i].MaxDistance = Handles.RadiusHandle(Quaternion.identity, myScript.MusicZones[i].Position, myScript.MusicZones[i].MaxDistance, false);
                if (myScript.MusicZones[i].MaxDistance < 0) myScript.MusicZones[i].MaxDistance = 0;
                else if (myScript.MusicZones[i].MaxDistance < myScript.MusicZones[i].MinDistance) myScript.MusicZones[i].MaxDistance = myScript.MusicZones[i].MinDistance;
            }

            Handles.color = tempColor;
        }

        static bool positionsFoldout = false;
        static List<bool> foldouts = new List<bool>();

        public void DrawPositionsEditor()
        {
            List<int> markedForDeletion = new List<int>();
            GUIContent blontent;

#if !UNITY_2020_3_OR_NEWER
            bool previousFoldout = positionsFoldout;
            EditorGUILayout.BeginHorizontal();
            positionsFoldout = EditorCompatability.SpecialFoldouts(positionsFoldout, "Music Zones");
            musicZones.arraySize = EditorGUILayout.DelayedIntField(musicZones.arraySize, new GUILayoutOption[] { GUILayout.MaxWidth(48) });
            EditorGUILayout.EndHorizontal();
            if (previousFoldout != positionsFoldout) // Toggle handles in scene view
            {
                if (SceneView.sceneViews.Count > 0) SceneView.lastActiveSceneView.Repaint();
            }
            if (positionsFoldout)
#endif
            {
                blontent = new GUIContent("Hide Transform Tool",
                    "If true, hides the transform handle of this gameObject in the scene view so you can " +
                    "better work with the handles of Music Zones.");
                EditorGUI.BeginChangeCheck();
                bool hideTool = EditorGUILayout.Toggle(blontent, hideTransformHandle);
                if (EditorGUI.EndChangeCheck())
                {
                    hideTransformHandle = hideTool;
                    Tools.hidden = hideTransformHandle;
                    if (SceneView.sceneViews.Count > 0) SceneView.lastActiveSceneView.Repaint();
                }

#if UNITY_2020_3_OR_NEWER
                EditorGUILayout.PropertyField(musicZones);
#else
                for (int i = 0; i < musicZones.arraySize; i++)
                {
                    var element = musicZones.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
                        SerializedProperty position = element.FindPropertyRelative("Position");
                        EditorGUILayout.PropertyField(position);
                        SerializedProperty minDistance = element.FindPropertyRelative("MinDistance");
                        SerializedProperty maxDistance = element.FindPropertyRelative("MaxDistance");

                        blontent = new GUIContent("Min Distance", "The minimum distance the listener can be at to hear the music " +
                            "in this music zone. The music will be at it's loudest when the listener is equal to or below this distance to this zone.");
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(minDistance, blontent);
                        //minDistance.floatValue = EditorGUILayout.FloatField(blontent, minDistance.floatValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (minDistance.floatValue > maxDistance.floatValue) minDistance.floatValue = maxDistance.floatValue;
                            else if (minDistance.floatValue < 0) minDistance.floatValue = 0;
                        }

                        blontent = new GUIContent("Max Distance", "The maximum distance the listener can be at to hear the music " +
                            "in this music zone. Volume of the music will increase as listener approaches the minimum distance.");
                        maxDistance.floatValue = EditorGUILayout.FloatField(blontent, maxDistance.floatValue);
                        if (maxDistance.floatValue < minDistance.floatValue) maxDistance.floatValue = minDistance.floatValue;
                        else if (maxDistance.floatValue < 0) maxDistance.floatValue = 0;
                    }
                    EditorGUILayout.EndVertical();
                }
#endif
                foreach (int item in markedForDeletion)
                {
                    foldouts.RemoveAt(item);
                    musicZones.DeleteArrayElementAtIndex(item);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add New Zone At World Center"))
                {
                    var e = musicZones.AddAndReturnNewArrayElement();
                    e.FindPropertyRelative("Position").vector3Value = Vector3.zero;
                    e.FindPropertyRelative("MaxDistance").floatValue = 15;
                    e.FindPropertyRelative("MinDistance").floatValue = 10;

                    positionsFoldout = true;
                    foldouts.Add(true);
                }

                if (GUILayout.Button("Add New Zone At This Position"))
                {
                    var e = musicZones.AddAndReturnNewArrayElement();
                    e.FindPropertyRelative("Position").vector3Value = myScript.transform.position;
                    e.FindPropertyRelative("MaxDistance").floatValue = 15;
                    e.FindPropertyRelative("MinDistance").floatValue = 10;

                    positionsFoldout = true;
                    foldouts.Add(true);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorCompatability.EndSpecialFoldoutGroup();
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