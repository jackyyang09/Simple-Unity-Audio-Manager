using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Sound File Object", menuName = "AudioManager/Sound File Object", order = 1)]
    public partial class SoundFileObject : BaseAudioFileObject
    {
        public SoundChannelHelper Play(Transform transform = null, SoundChannelHelper helper = null)
        {
            return AudioManager.InternalInstance.PlaySoundInternal(this, transform, helper);
        }

        public SoundChannelHelper Play(Vector3 position, SoundChannelHelper helper = null)
        {
            return AudioManager.InternalInstance.PlaySoundInternal(this, position, helper);
        }
    }
}