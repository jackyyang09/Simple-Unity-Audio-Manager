using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class AudioEvents : MonoBehaviour
    {
        int SoundNameToIndex(string enumName)
        {
            string name = enumName;
            if (enumName.Contains("."))
            {
                name = enumName.Remove(0, enumName.LastIndexOf('.') + 1);
            }

            List<string> enums = new List<string>();
            var library = AudioManager.Instance.Library;
            System.Type enumType =  AudioLibrary.GetEnumType(library.soundNamespaceGenerated + library.soundEnumGenerated);
            enums.AddRange(System.Enum.GetNames(enumType));
            return enums.IndexOf(name);
        }

        public void PlaySoundByReference(SoundFileObject sound)
        {
            AudioManager.PlaySound(sound, transform);
        }

        /// <summary>
        /// Takes the name of the Audio enum sound to be played as a string and plays it
        /// </summary>
        /// <param name="enumName">Either specify the name by it's Audio File name or use the entire enum</param>
        public void PlaySoundByEnum(string enumName)
        {
            int index = SoundNameToIndex(enumName);
            if (index > -1)
            {
                AudioManager.PlaySound(AudioManager.Instance.Library.Sounds[index], transform);
            }
        }

        public void StopSoundByEnum(string enumName)
        {
            int index = SoundNameToIndex(enumName);
            if (index > -1)
            {
                AudioManager.StopSound(AudioManager.Instance.Library.Sounds[index], transform, false);
            }
        }

        public void StopSoundByEnumInstantly(string enumName)
        {
            int index = SoundNameToIndex(enumName);
            if (index > -1)
            {
                AudioManager.StopSound(AudioManager.Instance.Library.Sounds[index], transform);
            }
        }

        public void SetMasterVolume(float newVal)
        {
            AudioManager.SetMasterVolume(newVal);
        }

        public void SetMusicVolume(float newVal)
        {
            AudioManager.SetMusicVolume(newVal);
        }

        public void SetSoundVolume(float newVal)
        {
            AudioManager.SetSoundVolume(newVal);
        }
    }
}