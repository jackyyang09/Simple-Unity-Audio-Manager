using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("AudioManager/Audio Player")]
    public class AudioPlayer : BaseAudioFeedback
    {
        [Tooltip("Play the sound when the scene starts up")]
        [SerializeField]
        bool playOnStart = true;

        [Tooltip("Play the sound both when the scene starts up and when the object this is attached to is created or set to active")]
        [SerializeField]
        bool playOnEnable = false;

        [Tooltip("Stop the sound when the object this is attached to is destroyed or set to in-active")]
        [SerializeField]
        bool stopOnDisable = true;

        [Tooltip("Stop the sound when the object this is attached to is destroyed")]
        [SerializeField]
        bool stopOnDestroy = false;

        /// <summary>
        /// Boolean prevents the sound from being played multiple times when the Start and OnEnable callbacks intersect
        /// </summary>
        bool activated;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            if (playOnStart && !activated)
            {
                activated = true;
                Play();
            }
        }

        public AudioSource Play()
        {
            AudioManager am = AudioManager.instance;
            AudioSource source;

            if (soundFile != null)
            {
                Transform t = (spatialSound) ? transform : null;

                if (loopSound)
                {
                    source = am.PlaySoundLoopInternal(soundFile, sTransform, spatialSound, priority, delay, ignoreTimeScale);
                }
                else source = am.PlaySoundInternal(soundFile, sTransform, priority, pitchShift, delay, ignoreTimeScale);
            }
            else
            {
                if (loopSound)
                {
                    source = am.PlaySoundLoopInternal(sound, sTransform);
                }
                else source = am.PlaySoundInternal(sound, sTransform);
            }

            // Ready to play again later
            activated = false;

            return source;
        }

        /// <summary>
        /// Stops the sound instantly
        /// </summary>
        public void Stop()
        {
            AudioManager am = AudioManager.instance;

            if (soundFile != null)
            {
                if (!loopSound)
                {
                    if (am.IsSoundPlayingInternal(soundFile, sTransform))
                    {
                        am.StopSoundInternal(soundFile, sTransform);
                    }
                }
                else
                {
                    if (am.IsSoundLoopingInternal(soundFile))
                    {
                        am.StopSoundLoopInternal(sound, true, sTransform);
                    }
                }
            }
            else
            {
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
        }

        private void OnEnable()
        {
            if (playOnEnable && !activated)
            {
                activated = true;
                StartCoroutine(PlayOnEnable());
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
    }
}