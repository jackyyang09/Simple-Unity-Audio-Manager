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
        am.PlaySoundOnce(sound, transform, priority, AudioManager.UsePitch(pitch));
    }
}