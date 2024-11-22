using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(2)]
    [RequireComponent(typeof(AudioSource))]
    public class SoundChannelHelper : BaseAudioChannelHelper<SoundFileObject>
    {
        protected override VolumeChannel DefaultChannel => VolumeChannel.Sound;

        float prevPlaybackTime;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (audioFile)
            {
                if (audioFile.maxPlayingInstances > 0)
                {
                    AudioManager.InternalInstance.RemovePlayingSound(audioFile, this);
                }
            }
        }

        protected override void Update()
        {
            if (audioFile.loopMode <= LoopMode.Looping)
            {
                if (AudioSource.loop)
                {
                    // Check if the AudioSource is beginning to loop
                    if (prevPlaybackTime > AudioSource.time)
                    {
                        AssignNewAudioClip();
                        AudioSource.pitch = audioFile.GetRandomPitch();
                        AudioSource.Play();
                    }
                    prevPlaybackTime = AudioSource.time;
                }
            }

            base.Update();
        }

        public override AudioSource Play()
        {
            if (audioFile == null)
            {
                AudioManager.DebugWarning("Tried to play a Sound when no Sound File was assigned!");
                return AudioSource;
            }

            switch (audioFile.loopMode)
            {
                case LoopMode.NoLooping:
                    AudioSource.loop = false;
                    break;
                case LoopMode.Looping:
                case LoopMode.LoopWithLoopPoints:
                case LoopMode.ClampedLoopPoints:
                    AudioSource.loop = true;
                    break;
            }

            return base.Play();
        }

        public override void Stop(bool stopInstantly = true)
        {
            base.Stop(stopInstantly);
            prevPlaybackTime = -1;
        }
    }
}