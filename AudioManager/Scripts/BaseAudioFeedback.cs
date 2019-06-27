using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAudioFeedback : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField]
    protected AudioManager.Sound sound;

    [SerializeField]
    protected AudioManager.Priority priority = AudioManager.Priority.Default;

    [SerializeField]
    protected AudioManager.Pitch pitch = AudioManager.Pitch.VeryLow;

    [SerializeField]
    protected bool spatialSound;
    protected Transform sTransform;

    protected AudioManager am;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        am = AudioManager.GetInstance();

        sTransform = (spatialSound) ? transform : null;
    }
}
