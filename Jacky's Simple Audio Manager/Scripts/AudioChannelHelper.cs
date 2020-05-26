using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
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

        public void Play(float delay, AudioFileObject file, bool loop = false)
        {
            // Make sure no remnants from a previous sound remain
            StopAllCoroutines();
            aSource.Stop();
            enabled = true; // Enable updates on the script
            aSource.PlayDelayed(delay);
            audioFile = file;
            looping = loop;
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
        public void PlayDebug(AudioFileObject file)
        {
            aSource.Stop();
            aSource.Play();
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

        AudioLowPassFilter lowPassFilter;
        AudioHighPassFilter highPassFilter;

        public void ApplyEffects()
        {
            if (audioFile.lowPassFilter.enabled)
            {
                if (!lowPassFilter)
                {
                    lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                }
                lowPassFilter.cutoffFrequency = audioFile.lowPassFilter.cutoffFrequency;
                lowPassFilter.lowpassResonanceQ = audioFile.lowPassFilter.lowpassResonanceQ;
            }
            else if (lowPassFilter) lowPassFilter.enabled = false;

            if (audioFile.highPassFilter.enabled)
            {
                if (!highPassFilter)
                {
                    highPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
                }
                highPassFilter.cutoffFrequency = audioFile.highPassFilter.cutoffFrequency;
                highPassFilter.highpassResonanceQ = audioFile.highPassFilter.highpassResonanceQ;
            }
            else if (highPassFilter) highPassFilter.enabled = false;
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