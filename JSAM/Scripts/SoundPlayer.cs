using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public enum AudioPlaybackBehaviour
    {
        None,
        Play,
        Stop
    }

    [AddComponentMenu("AudioManager/Sound Player")]
    public class SoundPlayer : BaseAudioFeedback<SoundFileObject>
    {
        [Tooltip("Behaviour to trigger when the object this is attached to is created")]
        [SerializeField]
        AudioPlaybackBehaviour onStart = AudioPlaybackBehaviour.Play;

        [Tooltip("Behaviour to trigger when the object this is attached to is enabled or when the object is created")]
        [SerializeField]
        AudioPlaybackBehaviour onEnable = AudioPlaybackBehaviour.None;

        [Tooltip("Behaviour to trigger when the object this is attached to is destroyed or set to in-active")]
        [SerializeField]
        AudioPlaybackBehaviour onDisable = AudioPlaybackBehaviour.Stop;

        [Tooltip("Behaviour to trigger when the object this is attached to is destroyed")]
        [SerializeField]
        AudioPlaybackBehaviour onDestroy = AudioPlaybackBehaviour.None;

        /// <summary>
        /// Boolean prevents the sound from being played multiple times when the Start and OnEnable callbacks intersect
        /// </summary>
        bool activated;

        SoundChannelHelper helper;
        public SoundChannelHelper SoundHelper => helper;

        // Start is called before the first frame update
        protected void Start()
        {
            switch (onStart)
            {
                case AudioPlaybackBehaviour.Play:
                    if (!activated)
                    {
                        activated = true;
                        StartCoroutine(PlayOnEnable());
                    }
                    break;
                case AudioPlaybackBehaviour.Stop:
                    Stop();
                    break;
            }
        }

        public void Play()
        {
            helper = AudioManager.PlaySound(audio, transform);

            // Ready to play again later
            activated = false;
        }

        public void PlaySound()
        {
            Play();
        }

        /// <summary>
        /// Stops the sound instantly
        /// </summary>
        public void Stop()
        {
            AudioManager.StopSoundIfPlaying(audio, transform);
        }

        private void OnEnable()
        {
            switch (onEnable)
            {
                case AudioPlaybackBehaviour.Play:
                    if (!activated)
                    {
                        activated = true;
                        StartCoroutine(PlayOnEnable());
                    }
                    break;
                case AudioPlaybackBehaviour.Stop:
                    Stop();
                    break;
            }
        }

        IEnumerator PlayOnEnable()
        {
            while (!AudioManager.Instance)
            {
                yield return null;
            }
            while (!AudioManager.Instance.Initialized)
            {
                yield return null;
            }

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
    }
}