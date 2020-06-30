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
        [Tooltip("Overrides the \"Sound\" parameter with an AudioClip if not null")]
        protected AudioClip soundFile;
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
            if (soundFile == null && audioObject == null)
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

        public AudioClip GetAttachedSound()
        {
            return soundFile;
        }

        void DesignateSound()
        {
            if (soundFile == null && sound != "")
            {
                if (!AudioManager.instance) return;
                print(AudioManager.instance.gameObject.scene.name + " " + sound);
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
                audioObject = AudioManager.instance.GetSoundLibrary()[0];
                if (audioObject != null) sound = audioObject.safeName;
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