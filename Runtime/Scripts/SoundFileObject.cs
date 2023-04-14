using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Sound File Object", menuName = "AudioManager/Sound File Object", order = 1)]
    public class SoundFileObject : BaseAudioFileObject
    {
        public void Play(Transform transform = null, SoundChannelHelper helper = null)
        {
            AudioManager.InternalInstance.PlaySoundInternal(this, transform, helper);
        }

        public void Play(Vector3 position, SoundChannelHelper helper = null)
        {
            AudioManager.InternalInstance.PlaySoundInternal(this, position, helper);
        }

        /// <summary>
        /// Given an AudioFileSoundObject, returns a pitch with a modified pitch depending on the Audio File Object's settings
        /// </summary>
        /// <param name="audioFile"></param>
        public static float GetRandomPitch(SoundFileObject audioFile)
        {
            float pitch = audioFile.pitchShift;
            float newPitch = audioFile.startingPitch;
            bool ignoreTimeScale = audioFile.ignoreTimeScale;

            bool timeScaledSounds = false;
            timeScaledSounds = JSAMSettings.Settings.TimeScaledSounds;

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