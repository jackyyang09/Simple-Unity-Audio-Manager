using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class AudioEvents : MonoBehaviour
    {
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
            var s = AudioManagerInternal.Instance.AudioFileFromString(enumName);
            if (s)
            {
                AudioManager.PlaySound(s as SoundFileObject, transform);
            }
        }

        public void StopSoundByEnum(string enumName)
        {
            var s = AudioManagerInternal.Instance.AudioFileFromString(enumName);
            if (s)
            {
                AudioManager.StopSound(s as SoundFileObject, transform, false);
            }
        }

        public void StopSoundByEnumInstantly(string enumName)
        {
            var s = AudioManagerInternal.Instance.AudioFileFromString(enumName);
            if (s)
            {
                AudioManager.StopSound(s as SoundFileObject, transform);
            }
        }

        public void SetMasterVolume(float newVal)
        {
            AudioManager.MasterVolume = newVal;
        }

        public void SetMusicVolume(float newVal)
        {
            AudioManager.MusicVolume = newVal;
        }

        public void SetSoundVolume(float newVal)
        {
            AudioManager.SoundVolume = newVal;
        }

        public void SetVoiceVolume(float newVal)
        {
            AudioManager.VoiceVolume = newVal;
        }
    }
}