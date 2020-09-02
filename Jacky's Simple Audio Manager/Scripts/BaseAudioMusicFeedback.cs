using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioMusicFeedback : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public string music = "";

        [Tooltip("Play Music in 3D space, will override Music Fading if true")]
        [SerializeField]
        protected bool spatializeSound;

        [Tooltip("Adds a transition effect for playing this music. If not none, music will fade-in and out during Play events and will fadeout during Stop events")]
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

        [SerializeField, HideInInspector]
        protected AudioFileMusicObject audioObject;

        // Start is called before the first frame update
        protected void Start()
        {
            CheckForMusic();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (AudioManager.instance) CheckForMusic();
        }
#endif

        public void CheckForMusic()
        {
            if (audioObject == null)
            {
                DesignateMusic();

                if (audioObject != null)
                {
                    loopMode = audioObject.loopMode;
                    spatializeSound = audioObject.spatialize;
                }
            }
        }

        protected void DesignateMusic()
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
                List<AudioFileMusicObject> audio = AudioManager.instance.GetMusicLibrary();
                if (audio.Count > 0)
                {
                    audioObject = audio[0];
                    if (audioObject != null) music = audioObject.safeName;
                }
            }
        }

        public TransitionMode GetTransitionMode()
        {
            return transitionMode;
        }
    }
}