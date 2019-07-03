using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    ParticleSystem particles;

    int prevParticleCount;

    protected override void Start()
    {
        base.Start();
        particles = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        switch (playSoundOn)
        {
            case PlayCondition.ParticleEmitted:
                if (particles.particleCount > prevParticleCount)
                {
                    am.PlaySoundOnce(sound, sTransform, priority, AudioManager.UsePitch(pitchShift));
                }
                break;
            case PlayCondition.ParticleDeath:
                if (particles.particleCount < prevParticleCount)
                {
                    am.PlaySoundOnce(sound, sTransform, priority, AudioManager.UsePitch(pitchShift));
                }
                break;
        }
        
        prevParticleCount = particles.particleCount;
    }
}
