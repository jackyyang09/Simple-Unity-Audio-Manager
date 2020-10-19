using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JSAM
{
    /// <summary>
    /// From 0 (least important) to 255 (most important)
    /// </summary>
	public enum Priority
    {
        Music = 0,
        High = 64,
        Default = 128,
        Low = 192,
        Spam = 256
    }

    /// <summary>
    /// Defines the different ways sounds can fade out
    /// </summary>
    public enum FadeMode
    {
        None,
        FadeIn,
        FadeOut,
        FadeInAndOut
    }

    /// <summary>
    /// Defines the different ways music can loop
    /// </summary>
    public enum LoopMode
    {
        /// <summary> Music will not loop </summary>
        NoLooping,
        /// <summary> Music will loop back to start after reaching the end regardless of loop points </summary>
        Looping,
        /// <summary> Music will loop between loop points </summary>
        LoopWithLoopPoints
    }

    /// <summary>
    /// AudioManager singleton that manages all audio in the game
    /// </summary>
    [DefaultExecutionOrder(1)]
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField]
        List<AudioFileObject> audioFileObjects = new List<AudioFileObject>();
        [SerializeField]
        List<AudioFileMusicObject> audioFileMusicObjects = new List<AudioFileMusicObject>();

        [SerializeField]
        [HideInInspector]
        string sceneSoundEnumName = "Sounds";
        Type sceneSoundEnumType = null;

        [SerializeField]
        [HideInInspector]
        string sceneMusicEnumName = "Music";
        Type sceneMusicEnumType = null;

        /// <summary>
        /// List of sources allocated to play looping sounds
        /// </summary>
        List<AudioSource> loopingSources;

        /// <summary>
        /// List of sources designated to ignore the timeScaledSounds parameter
        /// </summary>
        List<AudioSource> ignoringTimeScale = new List<AudioSource>();

        Dictionary<AudioSource, Transform> sourcePositions = new Dictionary<AudioSource, Transform>();

        List<AudioSource> sources;
        List<AudioChannelHelper> helpers;
        List<AudioChannelHelper> musicHelpers;

        /// <summary>
        /// Sources dedicated to playing music
        /// </summary>
        AudioSource[] musicSources;

        [SerializeField]
        [HideInInspector]
        float masterVolume;
        [SerializeField]
        [HideInInspector]
        bool masterMuted;

        [SerializeField]
        [HideInInspector]
        float soundVolume;
        [SerializeField]
        [HideInInspector]
        bool soundMuted;

        [SerializeField]
        [HideInInspector]
        float musicVolume;
        [SerializeField]
        [HideInInspector]
        bool musicMuted;

        [Header("General Settings")]

        [SerializeField]
        [Tooltip("If true, enables 3D spatialized audio for all sound effects, does not effect music")]
        bool spatialSound = true;

        public static AudioManager instance;

        /// <summary>
        /// Only used if you have super special music with a custom looping portion that can be enabled/disabled on the fly
        /// </summary>
        bool enableLoopPoints;
        /// <summary>
        /// Loop time stored using samples
        /// </summary>
        float loopStartTime, loopEndTime;
        /// <summary>
        /// Used in the case that a track has a loop end point placed at the end of the track
        /// </summary>
        bool loopTrackAfterStopping = false;
        bool clampBetweenLoopPoints = false;

        [Header("System Settings")]

        /// <summary>
        /// Number of Audio Sources to be created on start
        /// </summary>
        [SerializeField]
        [Tooltip("Number of Audio Sources to be created on start")]
        int audioSources = 16;

        /// <summary>
        /// If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings
        /// </summary>
        [Tooltip("If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings")]
        [SerializeField]
        bool disableConsoleLogs = false;

        /// <summary>
        /// If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced
        /// </summary>
        [Tooltip("If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced")]
        [SerializeField]
        bool dontDestroyOnLoad = true;

        /// <summary>
        /// If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled
        /// </summary>
        [Tooltip("If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled")]
        [SerializeField]
        bool dynamicSourceAllocation = true;

        /// <summary>
        /// If true, stops all sounds when you load a scene
        /// </summary>
        [Tooltip("If true, stops all sounds when you load a scene")]
        [SerializeField]
        bool stopOnSceneLoad = false;

        [SerializeField]
        [Tooltip("Use if spatialized sounds are spatializing late when playing in-editor, often happens with OVR")]
        bool spatializeLateUpdate = false;

        [SerializeField]
        [Tooltip("Changes the pitch of sounds according to Time.timeScale. When Time.timeScale is set to 0, pauses all sounds instead")]
        bool timeScaledSounds = true;

        /// <summary>
        /// If true, enums are generated to be unique to scenes.
        /// Otherwise, enums are generated to be global across the project
        /// </summary>
        [SerializeField]
        [HideInInspector]
        bool instancedEnums = false;

        [SerializeField]
        [HideInInspector]
        bool wasInstancedBefore = false;

        [Header("Scene AudioListener Reference (Optional)")]

        /// <summary>
        /// The Audio Listener in your scene, will try to automatically set itself on start by looking at the object tagged as \"Main Camera\"
        /// </summary>
        [Tooltip("The Audio Listener in your scene, will try to automatically set itself on Start by looking in the object tagged as \"Main Camera\"")]
        [SerializeField]
        AudioListener listener = null;

        [Header("AudioSource Reference Prefab (MANDATORY)")]

        [SerializeField]
        AudioSource sourcePrefab = null;

        /// <summary>
        /// This object holds all AudioChannels
        /// </summary>
        GameObject sourceHolder;

        bool doneLoading;

        bool gamePaused = false;

        bool initialized = false;

        Coroutine fadeInRoutine;

        Coroutine fadeOutRoutine;

        float prevTimeScale = 1;

        float defaultSoundDistance = 7;

        /// <summary>
        /// A bit like float Epsilon, but large enough for the purpose of pushing the playback position of AudioSources just far enough to not throw an error
        /// </summary>
        public static float EPSILON = 0.000001f;

        // Use this for initialization
        void Awake()
        {
            // AudioManager is important, keep it between scenes
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            EstablishSingletonDominance(true);

            if (!initialized)
            {
                // Initialize helper arrays
                sources = new List<AudioSource>();
                helpers = new List<AudioChannelHelper>();
                musicHelpers = new List<AudioChannelHelper>();
                loopingSources = new List<AudioSource>();

                sourceHolder = new GameObject("Sources");
                sourceHolder.transform.SetParent(transform);

                for (int i = 0; i < audioSources; i++)
                {
                    sources.Add(Instantiate(sourcePrefab, sourceHolder.transform).GetComponent<AudioSource>());
                    helpers.Add(sources[i].gameObject.GetComponent<AudioChannelHelper>());
                    helpers[i].Init();
                    sources[i].name = "AudioSource " + i;
                }

                // Subscribes itself to the sceneLoaded notifier
                SceneManager.sceneLoaded += OnSceneLoaded;

                // Get a reference to all our AudioSources on startup
                sources = new List<AudioSource>(sourceHolder.GetComponentsInChildren<AudioSource>());

                // Create music sources
                musicSources = new AudioSource[3];
                GameObject m = new GameObject("MusicSource");
                m.transform.parent = transform;
                m.AddComponent<AudioSource>();
                musicHelpers.Add(m.AddComponent<AudioChannelHelper>());
                musicHelpers[0].Init(true);
                musicSources[0] = m.GetComponent<AudioSource>();
                musicSources[0].priority = (int)Priority.Music;
                musicSources[0].playOnAwake = false;

                m = new GameObject("SecondaryMusicSource");
                m.transform.parent = transform;
                m.AddComponent<AudioSource>();
                musicHelpers.Add(m.AddComponent<AudioChannelHelper>());
                musicHelpers[1].Init(true);
                musicSources[1] = m.GetComponent<AudioSource>();
                musicSources[1].priority = (int)Priority.Music;
                musicSources[1].playOnAwake = false;

                musicSources[2] = Instantiate(sourcePrefab, transform).GetComponent<AudioSource>();
                musicSources[2].gameObject.name = "SpatialMusicSource";
                musicSources[2].priority = (int)Priority.Music;
                musicSources[2].playOnAwake = false;
                musicHelpers.Add(musicSources[2].GetComponent<AudioChannelHelper>());
                musicHelpers[2].Init(true);

                defaultSoundDistance = sourcePrefab.maxDistance;

                //Set sources properties based on current settings
                ApplyVolumeGlobal();
                SetSpatialSound(spatialSound);

                // Find the listener if not manually set
                FindNewListener();

                doneLoading = true;
            }
        }

        void Start()
        {
            for (int i = 0; i < audioFileObjects.Count; i++)
            {
                audioFileObjects[i].Initialize();
            }

            initialized = true;
        }

        public bool Initialized()
        {
            return initialized;
        }

        void FindNewListener()
        {
            if (listener == null)
            {
                if (Camera.main != null)
                {
                    listener = Camera.main.GetComponent<AudioListener>();
                }
                if (listener != null)
                {
                    DebugLog("AudioManager located an AudioListener successfully!");
                }
                else if (listener == null) // Try to find one ourselves
                {
                    listener = FindObjectOfType<AudioListener>();
                    DebugLog("AudioManager located an AudioListener successfully!");
                }
                if (listener == null) // In the case that there still isn't an AudioListener
                {
                    Debug.LogWarning("AudioManager Warning: Scene is missing an AudioListener! Mark the listener with the \"Main Camera\" tag or set it manually!");
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reapply music source volume
            ApplyMusicVolume();

            FindNewListener();
            if (stopOnSceneLoad)
            {
                StopAllSounds();
            }
            else
            {
                StopSoundLoopAll(true);
            }
        }

        private void Update()
        {
            // Don't want these functions to fire when music is allegedly paused
            if ((Time.timeScale > 0 && !gamePaused) && timeScaledSounds || !timeScaledSounds)
            {
                if (enableLoopPoints)
                {
                    if (musicSources[0].isPlaying)
                    {
                        if (musicSources[0].timeSamples >= loopEndTime)
                        {
                            musicSources[0].timeSamples = (int)loopStartTime;
                        }
                    }
                    else if (loopTrackAfterStopping && fadeInRoutine == null)
                    {
                        musicSources[0].timeSamples = (int)loopStartTime;
                        musicSources[0].Play();
                    }
#if UNITY_EDITOR
                    if (Mathf.Abs((float)(loopStartTime - loopEndTime)) < 1)
                    {
                        Debug.LogWarning("AudioManager Warning! The difference in time in your loop start/end points is less than 1 second! " +
                            "Are you sure you meant to enable loop points in your music?");
                    }
#endif
                    if (clampBetweenLoopPoints && musicSources[0].isPlaying)
                    {
                        if (musicSources[0].timeSamples < (int)loopStartTime)
                        {
                            musicSources[0].timeSamples = (int)loopStartTime;
                        }
                        else if (musicSources[0].timeSamples >= loopEndTime)
                        {
                            musicSources[0].Stop();
                        }
                    }
                }
            }

            if (spatialSound)
            {
                TrackSounds();
            }

            if (timeScaledSounds)
            {
                if (Time.timeScale == 0 && !gamePaused)
                {
                    foreach (AudioSource a in sources)
                    {
                        // Check to make sure this sound wasn't designated to ignore timescale
                        if (ignoringTimeScale.Contains(a)) continue;
                        if (a.isPlaying)
                        {
                            a.Pause();
                        }
                    }
                    // No mercy for music either
                    foreach (AudioSource a in musicSources)
                    {
                        if (ignoringTimeScale.Contains(a)) continue;
                        if (a.isPlaying)
                        {
                            a.Pause();
                        }
                    }
                    gamePaused = true;
                }
                else if (Time.timeScale != 0 && gamePaused)
                {
                    foreach (AudioSource a in sources)
                    {
                        // Check to make sure this sound wasn't designated to ignore timescale
                        if (ignoringTimeScale.Contains(a)) continue;
                        a.UnPause();
                    }

                    foreach (AudioSource a in musicSources)
                    {
                        if (ignoringTimeScale.Contains(a)) continue;
                        a.UnPause();
                    }
                    gamePaused = false;
                }

                // Update AudioSource pitches if timeScale changed
                if (Mathf.Abs(Time.timeScale - prevTimeScale) > 0)
                {
                    foreach (AudioSource a in sources)
                    {
                        if (ignoringTimeScale.Contains(a)) continue;
                        float offset = a.pitch - prevTimeScale;
                        bool reversed = a.pitch < 0;
                        a.pitch = Time.timeScale;
                        a.pitch += offset;
                        if (reversed) a.pitch = -Mathf.Abs(a.pitch);
                    }
                    foreach (AudioSource a in musicSources)
                    {
                        if (ignoringTimeScale.Contains(a)) continue;
                        float offset = a.pitch - prevTimeScale;
                        bool reversed = a.pitch < 0;
                        a.pitch = Time.timeScale;
                        a.pitch += offset;
                        if (reversed) a.pitch = -Mathf.Abs(a.pitch);
                    }
                }
                prevTimeScale = Time.timeScale;
            }
        }

        /// <summary>
        /// Set whether or not sounds are 2D or 3D (spatial)
        /// </summary>
        /// <param name="b">Enable spatial sound if true</param>
        public void SetSpatialSound(bool b)
        {
            spatialSound = b;
            float val = (b) ? 1 : 0;
            foreach (AudioSource s in sources)
            {
                s.spatialBlend = val;
            }
        }

        /// <summary>
        /// Enables/Disables loop points in track currently being played,
        /// does not affect whether or not the track will loop after finishing
        /// </summary>
        /// <param name="b">If false, music will play from start to end in it's entirety</param>
        public void SetLoopPoints(bool b)
        {
            enableLoopPoints = b;
        }

        /// <summary>
        /// Swaps the current music track with the new music track,
        /// music is played globally and does not change volume with the listener's position
        /// </summary>
        /// <param name="track">Enum value for the music to be played. Check AudioManager for the appropriate value to use</param>
        public static AudioSource PlayMusic<T>(T track) where T : Enum
        {
            if (!instance) return null;
            return instance.PlayMusicInternal(track);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.PlayMusic. 
        /// Swaps the current music track with the new music track,
        /// music is played globally and does not change volume with the listener's position
        /// </summary>
        /// <param name="track">Enum value for the music to be played. Check AudioManager for the appropriate value to use</param>
        public AudioSource PlayMusicInternal<T>(T track) where T : Enum
        {
            int t = Convert.ToInt32(track);
            return PlayMusicInternal(audioFileMusicObjects[t]);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.PlayMusic. 
        /// Swaps the current music track with the new music track,
        /// music is played globally and does not change volume with the listener's position
        /// </summary>
        /// <param name="track">Enum value for the music to be played. Check AudioManager for the appropriate value to use</param>

        public AudioSource PlayMusicInternal(int track)
        {
            return PlayMusicInternal(audioFileMusicObjects[track]);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="loopTrack">Does the music play forever?</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public AudioSource PlayMusicInternal(AudioFileMusicObject track)
        {
            musicSources[0].clip = track.GetFile();

            switch (track.loopMode)
            {
                case LoopMode.NoLooping:
                    enableLoopPoints = false;
                    musicSources[0].loop = false;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.Looping:
                    enableLoopPoints = false;
                    musicSources[0].loop = true;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.LoopWithLoopPoints:
                    enableLoopPoints = true;
                    loopStartTime = track.loopStart * musicSources[0].clip.frequency;
                    loopEndTime = track.loopEnd * musicSources[0].clip.frequency;
                    musicSources[0].loop = false;
                    loopTrackAfterStopping = true;
                    clampBetweenLoopPoints = track.clampToLoopPoints;
                    break;
            }

            musicSources[0].spatialBlend = 0;

            bool ignoreTimeScale = track.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    musicSources[0].Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(musicSources[0]);
            }
            else
            {
                if (ignoringTimeScale.Contains(musicSources[0])) ignoringTimeScale.Remove(musicSources[0]);
            }

            musicSources[0].pitch = track.startingPitch;
            if (track.playReversed)
            {
                musicSources[0].pitch = -Mathf.Abs(musicSources[0].pitch);
                musicSources[0].time = musicSources[0].clip.length - EPSILON;
            }
            else musicSources[0].time = 0;

            musicSources[0].Stop();
            musicHelpers[0].Play(track);

            return musicSources[0];
        }

        /// <summary>
        /// Music is played in the scene and becomes quieter as the listener moves away from the source. 
        /// 3D music is independent from regular music, they can overlap if you let them
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The transform of the gameobject playing the music</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public static AudioSource PlayMusic3D<T>(T track, Transform trans, LoopMode loopMode = LoopMode.LoopWithLoopPoints) where T : Enum
        {
            return instance.PlayMusic3DInternal(track, trans, loopMode);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.PlayMusic3D. 
        /// Music is played in the scene and becomes quieter as the listener moves away from the source. 
        /// 3D music is independent from regular music, they can overlap if you let them
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The transform of the gameobject playing the music</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlayMusic3DInternal<T>(T track, Transform trans, LoopMode loopMode = LoopMode.LoopWithLoopPoints) where T : Enum
        {
            int t = Convert.ToInt32(track);
            return PlayMusic3DInternal(audioFileMusicObjects[t], trans, loopMode);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.PlayMusic3D. 
        /// Music is played in the scene and becomes quieter as the listener moves away from the source. 
        /// 3D music is independent from regular music, they can overlap if you let them
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The transform of the gameobject playing the music</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlayMusic3DInternal(int track, Transform trans, LoopMode loopMode = LoopMode.LoopWithLoopPoints)
        {
            return PlayMusic3DInternal(audioFileMusicObjects[track], trans, loopMode);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The transform of the gameobject playing the music</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlayMusic3DInternal(AudioFileMusicObject track, Transform trans, LoopMode loopMode = LoopMode.LoopWithLoopPoints)
        {
            sourcePositions[musicSources[2]] = trans;

            musicSources[2].clip = track.GetFile();

            switch (loopMode)
            {
                case LoopMode.NoLooping:
                    enableLoopPoints = false;
                    musicSources[2].loop = false;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.Looping:
                    enableLoopPoints = false;
                    musicSources[2].loop = true;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.LoopWithLoopPoints:
                    enableLoopPoints = true;
                    loopStartTime = track.loopStart * musicSources[2].clip.frequency;
                    loopEndTime = track.loopEnd * musicSources[2].clip.frequency;
                    musicSources[2].loop = false;
                    loopTrackAfterStopping = true;
                    break;
            }

            clampBetweenLoopPoints = track.clampToLoopPoints;

            bool ignoreTimeScale = track.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    musicSources[2].Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(musicSources[2]);
            }
            else
            {
                if (ignoringTimeScale.Contains(musicSources[2])) ignoringTimeScale.Remove(musicSources[2]);
            }

            musicSources[2].pitch = track.startingPitch;
            if (track.playReversed)
            {
                musicSources[2].pitch = -Mathf.Abs(musicSources[2].pitch);
                musicSources[2].time = musicSources[2].clip.length - EPSILON;
            }
            else musicSources[2].time = 0;

            musicHelpers[2].Play(track.delay, track);

            return musicSources[2];
        }

        /// <summary>
        /// Pause music that's currently playing. 
        /// For music played with PlayMusic3D, use PauseMusic3D
        /// </summary>
        public static void PauseMusic()
        {
            if (!instance) return;
            instance.PauseMusicInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.PauseMusic. 
        /// Pause music that's currently playing. 
        /// For music played with PlayMusic3D, use PauseMusic3D
        /// </summary>
        public void PauseMusicInternal()
        {
            if (musicSources[0].clip != null)
            {
                if (musicSources[0].isPlaying)
                {
                    musicSources[0].Pause();
                }
            }

            if (musicSources[1].clip != null)
            {
                if (musicSources[1].isPlaying)
                {
                    musicSources[1].Pause();
                }
            }
        }

        /// <summary>
        /// Pauses any spatialized music. Will not pause regular music played using PlayMusic
        /// </summary>
        public static void PauseMusic3D()
        {
            if (!instance) return;
            instance.PauseMusic3DInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.PauseMusic3D. 
        /// Pauses any spatialized music. Will not pause regular music played using PlayMusic
        /// </summary>
        public void PauseMusic3DInternal()
        {
            if (musicSources[2].clip != null)
            {
                if (musicSources[2].isPlaying)
                {
                    musicSources[2].Pause();
                }
            }
        }

        /// <summary>
        /// If music is currently paused, resume music. 
        /// To resume playback of spatialized music, use ResumeMusic3D
        /// </summary>
        public static void ResumeMusic()
        {
            if (!instance) return;
            instance.ResumeMusicInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.ResumeMusic. 
        /// If music is currently paused, resume music. 
        /// To resume playback of spatialized music, use ResumeMusic3D
        /// </summary>
        public void ResumeMusicInternal()
        {
            if (musicSources[0].clip == null) return;
            if (!musicSources[0].isPlaying)
            {
                musicSources[0].UnPause();
            }
            if (musicSources[1].clip != null)
            {
                if (!musicSources[1].isPlaying)
                {
                    musicSources[1].UnPause();
                }
            }
        }

        /// <summary>
        /// If 3D music track is currently paused, resumes the music. 
        /// This method only works on music played with PlayMusic3D
        /// </summary>
        public static void ResumeMusic3D()
        {
            if (!instance) return;
            instance.ResumeMusic3DInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.ResumeMusic3D
        /// If 3D music track is currently paused, resumes the music. 
        /// This method only works on music played with PlayMusic3D
        /// </summary>
        public void ResumeMusic3DInternal()
        {
            if (musicSources[2].clip != null)
            {
                if (!musicSources[2].isPlaying)
                {
                    musicSources[2].UnPause();
                }
            }
        }

        /// <summary>
        /// Immediately stops playback of music. 
        /// To stop playback of music played using PlayMusic3D, use StopMusic3D
        /// </summary>
        public static void StopMusic()
        {
            if (!instance) return;
            instance.StopMusicInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.StopMusic. 
        /// Immediately stops playback of music. 
        /// To stop playback of music played using PlayMusic3D, use StopMusic3D
        /// </summary>
        public void StopMusicInternal()
        {
            musicSources[0].Stop();
            musicSources[1].Stop();
            loopTrackAfterStopping = false;
        }

        /// <summary>
        /// Immediately stop playback of spatialized music.
        /// To stop playback of music played using PlayMusic, use StopMusic instead.
        /// </summary>
        public static void StopMusic3D()
        {
            if (!instance) return;
            instance.StopMusic3DInternal();
        }

        /// <summary>
        /// This method can be shortened using AudioManager.StopMusic3D. 
        /// Immediately stop playback of spatialized music.
        /// To stop playback of music played using PlayMusic, use StopMusic instead.
        /// </summary>
        public void StopMusic3DInternal()
        {
            musicSources[2].Stop();
        }

        /// <summary>
        /// Move the current music's playing position to the specified time
        /// </summary>
        /// <param name="time">Time in seconds, must be between 0 and the current track's duration</param>
        public static void SetMusicPlaybackPosition(float time)
        {
            if (!instance) return;
            instance.SetMusicPlaybackPositionInternal(time);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMusicPlaybackPosition. 
        /// Move the current music's playing position to the specified time
        /// </summary>
        /// <param name="time">Time in seconds, must be between 0 and the current track's duration</param>
        public void SetMusicPlaybackPositionInternal(float time)
        {
            if (musicSources[0].clip == null)
            {
                Debug.LogError("AudioManager Error! Tried to modify music playback while no music was playing!");
                return;
            }
            musicSources[0].time = Mathf.Clamp(time, 0, musicSources[0].clip.length);
            if (musicSources[1].clip != null)
            {
                musicSources[1].time = Mathf.Clamp(time, 0, musicSources[1].clip.length);
            }
        }

        /// <summary>
        /// Move the current music's playing position to the specified time
        /// </summary>
        /// <param name="samples">Time in samples, must be between 0 and the current track's sample length</param>
        public static void SetMusicPlaybackPosition(int samples)
        {
            if (!instance) return;
            instance.SetMusicPlaybackPositionInternal(samples);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMusicPlaybackPosition. 
        /// Move the current music's playing position to the specified time
        /// </summary>
        /// <param name="samples">Time in samples, must be between 0 and the current track's sample length</param>
        public void SetMusicPlaybackPositionInternal(int samples)
        {
            if (musicSources[0].clip == null)
            {
                Debug.LogError("AudioManager Error! Tried to modify music playback while no music was present!");
                return;
            }
            musicSources[0].timeSamples = Mathf.Clamp(samples, 0, musicSources[0].clip.samples);
            if (musicSources[1].clip != null)
            {
                musicSources[1].timeSamples = Mathf.Clamp(samples, 0, musicSources[1].clip.samples);
            }
        }

        /// <summary>
        /// Fade out the current track to silence, and then fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public static AudioSource FadeMusic<T>(T track, float time) where T : Enum
        {
            if (!instance) return null;
            return instance.FadeMusicInternal(track, time);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.FadeMusic. 
        /// Fade out the current track to silence, and then fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public AudioSource FadeMusicInternal<T>(T track, float time) where T : Enum
        {
            int t = Convert.ToInt32(track);
            return FadeMusicInternal(audioFileMusicObjects[t], time);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.FadeMusic. 
        /// Fade out the current track to silence, and then fade in a new track
        /// </summary>
        /// <param name="track">The music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public AudioSource FadeMusicInternal(int track, float time)
        {
            return FadeMusicInternal(audioFileMusicObjects[track], time);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        public AudioSource FadeMusicInternal(AudioFileMusicObject track, float time)
        {
            musicSources[1].clip = track.GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            AudioChannelHelper tempHelper = musicHelpers[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
            musicHelpers[0] = musicHelpers[1];
            musicHelpers[1] = tempHelper;

            musicSources[0].clip = track.GetFile();
            musicSources[0].loop = true;

            switch (track.loopMode)
            {
                case LoopMode.NoLooping:
                    enableLoopPoints = false;
                    musicSources[0].loop = false;
                    musicSources[1].loop = false;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.Looping:
                    enableLoopPoints = false;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.LoopWithLoopPoints:
                    enableLoopPoints = true;
                    loopStartTime = track.loopStart * musicSources[0].clip.frequency;
                    loopEndTime = track.loopEnd * musicSources[0].clip.frequency;
                    musicSources[0].loop = false;
                    loopTrackAfterStopping = true;
                    break;
            }

            bool ignoreTimeScale = track.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    musicSources[0].Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(musicSources[0]);
            }
            else
            {
                if (ignoringTimeScale.Contains(musicSources[0])) ignoringTimeScale.Remove(musicSources[0]);
            }

            musicSources[0].pitch = track.startingPitch;
            if (track.playReversed)
            {
                musicSources[0].pitch = -Mathf.Abs(musicSources[0].pitch);
                musicSources[0].time = musicSources[0].clip.length - EPSILON;
            }
            else musicSources[0].time = 0;

            clampBetweenLoopPoints = track.clampToLoopPoints;

            if (time > 0)
            {
                float stepTime = time / 2;

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(stepTime, ignoreTimeScale));

                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(stepTime, ignoreTimeScale, track));
            }
            else // Handle our user's incompetence somehow
            {
                musicHelpers[0].Play(track);
                musicHelpers[1].Stop();
                Debug.LogWarning("AudioManager Warning! Your music fade duration should be set above 0 seconds. Consider using a non-fade otherwise.");
            }

            return musicSources[0];
        }

        /// <summary>
        /// Fade in a new track without affecting playback of any currently playing track. 
        /// To fade the currently playing track out at the same time, use the FadeMusic function instead
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if loopMode is set to LoopWithLoopPoints</param>
        public AudioSource FadeMusicIn<T>(T track, float time, bool playFromStartPoint = false) where T : Enum
        {
            return instance.FadeMusicInInternal(track, time, playFromStartPoint);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.FadeMusicIn. 
        /// Fade in a new track without affecting playback of any currently playing track. 
        /// To fade the currently playing track out at the same time, use the FadeMusic function instead
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if loopMode is set to LoopWithLoopPoints</param>
        public AudioSource FadeMusicInInternal<T>(T track, float time, bool playFromStartPoint = false) where T : Enum
        {
            int t = Convert.ToInt32(track);
            return FadeMusicInInternal(audioFileMusicObjects[t], time, playFromStartPoint);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.FadeMusicIn. 
        /// Fade in a new track without affecting playback of any currently playing track. 
        /// To fade the currently playing track out at the same time, use the FadeMusic function instead
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if loopMode is set to LoopWithLoopPoints</param>
        public AudioSource FadeMusicInInternal(int track, float time, bool playFromStartPoint = false)
        {
            return FadeMusicInInternal(audioFileMusicObjects[track], time, playFromStartPoint);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if loopMode is set to LoopWithLoopPoints</param>
        public AudioSource FadeMusicInInternal(AudioFileMusicObject track, float time, bool playFromStartPoint = false)
        {
            musicSources[1].clip = track.GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            AudioChannelHelper tempHelper = musicHelpers[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
            musicHelpers[0] = musicHelpers[1];
            musicHelpers[1] = tempHelper;

            musicSources[0].clip = track.GetFile();
            musicSources[0].loop = true;

            switch (track.loopMode)
            {
                case LoopMode.NoLooping:
                    enableLoopPoints = false;
                    loopTrackAfterStopping = false;
                    musicSources[0].loop = false;
                    musicSources[1].loop = false;
                    break;
                case LoopMode.Looping:
                    enableLoopPoints = false;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.LoopWithLoopPoints:
                    enableLoopPoints = true;
                    loopStartTime = track.loopStart * musicSources[0].clip.frequency;
                    loopEndTime = track.loopEnd * musicSources[0].clip.frequency;
                    musicSources[0].loop = false;
                    loopTrackAfterStopping = true;
                    if (playFromStartPoint)
                    {
                        musicSources[0].timeSamples = (int)loopStartTime;
                    }
                    break;
            }

            bool ignoreTimeScale = track.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    musicSources[0].Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(musicSources[0]);
            }
            else
            {
                if (ignoringTimeScale.Contains(musicSources[0])) ignoringTimeScale.Remove(musicSources[0]);
            }

            musicSources[0].pitch = track.startingPitch;

            if (track.playReversed)
            {
                musicSources[0].pitch = -Mathf.Abs(musicSources[0].pitch);
                musicSources[0].time = musicSources[0].clip.length - EPSILON;
            }
            else musicSources[0].time = 0;

            clampBetweenLoopPoints = track.clampToLoopPoints;

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(time, ignoreTimeScale, track));
            }
            else // Handle our user's incompetence somehow
            {
                musicHelpers[0].Play(track);
                musicHelpers[1].Stop();
                Debug.LogWarning("AudioManager Warning! Your music fade duration should be set above 0 seconds. Consider using a non-fade otherwise.");
            }

            return musicSources[0];
        }

        /// <summary>
        /// Fade out the current track to silence
        /// </summary>
        /// <param name="time">Fade duration</param>
        public static void FadeMusicOut(float time)
        {
            if (!instance) return;
            instance.FadeMusicOutInternal(time);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.FadeMusicOut. 
        /// Fade out the current track to silence
        /// </summary>
        /// <param name="time">Fade duration</param>
        public void FadeMusicOutInternal(float time)
        {
            // Because this method can get called OnDestroy
            if (musicSources[1] == null) return;
            musicSources[1].clip = null;
            musicSources[1].loop = false;

            AudioSource temp = musicSources[0];
            AudioChannelHelper tempHelper = musicHelpers[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
            musicHelpers[0] = musicHelpers[1];
            musicHelpers[1] = tempHelper;

            if (time > 0)
            {
                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(time));
            }
        }

        IEnumerator FadeInMusicRoutine(float stepTime, bool ignoreTimeScale = false, AudioFileMusicObject file = null)
        {
            musicSources[0].volume = 0;

            //Wait for previous song to fade out
            yield return new WaitForSecondsRealtime(stepTime);
            fadeInRoutine = StartCoroutine(FadeInMusic(stepTime, ignoreTimeScale, file));

            if (file == null) musicHelpers[0].Play(0);
            else musicHelpers[0].Play(file);
        }

        /// <summary>
        /// Cross-fade music from the previous track to the new track specified
        /// </summary>
        /// <param name="track">The new track to fade to</param>
        /// <param name="time">How long the fade will last (between both tracks)</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public static AudioSource CrossfadeMusic<T>(T track, float time, bool keepMusicTime = false) where T : Enum
        {
            if (!instance) return null;
            return instance.CrossfadeMusicInternal(track, time, keepMusicTime);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.CrossfadeMusic. 
        /// Cross-fade music from the previous track to the new track specified
        /// </summary>
        /// <param name="track">The new track to fade to</param>
        /// <param name="time">How long the fade will last (between both tracks)</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public AudioSource CrossfadeMusicInternal<T>(T track, float time, bool keepMusicTime = false) where T : Enum
        {
            int t = Convert.ToInt32(track);
            return CrossfadeMusicInternal(audioFileMusicObjects[t], time, keepMusicTime);
        }

        public AudioSource CrossfadeMusicInternal(int track, float time = 0, bool keepMusicTime = false)
        {
            return CrossfadeMusicInternal(audioFileMusicObjects[track], time, keepMusicTime);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="track">The new track to fade to</param>
        /// <param name="time">How long the fade will last (between both tracks)</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public AudioSource CrossfadeMusicInternal(AudioFileMusicObject track, float time = 0, bool keepMusicTime = false)
        {
            musicSources[1].clip = track.GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            AudioChannelHelper tempHelper = musicHelpers[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
            musicHelpers[0] = musicHelpers[1];
            musicHelpers[1] = tempHelper;

            musicSources[0].clip = track.GetFile();
            musicSources[0].loop = true;

            switch (track.loopMode)
            {
                case LoopMode.NoLooping:
                    enableLoopPoints = false;
                    loopTrackAfterStopping = false;
                    musicSources[0].loop = false;
                    musicSources[1].loop = false;
                    break;
                case LoopMode.Looping:
                    enableLoopPoints = false;
                    loopTrackAfterStopping = false;
                    break;
                case LoopMode.LoopWithLoopPoints:
                    enableLoopPoints = true;
                    loopStartTime = track.loopStart * musicSources[0].clip.frequency;
                    loopEndTime = track.loopEnd * musicSources[0].clip.frequency;
                    musicSources[0].loop = false;
                    loopTrackAfterStopping = true;
                    break;
            }

            bool ignoreTimeScale = track.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    musicSources[0].Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(musicSources[0]);
            }
            else
            {
                if (ignoringTimeScale.Contains(musicSources[0])) ignoringTimeScale.Remove(musicSources[0]);
            }

            musicSources[0].pitch = track.startingPitch;

            if (track.playReversed)
            {
                musicSources[0].pitch = -Mathf.Abs(musicSources[0].pitch);
                musicSources[0].time = musicSources[0].clip.length - EPSILON;
            }
            else musicSources[0].time = 0;

            clampBetweenLoopPoints = track.clampToLoopPoints;

            musicHelpers[0].Play(track);

            musicSources[0].volume = 0;

            if (keepMusicTime)
            {
                SetMusicPlaybackPosition(musicSources[1].timeSamples);
            }

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusic(time, ignoreTimeScale, track));

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(time, ignoreTimeScale));
            }
            else // Handle our user's incompetence somehow
            {
                musicHelpers[0].Play(track);
                musicSources[0].volume = GetTrueMusicVolumeInternal() * track.relativeVolume;
                musicHelpers[1].Stop();
                Debug.LogWarning("AudioManager Warning! Your music fade duration should be set above 0 seconds. Consider using a non-fade otherwise.");
            }

            return musicSources[0];
        }

        private IEnumerator FadeInMusic(float time = 0, bool ignoreScale = false, AudioFileMusicObject file = null)
        {
            float timer = 0;
            float startingVolume = musicSources[0].volume;
            float finalVolume = 0;

            if (file == null)
            {
                finalVolume = GetTrueMusicVolume();
            }
            else
            {
                finalVolume = GetTrueMusicVolume() * file.relativeVolume;
                ignoreScale = file.ignoreTimeScale;
            }

            if (ignoreScale)
            {
                while (timer < time)
                {
                    musicSources[0].volume = Mathf.Lerp(startingVolume, finalVolume, timer / time);
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                while (timer < time)
                {
                    musicSources[0].volume = Mathf.Lerp(startingVolume, finalVolume, timer / time);
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            
            musicSources[0].volume = finalVolume;

            fadeInRoutine = null;
        }

        private IEnumerator FadeOutMusic(float time = 0, bool ignoreScale = false)
        {
            float timer = 0;
            float startingVolume = musicSources[1].volume;
            if (ignoreScale)
            {
                while (timer < time)
                {
                    musicSources[1].volume = Mathf.Lerp(startingVolume, 0, timer / time);
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                while (timer < time)
                {
                    musicSources[1].volume = Mathf.Lerp(startingVolume, 0, timer / time);
                    timer += Time.deltaTime;
                    yield return null;
                }
            }
            musicSources[1].volume = 0;
            musicHelpers[1].Stop();

            fadeOutRoutine = null;
        }

        /// <summary>
        /// Stops playback of the specified track
        /// </summary>
        /// <param name="track">The name of the track in question</param>
        public static void StopMusic<T>(T track) where T : Enum
        {
            if (!instance) return;
            instance.StopMusicInternal(track);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.StopMusic
        /// Stops playback of the specified track
        /// </summary>
        /// <param name="track">The name of the track in question</param>
        public void StopMusicInternal<T>(T track) where T : Enum
        {
            int t = Convert.ToInt32(track);
            StopMusicInternal(audioFileMusicObjects[t]);
        }

        public void StopMusicInternal(int track)
        {
            StopMusicInternal(audioFileMusicObjects[track]);
        }

        /// <summary>
        /// Stops playback of the specified track
        /// </summary>
        /// <param name="track">The name of the track in question</param>
        public void StopMusicInternal(AudioFileMusicObject track)
        {
            for (int i = 0; i < musicSources.Length; i++)
            {
                if (musicSources[i] == null) continue; // Sometimes AudioPlayerMusic calls StopMusic on scene stop
                if (musicSources[i].clip == track.GetFile())
                {
                    musicHelpers[i].Stop();
                }
            }
        }

        void TrackSounds()
        {
            if (spatialSound) // Only do this part if we have 3D sound enabled
            {
                for (int i = 0; i < sources.Count; i++) // Search every source
                {
                    if (i < sources.Count)
                    {
                        if (sourcePositions.ContainsKey(sources[i]))
                        {
                            if (sourcePositions[sources[i]] != null)
                            {
                                sources[i].transform.position = sourcePositions[sources[i]].transform.position;
                            }
                            else
                            {
                                sourcePositions.Remove(sources[i]);
                            }
                        }
                        if (!sources[i].isPlaying) // However, if it's not playing a sound
                        {
                            sourcePositions.Remove(sources[i]);
                        }
                    }
                }
                if (musicSources[2].isPlaying && sourcePositions.ContainsKey(musicSources[2]))
                {
                    musicSources[2].transform.position = sourcePositions[musicSources[2]].transform.position;
                }
            }
        }

        /// <summary>
        /// Plays the specified sound using the settings provided in the Audio File
        /// </summary>
        /// <param name="sound">The enum correlating with the audio file you wish to play</param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <returns>The AudioSource playing the sound</returns>
        public static AudioSource PlaySound<T>(T sound, Transform trans = null) where T : Enum
        {
            if (!instance) return null;
            int s = Convert.ToInt32(sound);
            return instance.PlaySoundInternal(instance.audioFileObjects[s], trans);
        }

        /// <summary>
        /// Plays the specified sound using the settings provided in the Audio File
        /// </summary>
        /// <param name="sound">The enum correlating with the audio file you wish to play</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <returns>The AudioSource playing the sound</returns>
        public static AudioSource PlaySound<T>(T sound, Vector3 position) where T : Enum
        {
            if (!instance) return null;
            int s = Convert.ToInt32(sound);
            return instance.PlaySoundInternal(instance.audioFileObjects[s], position);
        }

        /// <summary>
        /// You are strongly recommend to use AudioManager.PlaySound instead. 
        /// Plays the specified sound using the settings provided in the Audio File
        /// </summary>
        /// <param name="s">The index of the audio file in AudioManager's AudioFileObject list you wish to play</param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundInternal(int s, Transform trans = null)
        {
            return PlaySoundInternal(audioFileObjects[s], trans);
        }

        /// <summary>
        /// You are strongly recommend to use AudioManager.PlaySound instead. 
        /// Plays the specified sound using the settings provided in the Audio File
        /// </summary>
        /// <param name="s">The index of the audio file in AudioManager's AudioFileObject list you wish to play</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundInternal(int s, Vector3 position)
        {
            return PlaySoundInternal(audioFileObjects[s], position);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="s">The AudioFileobject you want AudioManager to play</param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundInternal(AudioFileObject s, Transform trans = null)
        {
            if (!Application.isPlaying) return null;
            int sourceIndex = GetAvailableSource();
            AudioSource a = sources[sourceIndex];

            if (s == null)
            {
                Debug.LogWarning("AudioManager warning! AudioFileObject supplied to PlaySoundInternal is null!");
                return null;
            }

            if (s.UsingLibrary())
            {
                AudioClip[] library = s.GetFiles().ToArray();
                int index = 0;
                if (s.GetFileCount() > 1) // The user actually bothered to include multiple files
                {
                    do
                    {
                        index = UnityEngine.Random.Range(0, library.Length);
                        a.clip = library[index];
                    } while (a.clip == null || index == s.lastClipIndex); // If the user is a dingus and left a few null references in the library
                    if (s.neverRepeat)
                    {
                        s.lastClipIndex = index;
                    }
                }
                else
                {
                    a.clip = s.GetFirstAvailableFile();
                }
            }
            else
            {
                a.clip = s.GetFile();
            }

            if (sourcePositions.ContainsKey(a)) sourcePositions.Remove(a);
            if (trans != null && s.spatialize)
            {
                sourcePositions.Add(a, trans);
                a.transform.position = trans.position;
                if (spatialSound)
                {
                    a.spatialBlend = 1;
                }
                if (s.maxDistance != 0)
                {
                    a.maxDistance = s.maxDistance;
                }
                else a.maxDistance = defaultSoundDistance;
            }
            else
            {
                a.transform.position = listener.transform.position;
                if (spatialSound)
                {
                    a.spatialBlend = 0;
                }
            }

            a.pitch = GetRandomPitch(s);

            bool ignoreTimeScale = s.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    a.Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            //This is the base unchanged pitch
            if (s.playReversed)
            {
                a.pitch = -Mathf.Abs(a.pitch);
                a.time = a.clip.length - -EPSILON;
            }
            else a.time = 0;

            a.priority = (int)s.priority;
            a.loop = false;
            helpers[sourceIndex].Play(s.delay, s);

            return a;
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="s">The AudioFileobject you want AudioManager to play</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundInternal(AudioFileObject s, Vector3 position)
        {
            if (!Application.isPlaying) return null;
            int sourceIndex = GetAvailableSource();
            AudioSource a = sources[sourceIndex];

            if (s == null)
            {
                Debug.LogWarning("AudioManager warning! AudioFileObject supplied to PlaySoundInternal is null!");
                return null;
            }

            if (s.UsingLibrary())
            {
                AudioClip[] library = s.GetFiles().ToArray();
                int index = 0;
                if (s.GetFileCount() > 1) // The user actually bothered to include multiple files
                {
                    do
                    {
                        index = UnityEngine.Random.Range(0, library.Length);
                        a.clip = library[index];
                    } while (a.clip == null || index == s.lastClipIndex); // If the user is a dingus and left a few null references in the library
                    if (s.neverRepeat)
                    {
                        s.lastClipIndex = index;
                    }
                }
                else
                {
                    a.clip = s.GetFirstAvailableFile();
                }
            }
            else
            {
                a.clip = s.GetFile();
            }

            if (sourcePositions.ContainsKey(a)) sourcePositions.Remove(a);
            if (s.spatialize)
            {
                a.transform.position = position;
                if (spatialSound)
                {
                    a.spatialBlend = 1;
                }
                if (s.maxDistance != 0)
                {
                    a.maxDistance = s.maxDistance;
                }
                else a.maxDistance = defaultSoundDistance;
            }
            else
            {
                a.transform.position = listener.transform.position;
                if (spatialSound)
                {
                    a.spatialBlend = 0;
                }
            }

            a.pitch = GetRandomPitch(s);

            bool ignoreTimeScale = s.ignoreTimeScale;
            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    a.Pause(); // If game is paused, pause the sound too
                }
            }
            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            //This is the base unchanged pitch
            if (s.playReversed)
            {
                a.pitch = -Mathf.Abs(a.pitch);
                a.time = a.clip.length - -EPSILON;
            }
            else a.time = 0;

            a.priority = (int)s.priority;
            a.loop = false;
            helpers[sourceIndex].Play(s.delay, s);

            return a;
        }

        /// <summary>
        /// Play a sound using the settings specified in the sound's Audio File and loop it forever
        /// </summary>
        /// <param name="sound">Sound to be played in the form of an enum. Check AudioManager for the appropriate value to be put here.</param>
        /// <param name="trans">The transform of the sound's source, makes it easier to stop the looping sound using StopSoundLoop</param>
        /// <returns>The AudioSource playing the sound</returns>
        public static AudioSource PlaySoundLoop<T>(T sound, Transform trans = null) where T : Enum
        {
            if (!instance) return null;
            int s = Convert.ToInt32(sound);
            return instance.PlaySoundLoopInternal(instance.audioFileObjects[s], trans);
        }

        /// <summary>
        /// Play a sound using the settings specified in the sound's Audio File and loop it forever
        /// </summary>
        /// <param name="sound">Sound to be played in the form of an enum. Check AudioManager for the appropriate value to be put here.</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <returns>The AudioSource playing the sound</returns>
        public static AudioSource PlaySoundLoop<T>(T sound, Vector3 position) where T : Enum
        {
            if (!instance) return null;
            int s = Convert.ToInt32(sound);
            return instance.PlaySoundLoopInternal(instance.audioFileObjects[s], position);
        }

        /// <summary>
        /// You are strongly recommend to use AudioManager.PlaySoundLoop instead. 
        /// Play a sound using the settings specified in the sound's Audio File and loop it forever
        /// </summary>
        /// <param name="sound">Index of the AudioFileObject to play</param>
        /// <param name="trans">The transform of the sound's source, makes it easier to stop the looping sound using StopSoundLoop</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoopInternal(int s, Transform trans = null)
        {
            return PlaySoundLoopInternal(audioFileObjects[s], trans);
        }

        /// <summary>
        /// You are strongly recommend to use AudioManager.PlaySoundLoop instead. 
        /// Play a sound using the settings specified in the sound's Audio File and loop it forever
        /// </summary>
        /// <param name="sound">Index of the AudioFileObject to play</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoopInternal(int s, Vector3 position)
        {
            return PlaySoundLoopInternal(audioFileObjects[s], position);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="s">The AudioFileobject you want AudioManager to play</param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoopInternal(AudioFileObject s, Transform trans = null)
        {
            if (!Application.isPlaying) return null;
            int sourceIndex = GetAvailableSource();
            AudioSource a = sources[sourceIndex];
            loopingSources.Add(a);

            if (s.UsingLibrary())
            {
                AudioClip[] library = s.GetFiles().ToArray();
                do
                {
                    a.clip = library[UnityEngine.Random.Range(0, library.Length)];
                } while (a.clip == null); // If the user is a dingus and left a few null references in the library
            }
            else
            {
                a.clip = s.GetFile();
            }

            if (sourcePositions.ContainsKey(a)) sourcePositions.Remove(a);
            if (trans != null && s.spatialize)
            {
                sourcePositions.Add(a, trans);
                a.transform.position = trans.position;
                if (s.maxDistance != 0)
                {
                    a.maxDistance = s.maxDistance;
                }
                else a.maxDistance = defaultSoundDistance;
            }
            else
            {
                a.transform.position = listener.transform.position;
            }

            bool ignoreTimeScale = s.ignoreTimeScale;

            a.spatialBlend = (spatialSound && s.spatialize) ? 1 : 0;
            a.priority = (int)s.priority;

            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    a.Pause(); // If game is paused, pause the sound too
                }
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            if (s.playReversed)
            {
                a.pitch = -Mathf.Abs(a.pitch);
                a.time = a.clip.length - EPSILON;
            }
            else a.time = 0;
            a.loop = true;
            helpers[sourceIndex].Play(s.delay, s, true);

            return a;
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="s">The AudioFileobject you want AudioManager to play</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoopInternal(AudioFileObject s, Vector3 position)
        {
            if (!Application.isPlaying) return null;
            int sourceIndex = GetAvailableSource();
            AudioSource a = sources[sourceIndex];
            loopingSources.Add(a);

            if (s.UsingLibrary())
            {
                AudioClip[] library = s.GetFiles().ToArray();
                do
                {
                    a.clip = library[UnityEngine.Random.Range(0, library.Length)];
                } while (a.clip == null); // If the user is a dingus and left a few null references in the library
            }
            else
            {
                a.clip = s.GetFile();
            }

            if (sourcePositions.ContainsKey(a)) sourcePositions.Remove(a);
            if (s.spatialize)
            {
                a.transform.position = position;
                if (s.maxDistance != 0)
                {
                    a.maxDistance = s.maxDistance;
                }
                else a.maxDistance = defaultSoundDistance;
            }
            else
            {
                a.transform.position = listener.transform.position;
            }

            bool ignoreTimeScale = s.ignoreTimeScale;

            a.spatialBlend = (spatialSound && s.spatialize) ? 1 : 0;
            a.priority = (int)s.priority;

            if (timeScaledSounds && !ignoreTimeScale)
            {
                if (Time.timeScale == 0)
                {
                    a.Pause(); // If game is paused, pause the sound too
                }
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            if (s.playReversed)
            {
                a.pitch = -Mathf.Abs(a.pitch);
                a.time = a.clip.length - EPSILON;
            }
            else a.time = 0;
            a.loop = true;
            helpers[sourceIndex].Play(s.delay, s, true);

            return a;
        }

        /// <summary>
        /// Stops all playing sounds maintained by AudioManager
        /// </summary>
        public static void StopAllSounds()
        {
            if (!instance) return;
            instance.StopAllSoundsInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.StopAllSounds. 
        /// Stops all playing sounds maintained by AudioManager
        /// </summary>
        public void StopAllSoundsInternal()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == null) continue;
                if (sources[i].isPlaying)
                {
                    helpers[i].Stop();
                }
            }
            loopingSources.Clear();
            sourcePositions.Clear();
            ignoringTimeScale.Clear();
        }

        /// <summary>
        /// Stops any sound playing through PlaySound and it's variants immediately. 
        /// For looping sounds, you are recommend to use StopSoundLoop
        /// </summary>
        /// <param name="s">The sound to be stopped</param>
        /// <param name="trans">For sources, helps with duplicate sounds</param>
        public static void StopSound<T>(T sound, Transform trans = null) where T : Enum
        {
            if (!instance) return;
            int s = Convert.ToInt32(sound);
            instance.StopSoundInternal(instance.audioFileObjects[s], trans);
        }

        /// <summary>
        /// Stops any sound playing through PlaySound and it's variants immediately 
        /// </summary>
        /// <param name="s">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate sounds</param>
        public void StopSoundInternal(int s, Transform t = null)
        {
            StopSoundLoopInternal(audioFileObjects[s], t);
        }

        /// <summary>
        /// Stops any sound playing through PlaySound and it's variants immediately 
        /// </summary>
        /// <param name="s">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate sounds</param>
        public void StopSoundInternal(AudioFileObject s, Transform t = null)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (s.HasAudioClip(sources[i].clip))
                {
                    if (t != null)
                    {
                        if (sourcePositions.ContainsKey(sources[i]))
                        {
                            if (sourcePositions[sources[i]] != t) continue;
                        }
                    }
                    sources[i].Stop();
                    helpers[i].Stop();
                    return;
                }
            }
        }

        /// <summary>
        /// Stops a looping sound played using PlaySoundLoop and it's variants. 
        /// To stop a basic a sound that was played through PlaySound and it's variants, use StopSound instead
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager for the proper value</param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="trans">Transform of the object playing the looping sound</param>
        public static void StopSoundLoop<T>(T sound, bool stopInstantly = false) where T : Enum
        {
            if (!instance) return;
            int s = Convert.ToInt32(sound);
            instance.StopSoundLoopInternal(instance.audioFileObjects[s], stopInstantly, null);
        }

        /// <summary>
        /// Stops a looping sound played using PlaySoundLoop and it's variants. 
        /// To stop a basic a sound that was played through PlaySound and it's variants, use StopSound instead
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager for the proper value</param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="trans">Transform of the object playing the looping sound</param>
        public static void StopSoundLoop<T>(T sound, Transform trans, bool stopInstantly = false) where T : Enum
        {
            if (!instance) return;
            int s = Convert.ToInt32(sound);
            instance.StopSoundLoopInternal(instance.audioFileObjects[s], stopInstantly, trans);
        }

        /// <summary>
        /// Stops a looping sound
        /// </summary>
        /// <param name="s"></param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="t">Transform of the object playing the looping sound</param>
        public void StopSoundLoopInternal(int s, bool stopInstantly = false, Transform t = null)
        {
            StopSoundLoopInternal(audioFileObjects[s], stopInstantly, t);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="s"></param>
        /// <param name="stopInstantly"></param>
        /// <param name="t"></param>
        public void StopSoundLoopInternal(AudioFileObject s, bool stopInstantly = false, Transform t = null)
        {
            for (int i = 0; i < loopingSources.Count; i++)
            {
                if (loopingSources[i] == null) continue;

                if (s.HasAudioClip(loopingSources[i].clip))
                {
                    int index = sources.IndexOf(loopingSources[i]);
                    if (sourcePositions.ContainsKey(loopingSources[i])) // Check if the t field matters
                    {
                        if (t != sourcePositions[sources[index]])
                            continue;
                        sourcePositions.Remove(loopingSources[i]);
                    }
                    if (stopInstantly) loopingSources[i].Stop();
                    loopingSources[i].GetComponent<AudioChannelHelper>().Stop(stopInstantly);
                    loopingSources[i].loop = false;
                    loopingSources.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Stops all looping sounds played using PlaySoundLoop and its variants
        /// </summary>
        /// <param name="stopInstantly">
        /// Stops sounds instantly if true, lets them finish if false
        /// </param>
        public static void StopSoundLoopAll(bool stopInstantly = false)
        {
            if (!instance) return;
            instance.StopSoundLoopAllInternal(stopInstantly);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.StopSoundLoopAll
        /// Stops all looping sounds played using PlaySoundLoop and its variants
        /// </summary>
        /// <param name="stopInstantly">
        /// Stops sounds instantly if true, lets them finish if false
        /// </param>
        public void StopSoundLoopAllInternal(bool stopInstantly = false)
        {
            if (loopingSources.Count > 0)
            {
                for (int i = 0; i < loopingSources.Count; i++)
                {
                    if (loopingSources[i] == null) continue;
                    if (stopInstantly) loopingSources[i].Stop();
                    loopingSources[i].GetComponent<AudioChannelHelper>().Stop(stopInstantly);
                    loopingSources[i].loop = false;
                    if (sourcePositions.ContainsKey(loopingSources[i])) sourcePositions.Remove(loopingSources[i]);
                    loopingSources.Remove(loopingSources[i]);
                }
            }
        }

        /// <summary>
        /// Sets all volumes according to the settings all at once to cut down on calculations. 
        /// Use this method when the user is likely to have modified all volume levels.
        /// </summary>
        public static void ApplyVolumeGlobal()
        {
            if (!instance) return;
            instance.masterVolume = Mathf.Clamp01(instance.masterVolume);
            instance.soundVolume = Mathf.Clamp01(instance.soundVolume);
            instance.musicVolume = Mathf.Clamp01(instance.musicVolume);
            instance.ApplySoundVolume();
            instance.ApplyMusicVolume();
        }

        /// <summary>
        /// Returns the volume of the master channel as a normalized float between 0 and 1
        /// </summary>
        public static float GetMasterVolume()
        {
            if (!instance) return 0;
            return instance.GetMasterVolumeInternal();
        }

        /// <summary>
        /// This method can be shortened AudioManager.GetMasterVolumeInternal. 
        /// Returns the volume of the master channel as a normalized float between 0 and 1
        /// </summary>
        public float GetMasterVolumeInternal()
        {
            return masterVolume;
        }

        /// <summary>
        /// Returns the volume of the master channel as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public static int GetMasterVolumeAsInt()
        {
            if (!instance) return 0;
            return instance.GetMasterVolumeAsIntInternal();
        }

        /// <summary>
        /// Returns the volume of the master channel as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetMasterVolumeAsIntInternal()
        {
            return Mathf.RoundToInt(masterVolume * 100f);
        }

        /// <summary>
        /// Sets the volume of the master channel and applies changes instantly across all sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="volume">The new volume level from 0 to 1</param>
        public static void SetMasterVolume(float volume)
        {
            if (!instance) return;
            instance.SetMasterVolumeInternal(volume);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMasterVolume. 
        /// Sets the volume of the master channel and applies changes instantly across all sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="volume">The new volume level from 0 to 1</param>
        public void SetMasterVolumeInternal(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplySoundVolume();
            ApplyMusicVolume();
        }

        /// <summary>
        /// Sets the volume of the master channel and applies changes instantly across all sources.
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="volume">The new volume level from 0 to 100</param>
        public static void SetMasterVolume(int volume)
        {
            if (!instance) return;
            instance.SetMasterVolumeInternal(volume);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMasterVolume
        /// Sets the volume of the master channel and applies changes instantly across all sources.
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="volume">The new volume level from 0 to 100</param>
        public void SetMasterVolumeInternal(int volume)
        {
            masterVolume = (float)Mathf.Clamp(volume, 0, 100) / 100f;
            ApplySoundVolume();
            ApplyMusicVolume();
        }

        /// <summary>
        /// Returns sound volume as a normalized float between 0 and 1
        /// </summary>
        public static float GetSoundVolume()
        {
            if (!instance) return 0;
            return instance.GetSoundVolumeInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetSoundVolume. 
        /// Returns sound volume as a normalized float between 0 and 1
        /// </summary>
        public float GetSoundVolumeInternal()
        {
            return soundVolume;
        }

        /// <summary>
        /// Returns sound volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public static int GetSoundVolumeAsInt()
        {
            if (!instance) return 0;
            return instance.GetSoundVolumeAsIntInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetSoundVolumeAsInt. 
        /// Returns sound volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetSoundVolumeAsIntInternal()
        {
            return Mathf.RoundToInt(soundVolume * 100f);
        }

        /// <summary>
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public static void SetSoundVolume(float volume)
        {
            if (!instance) return;
            instance.SetSoundVolumeInternal(volume);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetSoundVolume. 
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public void SetSoundVolumeInternal(float volume)
        {
            soundVolume = Mathf.Clamp01(volume);
            ApplySoundVolume();
        }

        /// <summary>
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="v">The new volume level from 0 to 100</param>
        public static void SetSoundVolume(int volume)
        {
            if (!instance) return;
            instance.SetSoundVolumeInternal(volume);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetSoundVolumeInternal. 
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="v">The new volume level from 0 to 100</param>
        public void SetSoundVolumeInternal(int volume)
        {
            soundVolume = (float)Mathf.Clamp(volume, 0, 100) / 100f;
            ApplySoundVolume();
        }

        /// <summary>
        /// Returns music volume as a normalized float between 0 and 1
        /// </summary>
        public static float GetMusicVolume()
        {
            if (!instance) return 0;
            return instance.GetMusicVolumeInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetMusicVolume. 
        /// Returns music volume as a normalized float between 0 and 1
        /// </summary>
        public float GetMusicVolumeInternal()
        {
            return musicVolume;
        }

        /// <summary>
        /// Returns music volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public static int GetMusicVolumeAsInt()
        {
            if (!instance) return 0;
            return instance.GetMusicVolumeAsIntInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetMusicVolumeAsInt
        /// Returns music volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetMusicVolumeAsIntInternal()
        {
            return Mathf.RoundToInt(musicVolume * 100f);
        }

        /// <summary>
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public static void SetMusicVolume(float volume)
        {
            if (!instance) return;
            instance.SetMusicVolumeInternal(volume);
        }

        /// <summary>
        /// This method can be shortened to SetMusicVolumeInternal. 
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public void SetMusicVolumeInternal(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolume();
        }

        /// <summary>
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="v">The new volume level from 0 to 100</param>
        public static void SetMusicVolume(int volume)
        {
            if (!instance) return;
            instance.SetMusicVolumeInternal(volume);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMusicVolume. 
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="v">The new volume level from 0 to 100</param>
        public void SetMusicVolumeInternal(int volume)
        {
            musicVolume = (float)Mathf.Clamp(volume, 0, 100) / 100f;
            ApplyMusicVolume();
        }

        /// <summary>
        /// Sets whether or not the master channel is muted
        /// </summary>
        /// <param name="mute"></param>
        public static void SetMasterChannelMute(bool mute)
        {
            if (!instance) return;
            instance.SetMasterChannelMuteInternal(mute);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMasterChannelMute. 
        /// Sets whether or not the master channel is muted
        /// </summary>
        /// <param name="mute"></param>
        public void SetMasterChannelMuteInternal(bool mute)
        {
            masterMuted = mute;
            ApplySoundVolume();
            ApplyMusicVolume();
        }

        /// <summary>
        /// Returns true if the master channel is muted
        /// </summary>
        /// <returns></returns>
        public static bool IsMasterMuted()
        {
            if (!instance) return false;
            return instance.IsMasterMutedInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsMasterMuted
        /// Returns true if the master channel is muted
        /// </summary>
        /// <returns></returns>
        public bool IsMasterMutedInternal()
        {
            return masterMuted;
        }

        /// <summary>
        /// Sets whether or not the sound channel is muted
        /// </summary>
        /// <param name="mute"></param>
        public static void SetSoundChannelMute(bool mute)
        {
            if (!instance) return;
            instance.SetSoundChannelMuteInternal(mute);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetSoundChannelMute. 
        /// Sets whether or not the sound channel is muted
        /// </summary>
        /// <param name="mute"></param>
        public void SetSoundChannelMuteInternal(bool mute)
        {
            soundMuted = mute;
            ApplySoundVolume();
        }

        /// <summary>
        /// Returns true if the sound channel is muted
        /// </summary>
        /// <returns></returns>
        public static bool IsSoundMuted()
        {
            if (!instance) return false;
            return instance.IsSoundMutedInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsSoundMuted. 
        /// Returns true if the sound channel is muted
        /// </summary>
        /// <returns></returns>
        public bool IsSoundMutedInternal()
        {
            return soundMuted;
        }

        /// <summary>
        /// Sets whether or not the music channel is muted
        /// </summary>
        /// <param name="mute"></param>
        public static void SetMusicChannelMute(bool mute)
        {
            if (!instance) return;
            instance.SetMusicChannelMuteInternal(mute);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.SetMusicChannelMute. 
        /// Sets whether or not the music channel is muted
        /// </summary>
        /// <param name="mute"></param>
        public void SetMusicChannelMuteInternal(bool mute)
        {
            musicMuted = mute;
            ApplyMusicVolume();
        }

        /// <summary>
        /// Returns true if the music channel is muted
        /// </summary>
        /// <returns></returns>
        public static bool IsMusicMuted()
        {
            if (!instance) return false;
            return instance.IsMusicMutedInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsMusicMuted. 
        /// Returns true if the music channel is muted
        /// </summary>
        /// <returns></returns>
        public bool IsMusicMutedInternal()
        {
            return musicMuted;
        }

        void ApplySoundVolume()
        {
            if (helpers == null) return;
            if (helpers.Count == 0) return;
            foreach (AudioChannelHelper a in helpers)
            {
                if (a != null)
                {
                    a.ApplyVolumeChanges();
                }
            }
        }

        void ApplyMusicVolume()
        {
            if (musicHelpers == null) return;
            if (musicHelpers.Count == 0) return;
            foreach (AudioChannelHelper a in musicHelpers)
            {
                if (a != null)
                {
                    a.ApplyVolumeChanges();
                }
            }
        }

        /// <summary>
        /// Returns the real float value applied to AudioSources playing music between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static float GetTrueMusicVolume()
        {
            if (!instance) return 0;
            return instance.GetTrueMusicVolumeInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetTrueMusicVolumeInternal. 
        /// Returns the real float value applied to AudioSources playing music between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public float GetTrueMusicVolumeInternal()
        {
            return musicVolume * masterVolume * Convert.ToInt32(!masterMuted) * Convert.ToInt32(!musicMuted);
        }

        /// <summary>
        /// Returns the real float value applied to AudioSources playing sounds between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public static float GetTrueSoundVolume()
        {
            if (!instance) return 0;
            return instance.GetTrueSoundVolumeInternal();
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetTrueSoundVolumeInternal. 
        /// Returns the real float value applied to AudioSources playing sounds between 0.0 and 1.0
        /// </summary>
        /// <returns></returns>
        public float GetTrueSoundVolumeInternal()
        {
            return soundVolume * masterVolume * Convert.ToInt32(!masterMuted) * Convert.ToInt32(!soundMuted);
        }

        /// <summary>
        /// Ensures that the AudioManager you think you're referring to actually exists in this scene
        /// </summary>
        public void EstablishSingletonDominance(bool killSelf = true)
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                // A unique case where the Singleton exists but not in this scene
                if (instance.gameObject.scene.name == null)
                {
                    instance = this;
                }
                else if (!instance.gameObject.activeInHierarchy)
                {
                    instance = this;
                }
                else if (instance.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    Destroy(gameObject);
                }
                else if (instance.gameObject.scene.name != gameObject.scene.name)
                {
                    instance = this;
                }
                else if (killSelf)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>
        /// Called externally to update slider values
        /// </summary>
        /// <param name="s"></param>
        public void UpdateSoundSlider(UnityEngine.UI.Slider s)
        {
            s.value = soundVolume;
        }

        /// <summary>
        /// Called externally to update slider values
        /// </summary>
        /// <param name="s"></param>
        public void UpdateMusicSlider(UnityEngine.UI.Slider s)
        {
            s.value = musicVolume;
        }

        /// <summary>
        /// Returns null if all sources are used
        /// </summary>
        /// <returns></returns>
        int GetAvailableSource()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (!sources[i].isPlaying && !loopingSources.Contains(sources[i]))
                {
                    return i;
                }
            }

            if (dynamicSourceAllocation)
            {
                AudioSource newSource = Instantiate(sourcePrefab, sourceHolder.transform).GetComponent<AudioSource>();
                AudioChannelHelper newHelper = newSource.gameObject.AddComponent<AudioChannelHelper>();
                newSource.name = "AudioSource " + sources.Count;
				newHelper.Init();
                sources.Add(newSource);
                helpers.Add(newHelper);
                return sources.Count - 1;
            }
            else
            {
                Debug.LogError("AudioManager Error: Ran out of Audio Sources!");
            }
            return 0;
        }

        /// <summary>
        /// Returns true if a sound that was played using PlaySound or it's variants is currently being played. 
        /// For sounds played using PlaySoundLoop, use IsSoundLooping instead
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public static bool IsSoundPlaying<T>(T sound, Transform trans = null) where T : Enum
        {
			if (!instance) return false;
            return instance.IsSoundPlayingInternal(sound, trans);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsSoundPlaying. 
        /// Returns true if a sound that was played using PlaySound or it's variants is currently being played. 
        /// For sounds played using PlaySoundLoop, use IsSoundLooping instead
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlayingInternal<T>(T sound, Transform trans = null) where T : Enum
        {
			if (!instance) return false;
            int s = Convert.ToInt32(sound);
            return IsSoundPlayingInternal(s, trans);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsSoundPlaying. 
        /// Returns true if a sound that was played using PlaySound or it's variants is currently being played. 
        /// For sounds played using PlaySoundLoop, use IsSoundLooping instead
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlayingInternal(int s, Transform trans = null)
        {
            return IsSoundPlayingInternal(audioFileObjects[s], trans);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlayingInternal(AudioFileObject s, Transform trans = null)
        {
            for (int i = 0; i < sources.Count; i++) // Loop through all sources
            {
                if (sources[i] == null) continue;
                if (s.HasAudioClip(sources[i].clip) && sources[i].isPlaying) // If this source is playing the clip
                {
                    if (trans != null)
                    {
                        if (sourcePositions.ContainsKey(sources[i]))
                        {
                            if (trans != sourcePositions[sources[i]]) // Continue if this isn't the specified source position
                            {
                                continue;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if music that was played through PlayMusic is currently playing
        /// </summary>
        /// <param name="music">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <returns></returns>
        public static bool IsMusicPlaying<T>(T music) where T : Enum
        {
            if (!instance) return false;
            return instance.IsMusicPlayingInternal(music);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsMusicPlaying
        /// Returns true if music that was played through PlayMusic is currently playing
        /// </summary>
        /// <param name="music">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <returns></returns>
        public bool IsMusicPlayingInternal<T>(T music) where T : Enum
        {
            int a = Convert.ToInt32(music);
            return IsMusicPlayingInternal(audioFileMusicObjects[a]);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsMusicPlaying
        /// Returns true if music that was played through PlayMusic is currently playing
        /// </summary>
        /// <param name="a">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <returns></returns>
        public bool IsMusicPlayingInternal(int a)
        {
            return IsMusicPlayingInternal(audioFileMusicObjects[a]);
        }

        /// <summary>
        /// If you are an advanced user and prefer to skip right to passing an AudioFileObject, then you are welcome to do so
        /// </summary>
        /// <param name="a">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <returns></returns>
        public bool IsMusicPlayingInternal(AudioFileMusicObject a)
        {
            foreach (AudioSource m in musicSources)
            {
                if (m.clip == a.GetFile() && m.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if a sound played using PlaySoundLoop or it's variants is currently playing. 
        /// This method is more efficient for looping sounds than IsSoundPlaying
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use.</param>
        /// <returns></returns>
        public static bool IsSoundLooping<T>(T sound) where T : Enum
        {
            if (!instance) return false;
            return instance.IsSoundLoopingInternal(sound);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsSoundLooping
        /// Returns true if a sound played using PlaySoundLoop or it's variants is currently playing. 
        /// This method is more efficient for looping sounds than IsSoundPlaying
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use.</param>
        /// <returns></returns>
        public bool IsSoundLoopingInternal<T>(T sound) where T : Enum
        {
            int s = Convert.ToInt32(sound);
            return IsSoundLoopingInternal(s);
        }

        /// <summary>
        /// This method can be shortened to AudioManager.IsSoundLooping
        /// Returns true if a sound played using PlaySoundLoop or it's variants is currently playing. 
        /// This method is more efficient for looping sounds than IsSoundPlaying
        /// </summary>
        /// <returns></returns>
        public bool IsSoundLoopingInternal(int sound)
        {
            return IsSoundLoopingInternal(audioFileObjects[sound]);
        }

        public bool IsSoundLoopingInternal(AudioFileObject sound)
        {
            foreach (AudioSource c in loopingSources)
            {
                if (c == null) continue;
                if (sound.HasAudioClip(c.clip))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Called internally by AudioManager's custom inspector. 
        /// Returns true so long as there was a difference in audio files
        /// </summary>
        /// <param name="audioFiles"></param>
        /// <param name="musicFiles"></param>
        /// <returns></returns>
        public bool GenerateAudioDictionarys(List<AudioFileObject> audioFiles, List<AudioFileMusicObject> musicFiles)
        {
            List<AudioFileObject> audioSoundDiff = audioFiles.Except(audioFileObjects).ToList();
            List<AudioFileMusicObject> audioMusicDiff = musicFiles.Except(audioFileMusicObjects).ToList();
            audioFileObjects = audioFiles;
            audioFileMusicObjects = musicFiles;

            return (audioSoundDiff.Count + audioMusicDiff.Count > 0);
        }

        /// <summary>
        /// Get reference to the music player for custom usage
        /// </summary>
        /// <returns></returns>
        public AudioSource GetMusicSource()
        {
            return musicSources[0];
        }

        /// <summary>
        /// Get reference to the 3D music player for custom usage
        /// </summary>
        /// <returns></returns>
        public AudioSource GetMusicSource3D()
        {
            return musicSources[2];
        }

        /// <summary>
        /// Called internally by AudioManager to output non-error console messages
        /// </summary>
        /// <param name="consoleOutput"></param>
        public void DebugLog(string consoleOutput)
        {
            if (disableConsoleLogs) return;
            Debug.Log(consoleOutput);
        }

        /// <summary>
        /// Given an enum, returns the corresponding AudioFileObject
        /// </summary>
        /// <typeparam name="T">The audio enum type corresponding to your scene. In most cases, this is just JSAM.Music</typeparam>
        /// <param name="track"></param>
        /// <returns></returns>
        public static AudioFileMusicObject GetMusic<T>(T track) where T : Enum
        {
            int a = Convert.ToInt32(track);
            return instance.audioFileMusicObjects[a];
        }

        /// <summary>
        /// Given an enum, returns the corresponding AudioFileObject
        /// </summary>
        /// <typeparam name="T">The audio enum type corresponding to your scene. In most cases, this is just JSAM.Sound</typeparam>
        /// <param name="sound"></param>
        /// <returns></returns>
        public static AudioFileObject GetSound<T>(T sound) where T : Enum
        {
            int a = Convert.ToInt32(sound);
            return instance.audioFileObjects[a];
        }

        public List<AudioFileMusicObject> GetMusicLibrary()
        {
            return audioFileMusicObjects;
        }

        public List<AudioFileObject> GetSoundLibrary()
        {
            return audioFileObjects;
        }

        public Type GetSceneSoundEnum()
        {
            if (sceneSoundEnumType == null || sceneSoundEnumType.ToString() != sceneSoundEnumName)
            {
                sceneSoundEnumType = Type.GetType(sceneSoundEnumName);
            }
            return sceneSoundEnumType;
        }

        public Type GetSceneMusicEnum()
        {
            if (sceneMusicEnumType == null || sceneMusicEnumType.ToString() != sceneMusicEnumName)
            {
                sceneMusicEnumType = Type.GetType(sceneMusicEnumName);
            }
            return sceneMusicEnumType;
        }

        /// <summary>
        /// Given an AudioFileObject, returns a pitch with a modified pitch depending on the Audio File Object's settings
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static float GetRandomPitch(AudioFileObject audioFile)
        {
            float pitch = audioFile.pitchShift;
            float newPitch = audioFile.startingPitch;
            bool ignoreTimeScale = audioFile.ignoreTimeScale;
            if (instance.timeScaledSounds && !ignoreTimeScale)
            {
                newPitch *= Time.timeScale;
                if (Time.timeScale == 0)
                {
                    return 0;
                }
            }
            //This is the base unchanged pitch
            if (pitch > 0)
            {
                newPitch += UnityEngine.Random.Range(-pitch, pitch);
                newPitch = Mathf.Clamp(newPitch, 0, 3);
            }
            if (audioFile.playReversed) newPitch = -Mathf.Abs(newPitch);

            return newPitch;
        }

        /// <summary>
        /// Returns the Audio Listener in the scene. 
        /// You can use this to apply Audio Effects globally
        /// </summary>
        /// <returns></returns>
        public static AudioListener GetListener()
        {
            return instance.listener;
        }

        /// <summary>
        /// This method can be shortened to AudioManager.GetListener(). 
        /// You can use this to apply Audio Effects globally
        /// </summary>
        /// <returns></returns>
        public AudioListener GetListenerInternal()
        {
            return instance.listener;
        }

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        string audioFolderLocation = "";

        List<string> categories = new List<string>();
        List<string> categoriesMusic = new List<string>();
        bool initialCategoryCheck = false;

        /// <summary>
        /// A MonoBehaviour function called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            EstablishSingletonDominance(false);
            if (GetListener() == null) FindNewListener();
            if (audioFolderLocation == "") audioFolderLocation = "Assets";
            ValidateSourcePrefab();
            // Do this once on editor startup just so we have the categories cached
            InitializeCategories();

            if (!doneLoading) return;

            SetSpatialSound(spatialSound);
        }

        public void InitializeCategories()
        {
            if (initialCategoryCheck) return;
            UpdateAudioFileObjectCategories();
            UpdateAudioFileMusicObjectCategories();
            initialCategoryCheck = true;
        }

        public void UpdateAudioFileObjectCategories()
        {
            categories = new List<string>();
            for (int i = 0; i < audioFileObjects.Count; i++)
            {
                if (audioFileObjects[i] == null)
                {
                    audioFileObjects.Remove(audioFileObjects[i]); // May as well remove it if it's missing
                    continue;
                }
                if (audioFileObjects[i].category == null) audioFileObjects[i].category = "";
                if (audioFileObjects[i].category != "" && audioFileObjects[i].category != "Hidden")
                {
                    if (categories.Contains(audioFileObjects[i].category)) continue;
                    categories.Add(audioFileObjects[i].category);
                }
            }
            categories.Sort();
        }

        public void UpdateAudioFileMusicObjectCategories()
        {
            categoriesMusic = new List<string>();
            for (int i = 0; i < audioFileMusicObjects.Count; i++)
            {
                if (audioFileMusicObjects[i] == null)
                {
                    audioFileMusicObjects.Remove(audioFileMusicObjects[i]); // May as well remove it if it's missing
                    continue;
                }
                if (audioFileMusicObjects[i].category == null) audioFileMusicObjects[i].category = "";
                if (audioFileMusicObjects[i].category != "" && audioFileMusicObjects[i].category != "Hidden")
                {
                    if (categoriesMusic.Contains(audioFileMusicObjects[i].category)) continue;
                    categoriesMusic.Add(audioFileMusicObjects[i].category);
                }
            }
            categoriesMusic.Sort();
        }

        public List<string> GetCategories()
        {
            return categories;
        }

        public List<string> GetMusicCategories()
        {
            return categoriesMusic;
        }

        public bool SourcePrefabExists()
        {
            return sourcePrefab != null;
        }

        void ValidateSourcePrefab()
        {
            if (!SourcePrefabExists()) return;
            if (!sourcePrefab.GetComponent<AudioChannelHelper>())
            {
                sourcePrefab.gameObject.AddComponent<AudioChannelHelper>().enabled = false;
                UnityEditor.EditorUtility.DisplayDialog("AudioManager Notice",
                    "The source prefab you specified was missing an AudioChannelHelper. This component is necessary for AudioManager to function. " +
                    "The prefab has been modified to include one, but do remember to attach one in the future.", "Thanks!");
            }
        }

        private void LateUpdate()
        {
            if (spatialSound && spatializeLateUpdate)
            {
                TrackSounds();
            }
        }

        public bool IsUsingInstancedEnums()
        {
            return instancedEnums;
        }

        bool WasInstancedBefore()
        {
            return wasInstancedBefore;
        }

        public string GetAudioFolderLocation()
        {
            return audioFolderLocation;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnCompile()
        {
            if (instance == null) return;
            instance.UpdateAudioFileObjectCategories();
            instance.UpdateAudioFileMusicObjectCategories();
        }
#endif
    }
}