using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTriggerFeedback : BaseAudioFeedback
{
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    private void OnTriggerEnter(Collider collision)
    {
        am.PlaySoundOnce(sound, transform, priority, AudioManager.UsePitch(pitchShift));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        am.PlaySoundOnce(sound, transform, priority, AudioManager.UsePitch(pitchShift));
    }
}