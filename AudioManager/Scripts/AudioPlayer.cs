using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : BaseAudioFeedback
{
    [SerializeField]
    bool playOnStart;

    [SerializeField]
    bool playOnEnable;

    [Tooltip("Play sound after this long")]
    [SerializeField]
    float delay = 0;

    [SerializeField]
    bool stopOnDisable = true;

    [SerializeField]
    bool stopOnDestroy = true;

    [SerializeField]
    bool loopSound = false;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        if (playOnStart)
        {
            Play();
        }

        if (am == null) am = AudioManager.instance;
    }

    public void Play()
    {
        Transform t = (spatialSound) ? transform : null;
        if (soundFile != null)
        {
            if (loopSound)
            {
                if (!am.IsSoundLooping(soundFile)) am.PlaySoundLoop(soundFile, transform, spatialSound, priority);
            }
            else am.PlaySoundOnce(soundFile, t, priority, pitchShift, delay);
        }
        else
        {
            if (loopSound)
            {
                if (!am.IsSoundLooping(sound)) am.PlaySoundLoop(sound, transform, spatialSound, priority);
            }
            else am.PlaySoundOnce(sound, t, priority, pitchShift, delay);
        }
    }

    /// <summary>
    /// Stops the sound instantly
    /// </summary>
    public void Stop()
    {
        Transform t = (spatialSound) ? transform : null;
        if (soundFile != null)
        {
            if (!loopSound)
            {
                if (am.IsSoundPlaying(soundFile, t))
                {
                    am.StopSound(soundFile, t);
                }
            }
            else
            {
                if (am.IsSoundLooping(soundFile))
                {
                    am.StopSoundLoop(sound, true, t);
                }
            }
        }
        else
        {
            if (!loopSound)
            {
                if (am.IsSoundPlaying(sound, t))
                {
                    am.StopSound(sound, t);
                }
            }
            else
            {
                if (am.IsSoundLooping(sound))
                {
                    am.StopSoundLoop(sound, true, t);
                }
            }
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            StartCoroutine(PlayOnEnable());
        }

        am = AudioManager.instance;
    }

    IEnumerator PlayOnEnable()
    {
        while (!AudioManager.instance)
        {
            yield return new WaitForEndOfFrame();
        }
        while (!AudioManager.instance.Initialized())
        {
            yield return new WaitForEndOfFrame();
        }

        am = AudioManager.instance;

        Play();
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
