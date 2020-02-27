using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM 
{
    public class AudioTriggerFeedback : BaseAudioFeedback
    {
        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
        }

        private void OnTriggerEnter(Collider collision)
        {
            AudioManager am = AudioManager.instance;

            if (soundFile != null)
            {
                AudioManager.instance.PlaySoundOnce(soundFile, transform, priority, pitchShift);
            }
            else
            {
                AudioManager.instance.PlaySoundOnce(sound, transform, priority, pitchShift);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            AudioManager am = AudioManager.instance;

            if (soundFile != null)
            {
                AudioManager.instance.PlaySoundOnce(soundFile, transform, priority, pitchShift);
            }
            else
            {
                AudioManager.instance.PlaySoundOnce(sound, transform, priority, pitchShift);
            }
        }
    }
}