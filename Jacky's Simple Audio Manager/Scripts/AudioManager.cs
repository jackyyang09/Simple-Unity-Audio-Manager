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
        Low = 64,
        Default = 128,
        High = 192,
        Spam = 256
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
    /// Most of the time you'll be using VeryLow or Low pitch variation
    /// </summary>
    public enum Pitch
    {
        /// <summary> No variation in pitch </summary>
        None,
        /// <summary> Pitch varies by 0.05 +/- </summary>
        VeryLow,
        /// <summary> Pitch varies by 0.15 +/- </summary>
        Low,
        /// <summary> Pitch varies by 0.25 +/- </summary>
        Medium,
        /// <summary> Pitch varies by 0.5 +/- </summary>
        High
    }

    /// <summary>
    /// AudioManager singleton that manages all audio in the game
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>
        /// Defined as a class with predefined static values to use as an enum of floats
        /// </summary>
        private class Pitches
        {
            public static float None = 0;
            public static float VeryLow = 0.05f;
            public static float Low = 0.15f;
            public static float Medium = 0.25f;
            public static float High = 0.5f;
        }

        [SerializeField]
        [HideInInspector]
        List<AudioFileObject> audioFileObjects = new List<AudioFileObject>();
        [SerializeField]
        [HideInInspector]
        List<AudioFileMusicObject> audioFileMusicObjects = new List<AudioFileMusicObject>();

        [SerializeField]
        [HideInInspector]
        string sceneSoundEnumName = "Sounds";

        [SerializeField]
        [HideInInspector]
        string sceneMusicEnumName = "Music";

        /// <summary>
        /// List of sources allocated to play looping sounds
        /// </summary>
        List<AudioSource> loopingSources;

        /// <summary>
        /// List of sources designated to ignore the timeScaledSounds parameter
        /// </summary>
        List<AudioSource> ignoringTimeScale = new List<AudioSource>();

        Dictionary<AudioSource, Transform> sourcePositions = new Dictionary<AudioSource, Transform>();

        /// <summary>
        /// Limits the number of each sounds being played. If at 0 or no value, assume infinite
        /// </summary>
        [Tooltip("Limits the number of each sounds being played. If at 0 or no value, assume infinite")]
        int[] exclusiveList;

        List<AudioSource> sources;

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
        bool disableConsoleLogs;

        /// <summary>
        /// If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced
        /// </summary>
        [Tooltip("If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced")]
        [SerializeField]
        bool dontDestroyOnLoad;

        /// <summary>
        /// If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled
        /// </summary>
        [Tooltip("If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled")]
        [SerializeField]
        bool dynamicSourceAllocation;

        /// <summary>
        /// If true, stops all sounds when you load a scene
        /// </summary>
        [Tooltip("If true, stops all sounds when you load a scene")]
        [SerializeField]
        bool stopSoundsOnSceneLoad;

        [SerializeField]
        [Tooltip("Use if spatialized sounds are spatializing late when playing in-editor, often happens with OVR")]
        bool spatializeLateUpdate = false;

        [SerializeField]
        [Tooltip("When Time.timeScale is set to 0, pause all sounds")]
        bool timeScaledSounds = true;

        [SerializeField]
        [HideInInspector]
        string audioFolderLocation;

        /// <summary>
        /// If true, enums are generated to be unique to scenes.
        /// Otherwise, enums are generated to be global across the project
        /// </summary>
        [SerializeField]
        [HideInInspector]
        bool instancedEnums;

        [SerializeField]
        [HideInInspector]
        bool wasInstancedBefore;

        [Header("Scene AudioListener Reference (Optional)")]

        /// <summary>
        /// The Audio Listener in your scene, will try to automatically set itself on start by looking at the object tagged as \"Main Camera\"
        /// </summary>
        [Tooltip("The Audio Listener in your scene, will try to automatically set itself on Start by looking in the object tagged as \"Main Camera\"")]
        [SerializeField]
        AudioListener listener;

        [Header("AudioSource Reference Prefab (MANDATORY)")]

        [SerializeField]
        GameObject sourcePrefab;

        /// <summary>
        /// This object holds all AudioChannels
        /// </summary>
        GameObject sourceHolder;

        bool doneLoading;

        string editorMessage = "";

        bool gamePaused = false;

        bool initialized = false;

        Coroutine fadeInRoutine;

        Coroutine fadeOutRoutine;

        // Use this for initialization
        void Awake()
        {
            // AudioManager is important, keep it between scenes
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            EstablishSingletonDominance();

            if (!initialized)
            {
                // Initialize helper arrays
                sources = new List<AudioSource>();
                loopingSources = new List<AudioSource>();

                sourceHolder = new GameObject("Sources");
                sourceHolder.transform.SetParent(transform);

                for (int i = 0; i < audioSources; i++)
                {
                    sources.Add(Instantiate(sourcePrefab, sourceHolder.transform).GetComponent<AudioSource>());
                    sources[i].name = "AudioSource " + i;
                }

                // Subscribes itself to the sceneLoaded notifier
                SceneManager.sceneLoaded += OnSceneLoaded;

                // Get a reference to all our audiosources on startup
                sources = new List<AudioSource>(sourceHolder.GetComponentsInChildren<AudioSource>());

                // Create music sources
                musicSources = new AudioSource[3];
                GameObject m = new GameObject("MusicSource");
                m.transform.parent = transform;
                m.AddComponent<AudioSource>();
                musicSources[0] = m.GetComponent<AudioSource>();
                musicSources[0].priority = (int)Priority.Music;
                musicSources[0].playOnAwake = false;

                m = new GameObject("SecondaryMusicSource");
                m.transform.parent = transform;
                m.AddComponent<AudioSource>();
                musicSources[1] = m.GetComponent<AudioSource>();
                musicSources[1].priority = (int)Priority.Music;
                musicSources[1].playOnAwake = false;

                musicSources[2] = Instantiate(sourcePrefab, transform).GetComponent<AudioSource>();
                musicSources[2].gameObject.name = "SpatialMusicSource";
                musicSources[2].priority = (int)Priority.Music;
                musicSources[2].playOnAwake = false;

                //Set sources properties based on current settings
                SetSoundVolume(soundVolume);
                SetMusicVolume(musicVolume);
                SetSpatialSound(spatialSound);

                // Find the listener if not manually set
                FindNewListener();

                doneLoading = true;
            }
        }

        void Start()
        {
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
                    editorMessage = "AudioManager Warning: Scene is missing an AudioListener! Mark the listener with the \"Main Camera\" tag or set it manually!";
                    Debug.LogWarning(editorMessage);
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Revert music source volume
            ApplyMusicVolume();

            FindNewListener();
            if (stopSoundsOnSceneLoad)
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
            }

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
                    gamePaused = true;
                }
                else if (Time.timeScale != 0)
                {
                    foreach (AudioSource a in sources)
                    {
                        // Check to make sure this sound wasn't designated to ignore timescale
                        if (ignoringTimeScale.Contains(a)) continue;
                        a.UnPause();
                    }
                    gamePaused = false;
                }
            }
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (spatialSound && spatializeLateUpdate)
            {
                TrackSounds();
            }
