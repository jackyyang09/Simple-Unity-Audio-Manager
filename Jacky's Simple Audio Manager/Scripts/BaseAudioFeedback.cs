using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioFeedback : MonoBehaviour
    {
        [SerializeField]
        protected bool spatialSound = true;

        [SerializeField]
        [HideInInspector]
        public int sound;

        [Tooltip("If true, sound will keep playing in a loop according to it's settings until you make it stop")]
        [SerializeField]
        protected bool loopSound = false;

        [SerializeField]
        protected Priority priority = Priority.Default;

        [SerializeField]
        protected float pitchShift = 0;

        [Tooltip("Play sound after this long")]
        [SerializeField]
        protected float delay = 0;

        [SerializeField]
        protected bool ignoreTimeScale = false;

        [SerializeField]
        [HideInInspector]
        [Tooltip("Overrides the \"Sound\" parameter with an AudioClip if not null")]
        protected AudioClip soundFile;
        protected AudioFileObject audioObject;

        /// <summary>
        /// Used as a shorthand for all sound functions that ask for a transform. Will set itself to null if spatialSound is set to null
        /// </summary>
        protected Transform sTransform;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            // Applies settings from the Audio File Object
            if (soundFile == null)
            {
                audioObject = AudioManager.instance.GetSoundLibrary()[sound];

                spatialSound = audioObject.spatialize;
                priority = audioObject.priority;
                pitchShift = audioObject.pitchShift;
                delay = audioObject.delay;
                ignoreTimeScale = audioObject.ignoreTimeScale;
            }

            sTransform = (spatialSound) ? transform : null;
        }

        public AudioClip GetAttachedSound()
        {
            return soundFile;
        }
    }
}