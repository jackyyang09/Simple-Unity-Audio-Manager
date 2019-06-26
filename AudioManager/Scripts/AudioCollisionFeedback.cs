using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCollisionFeedback : MonoBehaviour
{
    [SerializeField]
    AudioManager.Sound sound;

    [SerializeField]
    AudioManager.Priority priority = AudioManager.Priority.Default;

    [SerializeField]
    AudioManager.Pitch pitch = AudioManager.Pitch.VeryLow;

    AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        am = AudioManager.GetInstance();
    }

    private void OnCollisionEnter(Collision collision)
    {
        am.PlaySoundOnce(sound, transform, priority, AudioManager.UsePitch(pitch));
    }
}
