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
    public List<AudioClip> files;

    [HideInInspector]
    public bool useLibrary;

    public AudioClip GetFile()
    {
        return file;
    }

<<<<<<< HEAD
    public List<AudioClip> GetFiles()
=======
    public AudioClip[] GetFiles()
>>>>>>> ffad8f2e20ce18cefdd768d3cad36e1923868b17
    {
        return files;
    }

    public bool UsingLibrary()
    {
        return useLibrary;
    }
}
