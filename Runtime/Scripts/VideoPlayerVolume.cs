using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace JSAM
{
    public class VideoPlayerVolume : MonoBehaviour
    {
        enum VolumeChannel
        {
            Music,
            Sound,
            Voice
        }

        [SerializeField] VolumeChannel volumeChannel = VolumeChannel.Sound;
        VolumeChannel subscribedChannel;
        [SerializeField] [Range(0, 1)] float relativeVolume = 1;
        [SerializeField] VideoPlayer videoPlayer;
        [SerializeField] RawImage videoImage;

        delegate float VolumeDelegate();
        VolumeDelegate GetChannelVolume;
        public float Volume
        {
            get
            {
                var vol = GetChannelVolume();
                vol *= relativeVolume;
                return vol;
            }
        }

        SoundChannelHelper soundHelper;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!soundHelper) return;
            if (!videoPlayer)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            }
            if (Application.isPlaying && soundHelper)
            {
                if (subscribedChannel != volumeChannel)
                {
                    UnsubscribeFromAudioEvents();
                    SubscribeToVolumeEvents();
                    subscribedChannel = volumeChannel;
                }
                OnUpdateVolume();
            }
        }
#endif

        private void OnEnable()
        {
            if (videoPlayer)
            {
                videoPlayer.prepareCompleted += AttachAudioSource;
        
                if (videoPlayer.isPlaying && !soundHelper)
                {
                    Init();
                }
            }
        }
        
        private void OnDisable()
        {
            if (videoPlayer) videoPlayer.prepareCompleted -= AttachAudioSource;
        
            if (soundHelper)
            {
                UnsubscribeFromAudioEvents();
                soundHelper.Reserved = false;
                soundHelper = null;
            }
        }

        private void AttachAudioSource(VideoPlayer source)
        {
            Init();
        }

        [ContextMenu(nameof(Init))]
        void Init()
        {
            StartCoroutine(PlayRoutine());
        }

        IEnumerator PlayRoutine()
        {
            videoPlayer.enabled = false;

            soundHelper = AudioManager.InternalInstance.GetFreeSoundHelper();
            soundHelper.Reserved = true;
            
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, soundHelper.AudioSource);
            SubscribeToVolumeEvents();

            subscribedChannel = volumeChannel;
            
            videoPlayer.enabled = true;
            videoPlayer.prepareCompleted -= AttachAudioSource;
            videoPlayer.Prepare();

            yield return new WaitUntil(() => videoPlayer.isPrepared);
            
            videoPlayer.Play();
            videoPlayer.prepareCompleted += AttachAudioSource;
        }

        protected void SubscribeToVolumeEvents()
        {
            switch (volumeChannel)
            {
                case VolumeChannel.Music:
                    GetChannelVolume = () => AudioManager.InternalInstance.ModifiedMusicVolume;
                    AudioManager.OnMusicVolumeChanged += OnUpdateVolume;
                    break;
                case VolumeChannel.Sound:
                    GetChannelVolume = () => AudioManager.InternalInstance.ModifiedSoundVolume;
                    AudioManager.OnSoundVolumeChanged += OnUpdateVolume;
                    break;
                case VolumeChannel.Voice:
                    GetChannelVolume = () => AudioManager.InternalInstance.ModifiedVoiceVolume;
                    AudioManager.OnVoiceVolumeChanged += OnUpdateVolume;
                    break;
            }
        }

        protected void UnsubscribeFromAudioEvents()
        {
            GetChannelVolume = null;

            switch (volumeChannel)
            {
                case VolumeChannel.Music:
                    AudioManager.OnMusicVolumeChanged -= OnUpdateVolume;
                    break;
                case VolumeChannel.Sound:
                    AudioManager.OnSoundVolumeChanged -= OnUpdateVolume;
                    break;
                case VolumeChannel.Voice:
                    AudioManager.OnVoiceVolumeChanged -= OnUpdateVolume;
                    break;
            }
        }

        protected void OnUpdateVolume(float volume = 0)
        {
            soundHelper.AudioSource.volume = Volume;
        }
    }
}