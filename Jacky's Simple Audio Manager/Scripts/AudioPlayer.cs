using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class AudioPlayer : BaseAudioFeedback
    {
        [SerializeField]
        bool loopSound = false;

        [Tooltip("Play sound after this long")]
        [SerializeField]
        float delay = 0;

        [SerializeField]
        bool playOnStart;

        [SerializeField]
        bool playOnEnable;

        [SerializeField]
        bool stopOnDisable = true;

        [SerializeField]
        bool stopOnDestroy = true;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            if (playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            AudioManager am = AudioManager.instance;

            Transform t = (spatialSound) ? transform : null;

            if (soundFile != null)
            {
                if (loopSound)
                {
                    /*if (!am.IsSoundLooping(soundFile)) */am.PlaySoundLoop(soundFile, transform, spatialSound, priority);
                }
                else am.PlaySoundOnce(soundFile, t, priority, pitchShift, delay);
            }
            else
            {
                if (loopSound)
                {
                    /*if (!am.IsSoundLooping(sound)) */am.PlaySoundLoop(sound, transform, spatialSound, priority);
                }
                else am.PlaySoundOnce(sound, t, priority, pitchShift, delay);
            }
        }

        /// <summary>
        /// Stops the sound instantly
        /// </summary>
        public void Stop()
        {
            AudioManager am = AudioManager.instance;

            Transform t = (spatialSound) ? transform : null;
            if (soundFile != null)
            {
                if (!loopSound)
                {
                    if (am.IsSoundPlaying(soundFile, t))
                    {
                        am.StopSound(soundFile, t);
                    }
                }
                else
                {
                    if (am.IsSoundLooping(soundFile))
                    {
                        am.StopSoundLoop(sound, true, t);
                    }
                }
            }
            else
            {
                if (!loopSound)
                {
                    if (am.IsSoundPlaying(sound, t))
                    {
                        am.StopSound(sound, t);
                    }
                }
                else
                {
                    if (am.IsSoundLooping(sound))
                    {
                        am.StopSoundLoop(sound, true, t);
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
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