#endif
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
        /// music is played globally and does not change volume
        /// </summary>
        /// <param name="track">Enum value for the music to be played. Check AudioManager for the appropriate value to use</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public AudioSource PlayMusic<T>(T track, LoopMode loopMode = LoopMode.LoopWithLoopPoints) where T : Enum
        {
            int t = Convert.ToInt32(track);
            musicSources[0].clip = audioFileMusicObjects[t].GetFile();

            switch (loopMode)
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
                    if (audioFileMusicObjects[t].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[t].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[t].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    else
                    {
                        musicSources[0].loop = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[t].clampBetweenLoopPoints;

            musicSources[0].spatialBlend = 0;

            musicSources[0].Stop();
            musicSources[0].Play();

            return musicSources[0];
        }

        /// <summary>
        /// Swaps the current music track with the new music track,
        /// music is played globally and does not change volume
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="loopTrack">Does the music play forever?</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public AudioSource PlayMusic(int track, LoopMode loopMode = LoopMode.LoopWithLoopPoints)
        {
            musicSources[0].clip = audioFileMusicObjects[track].GetFile();

            switch (loopMode)
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
                    if (audioFileMusicObjects[track].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[track].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[track].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    else
                    {
                        musicSources[0].loop = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[track].clampBetweenLoopPoints;

            musicSources[0].spatialBlend = 0;

            musicSources[0].Stop();
            musicSources[0].Play();

            return musicSources[0];
        }

        /// <summary>
        /// Swaps the current music track with the new music track,
        /// music is played globally and does not change volume
        /// </summary>
        /// <param name="track">AudioClip to be played</param>
        /// <param name="loopTrack">Does the music play forever?</param>
        public AudioSource PlayMusic(AudioClip track, bool loopTrack = true)
        {
            if (track.Equals("None")) return null;

            musicSources[0].clip = track;
            musicSources[0].loop = loopTrack;
            musicSources[0].spatialBlend = 0;

            musicSources[0].Play();

            return musicSources[0];
        }

        public void PlayMusicFromStartPoint()
        {

        }

        /// <summary>
        /// Music is played in the scene and becomes quieter as you move away from the source.
        /// 3D music source is independent from the main music source, they can overlap if you let them
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The transform of the gameobject playing the music</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlayMusic3D<T>(T track, Transform trans, LoopMode loopMode = LoopMode.LoopWithLoopPoints) where T : Enum
        {
            int t = Convert.ToInt32(track);

            sourcePositions[musicSources[2]] = trans;

            musicSources[2].clip = audioFileMusicObjects[t].GetFile();

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
                    if (audioFileMusicObjects[t].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[t].loopStart * musicSources[2].clip.frequency;
                        loopEndTime = audioFileMusicObjects[t].loopEnd * musicSources[2].clip.frequency;
                        musicSources[2].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    else
                    {
                        musicSources[2].loop = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[t].clampBetweenLoopPoints;

            musicSources[2].Play();

            return musicSources[2];
        }

        /// <summary>
        /// Music is played in the scene and becomes quieter as you move away from the source.
        /// 3D music source is independent from the main music source, they can overlap if you let them
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The transform of the gameobject playing the music</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlayMusic3D(int track, Transform trans, LoopMode loopMode = LoopMode.LoopWithLoopPoints)
        {
            sourcePositions[musicSources[2]] = trans;

            musicSources[2].clip = audioFileMusicObjects[track].GetFile();

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
                    if (audioFileMusicObjects[track].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[track].loopStart * musicSources[2].clip.frequency;
                        loopEndTime = audioFileMusicObjects[track].loopEnd * musicSources[2].clip.frequency;
                        musicSources[2].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    else
                    {
                        musicSources[2].loop = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[track].clampBetweenLoopPoints;

            musicSources[2].Play();

            return musicSources[2];
        }

        /// <summary>
        /// Music is played in the scene and becomes quieter as you move away from the source
        /// 3D music source is independent from the main music source, they can overlap if you let them
        /// </summary>
        /// <param name="track">Index of the music</param>
        /// <param name="trans">The origin of the music source</param>
        /// <param name="loopTrack">Does the music play forever?</param>
        public AudioSource PlayMusic3D(AudioClip track, Transform trans, bool loopTrack = true)
        {
            if (track.Equals("None")) return null;

            sourcePositions[musicSources[2]] = trans;

            musicSources[2].clip = track;
            musicSources[2].loop = loopTrack;

            musicSources[2].Play();

            return musicSources[2];
        }

        /// <summary>
        /// Pause all music sources
        /// </summary>
        public void PauseMusic()
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
        /// Pauses the 3D music track specifically
        /// </summary>
        public void PauseMusic3D()
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
        /// If music is currently paused, resume music
        /// </summary>
        public void ResumeMusic()
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
        /// If 3D music track is currently paused, resumes the music
        /// </summary>
        public void ResumeMusic3D()
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
        /// Stop whatever is playing in musicSource
        /// </summary>
        public void StopMusic()
        {
            musicSources[0].Stop();
            musicSources[1].Stop();
            loopTrackAfterStopping = false;
        }

        /// <summary>
        /// Stop whatever 3D music is playing
        /// </summary>
        public void StopMusic3D()
        {
            musicSources[2].Stop();
        }

        /// <summary>
        /// Move the current music's playing position to the specified time
        /// </summary>
        /// <param name="time">Time in seconds, must be between 0 and the curernt track's duration</param>
        public void SetMusicPlaybackPosition(float time)
        {
            if (musicSources[0].clip == null)
            {
                Debug.LogError("AudioManager Error! Tried to modify music playback while no music was present!");
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
        public void SetMusicPlaybackPosition(int samples)
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
        /// Fade out the current track and fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public void FadeMusic<T>(T track, float time, LoopMode loopMode = LoopMode.LoopWithLoopPoints) where T : Enum
        {
            int t = Convert.ToInt32(track);

            musicSources[1].clip = audioFileMusicObjects[t].GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = audioFileMusicObjects[t].GetFile();
            musicSources[0].loop = true;

            switch (loopMode)
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
                    if (audioFileMusicObjects[t].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[t].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[t].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[t].clampBetweenLoopPoints;

            if (time > 0)
            {
                float stepTime = time / 2;

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(stepTime));

                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(stepTime));
            }
        }

        /// <summary>
        /// Fade out the current track and fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        public void FadeMusic(int track, float time, LoopMode loopMode = LoopMode.LoopWithLoopPoints)
        {
            musicSources[1].clip = audioFileMusicObjects[track].GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = audioFileMusicObjects[track].GetFile();
            musicSources[0].loop = true;

            switch (loopMode)
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
                    if (audioFileMusicObjects[track].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[track].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[track].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[track].clampBetweenLoopPoints;

            if (time > 0)
            {
                float stepTime = time / 2;

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(stepTime));

                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(stepTime));
            }
        }

        /// <summary>
        /// Fade out the current track and fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        public void FadeMusic(AudioClip track, float time)
        {
            if (track.Equals("None")) return;

            musicSources[1].clip = track;
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            if (time > 0)
            {
                float stepTime = time / 2;

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(stepTime));

                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(stepTime));
            }
        }

        /// <summary>
        /// Fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if loopMode is set to LoopWithLoopPoints</param>
        public AudioSource FadeMusicIn<T>(T track, float time, LoopMode loopMode = LoopMode.LoopWithLoopPoints, bool playFromStartPoint = false) where T : Enum
        {
            int t = Convert.ToInt32(track);

            musicSources[1].clip = audioFileMusicObjects[t].GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = audioFileMusicObjects[t].GetFile();
            musicSources[0].loop = true;

            switch (loopMode)
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
                    if (audioFileMusicObjects[t].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[t].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[t].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                        if (playFromStartPoint)
                        {
                            musicSources[0].timeSamples = (int)loopStartTime;
                        }
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[t].clampBetweenLoopPoints;

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(time));
            }

            return musicSources[0];
        }

        /// <summary>
        /// Fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if loopMode is set to LoopWithLoopPoints</param>
        public AudioSource FadeMusicIn(int track, float time, LoopMode loopMode = LoopMode.LoopWithLoopPoints, bool playFromStartPoint = false)
        {
            musicSources[1].clip = audioFileMusicObjects[track].GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = audioFileMusicObjects[track].GetFile();
            musicSources[0].loop = true;

            switch (loopMode)
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
                    if (audioFileMusicObjects[track].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[track].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[track].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                        if (playFromStartPoint)
                        {
                            musicSources[0].timeSamples = (int)loopStartTime;
                        }
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[track].clampBetweenLoopPoints;

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(time));
            }

            return musicSources[0];
        }

        /// <summary>
        /// Fade in a new track
        /// </summary>
        /// <param name="track">The name of the music track</param>
        /// <param name="time">Should be greater than 0, entire fade process lasts this long</param>
        public void FadeMusicIn(AudioClip track, float time)
        {
            if (track.Equals("None")) return;

            musicSources[1].clip = track;
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusicRoutine(time));
            }
        }

        /// <summary>
        /// Fade out the current track to silence
        /// </summary>
        /// <param name="time">Fade duration</param>
        public void FadeMusicOut(float time)
        {
            musicSources[1].clip = null;
            musicSources[1].loop = false;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            if (time > 0)
            {
                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(time));
            }
        }

        IEnumerator FadeInMusicRoutine(float stepTime)
        {
            musicSources[0].volume = 0;

            //Wait for previous song to fade out
            yield return new WaitForSecondsRealtime(stepTime);
            fadeInRoutine = StartCoroutine(FadeInMusic(stepTime));
            musicSources[0].Play();
        }

        /// <summary>
        /// Crossfade music from the previous track to the new track specified
        /// </summary>
        /// <param name="track">The new track to fade to</param>
        /// <param name="time">How long the fade will last (between both tracks)</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public void CrossfadeMusic<T>(T track, float time = 0, LoopMode loopMode = LoopMode.LoopWithLoopPoints, bool keepMusicTime = false) where T : Enum
        {
            int t = Convert.ToInt32(track);

            musicSources[1].clip = audioFileMusicObjects[t].GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = audioFileMusicObjects[t].GetFile();
            musicSources[0].loop = true;

            switch (loopMode)
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
                    if (audioFileMusicObjects[t].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[t].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[t].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[t].clampBetweenLoopPoints;

            musicSources[0].volume = 0;

            musicSources[0].Play();

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusic(time));

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(time));
            }

            if (keepMusicTime)
            {
                SetMusicPlaybackPosition(musicSources[1].timeSamples);
            }
        }

        /// <summary>
        /// Crossfade music from the previous track to the new track specified
        /// </summary>
        /// <param name="track">The new track to fade to</param>
        /// <param name="time">How long the fade will last (between both tracks)</param>
        /// <param name="loopMode">Does the track loop from start to finish? Does the track loop between loop points?</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public void CrossfadeMusic(int track, float time = 0, LoopMode loopMode = LoopMode.LoopWithLoopPoints, bool keepMusicTime = false)
        {
            musicSources[1].clip = audioFileMusicObjects[track].GetFile();
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = audioFileMusicObjects[track].GetFile();
            musicSources[0].loop = true;

            switch (loopMode)
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
                    if (audioFileMusicObjects[track].useLoopPoints) // Only apply loop points if the audio music file has loop points set
                    {
                        enableLoopPoints = true;
                        loopStartTime = audioFileMusicObjects[track].loopStart * musicSources[0].clip.frequency;
                        loopEndTime = audioFileMusicObjects[track].loopEnd * musicSources[0].clip.frequency;
                        musicSources[0].loop = false;
                        loopTrackAfterStopping = true;
                    }
                    break;
            }

            clampBetweenLoopPoints = audioFileMusicObjects[track].clampBetweenLoopPoints;

            musicSources[0].volume = 0;

            musicSources[0].Play();

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusic(time));

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(time));
            }

            if (keepMusicTime)
            {
                SetMusicPlaybackPosition(musicSources[1].timeSamples);
            }
        }

        /// <summary>
        /// Crossfade music from the previous track to the new track specified
        /// </summary>
        /// <param name="track">The new track to fade to</param>
        /// <param name="time">How long the fade will last (between both tracks)</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public void CrossfadeMusic(AudioClip track, float time = 0, bool keepMusicTime = false)
        {
            if (track.Equals(null)) return;

            musicSources[1].clip = track;
            musicSources[1].loop = true;

            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;

            musicSources[0].clip = track;
            musicSources[0].loop = true;
            musicSources[0].volume = 0;

            musicSources[0].Play();

            if (time > 0)
            {
                if (fadeInRoutine != null) StopCoroutine(fadeInRoutine);
                fadeInRoutine = StartCoroutine(FadeInMusic(time));

                if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = StartCoroutine(FadeOutMusic(time));
            }

            if (keepMusicTime)
            {
                SetMusicPlaybackPosition(musicSources[1].timeSamples);
            }
        }

        private IEnumerator FadeInMusic(float time = 0)
        {
            float timer = 0;
            float startingVolume = musicSources[0].volume;
            while (timer < time)
            {
                musicSources[0].volume = Mathf.Lerp(startingVolume, musicVolume, timer / time);
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            musicSources[0].volume = musicVolume;

            fadeInRoutine = null;
        }

        private IEnumerator FadeOutMusic(float time = 0)
        {
            float timer = 0;
            float startingVolume = musicSources[1].volume;
            while (timer < time)
            {
                musicSources[1].volume = Mathf.Lerp(startingVolume, 0, timer / time);
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            musicSources[1].volume = 0;
            musicSources[1].Stop();

            fadeOutRoutine = null;
        }

        /// <summary>
        /// Stops the specified track
        /// </summary>
        /// <param name="track">The name of the track in question</param>
        public void StopMusic<T>(T track) where T : Enum
        {
            int t = Convert.ToInt32(track);
            foreach (AudioSource a in musicSources)
            {
                if (a == null) continue; // Sometimes AudioPlayerMusic calls StopMusic on scene stop
                if (a.clip == audioFileMusicObjects[t].GetFile())
                {
                    a.Stop();
                }
            }
        }

        /// <summary>
        /// Stops the specified track
        /// </summary>
        /// <param name="track">The name of the track in question</param>
        public void StopMusic(int track)
        {
            foreach (AudioSource a in musicSources)
            {
                if (a == null) continue; // Sometimes AudioPlayerMusic calls StopMusic on scene stop
                if (a.clip == audioFileMusicObjects[track].GetFile())
                {
                    a.Stop();
                }
            }
        }

        /// <summary>
        /// Stops the specified track
        /// </summary>
        /// <param name="m">The track's audio file</param>
        public void StopMusic(AudioClip m)
        {
            foreach (AudioSource a in musicSources)
            {
                if (a == null) return; // Chances are sources got destroyed on scene cleanup
                if (a.clip == m)
                {
                    a.Stop();
                }
            }
        }

        void TrackSounds()
        {
            if (spatialSound) // Only do this part if we have 3D sound enabled
            {
                for (int i = 0; i < audioSources + 1; i++) // Search every sources
                {
                    if (i < audioSources - 1)
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
                        if (!sources[i].isPlaying) // However if it's not playing a sound
                        {
                            sourcePositions.Remove(sources[i]);
                        }
                    }
                    else
                    {
                        if (musicSources[2].isPlaying && sourcePositions.ContainsKey(sources[i]))
                        {
                            musicSources[2].transform.position = sourcePositions[sources[i]].transform.position;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Equivalent of PlayOneShot
        /// </summary>
        /// <param name="sound">The enum correlating with the audio file you wish to play</param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="pitchShift">If not None, randomizes the pitch of the sound, use AudioManager.Pitches for presets</param>
        /// <param name="delay">Amount of seconds to wait before playing the sound</param>
        /// <param name="ignoreTimeScale">If true, will not be paused by AudioManager when TimeScale is 0. To change this option for all sounds, check AudioManager's advanced settings</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundOnce<T>(T sound, Transform trans = null, Priority p = Priority.Default, Pitch pitchShift = Pitch.None, float delay = 0, bool ignoreTimeScale = false) where T : Enum
        {
            if (!Application.isPlaying) return null;
            AudioSource a = GetAvailableSource();

            if (trans != null)
            {
                sourcePositions[a] = trans;
                a.transform.position = trans.position;
                if (spatialSound)
                {
                    a.spatialBlend = 1;
                }
            }
            else
            {
                a.transform.position = listener.transform.position;
                if (spatialSound)
                {
                    a.spatialBlend = 0;
                }
            }

            float pitch = UsePitch(pitchShift);

            //This is the base unchanged pitch
            if (pitch > Pitches.None)
            {
                a.pitch = 1 + UnityEngine.Random.Range(-pitch, pitch);
            }
            else
            {
                a.pitch = 1;
            }

            Enum test = Enum.Parse(typeof(T), sound.ToString()) as Enum;
            int s = Convert.ToInt32(test); // x is the integer value of enum

            if (audioFileObjects[s].UsingLibrary())
            {
                AudioClip[] library = audioFileObjects[s].GetFiles().ToArray();
                do
                {
                    a.clip = library[UnityEngine.Random.Range(0, library.Length)];
                } while (a.clip == null); // If the user is a dingus and left a few null references in the library
            }
            else
            {
                a.clip = audioFileObjects[s].GetFile();
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }
            a.priority = (int)p;
            a.loop = false;
            a.PlayDelayed(delay);

            return a;
        }

        /// <summary>
        /// Equivalent of PlayOneShot
        /// </summary>
        /// <param name="s"></param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="pitchShift">If not None, randomizes the pitch of the sound, use AudioManager.Pitches for presets</param>
        /// <param name="delay">Amount of seconds to wait before playing the sound</param>
        /// <param name="ignoreTimeScale">If true, will not be paused by AudioManager when TimeScale is 0. To change this option for all sounds, check AudioManager's advanced settings</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundOnce(int s, Transform trans = null, Priority p = Priority.Default, Pitch pitchShift = Pitch.None, float delay = 0, bool ignoreTimeScale = false)
        {
            if (!Application.isPlaying) return null;
            AudioSource a = GetAvailableSource();

            if (trans != null)
            {
                sourcePositions[a] = trans;
                a.transform.position = trans.position;
                if (spatialSound)
                {
                    a.spatialBlend = 1;
                }
            }
            else
            {
                a.transform.position = listener.transform.position;
                if (spatialSound)
                {
                    a.spatialBlend = 0;
                }
            }

            float pitch = UsePitch(pitchShift);

            //This is the base unchanged pitch
            if (pitch > Pitches.None)
            {
                a.pitch = 1 + UnityEngine.Random.Range(-pitch, pitch);
            }
            else
            {
                a.pitch = 1;
            }

            if (audioFileObjects[s].UsingLibrary())
            {
                AudioClip[] library = audioFileObjects[s].GetFiles().ToArray();
                do
                {
                    a.clip = library[UnityEngine.Random.Range(0, library.Length)];
                } while (a.clip == null); // If the user is a dingus and left a few null references in the library
            }
            else
            {
                a.clip = audioFileObjects[s].GetFile();
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }
            a.priority = (int)p;
            a.loop = false;
            a.PlayDelayed(delay);

            return a;
        }

        /// <summary>
        /// Equivalent of PlayOneShot
        /// </summary>
        /// <param name="s">The audioclip to play</param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="pitchShift">If not None, randomizes the pitch of the sound, use AudioManager.Pitches for presets</param>
        /// <param name="delay">Amount of seconds to wait before playing the sound</param>
        /// <param name="ignoreTimeScale">If true, will not be paused by AudioManager when TimeScale is 0. To change this option for all sounds, check AudioManager's advanced settings</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundOnce(AudioClip audioClip, Transform trans = null, Priority p = Priority.Default, Pitch pitchShift = Pitch.None, float delay = 0, bool ignoreTimeScale = false)
        {
            if (!Application.isPlaying) return null;
            AudioSource a = GetAvailableSource();

            if (trans != null)
            {
                sourcePositions[a] = trans;
                a.transform.position = trans.position;
                if (spatialSound)
                {
                    a.spatialBlend = 1;
                }
            }
            else
            {
                a.transform.position = listener.transform.position;
                if (spatialSound)
                {
                    a.spatialBlend = 0;
                }
            }

            float pitch = UsePitch(pitchShift);

            //This is the base unchanged pitch
            if (pitch > Pitches.None)
            {
                a.pitch = 1 + UnityEngine.Random.Range(-pitch, pitch);
            }
            else
            {
                a.pitch = 1;
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            a.clip = audioClip;
            a.priority = (int)p;
            a.loop = false;
            a.PlayDelayed(delay);

            return a;
        }

        /// <summary>
        /// Play a sound and loop it forever
        /// </summary>
        /// <param name="sound">Sound to be played in the form of an enum. Check AudioManager for the appropriate value to be put here.</param>
        /// <param name="trans">The transform of the sound's source, makes it easier to stop the looping sound using StopSoundLoop</param>
        /// <param name="spatialSound">Makes the sound 3D if true, otherwise 2D</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="ignoreTimeScale">If true, will not be paused by AudioManager when TimeScale is 0. To change this option for all sounds, check AudioManager's advanced settings</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoop<T>(T sound, Transform trans = null, bool useSpatialSound = false, Priority p = Priority.Default, float delay = 0, bool ignoreTimeScale = false) where T : Enum
        {
            if (!Application.isPlaying) return null;
            AudioSource a = GetAvailableSource();
            loopingSources.Add(a);
            if (trans != null)
            {
                sourcePositions[a] = trans;
                a.transform.position = trans.position;
            }
            else
            {
                sourcePositions[a] = null;
            }

            int s = Convert.ToInt32(sound);

            if (audioFileObjects[s].UsingLibrary())
            {
                AudioClip[] library = audioFileObjects[s].GetFiles().ToArray();
                do
                {
                    a.clip = library[UnityEngine.Random.Range(0, library.Length)];
                } while (a.clip == null); // If the user is a dingus and left a few null references in the library
            }
            else
            {
                a.clip = audioFileObjects[s].GetFile();
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            a.spatialBlend = (spatialSound && useSpatialSound) ? 1 : 0;
            a.priority = (int)p;
            a.pitch = 1;
            a.loop = true;
            a.PlayDelayed(delay);

            return a;
        }

        /// <summary>
        /// Play a sound and loop it forever
        /// </summary>
        /// <param name="s"></param>
        /// <param name="trans">The transform of the sound's source, makes it easier to stop the looping sound using StopSoundLoop</param>
        /// <param name="spatialSound">Makes the sound 3D if true, otherwise 2D</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="ignoreTimeScale">If true, will not be paused by AudioManager when TimeScale is 0. To change this option for all sounds, check AudioManager's advanced settings</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoop(int s, Transform trans = null, bool useSpatialSound = false, Priority p = Priority.Default, float delay = 0, bool ignoreTimeScale = false)
        {
            if (!Application.isPlaying) return null;
            AudioSource a = GetAvailableSource();
            loopingSources.Add(a);
            if (trans != null)
            {
                sourcePositions[a] = trans;
                a.transform.position = trans.position;
            }
            else
            {
                sourcePositions[a] = null;
            }

            if (audioFileObjects[s].UsingLibrary())
            {
                AudioClip[] library = audioFileObjects[s].GetFiles().ToArray();
                do
                {
                    a.clip = library[UnityEngine.Random.Range(0, library.Length)];
                } while (a.clip == null); // If the user is a dingus and left a few null references in the library
            }
            else
            {
                a.clip = audioFileObjects[s].GetFile();
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }

            a.spatialBlend = (spatialSound && useSpatialSound) ? 1 : 0;
            a.priority = (int)p;
            a.pitch = 1;
            a.loop = true;
            a.PlayDelayed(delay);

            return a;
        }

        /// <summary>
        /// Play a sound and loop it forever
        /// </summary>
        /// <param name="s">The audioclip to play</param>
        /// <param name="trans">The transform of the sound's source, makes it easier to stop the looping sound using StopSoundLoop</param>
        /// <param name="spatialSound">Makes the sound 3D if true, otherwise 2D</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="ignoreTimeScale">If true, will not be paused by AudioManager when TimeScale is 0. To change this option for all sounds, check AudioManager's advanced settings</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundLoop(AudioClip s, Transform trans = null, bool useSpatialSound = false, Priority p = Priority.Default, float delay = 0, bool ignoreTimeScale = false)
        {
            if (!Application.isPlaying) return null;
            AudioSource a = GetAvailableSource();
            loopingSources.Add(a);
            if (trans != null)
            {
                sourcePositions[a] = trans;
                a.transform.position = trans.position;
            }
            else
            {
                sourcePositions[a] = null;
            }

            if (ignoreTimeScale)
            {
                ignoringTimeScale.Add(a);
            }
            else
            {
                if (ignoringTimeScale.Contains(a)) ignoringTimeScale.Remove(a);
            }
            
            a.spatialBlend = (spatialSound && useSpatialSound) ? 1 : 0;
            a.clip = s;
            a.priority = (int)p;
            a.pitch = 1;
            a.loop = true;
            a.PlayDelayed(delay);

            return a;
        }

        /// <summary>
        /// Stops all playing sounds maintained by AudioManager
        /// </summary>
        public void StopAllSounds()
        {
            foreach (AudioSource s in sources)
            {
                if (s == null) continue;
                if (s.isPlaying)
                {
                    s.Stop();
                }
            }
            loopingSources.Clear();
            sourcePositions.Clear();
            ignoringTimeScale.Clear();
        }

        /// <summary>
        /// Stops any sound playing through PlaySoundOnce() immediately 
        /// </summary>
        /// <param name="s">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate soundss</param>
        public void StopSound<T>(T sound, Transform t = null) where T : Enum
        {
            int s = Convert.ToInt32(sound);
            for (int i = 0; i < audioSources; i++)
            {
                if (audioFileObjects[s].HasAudioClip(sources[i].clip))
                {
                    if (t != null)
                    {
                        if (sourcePositions[sources[i]] != t) continue;
                    }
                    sources[i].Stop();
                    return;
                }
            }
        }

        /// <summary>
        /// Stops any sound playing through PlaySoundOnce() immediately 
        /// </summary>
        /// <param name="s">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate soundss</param>
        public void StopSound(int s, Transform t = null)
        {
            for (int i = 0; i < audioSources; i++)
            {
                if (audioFileObjects[s].HasAudioClip(sources[i].clip))
                {
                    if (t != null)
                    {
                        if (sourcePositions[sources[i]] != t) continue;
                    }
                    sources[i].Stop();
                    return;
                }
            }
        }

        /// <summary>
        /// Stops any sound playing through PlaySoundOnce() immediately 
        /// </summary>
        /// <param name="audioClip">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate soundss</param>
        public void StopSound(AudioClip audioClip, Transform t = null)
        {
            for (int i = 0; i < audioSources; i++)
            {
                if (sources[i].clip == audioClip)
                {
                    if (t != null)
                    {
                        if (sourcePositions[sources[i]] != t) continue;
                    }
                    sources[i].Stop();
                    return;
                }
            }
        }

        /// <summary>
        /// Stops a looping sound
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager for the proper value</param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="t">Transform of the object playing the looping sound</param>
        public void StopSoundLoop<T>(T sound, bool stopInstantly = false, Transform t = null) where T : Enum
        {
            int s = Convert.ToInt32(sound);
            for (int i = 0; i < loopingSources.Count; i++)
            {
                if (loopingSources[i] == null) continue;

                if (audioFileObjects[s].HasAudioClip(loopingSources[i].clip))
                {
                    for (int j = 0; j < sources.Count; j++)
                    { // Thanks Connor Smiley 
                        if (sources[j] == loopingSources[i])
                        {
                            if (t != sources[j].transform)
                                continue;
                        }
                    }
                    if (stopInstantly) loopingSources[i].Stop();
                    loopingSources[i].loop = false;
                    loopingSources.RemoveAt(i);
                    sourcePositions.Remove(sources[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Stops a looping sound
        /// </summary>
        /// <param name="s"></param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="t">Transform of the object playing the looping sound</param>
        public void StopSoundLoop(int s, bool stopInstantly = false, Transform t = null)
        {
            for (int i = 0; i < loopingSources.Count; i++)
            {
                if (loopingSources[i] == null) continue;

                if (audioFileObjects[s].HasAudioClip(loopingSources[i].clip))
                {
                    for (int j = 0; j < sources.Count; j++)
                    { // Thanks Connor Smiley 
                        if (sources[j] == loopingSources[i])
                        {
                            if (t != sources[j].transform)
                                continue;
                        }
                    }
                    if (stopInstantly) loopingSources[i].Stop();
                    loopingSources[i].loop = false;
                    loopingSources.RemoveAt(i);
                    sourcePositions.Remove(sources[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Stops a looping sound
        /// </summary>
        /// <param name="s"></param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="t">Transform of the object playing the looping sound</param>
        public void StopSoundLoop(AudioClip s, bool stopInstantly = false, Transform t = null)
        {
            for (int i = 0; i < loopingSources.Count; i++)
            {
                if (loopingSources[i].clip == s)
                {
                    for (int j = 0; j < sources.Count; j++)
                    { // Thanks Connor Smiley 
                        if (sources[j] == loopingSources[i])
                        {
                            if (t != sources[j].transform)
                                continue;
                        }
                    }
                    if (stopInstantly) loopingSources[i].Stop();
                    loopingSources[i].loop = false;
                    loopingSources.RemoveAt(i);
                    sourcePositions.Remove(sources[i]);
                    return;
                }
            }
            //Debug.LogError("AudioManager Error: Did not find specified loop to stop!");
        }

        /// <summary>
        /// Stops all looping sounds
        /// </summary>
        /// <param name="stopPlaying">
        /// Stops sounds instantly if true, lets them finish if false
        /// </param>
        public void StopSoundLoopAll(bool stopPlaying = false)
        {
            if (loopingSources.Count > 0)
            {
                for (int i = 0; i < loopingSources.Count; i++)
                {
                    if (loopingSources[i] == null) continue;
                    if (stopPlaying) loopingSources[i].Stop();
                    loopingSources[i].loop = false;
                    loopingSources.Remove(loopingSources[i]);
                }
            }
        }

        /// <summary>
        /// Returns master volume as a normalized float between 0 and 1
        /// </summary>
        public float GetMasterVolume()
        {
            return masterVolume;
        }

        /// <summary>
        /// Returns master volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetMasterVolumeAsInt()
        {
            return Mathf.RoundToInt(masterVolume * 100f);
        }

        /// <summary>
        /// Sets the volume of the master channel and applies changes instantly across all sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="volume">The new volume level from 0 to 1</param>
        public void SetMasterVolume(float volume)
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
        public void SetMasterVolume(int volume)
        {
            masterVolume = (float)Mathf.Clamp(volume, 0, 100) / 100f;
            ApplySoundVolume();
            ApplyMusicVolume();
        }

        /// <summary>
        /// Returns sound volume as a normalized float between 0 and 1
        /// </summary>
        public float GetSoundVolume()
        {
            return soundVolume;
        }

        /// <summary>
        /// Returns sound volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetSoundVolumeAsInt()
        {
            return Mathf.RoundToInt(soundVolume * 100f);
        }

        /// <summary>
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public void SetSoundVolume(float volume)
        {
            soundVolume = Mathf.Clamp01(volume);
            ApplySoundVolume();
        }

        /// <summary>
        /// /// <summary>
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="v">The new volume level from 0 to 100</param>
        public void SetSoundVolume(int volume)
        {
            soundVolume = (float)Mathf.Clamp(volume, 0, 100) / 100f;
            ApplySoundVolume();
        }

        /// <summary>
        /// Returns music volume as a normalized float between 0 and 1
        /// </summary>
        public float GetMusicVolume()
        {
            return musicVolume;
        }

        /// <summary>
        /// Returns music volume as an integer between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetMusicVolumeAsInt()
        {
            return Mathf.RoundToInt(musicVolume * 100f);
        }

        /// <summary>
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// Volume is clamped from 0 to 1
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolume();
        }

        /// <summary>
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// This method takes values from 0 to 100 and will normalize it between 0 and 1 automatically
        /// </summary>
        /// <param name="v">The new volume level from 0 to 100</param>
        public void SetMusicVolume(int volume)
        {
            musicVolume = (float)Mathf.Clamp(volume, 0, 100) / 100f;
            ApplyMusicVolume();
        }

        void ApplySoundVolume()
        {
            if (sources == null) return;
            if (sources.Count == 0) return;
            float newVolume = soundVolume * masterVolume * Convert.ToInt32(!masterMuted) * Convert.ToInt32(!soundMuted);
            foreach (AudioSource s in sources)
            {
                if (s != null)
                {
                    s.volume = newVolume;
                }
            }
        }

        void ApplyMusicVolume()
        {
            if (musicSources == null) return;
            if (musicSources.Length == 0) return;
            float newVolume = musicVolume * masterVolume * Convert.ToInt32(!masterMuted) * Convert.ToInt32(!musicMuted);
            foreach (AudioSource m in musicSources)
            {
                if (m != null)
                {
                    m.volume = newVolume;
                }
            }
        }

        /// <summary>
        /// A Monobehaviour function called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            EstablishSingletonDominance(false);
            //GenerateAudioDictionarys();
            if (GetListener() == null) FindNewListener();
            if (audioFolderLocation == "") audioFolderLocation = "Assets";
            if (!doneLoading) return;

            SetSpatialSound(spatialSound);
        }

        /// <summary>
        /// Ensures that the Audiomanager you think you're referring to actually exists in this scene
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
                else if (killSelf)
                {
                    Destroy(gameObject);
                }
            }
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
        AudioSource GetAvailableSource()
        {
            foreach (AudioSource a in sources)
            {
                if (!a.isPlaying && !loopingSources.Contains(a))
                {
                    return a;
                }
            }

            if (dynamicSourceAllocation)
            {
                AudioSource newSource = Instantiate(sourcePrefab, sourceHolder.transform).GetComponent<AudioSource>();
                newSource.name = "AudioSource " + sources.Count;
                sources.Add(newSource);
                return newSource;
            }
            else
            {
                Debug.LogError("AudioManager Error: Ran out of Audio Sources!");
            }
            return null;
        }

        /// <summary>
        /// Returns true if a sound is currently being played
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlaying<T>(T sound, Transform trans = null) where T : Enum
        {
            int s = Convert.ToInt32(sound);
            for (int i = 0; i < Mathf.Clamp(audioSources, 0, sources.Count); i++) // Loop through all sources
            {
                if (sources[i] == null) continue;
                if (audioFileObjects[s].HasAudioClip(sources[i].clip) && sources[i].isPlaying) // If this source is playing the clip
                {
                    if (trans != null)
                    {
                        if (trans != sourcePositions[sources[i]]) // Continue if this isn't the specified source position
                        {
                            continue;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if a sound is currently being played
        /// </summary>
        /// <param name="s">The sound in question</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlaying(int s, Transform trans = null)
        {
            for (int i = 0; i < Mathf.Clamp(audioSources, 0, sources.Count); i++) // Loop through all sources
            {
                if (sources[i] == null) continue;
                if (audioFileObjects[s].HasAudioClip(sources[i].clip) && sources[i].isPlaying) // If this source is playing the clip
                {
                    if (trans != null)
                    {
                        if (trans != sourcePositions[sources[i]]) // Continue if this isn't the specified source position
                        {
                            continue;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if a sound is currently being played
        /// </summary>
        /// <param name="a">The sound in question</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlaying(AudioClip a, Transform trans = null)
        {
            for (int i = 0; i < audioSources; i++) // Loop through all sources
            {
                if (sources[i].clip == a && sources[i].isPlaying) // If this source is playing the clip
                {
                    if (trans != null)
                    {
                        if (trans != sourcePositions[sources[i]]) // Continue if this isn't the specified source position
                        {
                            continue;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if music is currently being played by any music source
        /// </summary>
        /// <param name="music"></param>
        /// <returns></returns>
        public bool IsMusicPlaying<T>(T music) where T : Enum
        {
            int a = Convert.ToInt32(music);
            foreach (AudioSource m in musicSources)
            {
                if (m.clip == audioFileMusicObjects[a] && m.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if music is currently being played by any music source
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public bool IsMusicPlaying(int a)
        {
            foreach (AudioSource m in musicSources)
            {
                if (m.clip == audioFileMusicObjects[a] && m.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if music is currently being played by any music source
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public bool IsMusicPlaying(AudioClip a)
        {
            foreach (AudioSource m in musicSources)
            {
                if (m.clip == a && m.isPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if a sound is currently being played by a looping sources, more efficient for looping sounds than IsSoundPlaying
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use.</param>
        /// <returns></returns>
        public bool IsSoundLooping<T>(T sound) where T : Enum
        {
            int s = Convert.ToInt32(sound);
            foreach (AudioSource c in loopingSources)
            {
                if (c == null) continue;
                if (audioFileObjects[s].HasAudioClip(c.clip))
                {
                    return true;
                }

            }
            return false;
        }

        /// <summary>
        /// Returns true if a sound is currently being played by a looping sources, more efficient for looping sounds than IsSoundPlaying
        /// </summary>
        /// <param name="s">The sound in question</param>
        /// <returns></returns>
        public bool IsSoundLooping(int s)
        {
            foreach (AudioSource c in loopingSources)
            {
                if (c == null) continue;
                if (audioFileObjects[s].HasAudioClip(c.clip))
                {
                    return true;
                }

            }
            return false;
        }

        /// <summary>
        /// Returns true if a sound is currently being played by a looping sources, more efficient for looping sounds than IsSoundPlaying
        /// </summary>
        /// <param name="a">The sound in question</param>
        /// <returns></returns>
        public bool IsSoundLooping(AudioClip a)
        {
            foreach (AudioSource c in loopingSources)
            {
                if (c.clip == a)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Converts a pitch enum to float value
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static float UsePitch(Pitch p)
        {
            switch (p)
            {
                case Pitch.None:
                    return Pitches.None;
                case Pitch.VeryLow:
                    return Pitches.VeryLow;
                case Pitch.Low:
                    return Pitches.Low;
                case Pitch.Medium:
                    return Pitches.Medium;
                case Pitch.High:
                    return Pitches.High;
            }
            return 0;
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
        /// Used by the custom inspector to get error messages
        /// </summary>
        /// <returns></returns>
        public string GetEditorMessage()
        {
            return editorMessage;
        }

        /// <summary>
        /// Called internally by AudioManager to output non-error console messages
        /// </summary>
        /// <param name="consoleOutput"></param>
        void DebugLog(string consoleOutput)
        {
            if (disableConsoleLogs) return;
            Debug.Log(consoleOutput);
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
            return Type.GetType(sceneSoundEnumName);
        }

        public Type GetSceneMusicEnum()
        {
            return Type.GetType(sceneMusicEnumName);
        }

        public AudioListener GetListener()
        {
            return listener;
        }

        public bool SourcePrefabExists()
        {
            return sourcePrefab != null;
        }

        public void MuteMasterVolume(bool b)
        {
            masterMuted = b;
            ApplySoundVolume();
            ApplyMusicVolume();
        }

        public bool IsMasterVolumeMuted()
        {
            return masterMuted;
        }

        public void MuteSoundVolume(bool b)
        {
            soundMuted = b;
            ApplySoundVolume();
        }

        public bool IsSoundVolumeMuted()
        {
            return soundMuted;
        }

        public void MuteMusicVolume(bool b)
        {
            musicMuted = b;
            ApplyMusicVolume();
        }

        public bool IsMusicVolumeMuted()
        {
            return musicMuted;
        }
    }
}