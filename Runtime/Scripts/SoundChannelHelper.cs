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

        public override AudioSource Play()
        {
            if (audioFile == null)
            {
                AudioManager.DebugWarning("Tried to play a Sound when no Sound File was assigned!");
                return AudioSource;
            }

            return base.Play();
        }
    }
}