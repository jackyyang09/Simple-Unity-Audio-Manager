using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using System;

namespace JSAM.JSAMEditor
{
    public enum AudioFileType
    {
        Music,
        Sound
    }

    public class JSAMAudioFileWizard : ScriptableObject
    {
        public List<AudioClip> files = new List<AudioClip>();
        public AudioFileType fileType = AudioFileType.Sound;
        public EditorCompatability.AgnosticGUID<Preset> selectedSoundPreset;
        public EditorCompatability.AgnosticGUID<Preset> selectedMusicPreset;
        public string outputFolder = "Assets";

        static string helperAssetName = "JSAMAudioFileWizard.asset";

        public static string HelperSavePath
        {
            get
            {
                string path = JSAMSettings.Settings.PackagePath;
                path += "/Editor/Preferences";
                return path;
            }
        }

        public static JSAMAudioFileWizard Settings
        {
            get
            {
                var settings = AssetDatabase.LoadAssetAtPath<JSAMAudioFileWizard>(HelperSavePath + "/" + helperAssetName);
                if (settings == null)
                {
                    if (!AssetDatabase.IsValidFolder(HelperSavePath))
                    {
                        JSAMEditorHelper.GenerateFolderStructureAt(HelperSavePath, true);
                    }
                    settings = ScriptableObject.CreateInstance<JSAMAudioFileWizard>();
                    JSAMEditorHelper.CreateAssetSafe(settings, HelperSavePath + "/" + helperAssetName);
                    AssetDatabase.SaveAssets();
                }
                return settings;
            }
        }
    }

    public class JSAMAudioFileWizardEditor : JSAMSerializedEditorWindow<JSAMAudioFileWizard, JSAMAudioFileWizardEditor>
    {
        Vector2 scroll;

        Preset selectedPreset = null;

        List<Preset> soundPresets = new List<Preset>();
        List<Preset> musicPresets = new List<Preset>();
        List<string> soundPresetNames;
        List<string> musicPresetNames;

        int selectedSoundPresetIndex = 0;
        int selectedMusicPresetIndex = 0;

        Dictionary<Preset, PropertyModification> presetToProp = new Dictionary<Preset, PropertyModification>();

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/JSAM/Audio File Wizard")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Window.Show();
            window.SetWindowTitle();
            window.DesignateSerializedProperties();   
        }

        new protected void OnEnable()
        {
            base.OnEnable();
            DesignateSerializedProperties();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            JSAMAssetModificationProcessor.OnJSAMAssetDeleted += OnJSAMAssetDeleted;
            LoadPresets();
        }

