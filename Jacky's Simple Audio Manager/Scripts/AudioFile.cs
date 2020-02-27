using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFile : MonoBehaviour
{
    [Header("Attach audio file here to use")]
    [SerializeField]
    public AudioClip file;

    [Header("Attach audio files here to use")]
    [SerializeField]
    public List<AudioClip> files = new List<AudioClip>();

    [HideInInspector]
    public bool useLibrary;

    public AudioClip GetFile()
    {
        return file;
    }

    public List<AudioClip> GetFiles()
    {
        return files;
    }

    public bool HasAudioClip(AudioClip a)
    {
        return file == a || files.Contains(a);
    }

    public bool UsingLibrary()
    {
        return useLibrary;
    }
}
