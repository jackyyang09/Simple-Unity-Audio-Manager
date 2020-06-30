using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace JSAM
{
    public class BaseAudioMusicFeedback : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public string music = "";

        [Tooltip("Play Music in 3D space, will override Music Fading if true")]
        [SerializeField]
        protected bool spatializeSound;

        [Tooltip("Adds a transition effect for playing this music")]
        [SerializeField]
        protected TransitionMode transitionMode = TransitionMode.None;

        [SerializeField]
        [Tooltip("Play music starting from previous track's playback position, only works when Music Fade Time is greater than 0")]
        protected bool keepPlaybackPosition = true;

        [SerializeField]
        [Tooltip("If true, playing this audio file while its currently playing will restart playback from the start point. Otherwise, the call to Play the track will be ignored if it's currently playing.")]
        protected bool restartOnReplay = false;

        [SerializeField]
        protected float musicFadeInTime = 0;

        [SerializeField]
        protected float musicFadeOutTime = 0;

        [Tooltip("Standard looping disregards all loop point logic, loop point use is enabled in the audio music file")]
        [SerializeField]
        protected LoopMode loopMode = LoopMode.Looping;

        [Tooltip("Overrides the \"Music\" parameter with an AudioClip if not null")]
        [SerializeField]
        protected AudioClip musicFile = null;
        protected AudioFileMusicObject audioObject;

        // Start is called before the first frame update
        protected void Start()
        {
            if (musicFile == null)
            {
                DesignateSound();

                loopMode = audioObject.loopMode;
                spatializeSound = audioObject.spatialize;
            }
        }

        protected void DesignateSound()
        {
            if (audioObject == null && music != "")
            {
                if (!AudioManager.instance) return;
                foreach (AudioFileMusicObject a in AudioManager.instance.GetMusicLibrary())
                {
                    if (a.safeName == music)
                    {
                        audioObject = a;
                        return;
                    }
                }
            }
            if (audioObject == null)
            {
                audioObject = AudioManager.instance.GetMusicLibrary()[0];
                if (audioObject != null) music = audioObject.safeName;
            }
        }

        public AudioClip GetAttachedFile()
        {
            return musicFile;
        }

        public TransitionMode GetTransitionMode()
        {
            return transitionMode;
        }
    }
}