using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JSAM 
{
    /// <summary>
    /// Plays sounds when a particle system emits particles and when particles die
    /// https://answers.unity.com/questions/693044/play-sound-on-particle-emit-sub-emitter.html
    /// With help from these lovely gents
    /// </summary>

    [RequireComponent(typeof(ParticleSystem))]
    public class AudioParticles : BaseAudioFeedback
    {
        enum PlayCondition
        {
            ParticleEmitted,
            ParticleDeath
        }

        [Header("Sounds")]

        [SerializeField]
        PlayCondition playSoundOn = PlayCondition.ParticleEmitted;

        [SerializeField]
        [Tooltip("Reduces particle lifetime by this much, used to keep track of particles and keep them from spawning/dying at the same time")]
        [Range(0.01f, 0.1f)]
        float lifeTimeOffset = 0.05f;

        ParticleSystem particles;

        int prevParticleCount;

        protected override void Start()
        {
            base.Start();
            particles = GetComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            float offset = main.startLifetime.constant - lifeTimeOffset;
            main.startLifetime = offset;
        }

        // Update is called once per frame
        void Update()
        {
            AudioManager am = AudioManager.instance;

            switch (playSoundOn)
            {
                case PlayCondition.ParticleEmitted:
                    if (particles.particleCount > prevParticleCount)
                    {
                        am.PlaySoundOnce(sound, sTransform, priority, pitchShift);
                    }
                    break;
                case PlayCondition.ParticleDeath:
                    if (particles.particleCount < prevParticleCount)
                    {
                        am.PlaySoundOnce(sound, sTransform, priority, pitchShift);
                    }
                    break;
            }

            prevParticleCount = particles.particleCount;
        }
    }
}