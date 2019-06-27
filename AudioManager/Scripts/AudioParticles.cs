using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// https://answers.unity.com/questions/693044/play-sound-on-particle-emit-sub-emitter.html
/// With help from these lovely gents
/// </summary>

[RequireComponent(typeof(ParticleSystem))]
public class AudioParticles : BaseAudioFeedback
{
    [Header("Sounds")]

    [SerializeField]
    AudioManager.Sound playOnParticleEmit;

    [SerializeField]
    AudioManager.Sound playOnParticleDeath;

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
        if (particles.particleCount > prevParticleCount)
        {
            am.PlaySoundOnce(playOnParticleEmit, sTransform, priority, AudioManager.UsePitch(pitch));
        }
        else if (particles.particleCount < prevParticleCount)
        {
            am.PlaySoundOnce(playOnParticleDeath, sTransform, priority, AudioManager.UsePitch(pitch));
        }
        prevParticleCount = particles.particleCount;
    }
}
