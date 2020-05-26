using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public enum TransitionMode
    {
        None,
        FadeFromSilence,
        Crossfade
    }

    [AddComponentMenu("AudioManager/Audio Player Music")]
    public class AudioPlayerMusic : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        int music = 0;

        [Tooltip("Play Music in 3D space, will override Music Fading if true")]
        [SerializeField]
        bool spatializeSound;

        [Tooltip("Adds a transition effect for playing this music")]
        [SerializeField]
        TransitionMode transitionMode = TransitionMode.None;

        [SerializeField]
        [Tooltip("Play music starting from previous track's playback position, only works when Music Fade Time is greater than 0")]
        bool keepPlaybackPosition = true;

        [SerializeField]
        [Tooltip("If true, playing this audio file while its currently playing will restart playback from the start point. Otherwise, the call to Play the track will be ignored if it's currently playing.")]
        bool restartOnReplay = false;

        [SerializeField]
        float musicFadeTime = 0;

        [Tooltip("Standard looping disregards all loop point logic, loop point use is enabled in the audio music file")]
        [SerializeField]
        LoopMode loopMode = LoopMode.Looping;

        [Tooltip("Plays the music when this component or the GameObject its attached is first created")]
        [SerializeField]
        bool playOnStart = true;

        [Tooltip("Plays the music when this component or the GameObject its attached to is enabled")]
        [SerializeField]
        bool playOnEnable = false;

        [Tooltip("Stops the music when this component or the GameObject its attached to is disabled")]
        [SerializeField]
        bool stopOnDisable = false;

        [Tooltip("Stops the music when this component or the GameObject its attached to is destroyed")]
        [SerializeField]
        bool stopOnDestroy = true;

        [Tooltip("Overrides the \"Music\" parameter with an AudioClip if not null")]
        [SerializeField]
        AudioClip musicFile = null;
        AudioFileMusicObject audioObject;

        AudioSource sourceBeingUsed;

        Coroutine playRoutine;

        // Start is called before the first frame update
        void Start()
        {
            if (musicFile == null)
            {
                audioObject = AudioManager.instance.GetMusicLibrary()[music];

                loopMode = audioObject.loopMode;
                spatializeSound = audioObject.spatialize;
            }

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
                if (am.IsMusicPlayingInternal(musicFile) && !restartOnReplay) return;

                if (spatializeSound)
                {
                    sourceBeingUsed = am.PlayMusic3DInternal(musicFile, transform, loopMode > LoopMode.NoLooping);
                }
                else
                {
                    switch (transitionMode)
                    {
                        case TransitionMode.None:
                            sourceBeingUsed = am.PlayMusicInternal(musicFile, loopMode > LoopMode.NoLooping);
                            break;
                        case TransitionMode.FadeFromSilence:
                            sourceBeingUsed = am.FadeMusic(musicFile, musicFadeTime, loopMode > LoopMode.NoLooping);
                            break;
                        case TransitionMode.Crossfade:
                            sourceBeingUsed = am.CrossfadeMusicInternal(musicFile, musicFadeTime, keepPlaybackPosition);
                            break;
                    }
                }
            }
            else
            {
                if (am.IsMusicPlayingInternal(music) && !restartOnReplay) return;

                if (spatializeSound)
                {
                    sourceBeingUsed = am.PlayMusic3DInternal(music, transform, loopMode);
                }
                else
                {
                    switch (transitionMode)
                    {
                        case TransitionMode.None:
                            sourceBeingUsed = am.PlayMusicInternal(music, loopMode);
                            break;
                        case TransitionMode.FadeFromSilence:
                            sourceBeingUsed = am.FadeMusicInternal(music, musicFadeTime, loopMode);
                            break;
                        case TransitionMode.Crossfade:
                            sourceBeingUsed = am.CrossfadeMusicInternal(music, musicFadeTime, loopMode, keepPlaybackPosition);
                            break;
                    }
                }
            }
        }

        public void Stop()
        {
            AudioManager am = AudioManager.instance;

            if (musicFile != null)
            {
                am.StopMusicInternal(musicFile);
            }
            else
            {
                am.StopMusicInternal(music);
            }

            sourceBeingUsed = null;
        }

        /// <summary>
        /// Fades in the current track
        /// </summary>
        public void FadeIn(float time)
        {
            sourceBeingUsed = AudioManager.instance.FadeMusicInInternal(music, time);
        }

        /// <summary>
        /// Fades out the current track
        /// </summary>
        /// <param name="time"></param>
        public void FadeOut(float time)
        {
            AudioManager.instance.FadeMusicOutInternal(time);
            sourceBeingUsed = null;
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

        public AudioSource GetAudioSource()
        {
            return sourceBeingUsed;
        }

        public AudioClip GetAttachedFile()
        {
            return musicFile;
        }

        public TransitionMode GetTransitionMode()
        {
            return transitionMode;
        }
    }
}