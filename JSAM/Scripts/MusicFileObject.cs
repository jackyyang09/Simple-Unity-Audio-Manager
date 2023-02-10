using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Music File Object", menuName = "AudioManager/Music File Object", order = 1)]
    public class MusicFileObject : BaseAudioFileObject
    {
        public void Play(bool isMain)
        {
            AudioManager.InternalInstance.PlayMusicInternal(this, isMain);
        }

        public void Play(Transform transform = null, MusicChannelHelper helper = null)
        {
            AudioManager.InternalInstance.PlayMusicInternal(this, transform, helper);
        }

        public void Play(Vector3 position, MusicChannelHelper helper = null)
        {
            AudioManager.InternalInstance.PlayMusicInternal(this, position, helper);
        }
    }
}