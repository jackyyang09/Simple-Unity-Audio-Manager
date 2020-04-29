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
            OnCollisionStay,
            OnCollisionExit
        }

        [Header("Collision Settings")]
        [SerializeField]
        [Tooltip("Will only play sound on collision with another object these layers")]
        LayerMask collidesWith;

        [SerializeField]
        [Tooltip("The collision event that triggers the sound to play")]
        CollisionEvent triggerEvent;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
        }

        void TriggerSound(Collision collision)
        {
            if (Contains(collidesWith, collision.gameObject.layer))
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
        }

        void TriggerSound(Collision2D collision)
        {
            if (Contains(collidesWith, collision.gameObject.layer))
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

        /// <summary>
        /// Extension method to check if a layer is in a layermask
        /// With help from these lads https://answers.unity.com/questions/50279/check-if-layer-is-in-layermask.html
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static bool Contains(LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }
    }
}