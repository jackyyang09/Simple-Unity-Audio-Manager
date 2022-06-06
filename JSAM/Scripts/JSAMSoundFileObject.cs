using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Audio File", menuName = "AudioManager/New Audio File Object", order = 1)]
    public class JSAMSoundFileObject : BaseAudioFileObject
    {
        /// <summary>
        /// Given an AudioFileSoundObject, returns a pitch with a modified pitch depending on the Audio File Object's settings
        /// </summary>
        /// <param name="audioFile"></param>
        public static float GetRandomPitch(JSAMSoundFileObject audioFile)
        {
            float pitch = audioFile.pitchShift;
            float newPitch = audioFile.startingPitch;
            bool ignoreTimeScale = audioFile.ignoreTimeScale;

            bool timeScaledSounds = false;
            if (AudioManager.Instance)
            {
                if (AudioManager.Instance.Settings)
                {
                    timeScaledSounds = AudioManager.Instance.Settings.TimeScaledSounds;
                }
            }

            if (timeScaledSounds && !ignoreTimeScale)
            {
                newPitch *= Time.timeScale;
                if (Time.timeScale == 0)
                {
                    return 0;
                }
            }

            //This is the base unchanged pitch
            if (pitch > 0)
            {
                newPitch += UnityEngine.Random.Range(-pitch, pitch);
                newPitch = Mathf.Clamp(newPitch, 0, 3);
            }

            return newPitch;
        }
    }
}