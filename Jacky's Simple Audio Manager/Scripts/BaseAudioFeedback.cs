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
        public string sound;

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
        protected AudioFileObject audioObject;

        /// <summary>
        /// Used as a shorthand for all sound functions that ask for a transform. Will set itself to null if spatialSound is set to null
        /// </summary>
        protected Transform sTransform;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            // Applies settings from the Audio File Object
            if (audioObject == null)
            {
                DesignateSound();
                spatialSound = audioObject.spatialize;
                priority = audioObject.priority;
                pitchShift = audioObject.pitchShift;
                delay = audioObject.delay;
                ignoreTimeScale = audioObject.ignoreTimeScale;
            }

            sTransform = (spatialSound) ? transform : null;
        }

        void DesignateSound()
        {
            if (sound != "")
            {
                if (!AudioManager.instance) return;
                foreach (AudioFileObject a in AudioManager.instance.GetSoundLibrary())
                {
                    if (a.safeName == sound)
                    {
                        audioObject = a;
                        return;
                    }
                }
            }
            if (audioObject == null)
            {
                List<AudioFileObject> audio = AudioManager.instance.GetSoundLibrary();
                if (audio.Count > 0)
                {
                    audioObject = audio[0];
                    if (audioObject != null) sound = audioObject.safeName;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (AudioManager.instance) DesignateSound();
        }
#endif
    }
}