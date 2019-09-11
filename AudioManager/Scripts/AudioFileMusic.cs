using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFileMusic : AudioFile
{
    [Header("Attach music intro if any at all")]
    public AudioClip trackIntro;

    [HideInInspector] public float loopStart;

    new public AudioClip[] GetFile()
    {
        return new AudioClip[] { file, trackIntro };
    }
}
