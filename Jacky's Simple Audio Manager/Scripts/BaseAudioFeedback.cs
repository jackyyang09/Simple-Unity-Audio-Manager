using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioFeedback : MonoBehaviour
    {
        [SerializeField]
        protected bool spatialSound = true;

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
        protected AudioFileObject sound;

        /// <summary>
        /// Used as a shorthand for all sound functions that ask for a transform. Will set itself to null if spatialSound is set to null
        /// </summary>
        protected Transform sTransform;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (AudioManager.instance) SetSoundProperties();
        }
#endif

        // Start is called before the first frame update
        protected virtual void Start()
        {
            SetSoundProperties();
        }

        void SetSoundProperties()
        {
            // Applies settings from the Audio File Object
            if (sound != null)
            {
                spatialSound = sound.spatialize;
                priority = sound.priority;
                pitchShift = sound.pitchShift;
                delay = sound.delay;
                ignoreTimeScale = sound.ignoreTimeScale;
            }

            sTransform = (spatialSound) ? transform : null;
        }
    }
}