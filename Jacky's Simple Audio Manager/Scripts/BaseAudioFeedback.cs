using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class BaseAudioFeedback : MonoBehaviour
    {
        [Header("Sound Settings")]

        [SerializeField]
        protected bool spatialSound = true;

        [HideInInspector]
        public string sound;

        [SerializeField]
        protected Priority priority = Priority.Default;

        [SerializeField]
        protected Pitch pitchShift = Pitch.VeryLow;

        [Header("Set your sound settings here")]

        [SerializeField]
        [Tooltip("Overrides the \"Sound\" parameter with an AudioClip if not null")]
        protected AudioClip soundFile;

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

        //public void SetSound(string s)
        //{
        //    sound = s;
        //}
    }
}