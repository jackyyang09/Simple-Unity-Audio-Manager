using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Music File Object", menuName = "AudioManager/Music File Object", order = 1)]
    public partial class MusicFileObject : BaseAudioFileObject
    {
        public MusicChannelHelper Play(bool isMain)
        {
            return AudioManager.InternalInstance.PlayMusicInternal(this, isMain);
        }

        public MusicChannelHelper Play(Transform transform = null, MusicChannelHelper helper = null)
        {
            return AudioManager.InternalInstance.PlayMusicInternal(this, transform, helper);
        }

        public MusicChannelHelper Play(Vector3 position, MusicChannelHelper helper = null)
        {
            return AudioManager.InternalInstance.PlayMusicInternal(this, position, helper);
        }
    }
}