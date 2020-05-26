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

    [AddComponentMenu("AudioManager/Audio Particles")]
    [RequireComponent(typeof(ParticleSystem))]
    public class AudioParticles : BaseAudioFeedback
    {
        enum ParticleEvent
        {
            ParticleEmitted,
            ParticleDeath
        }

        [Header("Particle Settings")]

        [SerializeField]
        ParticleEvent playSoundOn = ParticleEvent.ParticleEmitted;

        ParticleSystem partSys;
        ParticleSystem.Particle[] particles;
        float lowestLifetime = 99f;

        void Awake()
        {
            partSys = GetComponent<ParticleSystem>();
            particles = new ParticleSystem.Particle[partSys.main.maxParticles];
        }

        protected override void Start()
        {
            base.Start();
        }

        public void PlaySound()
        {
            switch (playSoundOn)
            {
                case ParticleEvent.ParticleEmitted:
                    AudioManager.instance.PlaySoundInternal(sound, sTransform);
                    break;
                case ParticleEvent.ParticleDeath:
                    AudioManager.instance.PlaySoundInternal(sound, sTransform);
                    break;
            }
        }

        void LateUpdate()
        {
            if (partSys.particleCount == 0)
                return;

            var numParticlesAlive = partSys.GetParticles(particles);

            float youngestParticleLifetime;
            var part = GetYoungestParticle(numParticlesAlive, particles, out youngestParticleLifetime);
            if (lowestLifetime > youngestParticleLifetime)
            {
                PlaySound();
            }

            lowestLifetime = youngestParticleLifetime;
        }

        int GetYoungestParticle(int numPartAlive, ParticleSystem.Particle[] particles, out float lifetime)
        {
            int youngest = 0;

            // Change only the particles that are alive
            for (int i = 0; i < numPartAlive; i++)
            {

                if (i == 0)
                {
                    youngest = 0;
                    continue;
                }

                if (particles[i].remainingLifetime > particles[youngest].remainingLifetime)
                    youngest = i;
            }

            lifetime = particles[youngest].startLifetime - particles[youngest].remainingLifetime;

            return youngest;

        }
    }
}