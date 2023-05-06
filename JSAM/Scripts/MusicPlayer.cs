using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("AudioManager/Music Player")]
    public class MusicPlayer : BaseAudioMusicFeedback
    {
        public enum FadeBehaviour
        {
            None,
            AdditiveFadeIn,
            CrossFadeIn,
            FadeOutAndFadeIn
        }

        [Tooltip("Behaviour to trigger when the object this is attached to is created")]
        [SerializeField] protected AudioPlaybackBehaviour onStart = AudioPlaybackBehaviour.Play;

        [Tooltip("Behaviour to trigger when the object this is attached to is enabled or when the object is created")]
        [SerializeField] protected AudioPlaybackBehaviour onEnable = AudioPlaybackBehaviour.None;

        [Tooltip("Behaviour to trigger when the object this is attached to is destroyed or set to in-active")]
        [SerializeField] protected AudioPlaybackBehaviour onDisable = AudioPlaybackBehaviour.None;

        [Tooltip("Behaviour to trigger when the object this is attached to is destroyed")]
        [SerializeField] protected AudioPlaybackBehaviour onDestroy = AudioPlaybackBehaviour.Stop;

        [Tooltip(
            "Fade behaviour to use when music is played back.\n" +
            "None - No fading\n" +
            "AdditiveFadeIn - Music fades in with no regard for currently playing music\n" +
            "CrossFadeIn - Fades out current Main Music while fading in this music\n" +
            "FadeOutAndFadeIn - Fades out current Main Music, and only after it's done fading, fade in this music")]
        [SerializeField] protected FadeBehaviour fadeBehaviour = FadeBehaviour.None;

        [Tooltip("Total time of the fade process")]
        [SerializeField] float fadeTime;

        MusicChannelHelper helper;
        public MusicChannelHelper MusicHelper { get { return helper; } }

        Coroutine playRoutine;

        // Start is called before the first frame update
        void Start()
        {
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
            if (AudioManager.IsMusicPlaying(audio) && !restartOnReplay) return;

            if (keepPlaybackPosition)
            {
                if (AudioManager.MainMusicHelper != null)
                {
                    if (AudioManager.MainMusicHelper.AudioSource.isPlaying)
                    {
                        float time = AudioManager.MainMusicHelper.AudioSource.time;
                        helper = AudioManager.PlayMusic(audio, transform);
                        helper.AudioSource.time = time;
                    }
                    else
                    {
                        helper = AudioManager.PlayMusic(audio, transform);
                    }
                }
            }
            else
            {
                AudioManager.StopMusicIfPlaying(audio, transform);
                helper = AudioManager.PlayMusic(audio, transform);
            }
        }

        void PlayBehaviour()
        {
            switch (fadeBehaviour)
            {
                case FadeBehaviour.None:
                    Play();
                    break;
                case FadeBehaviour.AdditiveFadeIn:
                    helper = AudioManager.FadeMusicIn(audio, fadeTime);
                    break;
                case FadeBehaviour.CrossFadeIn:
                    if (!AudioManager.MainMusicHelper.IsFree)
                    {
                        AudioManager.FadeMainMusicOut(fadeTime);
                    }
                    Invoke(nameof(Play), fadeTime);
                    break;
                case FadeBehaviour.FadeOutAndFadeIn:
                    var halfTime = fadeTime / 2;
                    if (!AudioManager.MainMusicHelper.IsFree)
                    {
                        AudioManager.FadeMainMusicOut(halfTime);
                    }
                    Invoke(nameof(Play), halfTime);
                    break;
            }
        }

        public void Stop()
        {
            AudioManager.StopMusic(audio, transform, true);
            helper = null;
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
            yield return new WaitUntil(() => AudioManager.Instance);
            yield return new WaitUntil(() => AudioManager.Instance.Initialized);

            playRoutine = null;
            PlayBehaviour();
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
            if (!AudioManager.Instance) return;
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
    }
}