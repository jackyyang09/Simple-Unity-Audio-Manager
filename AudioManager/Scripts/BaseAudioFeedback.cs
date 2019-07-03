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
    
    protected Transform sTransform;

    protected AudioManager am;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        am = AudioManager.GetInstance();

        sTransform = (spatialSound) ? transform : null;
    }

    //public void SetSound(string s)
    //{
    //    sound = s;
    //}
}
