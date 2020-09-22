using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("AudioManager/Audio Collision Feedback")]
    public class AudioCollisionFeedback : BaseAudioFeedback
    {
        enum CollisionEvent
        {
            OnCollisionEnter,
            OnCollisionExit,
            OnCollisionStay
        }

        [Header("Collision Settings")]
        [SerializeField]
        [Tooltip("Will only play sound on collision with another object these layers")]
        LayerMask collidesWith = 0;

        [SerializeField]
        [Tooltip("The collision event that triggers the sound to play")]
        CollisionEvent triggerEvent = CollisionEvent.OnCollisionEnter;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
        }

        void TriggerSound(Collision collision)
        {
            if (collidesWith.Contains(collision.gameObject.layer))
            {
                AudioSource source = null;
                source = AudioManager.instance.PlaySoundInternal(sound, sTransform);
                if (spatialSound)
                {
                    source.gameObject.transform.position = collision.GetContact(0).point;
                }
            }
        }

        void TriggerSound(Collision2D collision)
        {
            if (collidesWith.Contains(collision.gameObject.layer))
            {
                AudioSource source = null;
                source = AudioManager.instance.PlaySoundInternal(sound, sTransform);
                if (spatialSound)
                {
                    source.gameObject.transform.position = collision.GetContact(0).point;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (triggerEvent == CollisionEvent.OnCollisionEnter) TriggerSound(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (triggerEvent == CollisionEvent.OnCollisionStay) TriggerSound(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (triggerEvent == CollisionEvent.OnCollisionExit) TriggerSound(collision);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (triggerEvent == CollisionEvent.OnCollisionEnter) TriggerSound(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (triggerEvent == CollisionEvent.OnCollisionStay) TriggerSound(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (triggerEvent == CollisionEvent.OnCollisionExit) TriggerSound(collision);
        }
    }
}