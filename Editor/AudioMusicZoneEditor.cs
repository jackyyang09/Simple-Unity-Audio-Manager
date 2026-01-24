using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

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
        Transform Transform => myScript.transform;

        SerializedProperty MinDistance;
        SerializedProperty MaxDistance;
        SerializedProperty keepPlayingWhenAway;
        SerializedProperty MusicZones;

        PositionList list;

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

        protected virtual string FOLDOUTSTATE => "JSAM_AMZ_FOLDOUTSTATE";

        List<bool> foldouts = new List<bool>();
        List<int> markedForDeletion = new();

        protected override void Setup()
        {
            base.Setup();

            myScript = (AudioMusicZone)target;

            keepPlayingWhenAway = serializedObject.FindProperty(nameof(keepPlayingWhenAway));
            MinDistance = serializedObject.FindProperty(nameof(MinDistance));
            MaxDistance = serializedObject.FindProperty(nameof(MaxDistance));
            MusicZones = serializedObject.FindProperty(nameof(MusicZones));

            LoadFoldoutState();

            RebuildList();

            TryHideTools();
        }

        /// <summary>
        /// Only works up to 32 foldouts, though I don't forsee a use case where you need more than 32
        /// Otherwise, consider switching to string-hex encoding
        /// </summary>
        void LoadFoldoutState()
        {
            int number = EditorPrefs.GetInt(FOLDOUTSTATE, 0);
            var bits = new BitArray(new int[] { number }).Cast<bool>();

            foldouts = new List<bool>(bits);
            if (foldouts.Count > MusicZones.arraySize)
            {
                var diff = foldouts.Count - MusicZones.arraySize;
                foldouts.RemoveRange(MusicZones.arraySize, diff);
            }
            else if (foldouts.Count < MusicZones.arraySize)
            {
                var diff = MusicZones.arraySize - foldouts.Count;
                foldouts.AddRange(new bool[diff]);
            }
        }

        public void SaveFoldoutState()
        {
            var bits = new BitArray(foldouts.ToArray());

            int result = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    result |= (1 << i);
                }
            }

            EditorPrefs.SetInt(FOLDOUTSTATE, result);
        }

        /// <summary>
        /// This hack circumvents ReorderableList's element height caching behaviour. 
        /// The height of all elements get saved and it takes 8 frames before it can get repainted, 
        /// at which point the delay created becomes visible and ugly to the player. 
        /// Rebuilding the list object is a bit heavy, but at least it feels good to use.
        /// </summary>
        public void RebuildList()
        {
            list = new(serializedObject, MusicZones, this);
            RepaintSceneView();
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        void TryHideTools() => Tools.hidden = positionsFoldout || (positionsFoldout && hideTransformHandle);

        public override void OnInspectorGUI()
        {
            if (myScript == null) return;

            serializedObject.Update();

            DrawAudioProperty();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(MinDistance);
            if (EditorGUI.EndChangeCheck())
            {
                MinDistance.floatValue = Mathf.Clamp(MinDistance.floatValue, 0, MaxDistance.floatValue);
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(MaxDistance);
            if (EditorGUI.EndChangeCheck())
            {
                MaxDistance.floatValue = Mathf.Max(MinDistance.floatValue, MaxDistance.floatValue);
            }

            EditorGUILayout.PropertyField(keepPlayingWhenAway);

            DrawPositionsEditor();

            serializedObject.ApplyModifiedProperties();

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
            EditorGUILayout.HelpBox("\"Music Zones\" are defined by a position, a min distance, and a max distance."
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
                if (!foldouts[i]) continue;

                Undo.RecordObject(myScript, "Modified Zone properties");
                var e = myScript.MusicZones[i];

                EditorGUI.BeginChangeCheck();
                e.Position = Handles.PositionHandle(e.Position, Quaternion.identity);

                var cam = SceneView.currentDrawingSceneView.camera;
                Vector3 normal = (cam.transform.position - e.Position).normalized;

                var temp2 = Handles.color;

                if (e.OverrideDistance)
                {
                    var vec = e.Distance;

                    vec.x = Handles.RadiusHandle(Quaternion.identity, e.Position, vec.x, true);
                    vec.x = Mathf.Clamp(vec.x, 0, vec.y);

                    vec.y = Handles.RadiusHandle(Quaternion.identity, e.Position, vec.y, true);
                    vec.y = Mathf.Max(vec.x, vec.y);

                    e.Distance = vec;

                    Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.1f);
                    Handles.DrawSolidDisc(e.Position, normal, e.Distance.x);
                    Handles.color = temp2;
                    Handles.DrawWireDisc(e.Position, normal, e.Distance.y);
                }
                else
                {
                    Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.1f);
                    Handles.DrawSolidDisc(e.Position, normal, MinDistance.floatValue);
                    Handles.color = temp2;
                    Handles.DrawWireDisc(e.Position, normal, MaxDistance.floatValue);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    myScript.MusicZones[i] = e;
                }
            }

            Handles.color = tempColor;
        }

        public void RepaintSceneView()
        {
            if (SceneView.sceneViews.Count > 0) SceneView.lastActiveSceneView.Repaint();
        }

        static bool positionsFoldout = false;

        public void DrawPositionsEditor()
        {
            GUIContent blontent;

            bool previousFoldout = positionsFoldout;
            EditorGUILayout.BeginHorizontal();
            positionsFoldout = EditorCompatability.SpecialFoldouts(positionsFoldout, "Music Zones");
            EditorGUILayout.EndHorizontal();
            if (previousFoldout != positionsFoldout) // Toggle handles in scene view
            {
                RepaintSceneView();
                TryHideTools();
            }

            if (positionsFoldout)
            {
                blontent = new GUIContent("Hide Transform Tool",
                    "If true, hides the transform handle of this gameObject in the scene view so you can " +
                    "better work with the handles of Music Zones.");
                EditorGUI.BeginChangeCheck();
                hideTransformHandle = EditorGUILayout.Toggle(blontent, hideTransformHandle);
                if (EditorGUI.EndChangeCheck())
                {
                    TryHideTools();
                    RepaintSceneView();
                }

                list.Draw();

                for (int i = markedForDeletion.Count - 1; i > -1; i--)
                {
                    MusicZones.DeleteArrayElementAtIndex(markedForDeletion[i]);
                    markedForDeletion.RemoveAt(i);
                    SaveFoldoutState();
                }
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

        public class PositionList
        {
            ReorderableList list;
            AudioMusicZoneEditor amze;

            public PositionList(SerializedObject obj, SerializedProperty prop, AudioMusicZoneEditor _amze)
            {
                list = new(obj, prop, true, false, true, true);
                list.onAddCallback += OnAdd;
                list.drawElementCallback += OnDrawElement;
                list.elementHeightCallback += GetElementHeight;

                amze = _amze;
            }

            public SerializedProperty AddZone()
            {
                amze.foldouts.Add(true);
                amze.SaveFoldoutState();
                return list.serializedProperty.AddAndReturnNewArrayElement();
            }

            private void OnAdd(ReorderableList list)
            {
                GenericMenu menu = new();

                menu.AddItem(new GUIContent("Add Duplicate"), false, AddDuplicate);
                menu.AddItem(new GUIContent("Add at World Origin"), false, AddAtWorldOrigin);
                menu.AddItem(new GUIContent("Add at Local Position"), false, AddAtLocalPosition);

                menu.ShowAsContext();
            }

            void AddDuplicate()
            {
                AddZone();
                amze.serializedObject.ApplyModifiedProperties();
            }

            void AddAtWorldOrigin()
            {
                var e = AddZone();
                e.FindPropertyRelative("Position").vector3Value = Vector3.zero;
                e.FindPropertyRelative("Distance").vector2Value = new Vector2(amze.myScript.MinDistance, amze.myScript.MaxDistance);
                e.serializedObject.ApplyModifiedProperties();
            }

            void AddAtLocalPosition()
            {
                var e = AddZone();
                e.FindPropertyRelative("Position").vector3Value = amze.Transform.position;
                e.FindPropertyRelative("Distance").vector2Value = new Vector2(amze.myScript.MinDistance, amze.myScript.MaxDistance);
                e.serializedObject.ApplyModifiedProperties();
            }

            float GetElementHeight(int index)
            {
                return amze.foldouts[index] ? 80 : 20;
            }

            void OnDrawElement(Rect rect, int i, bool isActive, bool isFocused)
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(i);

                rect.height = 20;
                Rect prevRect = new Rect(rect);
                Rect currentRect = new Rect(prevRect);

                string arrow = amze.foldouts[i] ? "▼" : "▶";

                EditorGUI.BeginChangeCheck();

                currentRect.xMax -= 120;

                amze.foldouts[i] = EditorGUI.Foldout(currentRect, amze.foldouts[i], new GUIContent("    " + arrow + " Zone " + i), true, EditorStyles.boldLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    amze.RepaintSceneView();
                    amze.SaveFoldoutState();
                }

                currentRect.xMax = rect.xMax - 30;
                currentRect.xMin = currentRect.xMax - 80;
                currentRect.y += 2;
                currentRect.height -= 4;

                if (GUI.Button(currentRect, new GUIContent("Duplicate")))
                {
                    amze.foldouts.Insert(i + 1, true);
                    amze.SaveFoldoutState();
                    list.serializedProperty.InsertArrayElementAtIndex(i);
                }

                currentRect.x += currentRect.width;
                currentRect.width = 30;

                JSAMEditorHelper.BeginColourChange(Color.red);
                if (GUI.Button(currentRect, new GUIContent("X")))
                {
                    amze.markedForDeletion.Add(i);
                }
                JSAMEditorHelper.EndColourChange();

                currentRect.xMax = rect.xMax;
                currentRect.xMin = rect.xMin;
                currentRect.height = rect.height - 2;
                currentRect.y = rect.y;

                if (amze.foldouts[i])
                {
                    currentRect.y += rect.height;

                    EditorGUI.PropertyField(currentRect, element.FindPropertyRelative("Position"));

                    currentRect.y += rect.height;

                    SerializedProperty OverrideDistance = element.FindPropertyRelative("OverrideDistance");
                    EditorGUI.PropertyField(currentRect, OverrideDistance);

                    using (new EditorGUI.DisabledGroupScope(!OverrideDistance.boolValue))
                    {
                        SerializedProperty Distance = element.FindPropertyRelative(nameof(Distance));

                        currentRect.y += rect.height;

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(currentRect, Distance, new GUIContent("Min/Max Distance"));
                        if (EditorGUI.EndChangeCheck())
                        {
                            var vec = Distance.vector2Value;
                            vec.x = Mathf.Clamp(vec.x, 0, vec.y);
                            vec.y = Mathf.Max(vec.x, vec.y);
                            Distance.vector2Value = vec;
                        }
                    }
                }
            }

            public void Draw()
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Expand All"))
                {
                    for (int i = 0; i < amze.foldouts.Count; i++)
                    {
                        amze.foldouts[i] = true;
                    }
                    amze.RebuildList();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Collapse All"))
                {
                    for (int i = 0; i < amze.foldouts.Count; i++)
                    {
                        amze.foldouts[i] = false;
                    }
                    amze.RebuildList();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                list.DoLayoutList();
                EditorGUILayout.EndVertical();
            }
        }
    }
}