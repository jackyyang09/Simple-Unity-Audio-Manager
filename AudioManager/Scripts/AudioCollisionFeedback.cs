using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCollisionFeedback : BaseAudioFeedback
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (soundFile != null)
        {
            am.PlaySoundOnce(soundFile, transform, priority, AudioManager.UsePitch(pitchShift));
        }
        else
        {
            am.PlaySoundOnce(sound, transform, priority, AudioManager.UsePitch(pitchShift));
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (soundFile != null)
        {
            am.PlaySoundOnce(soundFile, transform, priority, AudioManager.UsePitch(pitchShift));
        }
        else
        {
            am.PlaySoundOnce(sound, transform, priority, AudioManager.UsePitch(pitchShift));
        }
    }
}