        private void OnDisable()
        {
            JSAMAssetModificationProcessor.OnJSAMAssetDeleted -= OnJSAMAssetDeleted;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnJSAMAssetDeleted(string filePath)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filePath);
            if (asset.files.Contains(clip)) markedForDeletion = true;
        }

        bool markedForDeletion;
        void HandleAssetDeletion()
        {
            files.RemoveNullElementsFromArray();
            markedForDeletion = false;

            if (serializedObject.hasModifiedProperties) serializedObject.ApplyModifiedProperties();
        }

        protected override void SetWindowTitle()
        {
            window.titleContent = new GUIContent("Audio File Wizard");
        }

        SerializedProperty files;
        SerializedProperty fileType;
        SerializedProperty selectedSoundPreset;
        SerializedProperty selectedMusicPreset;
        SerializedProperty outputFolder;

        protected override void DesignateSerializedProperties()
        {
            asset = JSAMAudioFileWizard.Settings;
            serializedObject = new SerializedObject(asset);

            files = FindProp(nameof(asset.files));
            fileType = FindProp(nameof(asset.fileType));
            selectedSoundPreset = FindProp(nameof(asset.selectedSoundPreset));
            selectedMusicPreset = FindProp(nameof(asset.selectedMusicPreset));
            outputFolder = FindProp(nameof(asset.outputFolder));

            HandleAssetDeletion();
        }

        private void OnGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (markedForDeletion) HandleAssetDeletion();

            if (Event.current.type == EventType.MouseEnterWindow)
            {
                window.Repaint();
            }

            GUIContent blontent;

            Rect overlay = EditorGUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[] { GUILayout.MinHeight(70), GUILayout.MaxHeight(200) });
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (files.arraySize >= 0)
            {
                for (int i = 0; i < files.arraySize; i++)
                {
                    Rect rect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    var element = files.GetArrayElementAtIndex(i).objectReferenceValue;
                    blontent = new GUIContent(element.name);

                    EditorGUILayout.LabelField(i.ToString(), new GUILayoutOption[] { GUILayout.MaxWidth(25) });

                    EditorGUILayout.LabelField(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(Window.position.width / 3) });

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(files.GetArrayElementAtIndex(i), GUIContent.none);
                    }

                    JSAMEditorHelper.BeginColourChange(Color.red);
                    blontent = new GUIContent("X", "Remove this AudioClip from the list");
                    if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.MaxWidth(25) }))
                    {
                        files.DeleteArrayElementAtIndex(i);
                        files.DeleteArrayElementAtIndex(i);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                    JSAMEditorHelper.EndColourChange();
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            HandleDragAndDrop(overlay);

            blontent = new GUIContent("Clear Files", "Remove all files from the above list. This process can be undone with Edit -> Undo");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                files.ClearArray();
            }

            blontent = new GUIContent("Audio File Type", "The type of Audio File you want to generate from these Audio Clips");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fileType, blontent);
            if (EditorGUI.EndChangeCheck())
            {
                switch (fileType.enumValueIndex)
                {
                    case 0:
                        if (musicPresets.Count > 0) selectedPreset = musicPresets[selectedMusicPresetIndex];
                        break;
                    case 1:
                        if (soundPresets.Count > 0) selectedPreset = soundPresets[selectedSoundPresetIndex];
                        break;
                }
            }

            EditorGUILayout.BeginHorizontal();
            blontent = new GUIContent("Preset to Apply", "Audio File objects created through the Audio File Wizard will be created using this preset as a template.");
            // Music
            if (fileType.enumValueIndex == 0)
            {
                if (musicPresets.Count == 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                        selectedMusicPresetIndex = EditorGUILayout.Popup(blontent, selectedMusicPresetIndex, new string[] { "<None>" });
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    selectedMusicPresetIndex = EditorGUILayout.Popup(blontent, selectedMusicPresetIndex, musicPresetNames.ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        selectedPreset = musicPresets[selectedMusicPresetIndex];
                        selectedMusicPreset.FindPropertyRelative("savedObject").objectReferenceValue = selectedPreset;
                    }
                }
            }
            // Sound
            else
            {
                if (soundPresets.Count == 0)
                {
                    using (new EditorGUI.DisabledScope(true))
                        selectedSoundPresetIndex = EditorGUILayout.Popup(blontent, selectedSoundPresetIndex, new string[] { "<None>" });
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    selectedSoundPresetIndex = EditorGUILayout.Popup(blontent, selectedSoundPresetIndex, soundPresetNames.ToArray());
                    if (EditorGUI.EndChangeCheck())
                    {
                        selectedPreset = soundPresets[selectedSoundPresetIndex];
                        selectedSoundPreset.FindPropertyRelative("savedObject").objectReferenceValue = selectedPreset;
                    }
                }
            }

            blontent = new GUIContent("Refresh Presets", "Reload your assets from your configured preset folder. " +
                "/nPress this button if you recently created a new Audio File preset and don't see it in the drop-down menu. " +
                "/nYou can change your preset folder location in Project Settings -> Audio - JSAM");
            if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
            {
                LoadPresets();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (selectedPreset != null)
            {
                blontent = new GUIContent(presetToProp[selectedPreset].value);
            }
            else
            {
                blontent = new GUIContent("No preset selected");
            }

            var skin = GUI.skin.box.ApplyTextAnchor(TextAnchor.UpperLeft).SetTextColor(GUI.skin.label.normal.textColor);
            EditorGUILayout.LabelField(new GUIContent("Preset Description"), blontent, skin, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });
            EditorGUILayout.EndHorizontal();

            JSAMEditorHelper.RenderSmartFolderProperty(new GUIContent("Output Folder"), outputFolder);

            blontent = new GUIContent("Generate Audio File Objects", 
                "Create audio file objects with the provided Audio Clips according to the selected preset. " +
                "Audio File objects will be saved to the output folder.");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();

            bool cantGenerate = files.arraySize == 0 || selectedPreset == null;
            using (new EditorGUI.DisabledScope(cantGenerate))
            {
                if (GUILayout.Button(blontent, new GUILayoutOption[] { GUILayout.ExpandWidth(false) }))
                {
                    if (fileType.enumValueIndex == 0)
                    {
                        GenerateAudioFileObjects<JSAMMusicFileObject>(selectedPreset);
                    }
                    else
                    {
                        GenerateAudioFileObjects<JSAMSoundFileObject>(selectedPreset);
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnUndoRedoPerformed()
        {
            if (serializedObject != null)
            {
                Window.Repaint();
            }
        }

        void LoadPresets()
        {
            soundPresets = new List<Preset>();
            soundPresetNames = new List<string>();

            musicPresets = new List<Preset>();
            musicPresetNames = new List<string>();

            var presets = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<Preset>(JSAMSettings.Settings.PresetsPath);

            for (int i = 0; i < presets.Count; i++)
            {
                var testMusic = CreateInstance<JSAMMusicFileObject>();
                var testSound = CreateInstance<JSAMSoundFileObject>();

                if (presets[i].CanBeAppliedTo(testMusic))
                {
                    musicPresets.Add(presets[i]);
                    musicPresetNames.Add(presets[i].name);
                }
                else if (presets[i].CanBeAppliedTo(testSound))
                {
                    soundPresets.Add(presets[i]);
                    soundPresetNames.Add(presets[i].name);
                }

                presetToProp[presets[i]] = presets[i].FindProp("presetDescription");
            }

            if (selectedSoundPreset.FindPropertyRelative("savedObject").objectReferenceValue)
            {
                selectedSoundPresetIndex = soundPresets.IndexOf(selectedSoundPreset.FindPropertyRelative("savedObject").objectReferenceValue as Preset);
            }

            if (selectedMusicPreset.FindPropertyRelative("savedObject").objectReferenceValue)
            {
                selectedMusicPresetIndex = musicPresets.IndexOf(selectedMusicPreset.FindPropertyRelative("savedObject").objectReferenceValue as Preset);
            }

            switch (fileType.enumValueIndex)
            {
                case 0:
                    if (musicPresets.Count > 0) selectedPreset = musicPresets[selectedMusicPresetIndex];
                    break;
                case 1:
                    if (soundPresets.Count > 0) selectedPreset = soundPresets[selectedSoundPresetIndex];
                    break;
            }
        }

        void GenerateAudioFileObjects<T>(Preset preset) where T : BaseAudioFileObject
        {
            string folder = outputFolder.stringValue;
            EditorUtility.DisplayProgressBar("Generating Audio File Objects", "Starting...", 0);
            int i = 0;
            for (; i < asset.files.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Generating Audio File Objects (" + i + "/" + asset.files.Count + ")", 
                    asset.files[i].name, (float)i / (float)asset.files.Count)) break;

                var newObject = CreateInstance<T>();
                preset.ApplyTo(newObject);
                SerializedObject newSO = new SerializedObject(newObject);
                var files = newSO.FindProperty("files");
                files.ClearArray();
                files.AddAndReturnNewArrayElement().objectReferenceValue = asset.files[i];
                newSO.ApplyModifiedProperties();
                string finalPath = folder + "/" + asset.files[i].name + ".asset";
                if (!JSAMEditorHelper.CreateAssetSafe(newObject, finalPath))
                {
                    EditorUtility.DisplayDialog("Generation Interrupted", "Failed to create folders/assets!", "OK");
                    break;
                }
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.FocusProjectWindow();
            EditorUtility.DisplayDialog(
                "Generation Finished",
                "Successfully generated " + i + "/" + asset.files.Count + " files!", "OK");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder));
        }

        void HandleDragAndDrop(Rect dragRect)
        {
            string label = files.arraySize == 0 ? "Drag AudioClips here" : "";

            if (JSAMEditorHelper.DragAndDropRegion(dragRect, label, "Release to Add AudioClips"))
            {
                List<AudioClip> duplicates = new List<AudioClip>();

                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    string filePath = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[i]);
                    var clips = JSAMEditorHelper.ImportAssetsOrFoldersAtPath<AudioClip>(filePath);

                    for (int j = 0; j < clips.Count; j++)
                    {
                        if (asset.files.Contains(clips[j]))
                        {
                            duplicates.Add(clips[j]);
                            continue;
                        }

                        files.AddAndReturnNewArrayElement().objectReferenceValue = clips[j];
                    }
                }

                if (duplicates.Count > 0)
                {
                    string multiLine = string.Empty;
                    for (int i = 0; i < duplicates.Count; i++)
                    {
                        multiLine = duplicates[i].name + "/n";
                    }
                    EditorUtility.DisplayDialog("Duplicate Audio Clips!",
                        "The following Audio File Objects are already present in the wizard and have been skipped./n" + multiLine,
                        "OK");
                }

                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                    Window.Repaint();
                }
            }
        }
    }
}