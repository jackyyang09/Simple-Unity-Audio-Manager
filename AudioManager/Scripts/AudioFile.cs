using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFile : MonoBehaviour
{
    [Header("Attach audio file here to use")]
    [SerializeField]
    protected AudioClip file;

    public AudioClip GetFile()
    {
        return file;
    }
}
