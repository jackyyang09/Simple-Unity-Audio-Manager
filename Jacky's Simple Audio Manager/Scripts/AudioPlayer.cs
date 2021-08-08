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

    [AddComponentMenu("AudioManager/Audio Player")]
    public class AudioPlayer : BaseAudioFeedback
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

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

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

        public AudioSource Play()
        {
            AudioManager am = AudioManager.instance;
            AudioSource source;

            if (loopSound)
            {
                source = am.PlaySoundLoopInternal(sound, sTransform);
            }
            else source = am.PlaySoundInternal(sound, sTransform);

            // Ready to play again later
            activated = false;

            return source;
        }

        void PlayAtPosition()
        {
            AudioManager am = AudioManager.instance;
            AudioSource source;

            if (loopSound)
            {
                if (sound.spatialize)
                {
                    source = am.PlaySoundLoopInternal(sound, sTransform.position);
                }
                else
                {
                    source = am.PlaySoundLoopInternal(sound, null);
                }
            }
            else
            {
                if (sound.spatialize)
                {
                    source = am.PlaySoundInternal(sound, sTransform.position);
                }
                else
                {
                    source = am.PlaySoundInternal(sound, null);
                }
            }

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
            AudioManager am = AudioManager.instance;

            if (am == null) return;
            if (!loopSound)
            {
                if (am.IsSoundPlayingInternal(sound, sTransform))
                {
                    am.StopSoundInternal(sound, sTransform);
                }
            }
            else
            {
                if (am.IsSoundLoopingInternal(sound))
                {
                    am.StopSoundLoopInternal(sound, true, sTransform);
                }
            }
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
            while (!AudioManager.instance)
            {
                yield return new WaitForEndOfFrame();
            }
            while (!AudioManager.instance.Initialized())
            {
                yield return new WaitForEndOfFrame();
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