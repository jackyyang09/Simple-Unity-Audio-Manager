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
        [Tooltip("Behaviour to trigger when the object this is attached to is created")]
        [SerializeField]
        protected AudioPlaybackBehaviour onStart = AudioPlaybackBehaviour.Play;

        [Tooltip("Behaviour to trigger when the object this is attached to is enabled or when the object is created")]
        [SerializeField]
        protected AudioPlaybackBehaviour onEnable = AudioPlaybackBehaviour.None;

        [Tooltip("Behaviour to trigger when the object this is attached to is destroyed or set to in-active")]
        [SerializeField]
        protected AudioPlaybackBehaviour onDisable = AudioPlaybackBehaviour.None;

        [Tooltip("Behaviour to trigger when the object this is attached to is destroyed")]
        [SerializeField]
        protected AudioPlaybackBehaviour onDestroy = AudioPlaybackBehaviour.Stop;

        AudioSource sourceBeingUsed;

        Coroutine playRoutine;

        // Start is called before the first frame update
        new void Start()
        {
            base.Start();

            switch (onStart)
            {
                case AudioPlaybackBehaviour.Play:
                    StartCoroutine(PlayDelayed());
                    break;
                case AudioPlaybackBehaviour.Stop:
                    Stop();
                    break;
            }
        }

        public void Play()
        {
            AudioManager am = AudioManager.instance;

            if (am == null) return;
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
                        sourceBeingUsed = am.PlayMusicInternal(music);
                        break;
                    case TransitionMode.FadeFromSilence:
                        sourceBeingUsed = am.FadeMusicInternal(music, musicFadeInTime);
                        break;
                    case TransitionMode.Crossfade:
                        sourceBeingUsed = am.CrossfadeMusicInternal(music, musicFadeInTime, keepPlaybackPosition);
                        break;
                }
            }
        }

        public void Stop()
        {
            AudioManager am = AudioManager.instance;

            if (am != null)
            {
                switch (transitionMode)
                {
                    case TransitionMode.None:
                        am.StopMusicInternal(music);
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
            switch (onEnable)
            {
                case AudioPlaybackBehaviour.Play:
                    if (playRoutine != null) StopCoroutine(playRoutine);
                    playRoutine = StartCoroutine(PlayDelayed());
                    break;
                case AudioPlaybackBehaviour.Stop:
                    Stop();
                    break;
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
            switch (onDisable)
            {
                case AudioPlaybackBehaviour.Play:
                    Play();
                    break;
                case AudioPlaybackBehaviour.Stop:
                    Stop();
                    break;
            }
        }

        private void OnDestroy()
        {
            switch (onDestroy)
            {
                case AudioPlaybackBehaviour.Play:
                    Play();
                    break;
                case AudioPlaybackBehaviour.Stop:
                    Stop();
                    break;
            }
        }

        public AudioSource GetAudioSource()
        {
            return sourceBeingUsed;
        }
    }
}