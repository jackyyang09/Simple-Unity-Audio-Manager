using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFile : MonoBehaviour
{
    [Header("Attach audio file here to use")]
    [SerializeField]
    protected AudioClip file;

    [Header("Attach audio files here to use")]
    [SerializeField]
    protected AudioClip[] files;

    [SerializeField]
    private bool useLibrary;

    public AudioClip GetFile()
    {
        return file;
    }

    public bool UsingLibrary()
    {
        return useLibrary;
    }
}
