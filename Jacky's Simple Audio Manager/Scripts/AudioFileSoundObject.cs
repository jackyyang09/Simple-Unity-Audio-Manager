using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Audio File", menuName = "AudioManager/New Audio File Object", order = 1)]
    public class AudioFileSoundObject : BaseAudioFileObject
    {
        [HideInInspector] public bool neverRepeat;
        [HideInInspector] public int lastClipIndex = -1;

        public void Initialize()
        {
            lastClipIndex = -1;
        }
    }
}