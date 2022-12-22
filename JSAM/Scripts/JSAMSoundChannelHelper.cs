using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(2)]
    public class JSAMSoundChannelHelper : BaseAudioChannelHelper<JSAMSoundFileObject>
    {
        protected override float Volume 
        {
            get
            {
                var vol = AudioManager.InternalInstance.ModifiedSoundVolume;
                if (audioFile) vol *= audioFile.relativeVolume;
                return vol;
            }
        }

        float prevPlaybackTime;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (audioFile)
            {
                AudioManager.OnSoundVolumeChanged += OnUpdateVolume;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            AudioManager.OnSoundVolumeChanged -= OnUpdateVolume;
        }

        protected override void Update()
        {
            if (AudioSource.loop)
            {
                // Check if the AudioSource is beginning to loop
                if (prevPlaybackTime > AudioSource.time)
                {
                    AssignNewAudioClip();
                    AudioSource.pitch = JSAMSoundFileObject.GetRandomPitch(audioFile);
                    AudioSource.Play();
                }
                prevPlaybackTime = AudioSource.time;
            }

            base.Update();
        }

        public override AudioSource Play(JSAMSoundFileObject file)
        {
            ClearProperties();

            if (file == null)
            {
                AudioManager.DebugWarning("Attempted to play a non-existent JSAM Sound File Object!");
                return AudioSource;
            }

            audioFile = file;

            AudioSource.pitch = JSAMSoundFileObject.GetRandomPitch(file);
            OnUpdateVolume(AudioManager.SoundVolume);

            switch (file.loopMode)
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

            return base.Play(file);
        }

        public override void Stop(bool stopInstantly = true)
        {
            base.Stop(stopInstantly);
            prevPlaybackTime = -1;
        }

#if UNITY_EDITOR
        public void PlayDebug(JSAMSoundFileObject file, bool dontReset)
        {
            if (!dontReset)
            {
                AudioSource.Stop();
            }
            AudioSource.timeSamples = (int)Mathf.Clamp((float)AudioSource.timeSamples, 0, (float)AudioSource.clip.samples - 1);
            AudioSource.pitch = JSAMSoundFileObject.GetRandomPitch(file);

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