using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayerMusic : MonoBehaviour
{
    [SerializeField]
    [HideInInspector]
    string music = "None";

    [SerializeField]
    bool useMusicIntro = false;

    [SerializeField]
    bool spatializeSound;

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
        am = AudioManager.GetInstance();

        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (musicFile != null)
        {
            if (spatializeSound)
            {
                am.PlayMusic3D(musicFile, transform);
            }
            else
            {
                am.PlayMusic(musicFile);
            }
        }
        else
        {
            if (spatializeSound)
            {
                am.PlayMusic3D(music, transform, useMusicIntro);
            }
            else
            {
                am.PlayMusic(music, useMusicIntro);
            }
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
            Play();
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

    public AudioClip GetAttachedFile()
    {
        return musicFile;
    }
}
