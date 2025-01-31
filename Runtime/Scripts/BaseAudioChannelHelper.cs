using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace JSAM
{
    public abstract class BaseAudioChannelHelper<T> : MonoBehaviour, IAudioHelperEvents where T : BaseAudioFileObject
    {
        /// <summary>
        /// Set this to true to make AudioManager ignore this AudioChannel for Audio Playback. 
        /// Good if you want to reserve this channel for certain entities that can't have overlapping sounds
        /// </summary>
        public bool Reserved = false;
        /// <summary>
        /// Returns true if this Audio Channel is not playing any sounds and is not marked as "Reserved"
        /// </summary>
        public bool IsFree { get { return !Reserved && !enabled; } }

        protected T audioFile;
        public T AudioFile { get { return audioFile; } }

        protected abstract VolumeChannel DefaultChannel { get; }

        public VolumeChannel Channel
        {
            get
            {
                var channel = DefaultChannel;
                if (!audioFile) return channel;
                if (audioFile.channelOverride != VolumeChannel.None)
                {
                    channel = audioFile.channelOverride;
                }
                return channel;
            }
        }

        public float Volume
        {
            get
            {
                var vol = 0f;
                switch (Channel)
                {
                    case VolumeChannel.Music:
                        vol = AudioManager.InternalInstance.ModifiedMusicVolume;
                        break;
                    case VolumeChannel.Sound:
                        vol = AudioManager.InternalInstance.ModifiedSoundVolume;
                        break;
                    case VolumeChannel.Voice:
                        vol = AudioManager.InternalInstance.ModifiedVoiceVolume;
                        break;
                }
                if (audioFile) vol *= audioFile.relativeVolume;
                return vol;
            }
        }

        /// <summary>
        /// This property will only be assigned to if both the AudioFileObject and the AudioManager have spatialization enabled
        /// </summary>
        public Transform SpatializationTarget { get; private set; }
        /// <summary>
        /// This property will only be assigned to if both the AudioFileObject and the AudioManager have spatialization enabled
        /// </summary>
        public Vector3 SpatializationPosition { get; private set; }

        protected int LoopStart { get { return (int)(audioFile.loopStart * AudioSource.clip.frequency); } }
        protected int LoopEnd { get { return (int)(audioFile.loopEnd * AudioSource.clip.frequency); } }

        protected AudioChorusFilter chorusFilter;
        protected AudioDistortionFilter distortionFilter;
        protected AudioEchoFilter echoFilter;
        protected AudioHighPassFilter highPassFilter;
        protected AudioLowPassFilter lowPassFilter;
        protected AudioReverbFilter reverbFilter;

        public AudioSource AudioSource { get; private set; }
        protected AudioMixerGroup defaultMixerGroup;

        protected Transform originalParent;

        Coroutine fadeInRoutine, fadeOutRoutine;
        bool subscribedToEvents;
        bool applicationPaused, applicationFocused = true;
        protected void OnApplicationPause(bool pause)
        {
            applicationPaused = pause;
        }
        protected void OnApplicationFocus(bool focus)
        {
            applicationFocused = focus;
        }

        public void Init(AudioMixerGroup defaultGroup)
        {
            AudioSource = GetComponent<AudioSource>();
            enabled = false;
            originalParent = transform.parent;
            defaultMixerGroup = defaultGroup;
        }

        protected virtual void OnEnable()
        {
            if (JSAMSettings.Settings.TimeScaledSounds)
            {
                AudioManagerInternal.OnTimeScaleChanged.Add(this);
            }

            if (audioFile)
            {
                if (audioFile.spatialize)
                {
                    switch (JSAMSettings.Settings.SpatializationMode)
                    {
                        case JSAMSettings.SpatializeUpdateMode.Default:
                            AudioManagerInternal.OnSpatializeUpdate.Add(this);
                            break;
                        case JSAMSettings.SpatializeUpdateMode.FixedUpdate:
                            AudioManagerInternal.OnSpatializeFixedUpdate.Add(this);
                            break;
                        case JSAMSettings.SpatializeUpdateMode.LateUpdate:
                            AudioManagerInternal.OnSpatializeLateUpdate.Add(this);
                            break;
                        case JSAMSettings.SpatializeUpdateMode.Parented:
                            break;
                    }
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (JSAMSettings.Settings.TimeScaledSounds)
            {
                AudioManagerInternal.OnTimeScaleChanged.Remove(this);
            }

            if (audioFile)
            {
                if (audioFile.spatialize)
                {
                    switch (JSAMSettings.Settings.SpatializationMode)
                    {
                        case JSAMSettings.SpatializeUpdateMode.Default:
                            AudioManagerInternal.OnSpatializeUpdate.Remove(this);
                            break;
                        case JSAMSettings.SpatializeUpdateMode.FixedUpdate:
                            AudioManagerInternal.OnSpatializeFixedUpdate.Remove(this);
                            break;
                        case JSAMSettings.SpatializeUpdateMode.LateUpdate:
                            AudioManagerInternal.OnSpatializeLateUpdate.Remove(this);
                            break;
                        case JSAMSettings.SpatializeUpdateMode.Parented:
                            break;
                    }
                }
            }

            if (subscribedToEvents) UnsubscribeFromAudioEvents();
        }

        protected virtual void Update()
        {
            if (audioFile.fadeInOut)
            {
                var fadeInTime = audioFile.fadeInDuration * AudioSource.clip.length;
                var fadeOutTime = audioFile.fadeOutDuration * AudioSource.clip.length;

                if (AudioSource.time < AudioSource.clip.length - fadeOutTime)
                {
                    if (fadeInTime > 0)
                    {
                        AudioSource.volume = Mathf.Lerp(0, audioFile.relativeVolume, AudioSource.time / fadeInTime);
                    }
                }
                else
                {
                    if (fadeOutTime > 0)
                    {
                        AudioSource.volume = Mathf.Lerp(0, audioFile.relativeVolume, (AudioSource.clip.length - AudioSource.time) / fadeOutTime);
                    }
                }
            }

            if (audioFile.loopMode >= LoopMode.LoopWithLoopPoints)
            {
                if (AudioSource.timeSamples >= LoopEnd || !AudioSource.isPlaying)
                {
                    AudioSource.Play();
                    AudioSource.timeSamples = LoopStart;
                }

                if (audioFile.loopMode == LoopMode.ClampedLoopPoints)
                {
                    if (AudioSource.timeSamples < LoopStart)
                    {
                        AudioSource.timeSamples = LoopStart;
                    }
                }
            }
            else if (audioFile.loopMode <= LoopMode.LoopWithLoopPoints && !applicationPaused && applicationFocused)
            {
                // Disable self if not playing anymore
                enabled = AudioSource.isPlaying;
            }
        }

        protected void SubscribeToVolumeEvents()
        {
            switch (Channel)
            {
                case VolumeChannel.Music:
                    AudioManagerInternal.OnMusicVolumeChanged.Add(this);
                    break;
                case VolumeChannel.Sound:
                    AudioManagerInternal.OnSoundVolumeChanged.Add(this);
                    break;
                case VolumeChannel.Voice:
                    AudioManagerInternal.OnVoiceVolumeChanged.Add(this);
                    break;
            }
            subscribedToEvents = true;
        }

        protected void UnsubscribeFromAudioEvents()
        {
            switch (Channel)
            {
                case VolumeChannel.Music:
                    AudioManagerInternal.OnMusicVolumeChanged.Remove(this);
                    break;
                case VolumeChannel.Sound:
                    AudioManagerInternal.OnSoundVolumeChanged.Remove(this);
                    break;
                case VolumeChannel.Voice:
                    AudioManagerInternal.OnVoiceVolumeChanged.Remove(this);
                    break;
            }
            subscribedToEvents = false;
        }

        protected void ClearProperties()
        {
            // Make sure no remnants from a previous sound remain
            StopAllCoroutines();
            AudioSource.Stop();
            if (AudioSource.clip is AudioClip)
            {
                AudioSource.timeSamples = 0;
            }
            if (audioFile) UnsubscribeFromAudioEvents();
        }

        public void AssignNewFile(T file)
        {
            ClearProperties();

            audioFile = file;
        }

        /// <summary>
        /// To play new sounds one after another using the same Helper, 
        /// call the following methods in order; 
        /// helper.AssignNewFile(file),
        /// helper.SetSpatializationTarget(targetTransform),
        /// helper.Play()
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public virtual AudioSource Play()
        {
            if (!AssignNewAudioClip())
            {
                return AudioSource;
            }

            if (JSAMSettings.Settings.Spatialize && audioFile.spatialize)
            {
                AudioSource.spatialBlend = 1;
                if (audioFile.maxDistance != 0)
                {
                    AudioSource.maxDistance = audioFile.maxDistance;
                }
                else AudioSource.maxDistance = JSAMSettings.Settings.DefaultSoundMaxDistance;
            }
            else
            {
                AudioSource.spatialBlend = 0;
            }

            if (!subscribedToEvents) SubscribeToVolumeEvents();
            AudioSource.volume = Volume;

            AudioSource.outputAudioMixerGroup = audioFile.mixerGroupOverride ? audioFile.mixerGroupOverride : defaultMixerGroup;

            AudioSource.priority = (int)audioFile.priority;

            AudioSource.pitch = audioFile.GetRandomPitch();

            switch (audioFile.loopMode)
            {
                case LoopMode.NoLooping:
                    AudioSource.loop = false;
                    break;
                case LoopMode.Looping:
                case LoopMode.LoopWithLoopPoints:
                case LoopMode.ClampedLoopPoints:
                    AudioSource.loop = true;
                    break;
            }

            ApplyEffects();

            AudioSource.PlayDelayed(audioFile.delay);
            enabled = true;

            return AudioSource;
        }

        public virtual void Stop(bool stopInstantly = true)
        {
            if (stopInstantly) AudioSource.Stop();
            StopAllCoroutines();
            enabled = false;
            AudioSource.loop = false;
        }

        public virtual void TimeScaleChanged(float previousTimeScale)
        {
            if (audioFile.ignoreTimeScale) return;
            float offset = AudioSource.pitch - previousTimeScale;
            AudioSource.pitch = Time.timeScale;
            AudioSource.pitch += offset;
        }


        /// <summary>
        /// Returns false if no AudioClips exists
        /// </summary>
        /// <returns></returns>
        public bool AssignNewAudioClip()
        {
            if (audioFile.Files.Count > 1)
            {
                int index;
                do
                {
                    index = Random.Range(0, audioFile.Files.Count);
                    AudioSource.clip = audioFile.Files[index];
                    if (AudioSource.clip == null)
                    {
                        Debug.LogWarning("Missing AudioClip at index " + index +
                            " in " + audioFile.SafeName + "'s library!");
                    }
                } while (index == audioFile.lastClipIndex && audioFile.neverRepeat);
                if (audioFile.neverRepeat)
                {
                    audioFile.lastClipIndex = index;
                }
                return true;
            }
            else if (audioFile.Files.Count == 1)
            {
                AudioSource.clip = audioFile.Files[0];
                return true;
            }
            return false;
        }

        public void Spatialize()
        {
            if (SpatializationTarget != null)
            {
                transform.position = SpatializationTarget.position;
            }
        }

        public virtual void SetSpatializationTarget(Transform target)
        {
            if (target == null) return;
            if (!audioFile) return;
            if (!audioFile.spatialize) return;
            if (!JSAMSettings.Settings.Spatialize) return;

            switch (JSAMSettings.Settings.SpatializationMode)
            {
                case JSAMSettings.SpatializeUpdateMode.Default:
                case JSAMSettings.SpatializeUpdateMode.FixedUpdate:
                case JSAMSettings.SpatializeUpdateMode.LateUpdate:
                    transform.SetParent(originalParent);
                    SpatializationTarget = target;
                    break;
                case JSAMSettings.SpatializeUpdateMode.Parented:
                    transform.SetParent(target);
                    break;
            }
            transform.position = target.position;
        }

        public virtual void SetSpatializationTarget(Vector3 position)
        {
            if (!audioFile) return;
            if (!audioFile.spatialize) return;
            if (!JSAMSettings.Settings.Spatialize) return;

            SpatializationTarget = null;
            SpatializationPosition = position;
            transform.position = position;
        }

        public void VolumeChanged(float channelVolume, float realVolume)
        {
            AudioSource.volume = realVolume * audioFile.relativeVolume;
        }

        #region Fade Logic
        public void BeginFadeIn(float fadeTime)
        {
            if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
            fadeInRoutine = StartCoroutine(FadeIn(fadeTime));
        }

        public void BeginFadeOut(float fadeTime)
        {
            if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
            fadeOutRoutine = StartCoroutine(FadeOut(fadeTime));
        }

        /// <summary>
        /// </summary>
        /// <param name="fadeTime">Fade-in time in seconds</param>
        /// <returns></returns>
        protected IEnumerator FadeIn(float fadeTime)
        {
            // Check if FadeTime isn't actually just 0
            if (fadeTime != 0) // To prevent a division by zero
            {
                float timer = 0;
                while (timer < fadeTime)
                {
                    if (audioFile.ignoreTimeScale) timer += Time.unscaledDeltaTime;
                    else timer += Time.deltaTime;

                    AudioSource.volume = Mathf.Lerp(0, Volume, timer / fadeTime);
                    yield return null;
                }
            }

            fadeInRoutine = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="fadeTime">Fade-out time in seconds</param>
        /// <returns></returns>
        protected virtual IEnumerator FadeOut(float fadeTime)
        {
            if (fadeTime > 0)
            {
                float startingVolume = AudioSource.volume;
                float timer = 0;
                while (timer < fadeTime)
                {
                    if (audioFile.ignoreTimeScale) timer += Time.unscaledDeltaTime;
                    else timer += Time.deltaTime;

                    AudioSource.volume = Mathf.Lerp(startingVolume, 0, timer / fadeTime);
                    yield return null;
                }
                AudioSource.Stop();
            }

            fadeOutRoutine = null;
        }
        #endregion

        #region Audio Effects
        public void ApplyEffects()
        {
            AudioSource.bypassEffects = audioFile.bypassEffects;
            AudioSource.bypassListenerEffects = audioFile.bypassListenerEffects;
            AudioSource.bypassReverbZones = audioFile.bypassReverbZones;

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
                if (this.TryForComponent(out component))
                {
                    component.enabled = false;
                }
            }
            {
                AudioDistortionFilter component;
                if (this.TryForComponent(out component))
                {
                    component.enabled = false;
                }
            }
            {
                AudioEchoFilter component;
                if (this.TryForComponent(out component))
                {
                    component.enabled = false;
                }
            }
            {
                AudioHighPassFilter component;
                if (this.TryForComponent(out component))
                {
                    component.enabled = false;
                }
            }
            {
                AudioLowPassFilter component;
                if (this.TryForComponent(out component))
                {
                    component.enabled = false;
                }
            }
            {
                AudioReverbFilter component;
                if (this.TryForComponent(out component))
                {
                    component.enabled = false;
                }
            }
        }
        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Just for the playing of generic AudioClips
        /// </summary>
        /// <param name="dontReset"></param>
        public void PlayDebug(bool dontReset)
        {
            if (!dontReset)
            {
                AudioSource.Stop();
            }
            ClearEffects();
            AudioSource.timeSamples = (int)Mathf.Clamp((float)AudioSource.timeSamples, 0, (float)AudioSource.clip.samples - 1);
            AudioSource.Play();
            AudioSource.pitch = 1;

            // Don't apply if inspecting an AudioClip
            if (AudioFile) ApplyEffects();
        }

        public void PlayDebug(BaseAudioFileObject file, bool dontReset)
        {
            if (!dontReset)
            {
                AudioSource.Stop();
            }
            AudioSource.timeSamples = (int)Mathf.Clamp((float)AudioSource.timeSamples, 0, (float)AudioSource.clip.samples - 1);
            AudioSource.pitch = file.GetRandomPitch();

            audioFile = file as T;

            AudioSource.volume = file.relativeVolume;
            AudioSource.priority = (int)file.priority;
            float offset = AudioSource.pitch - 1;
            AudioSource.pitch = Time.timeScale + offset;

            ClearEffects();
            ApplyEffects();

            AudioSource.PlayDelayed(file.delay);
            enabled = true; // Enable updates on the script
        }
#endif
    }
}