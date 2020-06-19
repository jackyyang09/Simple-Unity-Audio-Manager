using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM 
{
    [AddComponentMenu("AudioManager/Audio Trigger Feedback")]
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
        TriggerEvent triggerEvent = TriggerEvent.OnTriggerEnter;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
        }

        void TriggerSound()
        {
            if (soundFile != null)
            {
                AudioManager.instance.PlaySoundInternal(soundFile, sTransform, priority, pitchShift);
            }
            else
            {
                AudioManager.instance.PlaySoundInternal(audioObject, sTransform);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerEvent == TriggerEvent.OnTriggerEnter) TriggerSound();
        }

        private void OnTriggerStay(Collider other)
        {
            if (triggerEvent == TriggerEvent.OnTriggerStay) TriggerSound();
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerEvent == TriggerEvent.OnTriggerExit) TriggerSound();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (triggerEvent == TriggerEvent.OnTriggerEnter) TriggerSound();
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (triggerEvent == TriggerEvent.OnTriggerStay) TriggerSound();
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (triggerEvent == TriggerEvent.OnTriggerExit) TriggerSound();
        }
    }
}