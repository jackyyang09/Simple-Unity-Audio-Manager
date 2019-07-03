using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFile : MonoBehaviour
{
    [Header("Name to use when referring to this sound")]
    [SerializeField]
    protected string audioName = "NEW AUDIO FILE";

    [Header("Attach audio file here to use")]
    [SerializeField]
    protected AudioClip file;

    public AudioClip GetFile()
    {
        return file;
    }

    private void OnValidate()
    {
        gameObject.name = audioName;
    }
}
