using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudioFileMusic))]
public class AudioFileMusicEditor : Editor
{
    bool clipPlaying = false;
    bool clipPaused = false;

    bool mouseDragging = false;
    bool loopClip = false;

    static bool showLoopPointTool = true;

    public override void OnInspectorGUI()
    {
        AudioFileMusic myScript = (AudioFileMusic)target;

        EditorGUILayout.LabelField("The name of this gameObject will be used to refer to audio in script");

        if (myScript.GetFile() == null)
        {
            EditorGUILayout.HelpBox("Error! Add an audio file before running!", MessageType.Error);
        }
        if (myScript.name.Contains("NEW AUDIO FILE") || myScript.name.Equals("None") || myScript.name.Equals("GameObject"))
        {
            EditorGUILayout.HelpBox("Warning! Change the name of the gameObject to something different or things will break!", MessageType.Warning);
        }

        DrawDefaultInspector();

        if (myScript.useLoopPoints)
        {
            showLoopPointTool = EditorGUILayout.Foldout(showLoopPointTool, "Loop Point Tools");
            if (showLoopPointTool)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Customize where music will loop between", EditorStyles.boldLabel);
                int option = (int)myScript.loopPointInputMode;
                option = EditorGUILayout.Popup("Loop Point Setting Mode", option, System.Enum.GetNames(typeof(AudioFileMusic.LoopPointTool)));
                if (option != (int)myScript.loopPointInputMode)
                {
                    Undo.RecordObject(myScript, "Modified loop point tool");
                    myScript.loopPointInputMode = (AudioFileMusic.LoopPointTool)option;
                    EditorUtility.SetDirty(myScript);
                }

                AudioClip music = myScript.GetFile();
                float loopStart = myScript.loopStart;
                float loopEnd = myScript.loopEnd;

                DrawPlaybackTool(music);

                switch (myScript.loopPointInputMode)
                {
                    case AudioFileMusic.LoopPointTool.Slider:
                        GUILayout.Label("Song Duration Samples: " + music.samples);
                        EditorGUILayout.MinMaxSlider(ref loopStart, ref loopEnd, 0, music.length);

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Loop Point Start: " + TimeToString(loopStart));
                        GUILayout.Label("Loop Point Start (Samples): " + myScript.loopStart * music.frequency);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Loop Point End: " + TimeToString(loopEnd));
                        GUILayout.Label("Loop Point End (Samples): " + myScript.loopEnd * music.frequency);
                        GUILayout.EndHorizontal();
                        break;
                    case AudioFileMusic.LoopPointTool.TimeInput:
                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();
                        float theTime = loopStart * 1000f;
                        GUILayout.Label("Loop Point Start:");
                        int minutes = EditorGUILayout.IntField((int)(theTime / 60000f));
                        GUILayout.Label(":");
                        int seconds = EditorGUILayout.IntField((int)(theTime % 60000) / 1000);
                        GUILayout.Label(":");
                        int milliseconds = EditorGUILayout.IntField((int)(theTime % 60000) % 1000);
                        loopStart = (float)minutes * 60f + (float)seconds + (float)milliseconds / 1000f;
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        theTime = loopEnd * 1000f;
                        GUILayout.Label("Loop Point End:  ");
                        minutes = EditorGUILayout.IntField((int)theTime / 60000);
                        GUILayout.Label(":");
                        seconds = EditorGUILayout.IntField((int)(theTime % 60000) / 1000);
                        GUILayout.Label(":");
                        milliseconds = EditorGUILayout.IntField((int)(theTime % 60000) % 1000);
                        loopEnd = (float)minutes * 60f + (float)seconds + (float)milliseconds / 1000f;
                        GUILayout.EndHorizontal();
                        break;
                    case AudioFileMusic.LoopPointTool.TimeSamplesInput:
                        GUILayout.Label("Song Duration (Samples): " + music.samples);
                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Loop Point Start:");
                        float samplesStart = EditorGUILayout.FloatField(myScript.loopStart * music.frequency);
                        GUILayout.EndHorizontal();
                        loopStart = samplesStart / music.frequency;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Loop Point End:  ");
                        float samplesEnd = EditorGUILayout.FloatField(myScript.loopEnd * music.frequency);
                        GUILayout.EndHorizontal();
                        loopEnd = samplesEnd / music.frequency;
                        break;
                    case AudioFileMusic.LoopPointTool.BPMInput/*WithBeats*/:
                        GUILayout.BeginHorizontal();
                        myScript.bpm = EditorGUILayout.IntField("Song BPM: ", myScript.bpm);

                        GUILayout.Label("Song Duration (Beats): " + music.length / (60f / myScript.bpm));

                        GUILayout.EndHorizontal();
                        EditorGUILayout.Space();

                        float startBeat = loopStart / (60f / (float)myScript.bpm);
                        startBeat = EditorGUILayout.FloatField("Starting Beat:", startBeat);

                        float endBeat = loopEnd / (60f / (float)myScript.bpm);
                        endBeat = EditorGUILayout.FloatField("Ending Beat:", endBeat);

                        loopStart = (float)startBeat * 60f / (float)myScript.bpm;
                        loopEnd = (float)endBeat * 60f / (float)myScript.bpm;
                        break;
                        //case AudioFileMusic.LoopPointTool.BPMInputWithBars:
                        //    GUILayout.BeginHorizontal();
                        //    GUILayout.Label("Song Duration: " + TimeToString(music.length));
                        //    myScript.bpm = EditorGUILayout.IntField("Song BPM: ", myScript.bpm);
                        //    GUILayout.EndHorizontal();
                        //
                        //    int startBar = (int)(loopStart / (60f / (float)myScript.bpm));
                        //    startBar = EditorGUILayout.IntField("Starting Bar:", startBar);
                        //
                        //    int endBar = (int)(loopEnd / (60f / (float)myScript.bpm));
                        //    endBar = EditorGUILayout.IntField("Ending Bar:", endBar);
                        //
                        //    loopStart = startBar * 60f / myScript.bpm;
                        //    loopEnd = endBar * 60f / myScript.bpm;
                        //    break;
                }

                GUIContent buttonText = new GUIContent("Reset Loop Points", "Click to set loop points to the start and end of the track.");
                if (GUILayout.Button(buttonText))
                {
                    loopStart = 0;
                    loopEnd = music.length;
                }

                if (myScript.loopStart != loopStart || myScript.loopEnd != loopEnd)
                {
                    Undo.RecordObject(myScript, "Modified loop point properties");
                    myScript.loopStart = Mathf.Clamp(loopStart, 0, music.length);
                    myScript.loopEnd = Mathf.Clamp(loopEnd, 0, Mathf.Ceil(music.length));
                    EditorUtility.SetDirty(myScript);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    public static string TimeToString(float time)
    {
        time *= 1000;
        int minutes = (int)time / 60000;
        int seconds = (int)time / 1000 - 60 * minutes;
        int milliseconds = (int)time - minutes * 60000 - 1000 * seconds;
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    /// <summary>
    /// Draws a playback 
    /// </summary>
    /// <param name="music"></param>
    public void DrawPlaybackTool(AudioClip music)
    {
        Rect progressRect = ProgressBar((float)AudioUtil.GetClipSamplePosition(music) / (float)AudioUtil.GetSampleCount(music), GetInfoString());
        EditorGUILayout.BeginHorizontal();

        Event evt = Event.current;

        if (evt.isMouse)
        {
            switch (evt.type)
            {
                case EventType.MouseMove:
                    break;
                case EventType.MouseUp:
                    mouseDragging = false;
                    break;
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (evt.type == EventType.MouseDown && !mouseDragging)
                    {
                        if (evt.mousePosition.y > progressRect.yMin && evt.mousePosition.y < progressRect.yMax)
                        {
                            mouseDragging = true;
                        }
                    }
                    if (!mouseDragging) break;
                    float newProgress = Mathf.InverseLerp(progressRect.xMin, progressRect.xMax, evt.mousePosition.x);
                    AudioUtil.SetClipSamplePosition(music, (int)(newProgress * music.length * music.frequency));
                    break;
            }
        }

        clipPlaying = AudioUtil.IsClipPlaying(music);

        GUIContent buttonIcon = (clipPlaying) ? s_PlayIcons[1] : s_PlayIcons[0];
        //string buttonText = (clipPlaying) ? "■" : "►";
        if (GUILayout.Button(buttonIcon))
        {
            clipPlaying = !clipPlaying;
            if (clipPlaying)
            {
                AudioUtil.PlayClip(music, AudioUtil.GetClipSamplePosition(music), loopClip);
            }
            else
            {
                AudioUtil.StopClip(music);
                clipPaused = false;
            }
        }
        GUIContent theText = new GUIContent(s_PlayIcons[0]);
        theText.text += " / ||";
        if (GUILayout.Button(theText))
        {
            clipPaused = !clipPaused;
            if (clipPaused)
            {
                AudioUtil.PauseClip(music);
            }
            else
            {
                AudioUtil.ResumeClip(music);
            }
        }
        buttonIcon = (loopClip) ? s_LoopIcons[1] : s_LoopIcons[0];
        if (GUILayout.Button(buttonIcon))
        {
            loopClip = !loopClip;
        }
        EditorGUILayout.LabelField(TimeToString(AudioUtil.GetClipPosition(music)) + " / " + (TimeToString(music.length)));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// Conveniently draws a progress bar
    /// Referenced from the official Unity documentation
    /// https://docs.unity3d.com/ScriptReference/Editor.html
    /// </summary>
    /// <param name="value"></param>
    /// <param name="label"></param>
    /// <returns></returns>
    Rect ProgressBar(float value, string label)
    {
        EditorGUILayout.Space();

        // Get a rect for the progress bar using the same margins as a textfield:
        Rect rect = GUILayoutUtility.GetRect(64, 64, "TextBox");

        AudioClip music = ((AudioFileMusic)target).GetFile();

        Texture2D waveformTexture = PaintWaveformSpectrum(music, (int)rect.width, (int)rect.height, new Color(1, 0.5f, 0));
        if (waveformTexture != null)    
            GUI.DrawTexture(rect, waveformTexture);

        if (clipPlaying)
        {
            Rect progressRect = new Rect(rect);
            progressRect.width *= value;
            progressRect.xMin = progressRect.xMax - 1;
            GUI.Box(progressRect, "", "SelectionRect");
        }

        //EditorGUI.ProgressBar(rect, value, label);

        EditorGUILayout.Space();

        return rect;
    }

    void OnEnable() {
        if (s_DefaultIcon == null) Init();
        AudioUtil.StopAllClips();
        EditorApplication.update += Update;
    }
    void OnDisable() {
        AudioUtil.StopAllClips();
        EditorApplication.update -= Update;
    }

    void Update()
    {
        if (clipPlaying && !clipPaused)
        {
            Repaint();

            AudioFileMusic myScript = (AudioFileMusic)target;
            AudioClip music = myScript.GetFile();
            float clipPos = (float)AudioUtil.GetClipSamplePosition(music) / (float)music.frequency;

            AudioUtil.LoopClip(music, loopClip);
            if (clipPos < myScript.loopStart || clipPos > myScript.loopEnd)
            {
                AudioUtil.SetClipSamplePosition(music, Mathf.CeilToInt(myScript.loopStart * music.frequency));
            }
        }
    }


    /// <summary>
    /// Code from these gents
    /// https://answers.unity.com/questions/189886/displaying-an-audio-waveform-in-the-editor.html
    /// </summary>
    public Texture2D PaintWaveformSpectrum(AudioClip audio, int width, int height, Color col)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float[] samples = new float[audio.samples];
        float[] waveform = new float[width];
        audio.GetData(samples, 0);

        int packSize = (audio.samples / width) + 1;
        int s = 0;
        for (int i = 0; i < audio.samples; i += packSize)
        {
            waveform[s] = Mathf.Abs(samples[i]);
            s++;
        }

        AudioFileMusic myScript = (AudioFileMusic)target;

        for (int x = 0; x < width; x++)
        {
            // Here we limit the scope of the area based on loop points
            if (x < waveform.Length * (myScript.loopStart / audio.length) || x > waveform.Length * (myScript.loopEnd / audio.length))
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, Color.black);
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, new Color(0.3f, 0.3f, 0.3f));
                }
            }
        }

        for (int x = 0; x < waveform.Length; x++)
        {
            for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
            {
                Color currentPixelColour = tex.GetPixel(x, (height / 2) + y);

                tex.SetPixel(x, (height / 2) + y, currentPixelColour + col * 0.75f);

                currentPixelColour = tex.GetPixel(x, (height / 2) - y);
                tex.SetPixel(x, (height / 2) - y, currentPixelColour + col * 0.75f);
            }
        }
        tex.Apply();

        return tex;
    }

