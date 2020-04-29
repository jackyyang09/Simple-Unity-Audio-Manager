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

        [SerializeField]
        protected bool loopSound = false;

        [SerializeField]
        protected Priority priority = Priority.Default;

        [SerializeField]
        protected Pitch pitchShift = Pitch.VeryLow;

        [Tooltip("Play sound after this long")]
        [SerializeField]
        protected float delay = 0;

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
                loopSound = audioObject.loopSound;
            }

            sTransform = (spatialSound) ? transform : null;
        }

        public AudioClip GetAttachedSound()
        {
            return soundFile;
        }
    }
}