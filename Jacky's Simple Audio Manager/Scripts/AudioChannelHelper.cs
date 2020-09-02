using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [DefaultExecutionOrder(2)]
    public class AudioChannelHelper : MonoBehaviour
    {
        float fadeTime;

        AudioFileObject audioFile;
        AudioSource aSource;
        bool looping;

        float prevPlaybackTime;

        bool musicHelper;

        public void Init(bool designateMusicHelper = false)
        {
            musicHelper = designateMusicHelper;
            aSource = GetComponent<AudioSource>();
            enabled = false;
        }

        public void SetAudioFile(AudioFileObject file)
        {
            audioFile = file;
        }

        public void Play(float delay)
        {
            StopAllCoroutines();
            aSource.Stop();
            aSource.PlayDelayed(delay);
            audioFile = null;
            looping = false;
            aSource.bypassEffects = false;
            aSource.bypassListenerEffects = false;
            aSource.bypassReverbZones = false;
            ClearEffects();
        }

        public void Play(AudioFileMusicObject file)
        {
            // Make sure no remnants from a previous sound remain
            StopAllCoroutines();
            if (file.playReversed)
            {
                aSource.time = aSource.clip.length - 0.01f;
            }
            aSource.Play();
            audioFile = file;
            looping = false;
            aSource.bypassEffects = file.bypassEffects;
            aSource.bypassListenerEffects = file.bypassListenerEffects;
            aSource.bypassReverbZones = file.bypassReverbZones;
            ApplyEffects();
            ApplyVolumeChanges();
        }

        public void Play(float delay, AudioFileObject file, bool loop = false)
        {
            // Make sure no remnants from a previous sound remain
            StopAllCoroutines();
            aSource.Stop();
            enabled = true; // Enable updates on the script
            if (file.playReversed)
            {
                aSource.time = aSource.clip.length - 0.01f;
            }
            aSource.PlayDelayed(delay);
            audioFile = file;
            looping = loop;
            aSource.bypassEffects = file.bypassEffects;
            aSource.bypassListenerEffects = file.bypassListenerEffects;
            aSource.bypassReverbZones = file.bypassReverbZones;
            switch (file.fadeMode)
            {
                case FadeMode.FadeIn:
                case FadeMode.FadeInAndOut:
                    StartCoroutine(FadeIn(file.fadeMode == FadeMode.FadeInAndOut));
                    break;
                case FadeMode.FadeOut:
                    StartCoroutine(FadeOut());
                    break;
            }
            ApplyVolumeChanges();
            ApplyEffects();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Just for the playing of generic AudioClips
        /// </summary>
        /// <param name="dontReset"></param>
        public void PlayDebug(bool dontReset)
        {
            if (!dontReset)
            {
                aSource.Stop();
            }
            aSource.timeSamples = (int)Mathf.Clamp((float)aSource.timeSamples, 0, (float)aSource.clip.samples - 1);
            aSource.Play();
            aSource.pitch = 1;
            ClearEffects();
        }

        public void PlayDebug(AudioFileObject file, bool dontReset)
        {
            if (!dontReset)
            {
                aSource.Stop();
            }
            if (file.playReversed) aSource.time = aSource.clip.length - AudioManager.EPSILON;
            else aSource.timeSamples = (int)Mathf.Clamp((float)aSource.timeSamples, 0, (float)aSource.clip.samples - 1);
            aSource.Play();
            aSource.pitch = AudioManager.GetRandomPitch(file);
            aSource.bypassEffects = file.bypassEffects;
            aSource.bypassListenerEffects = file.bypassListenerEffects;
            aSource.bypassReverbZones = file.bypassReverbZones;
            audioFile = file;
            ApplyVolumeChanges();
            ApplyEffects();
        }
#endif

        public void Stop(bool stopInstantly = true)
        {
            if (stopInstantly) aSource.Stop();
            StopAllCoroutines();
            prevPlaybackTime = -1;
            enabled = false;
        }

        public void ApplyVolumeChanges()
        {
            if (audioFile == null) return;
            if (musicHelper)
            {
                aSource.volume = AudioManager.GetTrueMusicVolume() * audioFile.relativeVolume;
            }
            else
            {
                aSource.volume = AudioManager.GetTrueSoundVolume() * audioFile.relativeVolume;
            }
        }

        private void Update()
        {
            if (looping)
            {
                // Check if the AudioSource is beginning to loop
                if (prevPlaybackTime > aSource.time)
                {
                    if (audioFile.UsingLibrary())
                    {
                        AudioClip[] library = audioFile.GetFiles().ToArray();
                        do
                        {
                            aSource.clip = library[Random.Range (0, library.Length)];
                        } while (aSource.clip == null); // If the user is a dingus and left a few null references in the library
                        aSource.Play();
                    }
                    aSource.pitch = AudioManager.GetRandomPitch(audioFile);
                }
                prevPlaybackTime = aSource.time;
            }
        }

        //private void LateUpdate()
        //{
        //    if (looping)
        //    {
        //    }
        //}

        AudioChorusFilter chorusFilter;
        AudioDistortionFilter distortionFilter;
        AudioEchoFilter echoFilter;
        AudioHighPassFilter highPassFilter;
        AudioLowPassFilter lowPassFilter;
        AudioReverbFilter reverbFilter;

        public void ApplyEffects()
        {
            if (audioFile.chorusFilter.enabled)
            {
                if (!chorusFilter)
                {
                    chorusFilter = gameObject.AddComponent<AudioChorusFilter>();
                }
                chorusFilter.enabled = true;
                chorusFilter.dryMix = audioFile.chorusFilter.dryMix;
                chorusFilter.wetMix1 = audioFile.chorusFilter.wetMix1;
                chorusFilter.wetMix2 = audioFile.chorusFilter.wetMix2;
                chorusFilter.wetMix3 = audioFile.chorusFilter.wetMix3;
                chorusFilter.delay = audioFile.chorusFilter.delay;
                chorusFilter.rate = audioFile.chorusFilter.rate;
                chorusFilter.depth = audioFile.chorusFilter.depth;
            }
            else if (!audioFile.chorusFilter.enabled && chorusFilter) chorusFilter.enabled = false;

            if (audioFile.distortionFilter.enabled)
            {
                if (!distortionFilter)
                {
                    distortionFilter = gameObject.AddComponent<AudioDistortionFilter>();
                }
                distortionFilter.enabled = true;
                distortionFilter.distortionLevel = audioFile.distortionFilter.distortionLevel;
            }
            else if (!audioFile.distortionFilter.enabled && distortionFilter) distortionFilter.enabled = false;

            if (audioFile.echoFilter.enabled)
            {
                if (!echoFilter)
                {
                    echoFilter = gameObject.AddComponent<AudioEchoFilter>();
                }
                echoFilter.enabled = true;
                echoFilter.delay = audioFile.echoFilter.delay;
                echoFilter.decayRatio = audioFile.echoFilter.decayRatio;
                echoFilter.wetMix = audioFile.echoFilter.wetMix;
                echoFilter.dryMix = audioFile.echoFilter.dryMix;
            }
            else if (!audioFile.echoFilter.enabled && echoFilter) echoFilter.enabled = false;

            if (audioFile.highPassFilter.enabled)
            {
                if (!highPassFilter)
                {
                    highPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
                }
                highPassFilter.enabled = true;
                highPassFilter.cutoffFrequency = audioFile.highPassFilter.cutoffFrequency;
                highPassFilter.highpassResonanceQ = audioFile.highPassFilter.highpassResonanceQ;
            }
            else if (!audioFile.highPassFilter.enabled && highPassFilter) highPassFilter.enabled = false;

            if (audioFile.lowPassFilter.enabled)
            {
                if (!lowPassFilter)
                {
                    lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                }
                lowPassFilter.enabled = true;
                lowPassFilter.cutoffFrequency = audioFile.lowPassFilter.cutoffFrequency;
                lowPassFilter.lowpassResonanceQ = audioFile.lowPassFilter.lowpassResonanceQ;
            }
            else if (!audioFile.lowPassFilter.enabled && lowPassFilter) lowPassFilter.enabled = false;

            if (audioFile.reverbFilter.enabled)
            {
                if (!reverbFilter)
                {
                    reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
                }
                reverbFilter.enabled = true;
                reverbFilter.reverbPreset = audioFile.reverbFilter.reverbPreset;
                reverbFilter.dryLevel = audioFile.reverbFilter.dryLevel;
                reverbFilter.room = audioFile.reverbFilter.room;
                reverbFilter.roomHF = audioFile.reverbFilter.roomHF;
                reverbFilter.roomLF = audioFile.reverbFilter.roomLF;
                reverbFilter.decayTime = audioFile.reverbFilter.decayTime;
                reverbFilter.decayHFRatio = audioFile.reverbFilter.decayHFRatio;
                reverbFilter.reflectionsLevel = audioFile.reverbFilter.reflectionsLevel;
                reverbFilter.reflectionsDelay = audioFile.reverbFilter.reflectionsDelay;
                reverbFilter.reverbLevel = audioFile.reverbFilter.reverbLevel;
                reverbFilter.reverbDelay = audioFile.reverbFilter.reverbDelay;
                reverbFilter.hfReference = audioFile.reverbFilter.hFReference;
                reverbFilter.lfReference = audioFile.reverbFilter.lFReference;
                reverbFilter.diffusion = audioFile.reverbFilter.diffusion;
                reverbFilter.density = audioFile.reverbFilter.density;
            }
            else if (!audioFile.reverbFilter.enabled && reverbFilter) reverbFilter.enabled = false;
        }

        public void ClearEffects()
        {
            {
                AudioChorusFilter component;
#if UNITY_2019_4_OR_NEWER
                TryGetComponent(out component);
#else
                component = GetComponent<AudioChorusFilter>();
#endif
                if (component) component.enabled = false;
            }
            {
                AudioDistortionFilter component;
#if UNITY_2019_4_OR_NEWER
                TryGetComponent(out component);
#else
                component = GetComponent<AudioDistortionFilter>();
#endif
                if (component) component.enabled = false;
            }
            {
                AudioEchoFilter component;
#if UNITY_2019_4_OR_NEWER
                TryGetComponent(out component);
#else
                component = GetComponent<AudioEchoFilter>();
#endif
                if (component) component.enabled = false;
            }
            {
                AudioHighPassFilter component;
#if UNITY_2019_4_OR_NEWER
                TryGetComponent(out component);
#else
                component = GetComponent<AudioHighPassFilter>();
#endif
                if (component) component.enabled = false;
            }
            {
                AudioLowPassFilter component;
#if UNITY_2019_4_OR_NEWER
                TryGetComponent(out component);
#else
                component = GetComponent<AudioLowPassFilter>();
#endif
                if (component) component.enabled = false;
            }
            {
                AudioReverbFilter component;
#if UNITY_2019_4_OR_NEWER
                TryGetComponent(out component);
#else
                component = GetComponent<AudioReverbFilter>();
#endif
                if (component) component.enabled = false;
            }
        }

        IEnumerator FadeIn(bool queueFadeout)
        {
            // Clip is already designated for us
            float fadeTime = audioFile.fadeInDuration * aSource.clip.length;
            // Check if FadeTime isn't actually just 0
            if (fadeTime != 0) // To prevent a division by zero
            {
                float timer = 0;
                while (timer < fadeTime)
                {
                    timer += Time.deltaTime;
                    aSource.volume = Mathf.Lerp(0, AudioManager.GetTrueSoundVolume(), timer / fadeTime);
                    yield return null;
                }
            }

            if (queueFadeout)
            {
                StartCoroutine(FadeOut());
            }
            else if (!looping)
            {
                enabled = false;
            }
        }

        IEnumerator FadeOut()
        {
            float fadeTime = audioFile.fadeOutDuration * aSource.clip.length;
            if (fadeTime != 0)
            {
                float timer = 0;
                while (timer < fadeTime)
                {
                    if (aSource.time >= aSource.clip.length - fadeTime)
                    {
                        aSource.volume = Mathf.Lerp(0, AudioManager.GetTrueSoundVolume(), (aSource.clip.length - aSource.time) / fadeTime);
                    }
                    yield return null;
                }
            }

            // Disable updates on this script if we only play once
            if (!looping)
            {
                enabled = false;
            }
        }
    }
}