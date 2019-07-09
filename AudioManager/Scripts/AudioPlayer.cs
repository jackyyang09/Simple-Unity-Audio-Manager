using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : BaseAudioFeedback
{
    [SerializeField]
    bool playOnStart;

    [SerializeField]
    bool stopOnDisable = true;

    [SerializeField]
    bool stopOnDestroy = true;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        if (playOnStart)
        {
            if (soundFile != null)
            {
                am.PlaySoundOnce(soundFile, transform, priority, pitchShift);
            }
            else
            {
                am.PlaySoundOnce(sound, transform, priority, pitchShift);
            }
        }
    }

    public void Play()
    {
        if (soundFile != null)
        {
            am.PlaySoundOnce(soundFile, transform, priority, pitchShift);
        }
        else
        {
            am.PlaySoundOnce(sound, transform, priority, pitchShift);
        }
    }

    public void Stop()
    {
        if (soundFile != null)
        {
            am.StopSound(soundFile, transform);
        }
        else
        {
            am.StopSound(sound, transform);
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable)
        {
            Stop();
        }
    }

    private void OnDestroy()
    {
        if (stopOnDestroy)
        {
            Stop();
        }
    }
}
