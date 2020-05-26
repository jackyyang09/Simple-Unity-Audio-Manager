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

        /// <summary>
        /// Takes the name of the Audio enum sound to be played as a string and plays it without spatializing.
        /// </summary>
        /// <param name="enumName">Either specify the name by it's Audio File name or use the entire enum</param>
        public void PlaySoundByEnum(string enumName)
        {
            string name = enumName;
            if (enumName.Contains("."))
            {
                name = enumName.Remove(0, enumName.LastIndexOf('.') + 1);
            }

            List<string> enums = new List<string>();
            System.Type enumType = AudioManager.instance.GetSceneSoundEnum();
            enums.AddRange(System.Enum.GetNames(enumType));
            int index = enums.IndexOf(name);

            if (index > 0)
            {
                AudioManager.instance.PlaySoundInternal(index, transform);
            }
        }

        public void PlaySpatializedSoundByEnum(string enumName)
        {
            string name = enumName;
            if (enumName.Contains("."))
            {
                name = enumName.Remove(0, enumName.LastIndexOf('.'));
            }

            List<string> enums = new List<string>();
            System.Type enumType = AudioManager.instance.GetSceneSoundEnum();
            enums.AddRange(System.Enum.GetNames(enumType));
            int index = enums.IndexOf(name);

            if (index > 0)
            {
                AudioManager.instance.PlaySoundInternal(index, transform);
            }
        }
    }
}