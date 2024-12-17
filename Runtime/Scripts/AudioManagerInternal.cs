using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace JSAM
{
    [AddComponentMenu("")]
    public class AudioManagerInternal : MonoBehaviour
    {
        public class LoadedLibrary
        {
            public AudioLibrary Library;
            public long[] SoundKeys;
            public long[] MusicKeys;
            public int Users;
        }

        readonly Dictionary<AudioLibrary, LoadedLibrary> loadedLibraries = new Dictionary<AudioLibrary, LoadedLibrary>();
        readonly Dictionary<string, BaseAudioFileObject> audioFileLookup = new Dictionary<string, BaseAudioFileObject>();
        readonly Dictionary<long, string> enumNameLookup = new Dictionary<long, string>();
        public Dictionary<AudioLibrary, LoadedLibrary> LoadedLibraries => loadedLibraries;
        public bool IsLibraryLoaded(AudioLibrary library) => loadedLibraries.ContainsKey(library);

        /// <summary>
        /// Sources dedicated to playing sound
        /// </summary>
        readonly List<SoundChannelHelper> soundHelpers = new List<SoundChannelHelper>();

        /// <summary>
        /// Sources dedicated to playing music
        /// </summary>
        readonly List<MusicChannelHelper> musicHelpers = new List<MusicChannelHelper>();
        public MusicChannelHelper MainMusic { get; private set; }

        JSAMSettings Settings => JSAMSettings.Settings;

        #region Volume Logic
        public bool MasterMuted = false;
        public float MasterVolume = 1;
        public float ModifiedMasterVolume => MasterVolume * Convert.ToInt32(!MasterMuted);

        public bool MusicMuted = false;
        public float MusicVolume = 1;
        public float ModifiedMusicVolume => ModifiedMasterVolume * MusicVolume * Convert.ToInt32(!MusicMuted);

        public bool SoundMuted = false;
        public float SoundVolume = 1;
        public float ModifiedSoundVolume => ModifiedMasterVolume * SoundVolume * Convert.ToInt32(!SoundMuted);

        public bool VoiceMuted = false;
        public float VoiceVolume = 1;
        public float ModifiedVoiceVolume => ModifiedMasterVolume * VoiceVolume * Convert.ToInt32(!VoiceMuted);

        public void SaveVolumeSettings()
        {
            if (!JSAMSettings.Settings.SaveVolumeToPlayerPrefs) return;

            PlayerPrefs.SetFloat(Settings.MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(Settings.MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(Settings.SoundVolumeKey, SoundVolume);
            PlayerPrefs.SetFloat(Settings.VoiceVolumeKey, VoiceVolume);

            PlayerPrefs.SetInt(Settings.MasterMutedKey, Convert.ToInt16(MasterMuted));
            PlayerPrefs.SetInt(Settings.MusicMutedKey, Convert.ToInt16(MusicMuted));
            PlayerPrefs.SetInt(Settings.SoundMutedKey, Convert.ToInt16(SoundMuted));
            PlayerPrefs.SetInt(Settings.VoiceMutedKey, Convert.ToInt16(VoiceMuted));

            PlayerPrefs.Save();
        }

        public void LoadVolumeSettings()
        {
            if (!Settings.SaveVolumeToPlayerPrefs) return;

            MasterVolume = PlayerPrefs.GetFloat(Settings.MasterVolumeKey, 1);
            MusicVolume = PlayerPrefs.GetFloat(Settings.MusicVolumeKey, 1);
            SoundVolume = PlayerPrefs.GetFloat(Settings.SoundVolumeKey, 1);
            VoiceVolume = PlayerPrefs.GetFloat(Settings.VoiceVolumeKey, 1);

            MasterMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.MasterMutedKey, 0));
            MusicMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.MusicMutedKey, 0));
            SoundMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.SoundMutedKey, 0));
            VoiceMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.VoiceMutedKey, 0));
        }

        void MusicVolumeChanged(float channelVolume, float realVolume)
        {
            foreach (var helper in OnMusicVolumeChanged) helper.VolumeChanged(channelVolume, realVolume);
        }
        void SoundVolumeChanged(float channelVolume, float realVolume)
        {
            foreach (var helper in OnSoundVolumeChanged) helper.VolumeChanged(channelVolume, realVolume);
        }
        void VoiceVolumeChanged(float channelVolume, float realVolume)
        {
            foreach (var helper in OnVoiceVolumeChanged) helper.VolumeChanged(channelVolume, realVolume);
        }
        #endregion

        /// <summary>
        /// This object holds all AudioChannels
        /// </summary>
        Transform sourceHolder;

        [SerializeField] GameObject sourcePrefab;

        float prevTimeScale = 1;

        // Passing a helperSource into a Play function bypasses audio limiting dictionaries.
        // So we check if their keys exist before removing.
        readonly Dictionary<BaseAudioFileObject, List<SoundChannelHelper>> limitedSounds = new Dictionary<BaseAudioFileObject, List<SoundChannelHelper>>();
        public void RemovePlayingSound(BaseAudioFileObject s, SoundChannelHelper h)
        {
            if (limitedSounds.ContainsKey(s)) limitedSounds[s].Remove(h);
        }
        readonly Dictionary<BaseAudioFileObject, List<MusicChannelHelper>> limitedMusic = new Dictionary<BaseAudioFileObject, List<MusicChannelHelper>>();
        public void RemovePlayingMusic(BaseAudioFileObject s, MusicChannelHelper h)
        {
            if (limitedMusic.ContainsKey(s)) limitedMusic[s].Remove(h);
        }

        /// <summary>
        /// A bit like float Epsilon, but large enough for the purpose of pushing the playback position of AudioSources just far enough to not throw an error
        /// </summary>
        public const float EPSILON = 0.000001f;

        /// <summary>
        /// Notifies Audio Channels to follow their target. 
        /// Only invoked when Spatialize is set to true
        /// </summary>
        public static List<IAudioHelperEvents> OnSpatializeUpdate = new List<IAudioHelperEvents>();
        /// <summary>
        /// Notifies Audio Channels to follow their target on LateUpdate 
        /// Only invoked when Spatialize is set to true
        /// </summary>
        public static List<IAudioHelperEvents> OnSpatializeLateUpdate = new List<IAudioHelperEvents>();
        /// <summary>
        /// Notifies Audio Channels to follow their target on FixedUpdate
        /// Only invoked when Spatialize is set to true
        /// </summary>
        public static List<IAudioHelperEvents> OnSpatializeFixedUpdate = new List<IAudioHelperEvents>();
        /// <summary>
        /// <para>float previousTimeScale</para>
        /// Invoked when the user changes the TimeScale
        /// Notifies Audio Channels to adjust pitch accordingly. 
        /// </summary>
        public static List<IAudioHelperEvents> OnTimeScaleChanged = new List<IAudioHelperEvents>();
        /// <summary>
        /// Notifies Audio Helpers of a specific channel to adjust their Volume
        /// </summary>
        public static List<IAudioHelperEvents> OnMusicVolumeChanged = new List<IAudioHelperEvents>();
        /// <summary> <inheritdoc cref="OnMusicVolumeChanged"/> </summary>
        public static List<IAudioHelperEvents> OnSoundVolumeChanged = new List<IAudioHelperEvents>();
        /// <summary> <inheritdoc cref="OnMusicVolumeChanged"/> </summary>
        public static List<IAudioHelperEvents> OnVoiceVolumeChanged = new List<IAudioHelperEvents>();

        public static AudioManagerInternal Instance => AudioManager.InternalInstance;

        void Awake()
        {
            LoadVolumeSettings();

            sourceHolder = new GameObject("Sources").transform;
            for (int i = 0; i < Settings.StartingSoundChannels; i++)
            {
                soundHelpers.Add(CreateSoundChannel());
            }

            for (int i = 0; i < Settings.StartingMusicChannels; i++)
            {
                musicHelpers.Add(CreateMusicChannel());
            }
            if (musicHelpers.Count > 0) MainMusic = musicHelpers[0];

            AudioManager.OnMusicVolumeChanged += MusicVolumeChanged;
            AudioManager.OnSoundVolumeChanged += SoundVolumeChanged;
            AudioManager.OnVoiceVolumeChanged += VoiceVolumeChanged;
        }

        private void Start()
        {
            sourceHolder.SetParent(transform);
        }

        // Update is called once per frame
        void Update()
        {
            if (Settings.SpatializationMode == JSAMSettings.SpatializeUpdateMode.Default)
            {
                foreach (var item in OnSpatializeUpdate)
                {
                    item.Spatialize();
                }
            }

            if (Mathf.Abs(Time.timeScale - prevTimeScale) > 0)
            {
                foreach (var item in OnSpatializeUpdate)
                {
                    item.TimeScaleChanged(prevTimeScale);
                }
            }
            prevTimeScale = Time.timeScale;
        }

        void FixedUpdate()
        {
            if (Settings.SpatializationMode == JSAMSettings.SpatializeUpdateMode.FixedUpdate)
            {
                foreach (var item in OnSpatializeFixedUpdate)
                {
                    item.Spatialize();
                }
            }
        }

        void LateUpdate()
        {
            if (Settings.SpatializationMode == JSAMSettings.SpatializeUpdateMode.LateUpdate)
            {
                foreach (var item in OnSpatializeLateUpdate)
                {
                    item.Spatialize();
                }
            }
        }

        private void OnDestroy()
        {
            SaveVolumeSettings();

            AudioManager.OnMusicVolumeChanged -= MusicVolumeChanged;
            AudioManager.OnSoundVolumeChanged -= SoundVolumeChanged;
            AudioManager.OnVoiceVolumeChanged -= VoiceVolumeChanged;
        }

        MusicChannelHelper HandleLimitedInstances(MusicFileObject music, MusicChannelHelper helper)
        {
            if (music.maxPlayingInstances > 0)
            {
                if (limitedMusic.ContainsKey(music))
                {
                    if (limitedMusic[music].Count > music.maxPlayingInstances)
                    {
                        var h = limitedMusic[music][0];
                        limitedMusic[music].RemoveAt(0);
                        limitedMusic[music].Add(h);
                        return h;
                    }
                }
                else
                {
                    limitedMusic.Add(music, new List<MusicChannelHelper>());
                }
                limitedMusic[music].Add(helper);
            }
            return helper;
        }

        bool PlaybackChecks(BaseAudioFileObject file)
        {
            if (!Application.isPlaying) return false;

            if (file) return true;
            else
            {
                AudioManager.DebugWarning("AudioManager was passed a null Audio File Object!");
                return false;
            }
        }

        #region PlayMusic
        public MusicChannelHelper PlayMusicInternal(MusicFileObject music, bool isMain)
        {
            if (!PlaybackChecks(music)) return null;

            if (isMain) PlayMusicInternal(music, null, MainMusic);
            else PlayMusicInternal(music, null, null);

            AudioManager.OnMusicPlayed?.Invoke(MainMusic, music);

            return MainMusic;
        }

        public MusicChannelHelper PlayMusicInternal(MusicFileObject music, Transform newTransform = null, MusicChannelHelper helper = null)
        {
            if (!PlaybackChecks(music)) return null;

            bool helperOverride = helper != null;
            if (helper == null) helper = GetFreeMusicHelper();
            if (helper == null) return null;
            if (!helperOverride)
            {
                helper = HandleLimitedInstances(music, helper);
            }
            helper.AssignNewFile(music);
            helper.SetSpatializationTarget(newTransform);
            helper.Play();
            AudioManager.OnMusicPlayed?.Invoke(helper, music);

            return helper;
        }

        public MusicChannelHelper PlayMusicInternal(MusicFileObject music, Vector3 position, MusicChannelHelper helper = null)
        {
            if (!PlaybackChecks(music)) return null;

            bool helperOverride = helper != null;
            if (helper == null) helper = GetFreeMusicHelper();
            if (helper == null) return null;
            if (!helperOverride)
            {
                helper = HandleLimitedInstances(music, helper);
            }
            helper.AssignNewFile(music);
            helper.SetSpatializationTarget(position);
            helper.Play();
            AudioManager.OnMusicPlayed?.Invoke(helper, music);

            return helper;
        }
        #endregion

        #region FadeMusic
        public MusicChannelHelper FadeMusicInInternal(MusicFileObject music, float fadeInTime, bool isMain)
        {
            if (!PlaybackChecks(music)) return null;

            MusicChannelHelper helper;

            if (isMain)
            {
                helper = MainMusic;
            }
            else
            {
                helper = musicHelpers[GetFreeMusicChannel()];
                if (helper == null) return null;
                helper = HandleLimitedInstances(music, helper);
            }

            helper.AssignNewFile(music);
            helper.Play();
            helper.BeginFadeIn(fadeInTime);

            AudioManager.OnMusicPlayed?.Invoke(helper, music);

            return helper;
        }

        public MusicChannelHelper FadeMainMusicOutInternal(float fadeOutTime)
        {
            if (!Application.isPlaying) return null;

            var helper = MainMusic;
            if (!MainMusic)
            {
                // As long as Awake as called before then, this line shouldn't be reached
                AudioManager.DebugWarning("Tried to fade out Main Music when no music was marked as Main! Marking now.");
                MainMusic = musicHelpers[0];
            }

            var newHelper = musicHelpers[GetFreeMusicChannel()];
            newHelper.AssignNewFile(MainMusic.AudioFile);
            newHelper.Play();
            newHelper.AudioSource.time = MainMusic.AudioSource.time;
            newHelper.BeginFadeOut(fadeOutTime);
            MainMusic.Stop();

            return newHelper;
        }

        public MusicChannelHelper FadeMusicOutInternal(MusicFileObject music, float fadeOutTime)
        {
            if (!PlaybackChecks(music)) return null;

            MusicChannelHelper helper;
            if (TryGetPlayingMusic(music, out helper))
            {
                helper.BeginFadeOut(fadeOutTime);
            }
            else
            {
                AudioManager.DebugWarning("Cannot fade out track " + music + " because track " +
                    "is not currently playing!");
            }

            return helper;
        }

        public MusicChannelHelper FadeMusicOutInternal(MusicChannelHelper helper, float fadeOutTime)
        {
            if (!Application.isPlaying) return null;

            if (!helper)
            {
                AudioManager.DebugWarning("AudioManager was passed a null music helper!");
                return null;
            }

            if (helper)
            {
                helper.BeginFadeOut(fadeOutTime);
            }
            else
            {
                AudioManager.DebugError("Music Fade Out Failed! Provided Music Channel Helper was null!");
            }

            return helper;
        }
        #endregion

        #region StopMusic
        public void StopAllMusicInternal(bool stopInstantly)
        {
            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioSource.isPlaying)
                {
                    musicHelpers[i].Stop(stopInstantly);
                }
            }
        }

        public MusicChannelHelper StopMusicInternal(MusicFileObject music, Transform t, bool stopInstantly)
        {
            if (!PlaybackChecks(music)) return null;

            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioSource == null) return null; // Prevent issues when called during OnApplicationQuit
                if (music.Files.Contains(musicHelpers[i].AudioSource.clip))
                {
                    if (t != null && music.spatialize)
                    {
                        if (musicHelpers[i].SpatializationTarget != t) continue;
                    }
                    musicHelpers[i].Stop(stopInstantly);
                    return musicHelpers[i];
                }
            }
            return null;
        }

        public MusicChannelHelper StopMusicInternal(MusicFileObject music, Vector3 pos, bool stopInstantly)
        {
            if (!PlaybackChecks(music)) return null;

            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioSource == null) return null; // Prevent issues when called from OnDestroy
                if (music.Files.Contains(musicHelpers[i].AudioSource.clip))
                {
                    if (musicHelpers[i].SpatializationPosition != pos && music.spatialize) continue;
                    musicHelpers[i].Stop(stopInstantly);
                    return musicHelpers[i];
                }
            }
            return null;
        }

        public bool StopMusicIfPlayingInternal(MusicFileObject music, Transform trans = null, bool stopInstantly = true)
        {
            if (!IsMusicPlayingInternal(music, trans)) return false;
            StopMusicInternal(music, trans, stopInstantly);
            return true;
        }

        public bool StopMusicIfPlayingInternal(MusicFileObject music, Vector3 pos, bool stopInstantly = true)
        {
            if (!IsMusicPlayingInternal(music, pos)) return false;
            StopMusicInternal(music, pos, stopInstantly);
            return true;
        }
        #endregion

        SoundChannelHelper HandleLimitedInstances(SoundFileObject sound, SoundChannelHelper helper)
        {
            if (sound.maxPlayingInstances > 0)
            {
                if (limitedSounds.ContainsKey(sound))
                {
                    if (limitedSounds[sound].Count >= sound.maxPlayingInstances)
                    {
                        var h = limitedSounds[sound][0];
                        limitedSounds[sound].RemoveAt(0);
                        limitedSounds[sound].Add(h);
                        return h;
                    }
                }
                else
                {
                    limitedSounds.Add(sound, new List<SoundChannelHelper>());
                }
                limitedSounds[sound].Add(helper);
            }
            return helper;
        }

        #region PlaySound
        public SoundChannelHelper PlaySoundInternal(SoundFileObject sound, Transform newTransform = null, SoundChannelHelper helper = null)
        {
            if (!PlaybackChecks(sound)) return null;

            bool helperOverride = helper != null;
            if (helper == null) helper = soundHelpers[GetFreeSoundChannel()];
            if (helper == null) return null;
            if (!helperOverride)
            {
                helper = HandleLimitedInstances(sound, helper);
            }
            helper.AssignNewFile(sound);
            helper.SetSpatializationTarget(newTransform);
            helper.Play();
            AudioManager.OnSoundPlayed?.Invoke(helper, sound);

            return helper;
        }

        public SoundChannelHelper PlaySoundInternal(SoundFileObject sound, Vector3 position, SoundChannelHelper helper = null)
        {
            if (!PlaybackChecks(sound)) return null;

            bool helperOverride = helper != null;
            if (helper == null) helper = soundHelpers[GetFreeSoundChannel()];
            if (helper == null) return null;
            if (!helperOverride)
            {
                helper = HandleLimitedInstances(sound, helper);
            }
            helper.AssignNewFile(sound);
            helper.SetSpatializationTarget(position);
            helper.Play();
            AudioManager.OnSoundPlayed?.Invoke(helper, sound);

            return helper;
        }
        #endregion

        #region StopSound
        public void StopAllSoundsInternal(bool stopInstantly = true)
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioSource.isPlaying)
                {
                    soundHelpers[i].Stop(stopInstantly);
                }
            }
        }

        public SoundChannelHelper StopSoundInternal(SoundFileObject sound, Transform t = null, bool stopInstantly = true)
        {
            if (!PlaybackChecks(sound)) return null;

            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioSource == null) return null; // Prevent issues when called from OnDestroy
                if (sound.Files.Contains(soundHelpers[i].AudioSource.clip))
                {
                    if (t != null && sound.spatialize)
                    {
                        if (soundHelpers[i].SpatializationTarget != t) continue;
                    }
                    soundHelpers[i].Stop(stopInstantly);
                    return soundHelpers[i];
                }
            }
            return null;
        }

        public SoundChannelHelper StopSoundInternal(SoundFileObject sound, Vector3 pos, bool stopInstantly = true)
        {
            if (!PlaybackChecks(sound)) return null;

            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioSource == null) return null; // Prevent issues when called from OnDestroy
                if (sound.Files.Contains(soundHelpers[i].AudioSource.clip))
                {
                    if (soundHelpers[i].SpatializationPosition != pos && sound.spatialize) continue;
                    soundHelpers[i].Stop(stopInstantly);
                    return soundHelpers[i];
                }
            }
            return null;
        }

        public bool StopSoundIfPlayingInternal(SoundFileObject sound, Transform trans = null, bool stopInstantly = true)
        {
            if (!IsSoundPlayingInternal(sound, trans)) return false;
            StopSoundInternal(sound, trans, stopInstantly);
            return true;
        }

        public bool StopSoundIfPlayingInternal(SoundFileObject sound, Vector3 pos, bool stopInstantly = true)
        {
            if (!IsSoundPlayingInternal(sound, pos)) return false;
            StopSoundInternal(sound, pos, stopInstantly);
            return true;
        }
        #endregion

        public MusicChannelHelper GetFreeMusicHelper() => musicHelpers[GetFreeMusicChannel()];

        /// <returns>The index of the next free music channel</returns>
        int GetFreeMusicChannel()
        {
            for (int i = 0; i < musicHelpers.Count; i++)
            {
                var helper = musicHelpers[i];
                if (helper.IsFree)
                {
                    return i;
                }
            }

            if (JSAMSettings.Settings.DynamicSourceAllocation)
            {
                musicHelpers.Add(CreateMusicChannel());
                return musicHelpers.Count - 1;
            }
            else
            {
                AudioManager.DebugError("Ran out of Music Sources! " +
                    "Please enable Dynamic Source Allocation in the AudioManager's settings or " +
                    "increase the number of Music Channels created on startup. " +
                    "You might be playing too many sounds at once.");
            }
            return -1;
        }

        public SoundChannelHelper GetFreeSoundHelper() => soundHelpers[GetFreeSoundChannel()];

        /// <returns>The index of the next free sound channel</returns>
        int GetFreeSoundChannel()
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                var helper = soundHelpers[i];
                if (helper.IsFree)
                {
                    return i;
                }
            }

            if (JSAMSettings.Settings.DynamicSourceAllocation)
            {
                soundHelpers.Add(CreateSoundChannel());
                return soundHelpers.Count - 1;
            }
            else
            {
                Debug.LogError(
                    "AudioManager Error: Ran out of Sound Sources! " +
                    "Please enable Dynamic Source Allocation in the AudioManager's settings or " +
                    "increase the number of Sound Channels created on startup. " +
                    "You might be playing too many sounds at once.");
            }
            return -1;
        }

        #region IsPlaying
        public bool IsSoundPlayingInternal(SoundFileObject s, Transform trans)
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioFile == s && soundHelpers[i].AudioSource.isPlaying)
                {
                    if (trans != null && s.spatialize)
                    {
                        if (soundHelpers[i].SpatializationTarget != trans) continue;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool IsSoundPlayingInternal(SoundFileObject s, Vector3 pos)
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioFile == s && soundHelpers[i].AudioSource.isPlaying)
                {
                    if (soundHelpers[i].SpatializationPosition != pos && s.spatialize) continue;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetPlayingSound(SoundFileObject s, out SoundChannelHelper helper)
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioFile == s && soundHelpers[i].AudioSource.isPlaying)
                {
                    helper = soundHelpers[i];
                    return true;
                }
            }
            helper = null;
            return false;
        }

        public bool IsMusicPlayingInternal(MusicFileObject a, Transform trans = null)
        {
            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioFile == a && musicHelpers[i].AudioSource.isPlaying)
                {
                    if (trans != null && a.spatialize)
                    {
                        if (musicHelpers[i].SpatializationTarget != trans) continue;
                    }
                    return true;
                }
            }
            return false;
        }

        public bool IsMusicPlayingInternal(MusicFileObject s, Vector3 pos)
        {
            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioFile == s && musicHelpers[i].AudioSource.isPlaying)
                {
                    if (musicHelpers[i].SpatializationPosition != pos && s.spatialize) continue;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetPlayingMusic(MusicFileObject a, out MusicChannelHelper helper)
        {
            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioFile == a && musicHelpers[i].AudioSource.isPlaying)
                {
                    helper = musicHelpers[i];
                    return true;
                }
            }
            helper = null;
            return false;
        }
        #endregion

        #region Channel Creation
        /// <summary>
        /// Creates a new GameObject and sets the parent to sourceHolder
        /// </summary>
        MusicChannelHelper CreateMusicChannel()
        {
            GameObject newChannel;
            MusicChannelHelper newHelper;
            if (JSAMSettings.Settings.MusicChannelPrefab)
            {
                newChannel = Instantiate(JSAMSettings.Settings.MusicChannelPrefab, sourceHolder);
                if (!newChannel.TryGetComponent(out newHelper))
                {
                    newHelper = newChannel.AddComponent<MusicChannelHelper>();
                }
            }
            else
            {
                newChannel = new GameObject("AudioChannel");
                newChannel.transform.SetParent(sourceHolder);
                newChannel.AddComponent<AudioSource>();
                newHelper = newChannel.AddComponent<MusicChannelHelper>();
            }

            newHelper.Init(Settings.MusicGroup);
            return newHelper;
        }

        /// <summary>
        /// Creates a new GameObject and sets the parent to sourceHolder
        /// </summary>
        SoundChannelHelper CreateSoundChannel()
        {
            GameObject newChannel;
            SoundChannelHelper newHelper;

            if (JSAMSettings.Settings.SoundChannelPrefab)
            {
                newChannel = Instantiate(JSAMSettings.Settings.SoundChannelPrefab, sourceHolder);
                if (!newChannel.TryGetComponent(out newHelper))
                {
                    newHelper = newChannel.AddComponent<SoundChannelHelper>();
                }
            }
            else
            {
                newChannel = new GameObject("AudioChannel");
                newChannel.transform.SetParent(sourceHolder);
                newChannel.AddComponent<AudioSource>();
                newHelper = newChannel.AddComponent<SoundChannelHelper>();
            }

            newHelper.Init(Settings.SoundGroup);
            return newHelper;
        }
        #endregion

        public BaseAudioFileObject AudioFileFromEnum<T>(T e) where T : Enum
        {
            long key = ComputeEnumHash(e);
            if (enumNameLookup.TryGetValue(key, out string name))
            {
                return AudioFileFromString(name);
            }

            throw new KeyNotFoundException($"Enum {e} of type {typeof(T).Name} not found in lookup!");
        }

        public BaseAudioFileObject AudioFileFromString(string s)
        {
            if (audioFileLookup.TryGetValue(s, out BaseAudioFileObject o))
            {
                return audioFileLookup[s];
            }
            AudioManager.DebugError("Could not find the Audio File for enum " + s + "! " +
                "Make sure its parent Library was loaded first.");
            return null;
        }

        public void LoadAudioLibrary(AudioLibrary l)
        {
            if (IsLibraryLoaded(l))
            {
                AudioManager.DebugWarning("Tried loading AudioLibrary " + l + " when it was already loaded!");
                return;
            }

            var newLib = new LoadedLibrary { Library = l, Users = 1 };

            List<string> enums = new List<string>();
            var soundType = l.soundEnumGenerated;

            if (!l.soundNamespaceGenerated.IsNullEmptyOrWhiteSpace())
            {
                soundType = l.soundNamespaceGenerated + "." + soundType;
            }
            var assembly = soundType + ", Assembly-CSharp";

            Type enumType = Type.GetType(assembly);
            enums.AddRange(Enum.GetNames(enumType));

            newLib.SoundKeys = new long[enums.Count];
            for (int i = 0; i < l.Sounds.Count; i++)
            {
                l.Sounds[i].Initialize();
                var soundName = soundType + "." + enums[i];
                long key = ComputeEnumHash(enumType, i);
                newLib.SoundKeys[i] = key;
                audioFileLookup.Add(soundName, l.Sounds[i]);
                enumNameLookup[key] = soundName;
            }

            enums.Clear();
            var musicType = l.musicEnumGenerated;

            if (!l.musicNamespaceGenerated.IsNullEmptyOrWhiteSpace())
            {
                musicType = l.musicNamespaceGenerated + "." + musicType;
            }
            assembly = musicType + ", Assembly-CSharp";

            enumType = Type.GetType(assembly);
            enums.AddRange(Enum.GetNames(enumType));

            newLib.MusicKeys = new long[enums.Count];
            for (int i = 0; i < l.Music.Count; i++)
            {
                var musicName = musicType + "." + enums[i];
                long key = ComputeEnumHash(enumType, i);
                newLib.MusicKeys[i] = key;
                audioFileLookup.Add(musicName, l.Sounds[i]);
                enumNameLookup[key] = musicName;
            }

            loadedLibraries.Add(l, newLib);
        }

        public void UnloadAudioLibrary(AudioLibrary l)
        {
            if (!IsLibraryLoaded(l))
            {
                AudioManager.DebugWarning("Tried unloading AudioLibrary " + l + " when it wasn't loaded!");
                return;
            }

            var lib = loadedLibraries[l];

            foreach (var s in lib.SoundKeys)
            {
                audioFileLookup.Remove(enumNameLookup[s]);
                enumNameLookup.Remove(s);
            }
            foreach (var m in lib.MusicKeys)
            {
                audioFileLookup.Remove(enumNameLookup[m]);
                enumNameLookup.Remove(m);
            }

            loadedLibraries.Remove(l);
        }

        private long ComputeEnumHash(Type enumType, int value)
        {
            int typeMetadataToken = enumType.MetadataToken;
            return ((long)typeMetadataToken << 32) | (uint)value; // Combine type hash and enum value into a long
        }

        // Computes a unique hash for an enum value of any type
        private long ComputeEnumHash<T>(T e) where T : Enum
        {
            // Cast the enum to its underlying int representation
            int enumValue = GetEnumUnderlyingValue(e);
            int typeMetadataToken = typeof(T).MetadataToken;
            return ((long)typeMetadataToken << 32) | (uint)enumValue; // Combine type hash and enum value into a long
        }

        // Extract the numeric value of an enum based on its underlying type
        private static int GetEnumUnderlyingValue<T>(T e) where T : Enum
        {
            return unchecked(UnsafeUtility.As<T, int>(ref e));
        }
    }
}