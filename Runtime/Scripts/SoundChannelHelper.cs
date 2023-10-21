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
            if (AudioSource.loop)
            {
                // Check if the AudioSource is beginning to loop
                if (prevPlaybackTime > AudioSource.time)
                {
                    AssignNewAudioClip();
                    AudioSource.pitch = SoundFileObject.GetRandomPitch(audioFile);
                    AudioSource.Play();
                }
                prevPlaybackTime = AudioSource.time;
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

            AudioSource.pitch = SoundFileObject.GetRandomPitch(audioFile);

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

#if UNITY_EDITOR
        public void PlayDebug(SoundFileObject file, bool dontReset)
        {
            if (!dontReset)
            {
                AudioSource.Stop();
            }
            AudioSource.timeSamples = (int)Mathf.Clamp((float)AudioSource.timeSamples, 0, (float)AudioSource.clip.samples - 1);
            AudioSource.pitch = SoundFileObject.GetRandomPitch(file);

            audioFile = file;

            AudioSource.volume = file.relativeVolume;
            AudioSource.priority = (int)file.priority;
            float offset = AudioSource.pitch - 1;
            AudioSource.pitch = Time.timeScale + offset;

            ClearEffects();
            ApplyEffects();

            AudioSource.PlayDelayed(file.delay);
            enabled = true; // Enable updates on the script
        }
#endif
    }
}