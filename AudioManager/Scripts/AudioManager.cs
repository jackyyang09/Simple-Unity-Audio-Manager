using System;
using System.Collections;
using System.Collections.Generic;
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

    public enum Pitch
    {
        None,
        VeryLow,
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Defined as a class with predefined static values to use as an enum of floats
    /// </summary>
    public class Pitches
    {
        public static float None = 0;
        public static float VeryLow = 0.05f;
        public static float Low = 0.15f;
        public static float Medium = 0.25f;
        public static float High = 0.5f;
    }

    /// <summary>
    /// AudioManager singleton that manages all audio in the game
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        Dictionary<string, AudioFile> sounds = new Dictionary<string, AudioFile>();
        Dictionary<string, AudioClip> music = new Dictionary<string, AudioClip>();
        Dictionary<string, AudioFileMusic> musicFiles = new Dictionary<string, AudioFileMusic>();
    
        /// <summary>
        /// List of sources allocated to play looping sounds
        /// </summary>
        //[SerializeField]
        [Tooltip("List of sources allocated to play looping sounds")]
        List<AudioSource> loopingSources;
    
        //[SerializeField]
        [Tooltip("[DON'T TOUCH THIS], looping sound positions")]
        Dictionary<AudioSource, Transform> sourcePositions = new Dictionary<AudioSource, Transform>();
    
        /// <summary>
        /// Limits the number of each sounds being played. If at 0 or no value, assume infinite
        /// </summary>
        //[SerializeField]
        [Tooltip("Limits the number of each sounds being played. If at 0 or no value, assume infinite")]
        int[] exclusiveList;
    
        List<AudioSource> sources;
    
        /// <summary>
        /// Sources dedicated to playing music
        /// </summary>
        //[SerializeField]
        AudioSource[] musicSources;
    
        [Header("Volume Controls")]

        [Tooltip("All volume is set relative to the Master Volume")]
        [SerializeField]
        [Range(0, 1)]
        float masterVolume = 1;
    
        [SerializeField]
        [Range(0, 1)]
        float soundVolume = 1;
    
        [SerializeField]
        [Range(0, 1)]
        float musicVolume = 1;
    
        [Header("General Settings")]

        /// <summary>
        /// Number of Audio Sources to be created on start
        /// </summary>
        [SerializeField]
        [Tooltip("Number of Audio Sources to be created on start")]
        int audioSources = 16;
        [SerializeField]
        [Tooltip("If true, enables 3D spatialized audio for sound effects, does not effect music")]
        bool spatialSound = true;
        [SerializeField]
        [Tooltip("Use if spatialized sounds act wonky in-editor")]
        bool spatializeLateUpdate = false;
    
        [SerializeField]
        [Tooltip("When Time.timeScale is set to 0, pause all sounds")]
        bool timeScaledSounds = true;
    
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
    
        [Header("System Settings")]

        /// <summary>
        /// If true, stops all sounds when you load a scene
        /// </summary>
        [Tooltip("If true, stops all sounds when you load a scene")]
        [SerializeField]
        bool stopSoundsOnSceneLoad;
    
        /// <summary>
        /// If true, keeps AudioManager alive through scene loads
        /// </summary>
        [Tooltip("If true, keeps AudioManager alive through scene loads")]
        [SerializeField]
        bool dontDestroyOnLoad;

        /// <summary>
        /// If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this disabled
        /// </summary>
        [Tooltip("If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this disabled")]
        [SerializeField]
        bool dynamicSourceAllocation;

        /// <summary>
        /// If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings
        /// </summary>
        [Tooltip("If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings")]
        [SerializeField]
        bool disableConsoleLogs;

        /// <summary>
        /// Current music that's playing
        /// </summary>
        //[Tooltip("Current music that's playing")]
        [HideInInspector]
        public string currentTrack = "None";

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
            EstablishSingletonDominance();
    
            // AudioManager is important, keep it between scenes
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
    
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
    
            //AddOffsetToArrays();
    
            if (!Application.isEditor)
            {
                GenerateAudioDictionarys();
            }
    
            doneLoading = true;
        }
    
        void Start()
        {
            if (!Application.isEditor)
            {
                GenerateAudioDictionarys();
            }
    
            initialized = true;
        }
    
        public bool Initialized(){
            return initialized;
        }
    
        void FindNewListener()
        {
            if (listener == null)
            {
                listener = Camera.main.GetComponent<AudioListener>();
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
                else if (loopTrackAfterStopping)
                {
                    musicSources[0].timeSamples = (int)loopStartTime;
                    musicSources[0].Play();
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
        /// <param name="m">Index of the music</param>
        /// <param name="loopTrack">Does the music play forever?</param>
        /// <param name="useLoopPoints">Does the clip have an intro portion that plays only once? True by default, will loop from start to end otherwise</param>
        public AudioSource PlayMusic(string track, bool loopTrack = true, bool useLoopPoints = true)
        {
            if (track.Equals("None")) return null;
            currentTrack = track;
    
            musicSources[0].clip = music[track];
    
            enableLoopPoints = useLoopPoints;
            if (enableLoopPoints && musicFiles[track].useLoopPoints)
            {
                loopStartTime = musicFiles[track].loopStart * music[track].frequency;
                loopEndTime = musicFiles[track].loopEnd * music[track].frequency;
            }
    
            if (enableLoopPoints && loopTrack && musicFiles[track].useLoopPoints)
            {
                if (musicFiles[track].loopEnd == music[track].length) loopTrack = false;
                loopTrackAfterStopping = true;
            }
            else
            {
                musicSources[0].loop = loopTrack;
                loopTrackAfterStopping = false;
            }
    
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
        public AudioSource PlayMusic(AudioClip track, bool loopTrack = true)
        {
            if (track.Equals("None")) return null;
            currentTrack = "Custom Audio File";
    
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
        /// <param name="loopTrack">Does the music play forever?</param>
        /// <param name="useLoopPoints">Does the clip have an intro portion that plays only once?</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlayMusic3D(string track, Transform trans, bool loopTrack = true, bool useLoopPoints = false)
        {
            if (track.Equals("None")) return null;
            currentTrack = track;
    
            sourcePositions[musicSources[2]] = trans;
    
            musicSources[2].clip = music[track];
            musicSources[2].loop = loopTrack;
    
            enableLoopPoints = useLoopPoints;
            if (enableLoopPoints)
            {
                loopStartTime = musicFiles[track].loopStart * music[track].frequency;
                loopEndTime = musicFiles[track].loopEnd * music[track].frequency;
            }
    
            if (enableLoopPoints && loopTrack)
            {
                if (musicFiles[track].loopEnd == music[track].length) loopTrack = false;
                loopTrackAfterStopping = true;
            }
            else
            {
                musicSources[2].loop = loopTrack;
                loopTrackAfterStopping = false;
            }
    
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
            currentTrack = "Custom Audio File";
    
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
        /// <param name="useLoopPoints">Loop the track between preset loop points?</param>
        public void FadeMusic(string track, float time, bool useLoopPoints = false)
        {
            if (track.Equals("None")) return;
    
            currentTrack = track;
    
            musicSources[1].clip = music[track];
            musicSources[1].loop = true;
    
            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
    
            musicSources[0].clip = music[track];
            musicSources[0].loop = true;
    
            enableLoopPoints = useLoopPoints;
    
            if (enableLoopPoints)
            {
                loopStartTime = musicFiles[track].loopStart * music[track].frequency;
                loopEndTime = musicFiles[track].loopEnd * music[track].frequency;
            }
    
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
    
            currentTrack = track.name;
    
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
        /// <param name="useLoopPoints">Plays using loop points enabled if true</param>
        /// <param name="playFromStartPoint">Start track playback from starting loop point, only works if useLoopPoints is true</param>
        public AudioSource FadeMusicIn(string track, float time, bool useLoopPoints = false, bool playFromStartPoint = false)
        {
            if (track.Equals("None")) return null;
    
            currentTrack = track;
    
            musicSources[1].clip = music[track];
            musicSources[1].loop = true;
    
            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
    
            musicSources[0].clip = music[track];
            musicSources[0].loop = true;
    
            enableLoopPoints = useLoopPoints;
            if (enableLoopPoints)
            {
                loopStartTime = musicFiles[track].loopStart * music[track].frequency;
                loopEndTime = musicFiles[track].loopEnd * music[track].frequency;
                if (playFromStartPoint)
                {
                    musicSources[0].timeSamples = (int)loopStartTime;
                }
            }
    
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
    
            currentTrack = track.ToString();
    
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
            currentTrack = "None";
    
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
        /// <param name="useLoopPoints">Loop the track between preset loop points?</param>
        /// <param name="keepMusicTime">Carry the current playback time of current track over to the next track?</param>
        public void CrossfadeMusic(string track, float time = 0, bool useLoopPoints = false, bool keepMusicTime = false)
        {
            if (track.Equals("None")) return;
            currentTrack = track;
    
            musicSources[1].clip = music[track];
            musicSources[1].loop = true;
    
            AudioSource temp = musicSources[0];
            musicSources[0] = musicSources[1];
            musicSources[1] = temp;
    
            musicSources[0].clip = music[track];
            musicSources[0].loop = true;
    
            enableLoopPoints = useLoopPoints;
            if (enableLoopPoints)
            {
                loopStartTime = musicFiles[track].loopStart * music[track].frequency;
                loopEndTime = musicFiles[track].loopEnd * music[track].frequency;
            }
    
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
            currentTrack = "Custom";
    
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
    
            fadeOutRoutine = null;
        }
    
        /// <summary>
        /// Stop whatever is playing in musicSource
        /// </summary>
        public void StopMusic()
        {
            musicSources[0].Stop();
            currentTrack = "None";
        }
    
        /// <summary>
        /// Stops the specified track
        /// </summary>
        /// <param name="m">The name of the track in question</param>
        public void StopMusic(string m)
        {
            if (!music.ContainsKey(m)) return;
            foreach(AudioSource a in musicSources)
            {
                if (a == null) continue; // Sometimes AudioPlayerMusic calls StopMusic on scene stop
                if (a.clip == music[m])
                {
                    a.Stop();
                }
            }
            currentTrack = "None";
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
                            sources[i].transform.position = sourcePositions[sources[i]].transform.position;
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
        /// <param name="s"></param>
        /// <param name="trans">The transform of the sound's source</param>
        /// <param name="p">The priority of the sound</param>
        /// <param name="pitchShift">If not None, randomizes the pitch of the sound, use AudioManager.Pitches for presets</param>
        /// <param name="delay">Amount of seconds to wait before playing the sound</param>
        /// <returns>The AudioSource playing the sound</returns>
        public AudioSource PlaySoundOnce(string s, Transform trans = null, Priority p = Priority.Default, Pitch pitchShift = Pitch.None, float delay = 0)
        {
            AudioSource a = GetAvailableSource();
    
            if (trans != null)
            {
                sourcePositions[a] = trans;
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
    
            if (sounds[s].UsingLibrary())
            {
                AudioClip[] library = sounds[s].GetFiles();
                a.clip = library[UnityEngine.Random.Range(0, library.Length)];
            }
            else
            {
                a.clip = sounds[s].GetFile();
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
        public AudioSource PlaySoundOnce(AudioClip s, Transform trans = null, Priority p = Priority.Default, Pitch pitchShift = Pitch.None, float delay = 0)
        {
            AudioSource a = GetAvailableSource();
    
            if (trans != null)
            {
                sourcePositions[a] = trans;
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
    
            a.clip = s;
            a.priority = (int)p;
            a.loop = false;
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
        public AudioSource PlaySoundLoop(string s, Transform trans = null, bool spatialSound = false, Priority p = Priority.Default, float delay = 0)
        {
            AudioSource a = GetAvailableSource();
            loopingSources.Add(a);
            if (trans != null)
            {
                sourcePositions[a] = trans;
            }
            else
            {
                sourcePositions[a] = null;
            }
    
            if (sounds[s].UsingLibrary())
            {
                AudioClip[] library = sounds[s].GetFiles();
                a.clip = library[UnityEngine.Random.Range(0, library.Length)];
            }
            else
            {
                a.clip = sounds[s].GetFile();
            }
            a.spatialBlend = spatialSound ? 1 : 0;
            a.priority = (int)p;
            a.pitch = 1;
            a.loop = true;
            if (delay > 0)
            {
                a.PlayDelayed(delay);
            }
            else a.Play();
    
            return a;
        }
    
        /// <summary>
        /// Play a sound and loop it forever
        /// </summary>
        /// <param name="s"></param>
        /// <param name="trans">The transform of the sound's source, makes it easier to stop the looping sound using StopSoundLoop</param>
        /// <param name="spatialSound">Makes the sound 3D if true, otherwise 2D</param>
        /// <param name="p">The priority of the sound</param>
        public AudioSource PlaySoundLoop(AudioClip s, Transform trans = null, bool spatialSound = false, Priority p = Priority.Default, float delay = 0)
        {
            AudioSource a = GetAvailableSource();
            loopingSources.Add(a);
            if (trans != null)
            {
                sourcePositions[a] = trans;
            }
            else
            {
                sourcePositions[a] = null;
            }
    
            a.spatialBlend = spatialSound ? 1 : 0;
            a.clip = s;
            a.priority = (int)p;
            a.pitch = 1;
            a.loop = true;
            if (delay > 0)
            {
                a.PlayDelayed(delay);
            }
            else a.Play();
    
            return a;
        }
    
        /// <summary>
        /// Stops all playing sounds maintained by AudioManager
        /// </summary>
        public void StopAllSounds()
        {
            foreach (AudioSource s in sources)
            {
                if (s.isPlaying)
                {
                    s.Stop();
                }
            }
        }
    
        /// <summary>
        /// Stops any sound playing through PlaySoundOnce() immediately 
        /// </summary>
        /// <param name="s">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate soundss</param>
        public void StopSound(string s, Transform t = null)
        {
            if (!sounds.ContainsKey(s)) return;
            for (int i = 0; i < audioSources; i++)
            {
                if (sources[i].clip == sounds[s])
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
        /// <param name="a">The sound to be stopped</param>
        /// <param name="t">For sources, helps with duplicate soundss</param>
        public void StopSound(AudioClip a, Transform t = null)
        {
            for (int i = 0; i < audioSources; i++)
            {
                if (sources[i].clip == a)
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
        /// <param name="s"></param>
        /// <param name="stopInstantly">Stops sound instantly if true</param>
        /// <param name="t">Transform of the object playing the looping sound</param>
        public void StopSoundLoop(string s, bool stopInstantly = false, Transform t = null)
        {
            for (int i = 0; i < loopingSources.Count; i++)
            {
                if (loopingSources[i] == null) continue;
                if (loopingSources[i].clip == sounds[s])
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
        /// Sets the volume of sounds and applies changes instantly across all sources
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public void SetSoundVolume(float v)
        {
            soundVolume = v;
            ApplySoundVolume();
        }
    
        /// <summary>
        /// Sets the volume of the music and applies changes instantly across all music sources
        /// </summary>
        /// <param name="v">The new volume level from 0 to 1</param>
        public void SetMusicVolume(float v)
        {
            musicVolume = v;
            foreach (AudioSource m in musicSources)
            {
                m.volume = musicVolume * masterVolume;
            }
            ApplyMusicVolume();
        }
    
        void ApplySoundVolume()
        {
            foreach (AudioSource s in sources)
            {
                s.volume = soundVolume * masterVolume;
            }
        }
    
        void ApplyMusicVolume()
        {
            foreach (AudioSource s in musicSources)
            {
                s.volume = musicVolume * masterVolume;
            }
        }

        /// <summary>
        /// A Monobehaviour function called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            EstablishSingletonDominance();
            GenerateAudioDictionarys();
            if (!doneLoading) return;
            //Updates volume
            ApplySoundVolume();
            ApplyMusicVolume();
            SetSpatialSound(spatialSound);
        }

        /// <summary>
        /// Ensures that the Audiomanager you think you're referring to actually exists in this scene
        /// </summary>
        public void EstablishSingletonDominance()
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
                else
                {
                    DestroyImmediate(this, false);
                }
            }
        }
    
        /// <summary>
        /// DEPRECATED
        /// Accomodates the "None" helper enum during runtime
        /// </summary>
        public void AddOffsetToArrays()
        {
            //if (clips[0] != null && clips.Count < (int)Sound.Count)
            //{
            //    clips.Insert(0, null);
            //}
    
            //if (tracks[0] != null && tracks.Count < (int)Music.Count)
            //{
            //    tracks.Insert(0, null);
            //}
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
            }
            else
            {
                Debug.LogError("AudioManager Error: Ran out of Audio Sources!");
            }
            return null;
        }
    
        /// <summary>
        /// Returns Master volume from 0-1
        /// </summary>
        public float GetMasterVolume()
        {
            return masterVolume;
        }
    
        /// <summary>
        /// Returns sound volume from 0-1
        /// </summary>
        public float GetSoundVolume()
        {
            return soundVolume;
        }
    
        /// <summary>
        /// Returns music volume from 0-1
        /// </summary>
        public float GetMusicVolume()
        {
            return musicVolume;
        }
    
        /// <summary>
        /// Returns true if a sound is currently being played
        /// </summary>
        /// <param name="s">The sound in question</param>
        /// <param name="trans">Specify is the sound is playing from that transform</param>
        /// <returns></returns>
        public bool IsSoundPlaying(string s, Transform trans = null)
        {
            for (int i = 0; i < Mathf.Clamp(audioSources, 0, sources.Count); i++) // Loop through all sources
            {
                if (sources[i] == null) continue;
                if (sources[i].clip == sounds[s] && sources[i].isPlaying) // If this source is playing the clip
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
        /// Returns true if music is currently being played by any music source
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public bool IsMusicPlaying(string a)
        {
            foreach (AudioSource m in musicSources)
            {
                if (m.clip == music[a] && m.isPlaying)
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
        /// <returns>True or false you dingus</returns>
        public bool IsSoundLooping(string s)
        {
            foreach (AudioSource c in loopingSources)
            {
                if (c == null) continue;
                if (c.clip == sounds[s])
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
        /// <returns>True or false you dingus</returns>
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
    
        public void GenerateAudioDictionarys()
        {
            // Create dictionary for sound
            if (transform.childCount == 0) return; // Probably script execution order issue
    
            bool regenerateSounds = transform.GetChild(0).childCount != sounds.Count;
    
            // Regenerate sounds if you rename sounds
            if (!regenerateSounds)
            {
                for (int i = 0; i < transform.GetChild(0).childCount; i++)
                {
                    if (!sounds.ContainsKey(transform.GetChild(0).GetChild(i).name))
                    {
                        regenerateSounds = true;
                        break;
                    }
                }
            }

            if (regenerateSounds)
            {
                sounds.Clear();
                foreach (AudioFile a in transform.GetChild(0).GetComponentsInChildren<AudioFile>())
                {
                    if (sounds.ContainsKey(a.name))
                    {
                        if (sounds[a.name].Equals(a)) continue;
                        else
                        {
                            sounds[a.name] = a;
                        }
                    }
                    else
                    {
                        sounds.Add(a.name, a);
                    }
                }
            }
    
            bool regenerateMusic = transform.GetChild(1).childCount != music.Count;
    
            // Regenerate music if you rename sounds
            if (!regenerateMusic)
            {
                for (int i = 0; i < transform.GetChild(1).childCount; i++)
                {
                    if (!sounds.ContainsKey(transform.GetChild(1).GetChild(i).name))
                    {
                        regenerateMusic = true;
                        break;
                    }
                }
            }
    
            // Create a dictionary for music
            if (regenerateMusic)
            {
                music.Clear();
                musicFiles.Clear();
                foreach (AudioFileMusic a in transform.GetChild(1).GetComponentsInChildren<AudioFileMusic>())
                {
                    if (music.ContainsKey(a.name))
                    {
                        if (music[a.name].Equals(a.GetFile())) continue;
                        else
                        {
                            music[a.name] = a.GetFile();
                        }
                    }
                    else
                    {
                        music.Add(a.name, a.GetFile());
                        musicFiles.Add(a.name, a);
                    }
                }
            }
    
            DebugLog("AudioManager: Audio Library Generated!");
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

        public Dictionary<string, AudioClip> GetMusicDictionary()
        {
            return music;
        }
    
        public Dictionary<string, AudioFile> GetSoundDictionary()
        {
            return sounds;
        }
    }
}
