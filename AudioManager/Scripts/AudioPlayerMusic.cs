using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayerMusic : MonoBehaviour
{
    [SerializeField]
    [HideInInspector]
    string music = "None";

    [SerializeField]
    [Tooltip("Start playing the music intro before playing the music loop, only works if the musicFile field is blank")]
    bool useMusicIntro = false;

    [SerializeField]
    [Tooltip("Enables looping using the loop points defined in the music file")]
    bool useLoopPoints = false;

    [SerializeField]
    [Tooltip("Play Music in 3D space, will override Music Fading if true")]
    bool spatializeSound;

    [SerializeField]
    [Tooltip("Play music starting from previous track's playback position, only works when musicFadeTime is greater than 0")]
    bool keepPlaybackPosition;

    [SerializeField]
    [Tooltip("If true, will restart playback of this music if the same music is being played right now")]
    bool restartOnReplay = false;

    [SerializeField]
    float musicFadeTime = 0;

    [SerializeField]
    bool loopMusic;

    [SerializeField]
    bool playOnStart;

    [SerializeField]
    bool playOnEnable;

    [SerializeField]
    bool stopOnDisable = true;

    [SerializeField]
    bool stopOnDestroy = true;

    [Tooltip("Overrides the \"Music\" parameter with an AudioClip if not null")]
    [SerializeField]
    AudioClip musicFile;

    AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        am = AudioManager.instance;

        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        am = AudioManager.instance;

        if (musicFile != null)
        {
            if (am.IsMusicPlaying(musicFile) && !restartOnReplay) return;

            if (spatializeSound)
            {
                am.PlayMusic3D(musicFile, transform, loopMusic);
            }
            else if (musicFadeTime > 0)
            {
                am.CrossfadeMusic(musicFile, musicFadeTime, keepPlaybackPosition);
            }
            else
            {
                am.PlayMusic(musicFile, loopMusic);
            }
        }
        else
        {
            if (am.IsMusicPlaying(music) && !restartOnReplay) return;

            if (spatializeSound)
            {
                am.PlayMusic3D(music, transform, loopMusic, useMusicIntro, useLoopPoints);
            }
            else if (musicFadeTime > 0)
            {
                am.CrossfadeMusic(music, musicFadeTime, useMusicIntro, useLoopPoints, keepPlaybackPosition);
            }
            else
            {
                am.PlayMusic(music, loopMusic, useMusicIntro, useLoopPoints);
            }
        }
    }

    public void Stop()
    {
        if (musicFile != null)
        {
            am.StopMusic(musicFile);
        }
        else
        {
            am.StopMusic(music);
        }
    }

    /// <summary>
    /// Fades in the current track
    /// </summary>
    public void FadeIn(float time)
    {
        am.FadeMusicIn(music, time, useMusicIntro);
    }

    /// <summary>
    /// Fades out the current track
    /// </summary>
    /// <param name="time"></param>
    public void FadeOut(float time)
    {
        am.FadeMusicOut(time);
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            StartCoroutine(PlayOnEnable());
        }
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

    public AudioClip GetAttachedFile()
    {
        return musicFile;
    }
}
