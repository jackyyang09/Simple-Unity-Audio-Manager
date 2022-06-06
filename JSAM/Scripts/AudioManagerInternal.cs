using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("")]
    public class AudioManagerInternal : MonoBehaviour
    {
        /// <summary>
        /// Sources dedicated to playing sound
        /// </summary>
        List<JSAMSoundChannelHelper> soundHelpers = new List<JSAMSoundChannelHelper>();

        /// <summary>
        /// Sources dedicated to playing music
        /// </summary>
        List<JSAMMusicChannelHelper> musicHelpers = new List<JSAMMusicChannelHelper>();
        public JSAMMusicChannelHelper mainMusic { get; private set; }

        #region Volume Logic
        public bool MasterMuted = false;
        public float MasterVolume = 1;
        public float ModifiedMasterVolume { get { return MasterVolume * Convert.ToInt32(!MasterMuted); } }

        public bool MusicMuted = false;
        public float MusicVolume = 1;
        public float ModifiedMusicVolume { get { return ModifiedMasterVolume * MusicVolume * Convert.ToInt32(!MusicMuted); } }

        public bool SoundMuted = false;
        public float SoundVolume = 1;
        public float ModifiedSoundVolume { get { return ModifiedMasterVolume * SoundVolume * Convert.ToInt32(!SoundMuted); } }

        public void SaveVolumeSettings()
        {
            if (!Settings.SaveVolumeToPlayerPrefs) return;

            PlayerPrefs.SetFloat(Settings.MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(Settings.MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(Settings.SoundVolumeKey, SoundVolume);

            PlayerPrefs.SetInt(Settings.MasterMutedKey, Convert.ToInt16(MasterMuted));
            PlayerPrefs.SetInt(Settings.MusicMutedKey, Convert.ToInt16(MusicMuted));
            PlayerPrefs.SetInt(Settings.SoundMutedKey, Convert.ToInt16(SoundMuted));

            PlayerPrefs.Save();
        }

        public void LoadVolumeSettings()
        {
            if (!Settings.SaveVolumeToPlayerPrefs) return;

            if (PlayerPrefs.HasKey(Settings.MasterVolumeKey))
            {
                MasterVolume = PlayerPrefs.GetFloat(Settings.MasterVolumeKey, 1);
            }

            if (PlayerPrefs.HasKey(Settings.MusicVolumeKey))
            {
                MusicVolume = PlayerPrefs.GetFloat(Settings.MusicVolumeKey, 1);
            }

            if (PlayerPrefs.HasKey(Settings.SoundVolumeKey))
            {
                SoundVolume = PlayerPrefs.GetFloat(Settings.SoundVolumeKey, 1);
            }

            if (PlayerPrefs.HasKey(Settings.MasterMutedKey))
            {
                MasterMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.MasterMutedKey, 0));
            }

            if (PlayerPrefs.HasKey(Settings.MusicMutedKey))
            {
                MusicMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.MusicMutedKey, 0));
            }

            if (PlayerPrefs.HasKey(Settings.SoundMutedKey))
            {
                SoundMuted = Convert.ToBoolean(PlayerPrefs.GetInt(Settings.SoundMutedKey, 0));
            }
        }
        #endregion

        /// <summary>
        /// This object holds all AudioChannels
        /// </summary>
        Transform sourceHolder;

        AudioManager audioManager;

        AudioManagerSettings Settings { get { return audioManager.Settings; } }

        [SerializeField] GameObject sourcePrefab;

        float prevTimeScale = 1;

        /// <summary>
        /// A bit like float Epsilon, but large enough for the purpose of pushing the playback position of AudioSources just far enough to not throw an error
        /// </summary>
        public static float EPSILON = 0.000001f;

        /// <summary>
        /// Notifies Audio Channels to follow their target. 
        /// Only invoked when Spatialize is set to true
        /// </summary>
        public static Action OnSpatializeUpdate;
        /// <summary>
        /// Notifies Audio Channels to follow their target on LateUpdate 
        /// Only invoked when Spatialize is set to true
        /// </summary>
        public static Action OnSpatializeLateUpdate;
        /// <summary>
        /// Notifies Audio Channels to follow their target on FixedUpdate
        /// Only invoked when Spatialize is set to true
        /// </summary>
        public static Action OnSpatializeFixedUpdate;

        /// <summary>
        /// <para>float previousTimeScale</para>
        /// Invoked when the user changes the TimeScale
        /// Notifies Audio Channels to adjust pitch accordingly. 
        /// </summary>
        public static Action<float> OnTimeScaleChanged;

        void Awake()
        {
            audioManager = GetComponent<AudioManager>();

            LoadVolumeSettings();

            sourceHolder = new GameObject("Sources").transform;
            sourceHolder.SetParent(transform);
            for (int i = 0; i < Settings.StartingSoundChannels; i++)
            {
                soundHelpers.Add(CreateSoundChannel());
            }

            for (int i = 0; i < Settings.StartingMusicChannels; i++)
            {
                musicHelpers.Add(CreateMusicChannel());
            }
            if (musicHelpers.Count > 0) mainMusic = musicHelpers[0];
        }

        // Update is called once per frame
        void Update()
        {
            OnSpatializeUpdate?.Invoke();

            if (Mathf.Abs(Time.timeScale - prevTimeScale) > 0)
            {
                OnTimeScaleChanged?.Invoke(prevTimeScale);
            }
            prevTimeScale = Time.timeScale;
        }

        void FixedUpdate()
        {
            OnSpatializeFixedUpdate?.Invoke();
        }

        void LateUpdate()
        {
            OnSpatializeLateUpdate?.Invoke();
        }

        private void OnDestroy()
        {
            SaveVolumeSettings();
        }

        private void OnApplicationQuit()
        {
        }

        /// <summary>
        /// Deprecated
        /// Set whether or not sounds are 2D or 3D (spatial)
        /// </summary>
        /// <param name="b">Enable spatial sound if true</param>
        public void SetSpatialSound(bool b)
        {

        }

        #region PlayMusic
        public JSAMMusicChannelHelper PlayMusicInternal(JSAMMusicFileObject music, bool isMain)
        {
            if (!Application.isPlaying) return null;

            PlayMusicInternal(music, null, mainMusic);
            
            AudioManager.OnMusicPlayed?.Invoke(music);

            return mainMusic;
        }

        public JSAMMusicChannelHelper PlayMusicInternal(JSAMMusicFileObject music, Transform newTransform = null, JSAMMusicChannelHelper helper = null)
        {
            if (!Application.isPlaying) return null;
            if (helper == null) helper = GetFreeMusicHelper();
            helper.Play(music);
            helper.SetSpatializationTarget(newTransform);
            AudioManager.OnMusicPlayed?.Invoke(music);

            return helper;
        }

        public JSAMMusicChannelHelper PlayMusicInternal(JSAMMusicFileObject music, Vector3 position, JSAMMusicChannelHelper helper = null)
        {
            if (!Application.isPlaying) return null;
            if (helper == null) helper = GetFreeMusicHelper();
            helper.Play(music);
            helper.SetSpatializationTarget(position);
            AudioManager.OnMusicPlayed?.Invoke(music);

            return helper;
        }
        #endregion

        #region FadeMusic
        public JSAMMusicChannelHelper FadeMusicInInternal(JSAMMusicFileObject music, float fadeInTime, bool isMain)
        {
            if (!Application.isPlaying) return null;

            var helper = musicHelpers[GetFreeMusicChannel()];
            helper.Play(music);
            helper.BeginFadeIn(fadeInTime);

            if (isMain) mainMusic = helper;

            AudioManager.OnMusicPlayed?.Invoke(music);

            return mainMusic;
        }

        public JSAMMusicChannelHelper FadeMainMusicOutInternal(float fadeOutTime)
        {
            if (!Application.isPlaying) return null;

            var helper = mainMusic;
            helper.BeginFadeOut(fadeOutTime);

            return mainMusic;
        }

        public JSAMMusicChannelHelper FadeMusicOutInternal(JSAMMusicFileObject music, float fadeOutTime)
        {
            if (!Application.isPlaying) return null;

            JSAMMusicChannelHelper helper;
            if (TryGetPlayingMusic(music, out helper))
            {
                helper.BeginFadeOut(fadeOutTime);
            }
            else
            {
                DebugWarning("Music Not Found!", "Cannot fade out track " + music + " because track " +
                    "is not currently playing!");
            }

            return helper;
        }

        public JSAMMusicChannelHelper FadeMusicOutInternal(JSAMMusicChannelHelper helper, float fadeOutTime)
        {
            if (!Application.isPlaying) return null;

            if (helper)
            {
                helper.BeginFadeOut(fadeOutTime);
            }
            else
            {
                DebugError("Music Fade Out Failed!", "Provided Music Channel Helper was null!");
            }

            return helper;
        }
        #endregion

        #region StopMusic
        public JSAMMusicChannelHelper StopMusicInternal(JSAMMusicFileObject music, Transform t, bool stopInstantly)
        {
            if (!Application.isPlaying) return null;
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

        public JSAMMusicChannelHelper StopMusicInternal(JSAMMusicFileObject s, Vector3 pos, bool stopInstantly)
        {
            if (!Application.isPlaying) return null;
            for (int i = 0; i < musicHelpers.Count; i++)
            {
                if (musicHelpers[i].AudioSource == null) return null; // Prevent issues when called from OnDestroy
                if (s.Files.Contains(musicHelpers[i].AudioSource.clip))
                {
                    if (musicHelpers[i].SpatializationPosition != pos && s.spatialize) continue;
                    musicHelpers[i].Stop(stopInstantly);
                    return musicHelpers[i];
                }
            }
            return null;
        }

        public bool StopMusicIfPlayingInternal(JSAMMusicFileObject music, Transform trans = null, bool stopInstantly = true)
        {
            if (!IsMusicPlayingInternal(music, trans)) return false;
            StopMusicInternal(music, trans, stopInstantly);
            return true;
        }

        public bool StopMusicIfPlayingInternal(JSAMMusicFileObject music, Vector3 pos, bool stopInstantly = true)
        {
            if (!IsMusicPlayingInternal(music, pos)) return false;
            StopMusicInternal(music, pos, stopInstantly);
            return true;
        }
        #endregion

        #region PlaySound
        public JSAMSoundChannelHelper PlaySoundInternal(JSAMSoundFileObject sound, Transform newTransform = null, JSAMSoundChannelHelper helper = null)
        {
            if (!Application.isPlaying) return null;

            if (helper == null) helper = soundHelpers[GetFreeSoundChannel()];
            if (sound)
            {
                helper.Play(sound);
                helper.SetSpatializationTarget(newTransform);
            }
            AudioManager.OnSoundPlayed?.Invoke(sound);

            return helper;
        }

        public JSAMSoundChannelHelper PlaySoundInternal(JSAMSoundFileObject sound, Vector3 position, JSAMSoundChannelHelper helper = null)
        {
            if (!Application.isPlaying) return null;

            if (helper == null) helper = soundHelpers[GetFreeSoundChannel()];
            if (sound)
            {
                helper.Play(sound);
                helper.SetSpatializationTarget(position);
            }
            AudioManager.OnSoundPlayed?.Invoke(sound);

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

        public void StopSoundInternal(JSAMSoundFileObject s, Transform t = null, bool stopInstantly = true)
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioSource == null) return; // Prevent issues when called from OnDestroy
                if (s.Files.Contains(soundHelpers[i].AudioSource.clip))
                {
                    if (t != null && s.spatialize)
                    {
                        if (soundHelpers[i].SpatializationTarget != t) continue;
                    }
                    soundHelpers[i].Stop(stopInstantly);
                    return;
                }
            }
        }

        public void StopSoundInternal(JSAMSoundFileObject s, Vector3 pos, bool stopInstantly = true)
        {
            for (int i = 0; i < soundHelpers.Count; i++)
            {
                if (soundHelpers[i].AudioSource == null) return; // Prevent issues when called from OnDestroy
                if (s.Files.Contains(soundHelpers[i].AudioSource.clip))
                {
                    if (soundHelpers[i].SpatializationPosition != pos && s.spatialize) continue;
                    soundHelpers[i].Stop(stopInstantly);
                    return;
                }
            }
        }

        public bool StopSoundIfPlayingInternal(JSAMSoundFileObject sound, Transform trans = null, bool stopInstantly = true)
        {
            if (!IsSoundPlayingInternal(sound, trans)) return false;
            StopSoundInternal(sound, trans, stopInstantly);
            return true;
        }

        public bool StopSoundIfPlayingInternal(JSAMSoundFileObject sound, Vector3 pos, bool stopInstantly = true)
        {
            if (!IsSoundPlayingInternal(sound, pos)) return false;
            StopSoundInternal(sound, pos, stopInstantly);
            return true;
        }
        #endregion

        public JSAMMusicChannelHelper GetFreeMusicHelper() => musicHelpers[GetFreeMusicChannel()];

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

            if (audioManager.Settings.DynamicSourceAllocation)
            {
                musicHelpers.Add(CreateMusicChannel());
                return musicHelpers.Count - 1;
            }
            else
            {
                DebugError("Ran out of Music Sources!",
                    "Please enable Dynamic Source Allocation in the AudioManager's settings or " +
                    "increase the number of Music Channels created on startup. " +
                    "You might be playing too many sounds at once.");
            }
            return -1;
        }

        public JSAMSoundChannelHelper GetFreeSoundHelper() => soundHelpers[GetFreeSoundChannel()];

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

            if (audioManager.Settings.DynamicSourceAllocation)
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
        public bool IsSoundPlayingInternal(JSAMSoundFileObject s, Transform trans)
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

        public bool IsSoundPlayingInternal(JSAMSoundFileObject s, Vector3 pos)
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

        public bool TryGetPlayingSound(JSAMSoundFileObject s, out JSAMSoundChannelHelper helper)
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

        public bool IsMusicPlayingInternal(JSAMMusicFileObject a, Transform trans = null)
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

        public bool IsMusicPlayingInternal(JSAMMusicFileObject s, Vector3 pos)
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

        public bool TryGetPlayingMusic(JSAMMusicFileObject a, out JSAMMusicChannelHelper helper)
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
        JSAMMusicChannelHelper CreateMusicChannel()
        {
            var newChannel = new GameObject("AudioChannel");
            newChannel.transform.SetParent(sourceHolder);
            newChannel.AddComponent<AudioSource>();
            var newHelper = newChannel.AddComponent<JSAMMusicChannelHelper>();
            newHelper.Init(Settings.MusicGroup);
            return newHelper;
        }

        /// <summary>
        /// Creates a new GameObject and sets the parent to sourceHolder
        /// </summary>
        JSAMSoundChannelHelper CreateSoundChannel()
        {
            var newChannel = new GameObject("AudioChannel");
            newChannel.transform.SetParent(sourceHolder);
            newChannel.AddComponent<AudioSource>();
            var newHelper = newChannel.AddComponent<JSAMSoundChannelHelper>();
            newHelper.Init(Settings.SoundGroup);
            return newHelper;
        }
        #endregion

        void DebugLog(string message)
        {
            Debug.Log("AudioManager: " + message);
        }

        void DebugWarning(string title, string reason)
        {
            Debug.LogWarning("AudioManager Warning!: " + title + "\n" + reason);
        }

        void DebugError(string title, string reason)
        {
            Debug.LogWarning("AudioManager Warning!: " + title + "\n" + reason);
        }
    }
}