using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace JSAM
{
    public class VideoPlayerVolume : MonoBehaviour
    {
        [SerializeField] bool useMusicVolume;
        [SerializeField] [Range(0, 1)] float relativeVolume = 1;
        [SerializeField] VideoPlayer videoPlayer;

        SoundChannelHelper soundHelper;
        MusicChannelHelper musicHelper;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
            if (Application.isPlaying)
            {
                UpdateVolume(relativeVolume);
            }
        }
#endif

        private void OnEnable()
        {
            if (videoPlayer) videoPlayer.started += AttachAudioSource;
            AudioManager.OnMasterVolumeChanged += UpdateVolume;
            if (useMusicVolume) AudioManager.OnMusicVolumeChanged += UpdateVolume;
            else AudioManager.OnSoundVolumeChanged += UpdateVolume;
        }

        private void OnDisable()
        {
            if (videoPlayer) videoPlayer.started -= AttachAudioSource;
            AudioManager.OnMasterVolumeChanged += UpdateVolume;
            if (useMusicVolume) AudioManager.OnMusicVolumeChanged += UpdateVolume;
            else AudioManager.OnSoundVolumeChanged += UpdateVolume;
        }

        private void AttachAudioSource(VideoPlayer source)
        {
            if (useMusicVolume)
            {
                musicHelper = AudioManager.InternalInstance.GetFreeMusicHelper();
                videoPlayer.SetTargetAudioSource(0, musicHelper.AudioSource);
                musicHelper.Reserved = true;
            }
            else
            {
                soundHelper = AudioManager.InternalInstance.GetFreeSoundHelper();
                videoPlayer.SetTargetAudioSource(0, soundHelper.AudioSource);
                soundHelper.Reserved = true;
            }
        }

        void UpdateVolume(float volume)
        {
            if (!useMusicVolume)
            {
                if (soundHelper) soundHelper.AudioSource.volume = AudioManager.InternalInstance.ModifiedSoundVolume * relativeVolume;
            }
            else
            {
                if (musicHelper) musicHelper.AudioSource.volume = AudioManager.InternalInstance.ModifiedMusicVolume * relativeVolume;
            }
        }
    }
}