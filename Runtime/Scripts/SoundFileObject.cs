using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Sound File Object", menuName = "AudioManager/Sound File Object", order = 1)]
    public class SoundFileObject : BaseAudioFileObject
    {
        public void Play(Transform transform = null, SoundChannelHelper helper = null)
        {
            AudioManager.InternalInstance.PlaySoundInternal(this, transform, helper);
        }

        public void Play(Vector3 position, SoundChannelHelper helper = null)
        {
            AudioManager.InternalInstance.PlaySoundInternal(this, position, helper);
        }
    }
}