using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFileMusic : AudioFile
{
    public enum LoopPointTool
    {
        Slider,
        TimeInput,
        TimeSamplesInput,
        BPMInput//WithBeats,
        //BPMInputWithBars
    }

    [Header("Attach music intro if any at all")]
    [Tooltip("Use this if you separated your music into an intro that plays once and a looping portion")]
    public AudioClip trackIntro;

    public bool useLoopPoints = false;

    /// <summary>
    /// Starting loop point, stored as time for accuracy sake, converted to samples in backend
    /// </summary>
    [HideInInspector] public float loopStart;
    /// <summary>
    /// Ending loop point, stored as time for accuracy sake, converted to samples in backend
    /// </summary>
    [HideInInspector] public float loopEnd;

    [HideInInspector] public LoopPointTool loopPointInputMode;

    [HideInInspector] public int bpm = 120;

    new public AudioClip[] GetFile()
    {
        return new AudioClip[] { file, trackIntro };
    }
}