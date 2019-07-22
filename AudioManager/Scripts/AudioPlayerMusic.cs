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
    [Tooltip("Play Music in 3D space, will override Music Fading if true")]
    bool spatializeSound;

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

    private void Awake()
    {
        am = AudioManager.GetInstance();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (musicFile != null)
        {
            if (am.IsMusicPlaying(musicFile) && !restartOnReplay) return;

            if (spatializeSound)
            {
                am.PlayMusic3D(musicFile, transform);
            }
            else if (musicFadeTime > 0)
            {
                am.FadeMusic(musicFile, musicFadeTime);
            }
            else
            {
                am.PlayMusic(musicFile);
            }
        }
        else
        {
            if (am.IsMusicPlaying(music) && !restartOnReplay) return;

            if (spatializeSound)
            {
                am.PlayMusic3D(music, transform, useMusicIntro);
            }
            else if (musicFadeTime > 0)
            {
                am.FadeMusic(music, musicFadeTime);
            }
            else
            {
                am.PlayMusic(music, useMusicIntro);
            }
            print("Playing");
        }
    }

    public void Stop()
    {
        if (musicFile != null)
        {
            am.StopSound(musicFile, transform);
        }
        else
        {
            am.StopSound(music, transform);
        }
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
        yield return new WaitForEndOfFrame();
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
