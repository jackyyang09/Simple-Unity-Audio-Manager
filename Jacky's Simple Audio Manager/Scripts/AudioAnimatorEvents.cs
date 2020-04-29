using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [RequireComponent(typeof(Animator))]
    public class AudioAnimatorEvents : MonoBehaviour
    {
        public void PlayAudioPlayer(AudioPlayer player)
        {
            player.Play();
        }

        /// <summary>
        /// Takes the name of the Audio enum sound to be played as a string and plays it without spatializing
        /// </summary>
        /// <param name="enumName"></param>
        public void PlaySoundByEnum(string enumName)
        {
            string name = enumName;
            if (enumName.Contains("."))
            {
                name = enumName.Remove(0, enumName.LastIndexOf('.'));
            }
            string[] enums = System.Enum.GetNames(AudioManager.instance.GetSceneSoundEnum());

            AudioFileObject file;

            for (int i = 0; i < enums.Length; i++)
            {
                if (name.Equals(enums[i]))
                {
                    file = AudioManager.instance.GetSoundLibrary()[i];
                    AudioManager.instance.PlaySoundOnce(i, null, file.priority, file.pitchShift, file.delay, file.ignoreTimeScale);
                }
            }
        }

        public void PlaySpatializedSoundByEnum(string enumName)
        {
            string name = enumName;
            if (enumName.Contains("."))
            {
                name = enumName.Remove(0, enumName.LastIndexOf('.'));
            }
            string[] enums = System.Enum.GetNames(AudioManager.instance.GetSceneSoundEnum());

            AudioFileObject file;

            for (int i = 0; i < enums.Length; i++)
            {
                if (name.Equals(enums[i]))
                {
                    file = AudioManager.instance.GetSoundLibrary()[i];
                    AudioManager.instance.PlaySoundOnce(i, transform, file.priority, file.pitchShift, file.delay, file.ignoreTimeScale);
                }
            }
        }
    }
}