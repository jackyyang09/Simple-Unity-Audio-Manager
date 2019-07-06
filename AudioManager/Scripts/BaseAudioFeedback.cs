using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAudioFeedback : MonoBehaviour
{
    [Header("Sound Settings")]

    [SerializeField]
    protected bool spatialSound = true;

    [HideInInspector]
    public string sound;

    [SerializeField]
    protected AudioManager.Priority priority = AudioManager.Priority.Default;

    [SerializeField]
    protected AudioManager.Pitch pitchShift = AudioManager.Pitch.VeryLow;

    [Header("Set your sound settings here")]

    [SerializeField]
    [Tooltip("Overrides the \"Sound\" parameter with an AudioClip if not null")]
    protected AudioClip soundFile;
    
    protected Transform sTransform;

    protected AudioManager am;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        am = AudioManager.GetInstance();

        sTransform = (spatialSound) ? transform : null;
    }

    public AudioClip GetAttachedSound()
    {
        return soundFile;
    }

    //public void SetSound(string s)
    //{
    //    sound = s;
    //}
}
