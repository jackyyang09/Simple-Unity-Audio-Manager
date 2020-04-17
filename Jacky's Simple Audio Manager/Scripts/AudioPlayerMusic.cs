using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class AudioPlayerMusic : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        int music = 0;

        [Header("Music Settings")]

        [SerializeField]
        [Tooltip("Play Music in 3D space, will override Music Fading if true")]
        bool spatializeSound;

        [SerializeField]
        [Tooltip("Play music starting from previous track's playback position, only works when Music Fade Time is greater than 0")]
        bool keepPlaybackPosition;

        [SerializeField]
        [Tooltip("If true, will restart playback of this music if the same music is being played right now")]
        bool restartOnReplay = false;

        [SerializeField]
        float musicFadeTime = 0;

        [Tooltip("Standard looping disregards all loop point logic, loop point use is enabled in the audio music file")]
        [SerializeField]
        LoopMode loopMode = LoopMode.Looping;

        [Tooltip("Plays the music when this component or the GameObject its attached is first created")]
        [SerializeField]
        bool playOnStart;

        [Tooltip("Plays the music when this component or the GameObject its attached to is enabled")]
        [SerializeField]
        bool playOnEnable;

        [Tooltip("Stops the music when this component or the GameObject its attached to is disabled")]
        [SerializeField]
        bool stopOnDisable = false;

        [Tooltip("Stops the music when this component or the GameObject its attached to is destroyed")]
        [SerializeField]
        bool stopOnDestroy = false;

        [Tooltip("Overrides the \"Music\" parameter with an AudioClip if not null")]
        [SerializeField]
        AudioClip musicFile;

        Coroutine playRoutine;

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
            AudioManager am = AudioManager.instance;

            if (musicFile != null)
            {
                if (am.IsMusicPlaying(musicFile) && !restartOnReplay) return;

                if (spatializeSound)
                {
                    am.PlayMusic3D(musicFile, transform, loopMode > LoopMode.NoLooping);
                }
                else if (musicFadeTime > 0)
                {
                    am.CrossfadeMusic(musicFile, musicFadeTime, keepPlaybackPosition);
                }
                else
                {
                    am.PlayMusic(musicFile, loopMode > LoopMode.NoLooping);
                }
            }
            else
            {
                if (am.IsMusicPlaying(music) && !restartOnReplay) return;

                if (spatializeSound)
                {
                    am.PlayMusic3D(music, transform, loopMode);
                }
                else if (musicFadeTime > 0)
                {
                    am.CrossfadeMusic(music, musicFadeTime, LoopMode.LoopWithLoopPoints, keepPlaybackPosition);
                }
                else
                {
                    am.PlayMusic(music, loopMode);
                }
            }
        }

        public void Stop()
        {
            AudioManager am = AudioManager.instance;

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
            AudioManager.instance.FadeMusicIn(music, time);
        }

        /// <summary>
        /// Fades out the current track
        /// </summary>
        /// <param name="time"></param>
        public void FadeOut(float time)
        {
            AudioManager.instance.FadeMusicOut(time);
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                if (playRoutine != null) StopCoroutine(playRoutine);
                playRoutine = StartCoroutine(PlayDelayed());
            }
        }

        IEnumerator PlayDelayed()
        {
            while (!AudioManager.instance)
            {
                yield return new WaitForEndOfFrame();
            }
            while (!AudioManager.instance.Initialized())
            {
                yield return new WaitForEndOfFrame();
            }

            playRoutine = null;
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
}