using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

namespace JSAM 
{
    [AddComponentMenu("AudioManager/Audio Trigger Feedback")]
    public class AudioTriggerFeedback : BaseAudioFeedback<SoundFileObject>
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
        LayerMask triggersWith = 0;

        [SerializeField]
        [Tooltip("The intersection event that triggers the sound to play")]
        TriggerEvent triggerEvent = TriggerEvent.OnTriggerEnter;

        void TriggerSound(Collider other)
        {
            if (triggersWith.Contains(other.gameObject.layer))
            {
                AudioManager.PlaySound(audio, transform);
            }
        }

        void TriggerSound(Collider2D collision)
        {
            if (triggersWith.Contains(collision.gameObject.layer))
            {
                AudioManager.PlaySound(audio, transform);
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