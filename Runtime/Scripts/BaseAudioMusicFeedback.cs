using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioMusicFeedback : BaseAudioFeedback<MusicFileObject>
    {
        [Tooltip("Play music starting from previous track's playback position, only works when Music Fade Time is greater than 0")]
        [SerializeField] protected bool keepPlaybackPosition = true;
        
        [Tooltip("If true, playing this audio file while its currently playing will restart playback from the start point. Otherwise, the call to Play the track will be ignored if it's currently playing.")]
        [SerializeField] protected bool restartOnReplay = false;

        [Tooltip("If true, when invoking the stop music function, music will instantly stop playback, ignoring all transition settings.")]
        [SerializeField] protected bool stopInstantly = false;
    }
}