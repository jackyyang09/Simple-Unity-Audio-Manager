using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(AudioSource))]
    public class MusicChannelHelper : BaseAudioChannelHelper<MusicFileObject>
    {
        protected override GameObject Prefab => JSAMSettings.Settings.MusicChannelPrefab;

        protected override void OnDisable()
        {
            base.OnDisable();

            if (audioFile)
            {
                if (audioFile.maxPlayingInstances > 0)
                {
                    AudioManager.InternalInstance.RemovePlayingMusic(audioFile, this);
                }
            }
        }

        public override void Stop(bool stopInstantly = true)
        {
            base.Stop(stopInstantly);
            if (stopInstantly)
            {
                AudioSource.Stop();
            }
        }
    }
}