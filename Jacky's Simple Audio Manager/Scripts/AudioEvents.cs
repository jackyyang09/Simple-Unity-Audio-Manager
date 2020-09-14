using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class AudioEvents : MonoBehaviour
    {
        public void PlayAudioPlayer(AudioPlayer player)
        {
            player.Play();
        }

        int SoundNameToIndex(string enumName)
        {
            string name = enumName;
            if (enumName.Contains("."))
            {
                name = enumName.Remove(0, enumName.LastIndexOf('.') + 1);
            }

            List<string> enums = new List<string>();
            System.Type enumType = AudioManager.instance.GetSceneSoundEnum();
            enums.AddRange(System.Enum.GetNames(enumType));
            return enums.IndexOf(name);
        }

        /// <summary>
        /// Takes the name of the Audio enum sound to be played as a string and plays it without spatializing.
        /// </summary>
        /// <param name="enumName">Either specify the name by it's Audio File name or use the entire enum</param>
        public void PlaySoundByEnum(string enumName)
        {
            int index = SoundNameToIndex(enumName);

            if (index > -1)
            {
                AudioManager.instance.PlaySoundInternal(index, transform);
            }
        }

        public void PlayLoopingSoundByEnum(string enumName)
        {
            int index = SoundNameToIndex(enumName);

            if (index > -1)
            {
                AudioManager.instance.PlaySoundLoopInternal(index, transform);
            }
        }

        public void StopLoopingSoundByEnum(string enumName)
        {
            int index = SoundNameToIndex(enumName);

            if (index > -1)
            {
                if (AudioManager.instance.IsSoundLoopingInternal(index))
                {
                    AudioManager.instance.StopSoundLoopInternal(index, false, transform);
                }
            }
        }

        public void SetMasterVolume(float newVal)
        {
            AudioManager.instance.SetMasterVolumeInternal(newVal);
        }

        public void SetMusicVolume(float newVal)
        {
            AudioManager.instance.SetMusicVolumeInternal(newVal);
        }

        public void SetSoundVolume(float newVal)
        {
            AudioManager.instance.SetSoundVolumeInternal(newVal);
        }
    }
}