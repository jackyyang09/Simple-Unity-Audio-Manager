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
    public class AudioPlayerMusic : BaseAudioMusicFeedback
    {
        [Tooltip("Plays the music when this component or the GameObject its attached is first created")]
        [SerializeField]
        protected bool playOnStart = true;

        [Tooltip("Plays the music when this component or the GameObject its attached to is enabled")]
        [SerializeField]
        protected bool playOnEnable = false;

        [Tooltip("Stops the music when this component or the GameObject its attached to is disabled")]
        [SerializeField]
        protected bool stopOnDisable = false;

        [Tooltip("Stops the music when this component or the GameObject its attached to is destroyed")]
        [SerializeField]
        protected bool stopOnDestroy = true;

        AudioSource sourceBeingUsed;

        Coroutine playRoutine;

        // Start is called before the first frame update
        new void Start()
        {
            base.Start();

            if (playOnStart)
            {
                StartCoroutine(PlayDelayed());
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
                            sourceBeingUsed = am.FadeMusic(musicFile, musicFadeInTime, loopMode > LoopMode.NoLooping);
                            break;
                        case TransitionMode.Crossfade:
                            sourceBeingUsed = am.CrossfadeMusicInternal(musicFile, musicFadeInTime, keepPlaybackPosition);
                            break;
                    }
                }
            }
            else
            {
                if (am.IsMusicPlayingInternal(audioObject) && !restartOnReplay) return;

                if (spatializeSound)
                {
                    sourceBeingUsed = am.PlayMusic3DInternal(audioObject, transform, loopMode);
                }
                else
                {
                    switch (transitionMode)
                    {
                        case TransitionMode.None:
                            sourceBeingUsed = am.PlayMusicInternal(audioObject);
                            break;
                        case TransitionMode.FadeFromSilence:
                            sourceBeingUsed = am.FadeMusicInternal(audioObject, musicFadeInTime);
                            break;
                        case TransitionMode.Crossfade:
                            sourceBeingUsed = am.CrossfadeMusicInternal(audioObject, musicFadeInTime, keepPlaybackPosition);
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
                switch (transitionMode)
                {
                    case TransitionMode.None:
                        am.StopMusicInternal(musicFile);
                        break;
                    case TransitionMode.FadeFromSilence:
                    case TransitionMode.Crossfade:
                        am.FadeMusicOutInternal(musicFadeOutTime);
                        break;
                }
            }
            else
            {
                switch (transitionMode)
                {
                    case TransitionMode.None:
                        am.StopMusicInternal(audioObject);
                        break;
                    case TransitionMode.FadeFromSilence:
                    case TransitionMode.Crossfade:
                        am.FadeMusicOutInternal(musicFadeOutTime);
                        break;
                }
            }

            sourceBeingUsed = null;
        }

        /// <summary>
        /// Fades in the current track
        /// </summary>
        public void FadeIn(float time)
        {
            sourceBeingUsed = AudioManager.instance.FadeMusicInInternal(audioObject, time);
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
            if (audioObject == null) DesignateSound();
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
    }
}