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
                source = am.PlaySoundLoopInternal(audioObject, sTransform);
            }
            else source = am.PlaySoundInternal(audioObject, sTransform);

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
                if (audioObject.spatialize)
                {
                    source = am.PlaySoundLoopInternal(audioObject, sTransform.position);
                }
                else
                {
                    source = am.PlaySoundLoopInternal(audioObject, null);
                }
            }
            else
            {
                if (audioObject.spatialize)
                {
                    source = am.PlaySoundInternal(audioObject, sTransform.position);
                }
                else
                {
                    source = am.PlaySoundInternal(audioObject, null);
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

            if (!loopSound)
            {
                if (am.IsSoundPlayingInternal(audioObject, sTransform))
                {
                    am.StopSoundInternal(audioObject, sTransform);
                }
            }
            else
            {
                if (am.IsSoundLoopingInternal(audioObject))
                {
                    am.StopSoundLoopInternal(audioObject, true, sTransform);
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