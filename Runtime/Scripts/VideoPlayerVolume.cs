using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace JSAM
{
    public class VideoPlayerVolume : VolumeListener
    {
        [SerializeField] VideoPlayer videoPlayer;
        [SerializeField] RawImage videoImage;

        void OnEnable()
        {
            if (videoPlayer)
            {
                videoPlayer.prepareCompleted += AttachAudioSource;
        
                if (videoPlayer.isPlaying)
                {
                    Init();
                }
            }
        }
        
        private void OnDisable()
        {
            if (videoPlayer) videoPlayer.prepareCompleted -= AttachAudioSource;
        
            UnsubscribeFromVolumeEvents();
        }

        private void AttachAudioSource(VideoPlayer source)
        {
            Init();
        }

        void Init()
        {
            StartCoroutine(PlayRoutine());
        }

        IEnumerator PlayRoutine()
        {
            videoPlayer.enabled = false;

            SubscribeToVolumeEvents();

            videoPlayer.enabled = true;
            videoPlayer.prepareCompleted -= AttachAudioSource;
            videoPlayer.Prepare();

            yield return new WaitUntil(() => videoPlayer.isPrepared);
            
            videoPlayer.Play();
            videoPlayer.prepareCompleted += AttachAudioSource;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (Application.isPlaying) return;

            if (!videoPlayer)
            {
                UnityEditor.Undo.RecordObject(this, $"{nameof(VideoPlayerVolume)}: Setting up");
                videoPlayer = GetComponent<VideoPlayer>();
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            }

            if (audioSource)
            {
                UnityEditor.Undo.RecordObject(videoPlayer, $"{nameof(VideoPlayerVolume)}: Assigning AudioSource to VideoPlayer");
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
        }
#endif
    }
}