    // Unity C# reference source
    // Copyright (c) Unity Technologies. For terms of use, see
    // https://unity3d.com/legal/licenses/Unity_Reference_Only_License

    private PreviewRenderUtility m_PreviewUtility;

    // Any number of AudioClip inspectors can be docked in addition to the object browser, and they are all showing and modifying the same shared state.
    static AudioFileMusicEditor m_PlayingInspector;
    static AudioClip m_PlayingClip;
    static bool playing { get { return m_PlayingClip != null && AudioUtil.IsClipPlaying(m_PlayingClip); } }
    static bool m_bAutoPlay;
    static bool m_bLoop;

    Vector2 m_Position = Vector2.zero;
    Rect m_wantedRect;

    static GUIStyle s_PreButton;

    static GUIContent[] s_PlayIcons = { null, null };
    static GUIContent[] s_AutoPlayIcons = { null, null };
    static GUIContent[] s_LoopIcons = { null, null };

    static Texture2D s_DefaultIcon;

    static void Init()
    {
        //if (s_PreButton != null)
        //    return;
        //s_PreButton = "preButton";

        m_bAutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);

        s_AutoPlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioAutoPlayOff", "Turn Auto Play on");
        s_AutoPlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioAutoPlayOn", "Turn Auto Play off");
        s_PlayIcons[0] = EditorGUIUtility.TrIconContent("preAudioPlayOff", "Play");
        s_PlayIcons[1] = EditorGUIUtility.TrIconContent("preAudioPlayOn", "Stop");
        s_LoopIcons[0] = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Loop on");
        s_LoopIcons[1] = EditorGUIUtility.TrIconContent("preAudioLoopOn", "Loop off");

        //s_DefaultIcon = EditorGUIUtility.LoadIcon("Profiler.Audio");
    }

    //public void OnDisable()
    //{
    //    // This check is necessary because the order of OnEnable/OnDisable varies depending on whether the inspector is embedded in the project browser or object selector.
    //    if (m_PlayingInspector == this)
    //    {
    //        AudioUtil.StopAllClips();
    //        m_PlayingClip = null;
    //    }
    //
    //    EditorPrefs.SetBool("AutoPlayAudio", m_bAutoPlay);
    //}
    //
    //public void OnEnable()
    //{
    //    AudioUtil.StopAllClips();
    //    m_PlayingClip = null;
    //    m_PlayingInspector = this;
    //
    //    m_bAutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);
    //}
    // Passing in clip and importer separately as we're not completely done with the asset setup at the time we're asked to generate the preview.

    public void OnDestroy()
    {
        if (m_PreviewUtility != null)
        {
            m_PreviewUtility.Cleanup();
            m_PreviewUtility = null;
        }
    }
}