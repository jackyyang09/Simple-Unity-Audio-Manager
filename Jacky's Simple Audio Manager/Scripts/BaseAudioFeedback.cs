using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioFeedback : MonoBehaviour
    {
        [Header("Sound Settings")]

        [SerializeField]
        protected bool spatialSound = true;

        [SerializeField]
        [HideInInspector]
        public int sound;

        [SerializeField]
        protected Priority priority = Priority.Default;

        [SerializeField]
        protected Pitch pitchShift = Pitch.VeryLow;

        [SerializeField]
        [HideInInspector]
        [Tooltip("Overrides the \"Sound\" parameter with an AudioClip if not null")]
        protected AudioClip soundFile;

        /// <summary>
        /// Used as a shorthand for all sound functions that ask for a transform. Will set itself to null if spatialSound is set to null
        /// </summary>
        protected Transform sTransform;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            sTransform = (spatialSound) ? transform : null;
        }

        public AudioClip GetAttachedSound()
        {
            return soundFile;
        }
    }
}