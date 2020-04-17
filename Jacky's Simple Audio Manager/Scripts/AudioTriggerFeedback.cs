using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM 
{
    public class AudioTriggerFeedback : BaseAudioFeedback
    {
        enum TriggerEvent
        {
            OnTriggerEnter,
            OnTriggerStay,
            OnTriggerExit
        }

        [Header("Trigger Settings")]
        [SerializeField]
        [Tooltip("Will only play sound on trigger with another object on these layers")]
        LayerMask triggersWith;

        [SerializeField]
        [Tooltip("The intersection event that triggers the sound to play")]
        TriggerEvent triggerEvent;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
        }

        void TriggerSound(Collider collision)
        {
            if (soundFile != null)
            {
                AudioManager.instance.PlaySoundOnce(soundFile, sTransform, priority, pitchShift);
            }
            else
            {
                AudioManager.instance.PlaySoundOnce(sound, sTransform, priority, pitchShift);
            }
        }

        void TriggerSound(Collider2D collision)
        {
            if (soundFile != null)
            {
                AudioManager.instance.PlaySoundOnce(soundFile, sTransform, priority, pitchShift);
            }
            else
            {
                AudioManager.instance.PlaySoundOnce(sound, sTransform, priority, pitchShift);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerEvent == TriggerEvent.OnTriggerEnter) TriggerSound(other);
        }

        private void OnTriggerStay(Collider other)
        {
            if (triggerEvent == TriggerEvent.OnTriggerStay) TriggerSound(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerEvent == TriggerEvent.OnTriggerExit) TriggerSound(other);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (triggerEvent == TriggerEvent.OnTriggerEnter) TriggerSound(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (triggerEvent == TriggerEvent.OnTriggerStay) TriggerSound(collision);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (triggerEvent == TriggerEvent.OnTriggerExit) TriggerSound(collision);
        }
    }
}