using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

namespace JSAM
{
    /// <summary>
    /// AudioManager singleton that manages all audio in the game
    /// </summary>
    [DefaultExecutionOrder(1)]
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        [Header("General Settings")]

        static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                bool missing = false;
                if (instance == null) missing = true;
                else if (instance.gameObject.scene == null) missing = true;
                if (missing)
                {
                    instance = FindObjectOfType<AudioManager>();
                    if (instance == null)
                    {
                        if (!isQuitting && Application.isPlaying)
                        {
                            DebugError("No AudioManager found in scene " + SceneManager.GetActiveScene().name);
                        }
                    }
                }
                return instance;
            }
        }

        [Tooltip("The Audio Library that this AudioManager should use")]
        [SerializeField, HideInInspector] AudioLibrary library = null;
        public AudioLibrary Library { get { return library; } }

        [Header("Scene AudioListener Reference")]

        /// <summary>
        /// The Audio Listener in your scene, will try to automatically set itself on start by looking at the object tagged as \"Main Camera\"
        /// </summary>
        [Tooltip("The Audio Listener in your scene, will try to automatically set itself on Start by looking in the object tagged as \"Main Camera\"")]
        [SerializeField] AudioListener listener = null;
        public static AudioListener AudioListener { get { return Instance.listener; } }

        bool doneLoading;

        bool initialized = false;
        /// <summary>
        /// True if AudioManager finishes setting up
        /// </summary>
        public bool Initialized { get { return initialized; } }

        public static MusicChannelHelper MainMusicHelper { get { return InternalInstance.mainMusic; } }
        public static MusicFileObject MainMusic { get { return MainMusicHelper.AudioFile; } }
        /// <summary>
        /// Returns the currently playing music as an integer for you to convert back into an enum. 
        /// This only works if the AudioManager in the current scene has the currently 
        /// playing music registered in it's AudioLibrary
        /// </summary>
        public static int MainMusicEnumAsInt
        {
            get
            {
                var library = Instance.library;
                return library.Music.IndexOf(MainMusic);
            }
        }

        static AudioManagerInternal internalInstance;
        public static AudioManagerInternal InternalInstance
        {
            get
            {
                if (internalInstance == null)
                {
                    if (Instance != null && Application.isPlaying)
                    {
                        internalInstance = Instance.gameObject.AddComponent<AudioManagerInternal>();
                    }
                }
                return internalInstance;
            }
        }

        static bool isQuitting;

        #region Events
        /// <summary>
        /// Invoked when the AudioManager finishes setting up
        /// </summary>
        public static Action OnAudioManagerInitialized;
        public static Action<SoundFileObject> OnSoundPlayed;
        public static Action<MusicFileObject> OnMusicPlayed;
        /// <summary>
        /// Invoked when the volume of the Music channel is changed. Passes the new volume as a normalized float value between 0 and 1
        /// </summary>
        public static Action<float> OnMasterVolumeChanged;
        /// <summary>
        /// Invoked when the volume of the Music channel is changed. Passes the new volume as a normalized float value between 0 and 1
        /// </summary>
        public static Action<float> OnMusicVolumeChanged;
        /// <summary>
        /// Invoked when the volume of the Sound channel is changed. Passes the new volume as a normalized float value between 0 and 1
        /// </summary>
        public static Action<float> OnSoundVolumeChanged;
        #endregion

        // Use this for initialization
        void Awake()
        {
            // AudioManager is important, keep it between scenes
            if (JSAMSettings.Settings.DontDestroyOnLoad)
            {
                gameObject.transform.SetParent(null, true); 
                DontDestroyOnLoad(gameObject);
            }

            EstablishSingletonDominance();

            if (!initialized)
            {
                // Find the listener if not manually set
                FindNewListener();

                doneLoading = true;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += Quitting;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= Quitting;
        }

        void Quitting()
        {
            isQuitting = true;
        }

        void Start()
        {
            if (!library)
            {
                DebugWarning("No Audio Library specified in AudioManager!");
            }
            else
            {
                for (int i = 0; i < library.Sounds.Count; i++)
                {
                    library.Sounds[i].Initialize();
                }
            }

            initialized = true;
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
                    DebugWarning("Scene is missing an AudioListener!");
                }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindNewListener();
            if (JSAMSettings.Settings.StopSoundsOnSceneLoad)
            {
                StopAllSounds();
            }
        }

        public static SoundFileObject GetSoundSafe<T>(T sound) where T : Enum
        {
            int s = Convert.ToInt32(sound);
            try { return Instance.Library.Sounds[s]; }
            catch (ArgumentOutOfRangeException)
            {
                DebugError("Provided sound \"" + sound + "\" does not exist in library \"" + instance.library.name + "\"!\n" +
                    "Make sure you're using the correct library or consider re-generating your Library!");
                return null;
            }
        }

        public static MusicFileObject GetMusicSafe<T>(T music) where T : Enum
        {
            int m = Convert.ToInt32(music);
            try { return Instance.Library.Music[m]; }
            catch (ArgumentOutOfRangeException)
            {
                DebugError("Provided music \"" + music + "\" does not exist in library \"" + instance.library.name + "\"!\n" +
                    "Make sure you're using the correct library or consider re-generating your Library!");
                return null;
            }
        }

        #region PlaySound
        /// <summary>
        /// Plays the specified sound using the settings provided in the Sound File Object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The enum correlating with the audio file you wish to play</param>
        /// <param name="transform">Optional: The transform of the sound's source</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only emit one sound at any time</para></param>
        /// <returns>The Sound Channel Helper playing the sound</returns>
        public static SoundChannelHelper PlaySound<T>(T sound, Transform transform = null, SoundChannelHelper helper = null) where T : Enum
        {
            return InternalInstance.PlaySoundInternal(GetSoundSafe(sound), transform, helper);
        }

        /// <summary>
        /// <inheritdoc cref="PlaySound{T}(T, Transform, SoundChannelHelper)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The enum correlating with the audio file you wish to play</param>
        /// <param name="position">Optional: The world position you want the sound to play from</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only emit one sound at any time</para></param>
        /// <returns><inheritdoc cref="PlaySound{T}(T, Transform, SoundChannelHelper)" path="/returns"/></returns>
        public static SoundChannelHelper PlaySound<T>(T sound, Vector3 position, SoundChannelHelper helper = null) where T : Enum
        {
            return InternalInstance.PlaySoundInternal(GetSoundSafe(sound), position, helper);
        }

        /// <summary>
        /// <inheritdoc cref="PlaySound{T}(T, Transform, SoundChannelHelper)"/>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to play directly</param>
        /// <param name="transform">Optional: The transform of the sound's source</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only emit one sound at any time</para></param>
        /// <returns><inheritdoc cref="PlaySound{T}(T, Transform, SoundChannelHelper)" path="/returns"/></returns>
        public static SoundChannelHelper PlaySound(SoundFileObject sound, Transform transform = null, SoundChannelHelper helper = null) => InternalInstance.PlaySoundInternal(sound, transform, helper);

        /// <summary>
        /// <inheritdoc cref="PlaySound{T}(T, Transform, SoundChannelHelper)"/>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to play directly</param>
        /// <param name="position">The position you want to play the sound at</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only emit one sound at any time</para></param>
        /// <returns><inheritdoc cref="PlaySound{T}(T, Transform, SoundChannelHelper)" path="/returns"/></returns>
        public static SoundChannelHelper PlaySound(SoundFileObject sound, Vector3 position, SoundChannelHelper helper = null) => InternalInstance.PlaySoundInternal(sound, position, helper);
        #endregion

        #region StopSound
        /// <summary>
        /// Stops the first playing instance of the given sound immediately
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The sound to be stopped</param>
        /// <param name="transform">Optional: If the sound was initially passed a reference to 
        /// a transform in PlaySound, passing the same Transform reference will stop that specific playing instance</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static void StopSound<T>(T sound, Transform transform = null, bool stopInstantly = true) where T : Enum =>
            InternalInstance.StopSoundInternal(GetSoundSafe(sound), transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSound{T}(T, Transform)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The sound to be stopped</param>
        /// <param name="position">The sound's playback position in world space. 
        /// Passing this property will limit playback stopping 
        /// to only the sound playing at this specific position</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static void StopSound<T>(T sound, Vector3 position, bool stopInstantly = true) where T : Enum => 
            InternalInstance.StopSoundInternal(GetSoundSafe(sound), position, stopInstantly);

        /// <summary>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to play directly</param>
        /// <param name="transform">Optional: If the sound was initially passed a reference to 
        /// a transform in PlaySound, passing the same Transform reference will stop that specific playing instance</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static void StopSound(SoundFileObject sound, Transform transform = null, bool stopInstantly = true) =>
            InternalInstance.StopSoundInternal(sound, transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSound{T}(T, Transform)"/>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to play directly</param>
        /// <param name="position">The sound's playback position in world space. 
        /// Passing this property will limit playback stopping 
        /// to only the sound playing at this specific position</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static void StopSound(SoundFileObject sound, Vector3 position, bool stopInstantly = true) => 
            InternalInstance.StopSoundInternal(sound, position, stopInstantly);

        /// <summary>
        /// Stops all playing sounds maintained by AudioManager
        /// <param name="stopInstantly">Optional: If true, stop all sounds immediately</param>
        /// </summary>
        public static void StopAllSounds(bool stopInstantly = true) =>
            InternalInstance.StopAllSoundsInternal(stopInstantly);

        /// <summary>
        /// A shorthand for wrapping StopSound in an IsSoundPlaying if-statement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The sound to be stopped</param>
        /// <param name="transform">Optional: If the sound was initially passed a reference to 
        /// a transform in PlaySound, passing the same Transform reference will stop that specific playing instance</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        /// <returns>True if sound was stopped successfully, false if sound wasn't playing</returns>
        public static bool StopSoundIfPlaying<T>(T sound, Transform transform = null, bool stopInstantly = true) where T : Enum =>
            InternalInstance.StopSoundIfPlayingInternal(GetSoundSafe(sound), transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSoundIfPlaying{T}(T, Transform)"/>
        /// </summary>
        /// <param name="sound">The sound to be stopped</param>
        /// <param name="position">The sound's playback position in world space. 
        /// Passing this property will limit playback stopping 
        /// to only the sound playing at this specific position</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        /// <returns>True if sound was stopped successfully, false if sound wasn't playing</returns>
        public static bool StopSoundIfPlaying<T>(T sound, Vector3 position, bool stopInstantly = true) where T : Enum =>
            InternalInstance.StopSoundIfPlayingInternal(GetSoundSafe(sound), position, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSoundIfPlaying{T}(T, Transform)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">A reference to the Sound File asset to stop directly</param>
        /// <param name="transform">Optional: If the sound was initially passed a reference to 
        /// a transform in PlaySound, passing the same Transform reference will stop that specific playing instance</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        /// <returns>True if sound was stopped successfully, false if sound wasn't playing</returns>
        public static bool StopSoundIfPlaying(SoundFileObject sound, Transform transform = null, bool stopInstantly = true) =>
            InternalInstance.StopSoundIfPlayingInternal(sound, transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSoundIfPlaying{T}(T, Transform)"/>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to stop directly</param>
        /// <param name="position">The sound's playback position in world space. 
        /// Passing this property will limit playback stopping 
        /// to only the sound playing at this specific position</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        /// <returns>True if sound was stopped successfully, false if sound wasn't playing</returns>
        public static bool StopSoundIfPlaying(SoundFileObject sound, Vector3 position, bool stopInstantly = true) =>
            InternalInstance.StopSoundIfPlayingInternal(sound, position, stopInstantly);
        #endregion

        #region IsSoundPlaying
        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="transform">Optional: Only return true if the sound is playing from this transform</param>
        /// <returns>True if a sound that was played using PlaySound is currently playing</returns>
        public static bool IsSoundPlaying<T>(T sound, Transform transform = null) where T : Enum =>
            InternalInstance.IsSoundPlayingInternal(GetSoundSafe(sound), transform);

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="position">Only return true if the sound is played at this position</param>
        /// <returns><inheritdoc cref="IsSoundPlaying{T}(T, Transform)" path="/returns"/></returns>
        public static bool IsSoundPlaying<T>(T sound, Vector3 position) where T : Enum =>
            InternalInstance.IsSoundPlayingInternal(GetSoundSafe(sound), position);

        /// <summary>
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="transform">Optional: Only return true if the sound is playing from this transform</param>
        /// <returns><inheritdoc cref="IsSoundPlaying{T}(T, Transform)" path="/returns"/></returns>
        public static bool IsSoundPlaying(SoundFileObject sound, Transform transform = null) =>
            InternalInstance.IsSoundPlayingInternal(sound, transform);

        /// <summary>
        /// </summary>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="position">Only return true if the sound is played at this position</param>
        /// <returns><inheritdoc cref="IsSoundPlaying{T}(T, Transform)" path="/returns"/></returns>
        public static bool IsSoundPlaying(SoundFileObject sound, Vector3 position) =>
            InternalInstance.IsSoundPlayingInternal(sound, position);

        /// <summary>
        /// Very similar use case as TryGetComponent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <param name="helper">This helper reference will be given a value if the method returns true</param>
        /// <returns>The first Sound Helper that's currently playing the specified music</returns>
        public static bool TryGetPlayingSound<T>(T sound, out SoundChannelHelper helper) where T : Enum =>
            InternalInstance.TryGetPlayingSound(GetSoundSafe(sound), out helper);

        /// <summary>
        /// Very similar use case as TryGetComponent
        /// </summary>
        /// <param name="sound">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <param name="helper">This helper reference will be given a value if the method returns true</param>
        /// <returns>The first Sound Helper that's currently playing the specified music</returns>
        public static bool TryGetPlayingSound(SoundFileObject sound, out SoundChannelHelper helper) =>
            InternalInstance.TryGetPlayingSound(sound, out helper);

        #endregion

        #region PlayMusic
        /// <summary>
        /// Play Music globally without spatialization
        /// Supports built-in music transition operations
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="isMainMusic">If true, defines the music as the "Main Music", making future operations easier</param>
        /// <returns>The Music Channel helper playing the sound, useful for transitions, like copying the playback position to the next music</returns>
        public static MusicChannelHelper PlayMusic<T>(T music, bool isMainMusic) where T : Enum
        {
            return InternalInstance.PlayMusicInternal(GetMusicSafe(music), isMainMusic);
        }

        /// <summary>
        /// <inheritdoc cref="PlayMusic{T}(T, bool)"/>
        /// </summary>
        /// <param name="music"></param>
        /// <param name="isMainMusic">If true, defines the music as the "Main music", making future operations easier</param>
        /// <returns><inheritdoc cref="PlayMusic{T}(T, bool)" path="/returns"/></returns>
        public static MusicChannelHelper PlayMusic(MusicFileObject music, bool isMainMusic)
        {
            return InternalInstance.PlayMusicInternal(music, isMainMusic);
        }

        /// <summary>
        /// Plays the specified music using the settings provided in the Music File Object. 
        /// Supports spatialization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="transform">Optional: The transform of the music's source</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only play a single music at any time</para></param>
        /// <returns><inheritdoc cref="PlayMusic{T}(T, bool)" path="/returns"/></returns>
        public static MusicChannelHelper PlayMusic<T>(T music, Transform transform = null, MusicChannelHelper helper = null) where T : Enum
        {
            return InternalInstance.PlayMusicInternal(GetMusicSafe(music), transform, helper);
        }

        /// <summary>
        /// <inheritdoc cref="PlayMusic{T}(T, Transform, MusicChannelHelper)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="position">The world position you want the music to play from</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only play a single music at any time</para></param>
        /// <returns><inheritdoc cref="PlayMusic{T}(T, bool)" path="/returns"/></returns>
        public static MusicChannelHelper PlayMusic<T>(T music, Vector3 position, MusicChannelHelper helper = null) where T : Enum
        {
            return InternalInstance.PlayMusicInternal(GetMusicSafe(music), position, helper);
        }

        /// <summary>
        /// <inheritdoc cref="PlayMusic{T}(T, Transform, MusicChannelHelper)"/>
        /// </summary>
        /// <param name="music">A reference to the Music File asset to play directly</param>
        /// <param name="transform">Optional: The transform of the music's source</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only play a single music at any time</para></param>
        /// <returns><inheritdoc cref="PlayMusic{T}(T, bool)" path="/returns"/></returns>
        public static MusicChannelHelper PlayMusic(MusicFileObject music, Transform transform = null, MusicChannelHelper helper = null) => InternalInstance.PlayMusicInternal(music, transform, helper);

        /// <summary>
        /// <inheritdoc cref="PlayMusic{T}(T, Transform, MusicChannelHelper)"/>
        /// </summary>
        /// <param name="music">A reference to the Music File asset to play directly</param>
        /// <param name="position">The world position you want the music to play from</param>
        /// <param name="helper">Optional: The specific channel you want to play the sound from. 
        /// <para>Good if you want an entity to only play a single music at any time</para></param>
        /// <returns><inheritdoc cref="PlayMusic{T}(T, bool)" path="/returns"/></returns>
        public static MusicChannelHelper PlayMusic(MusicFileObject music, Vector3 position, MusicChannelHelper helper = null) => InternalInstance.PlayMusicInternal(music, position, helper);
        #endregion

        #region FadeMusic
        /// <summary>
        /// Play and fade in a new music
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="fadeInTime">Amount of time in seconds the fade will last</param>
        /// <param name="isMainmusic">If true, defines the music as the "Main music", making future operations easier</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMusicIn<T>(T music, float fadeInTime, bool isMainmusic = false) where T : Enum
        {
            return InternalInstance.FadeMusicInInternal(GetMusicSafe(music), fadeInTime, isMainmusic);
        }

        /// <summary>
        /// Play and fade in a new music
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="fadeInTime">Amount of time in seconds the fade will last</param>
        /// <param name="isMainmusic">If true, defines the music as the "Main music", making future operations easier</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMusicIn(MusicFileObject music, float fadeInTime, bool isMainmusic = false)
        {
            return InternalInstance.FadeMusicInInternal(music, fadeInTime, isMainmusic);
        }

        /// <summary>
        /// Fades out the currently designated "Main Music"
        /// </summary>
        /// <param name="fadeOutTime">Amount of time in seconds the fade will last</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMainMusicOut(float fadeOutTime)
        {
            return InternalInstance.FadeMainMusicOutInternal(fadeOutTime);
        }

        /// <summary>
        /// Fades music out
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music"></param>
        /// <param name="fadeOutTime">Amount of time in seconds the fade will last</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMusicOut<T>(T music, float fadeOutTime) where T : Enum
        {
            return InternalInstance.FadeMusicOutInternal(GetMusicSafe(music), fadeOutTime);
        }

        /// <summary>
        /// Fades music out provided a Music Channel Helper, 
        /// don't use this unless you understand what you're doing
        /// </summary>
        /// <param name="helper">Music Channel Helper to fade out</param>
        /// <param name="fadeOutTime">Amount of time in seconds the fade will last</param>
        /// <returns></returns>
        public MusicChannelHelper FadeMusicOut(MusicChannelHelper helper, float fadeOutTime)
        {
            return InternalInstance.FadeMusicOutInternal(helper, fadeOutTime);
        }
        #endregion

        #region IsMusicPlaying
        /// <summary>
        /// </summary>
        /// <param name="music">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <returns>True if music that was played through PlayMusic is currently playing</returns>
        public static bool IsMusicPlaying<T>(T music) where T : Enum =>
            InternalInstance.IsMusicPlayingInternal(GetMusicSafe(music));

        /// <summary>
        /// </summary>
        /// <param name="music">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <returns></returns>
        public static bool IsMusicPlaying(MusicFileObject music) => InternalInstance.IsMusicPlayingInternal(music);

        /// <summary>
        /// Very similar use case as TryGetComponent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <param name="helper">This helper reference will be given a value if the method returns true</param>
        /// <returns>The first Music Helper that's currently playing the specified music</returns>
        public static bool TryGetPlayingMusic<T>(T music, out MusicChannelHelper helper) where T : Enum =>
            InternalInstance.TryGetPlayingMusic(GetMusicSafe(music), out helper);

        /// <summary>
        /// Very similar use case as TryGetComponent
        /// </summary>
        /// <param name="music">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <param name="helper">This helper reference will be given a value if the method returns true</param>
        /// <returns>The first Music Helper that's currently playing the specified music</returns>
        public static bool TryGetPlayingMusic(MusicFileObject music, out MusicChannelHelper helper) =>
            InternalInstance.TryGetPlayingMusic(music, out helper);
        #endregion

        #region StopMusic
        /// <summary>
        /// Instantly stops the playback of the specified playing music music
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="transform">Optional: The transform of the music's source</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns>The Music Channel helper playing the sound, useful for transitions, like copying the playback position to the next music</returns>
        public static MusicChannelHelper StopMusic<T>(T music, Transform transform = null, bool stopInstantly = true) where T : Enum
        {
            return InternalInstance.StopMusicInternal(GetMusicSafe(music), transform, stopInstantly);
        }

        /// <summary>
        /// <inheritdoc cref="StopMusic(MusicFileObject, Transform){T}(T, Transform, JSAMMusicChannelHelper)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="position">The world position the music is playing from</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns><inheritdoc cref="StopMusic{T}(T, Transform, bool)"/></returns>
        public static MusicChannelHelper StopMusic<T>(T music, Vector3 position, bool stopInstantly = true) where T : Enum
        {
            return InternalInstance.StopMusicInternal(GetMusicSafe(music), position, stopInstantly);
        }

        /// <summary>
        /// <inheritdoc cref="StopMusic(MusicFileObject, Transform){T}(T, Transform, JSAMMusicChannelHelper)"/>
        /// </summary>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="transform">Optional: The transform of the music's source</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns><inheritdoc cref="StopMusic{T}(T, Transform, bool)"/></returns>
        public static MusicChannelHelper StopMusic(MusicFileObject music, Transform transform = null, bool stopInstantly = true)
        {
            return InternalInstance.StopMusicInternal(music, transform, stopInstantly);
        }

        /// <summary>
        /// <inheritdoc cref="StopMusic(MusicFileObject, Transform){T}(T, Transform, JSAMMusicChannelHelper)"/>
        /// </summary>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="position">The world position the music is playing from</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns><inheritdoc cref="StopMusic{T}(T, Transform, bool)"/></returns>
        public static MusicChannelHelper StopMusic(MusicFileObject music, Vector3 position, bool stopInstantly = true)
        {
            return InternalInstance.StopMusicInternal(music, position, stopInstantly);
        }

        /// <summary>
        /// A shorthand for wrapping StopMusic in an IsMusicPlaying if-statement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="transform">Optional: The transform of the music's source</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns>True if music was stopped successfully, false if music wasn't playing</returns>
        public static bool StopMusicIfPlaying<T>(T music, Transform transform = null, bool stopInstantly = true) where T : Enum
        {
            return InternalInstance.StopMusicIfPlayingInternal(GetMusicSafe(music), transform, stopInstantly);
        }

        /// <summary>
        /// <inheritdoc cref="StopMusicIfPlaying{T}(T, Transform, bool)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="position">The world position the music is playing from</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns><inheritdoc cref="StopMusicIfPlaying{T}(T, Transform, bool)"/></returns>
        public static bool StopMusicIfPlaying<T>(T music, Vector3 position, bool stopInstantly = true) where T : Enum =>
            InternalInstance.StopMusicIfPlayingInternal(GetMusicSafe(music), position, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopMusicIfPlaying{T}(T, Transform, bool)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="transform">Optional: The transform of the music's source</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns><inheritdoc cref="StopMusicIfPlaying{T}(T, Transform, bool)"/></returns>
        public static bool StopMusicIfPlaying(MusicFileObject music, Transform transform = null, bool stopInstantly = true) =>
            InternalInstance.StopMusicIfPlayingInternal(music, transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopMusicIfPlaying{T}(T, Transform, bool)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">The enum corresponding to the music music</param>
        /// <param name="position">The world position the music is playing from</param>
        /// <param name="stopInstantly">Optional: If false, will allow music to transition out using it's transition settings. 
        /// Otherwise, will immediately end playback</param>
        /// <returns><inheritdoc cref="StopMusicIfPlaying{T}(T, Transform, bool)"/></returns>
        public static bool StopMusicIfPlaying(MusicFileObject music, Vector3 position, bool stopInstantly = true) =>
            InternalInstance.StopMusicIfPlayingInternal(music, position, stopInstantly);
        #endregion

        #region Volume
        /// <summary>
        /// Get the current overall volume as a normalized float from 0 to 1
        /// </summary>
        public static float MasterVolume { get { return InternalInstance.MasterVolume; } }
        public static bool MasterMuted { get { return InternalInstance.MasterMuted; } 
            set 
            { 
                InternalInstance.MasterMuted = value;
                OnMasterVolumeChanged?.Invoke(InternalInstance.MasterVolume); 
                OnMusicVolumeChanged?.Invoke(InternalInstance.MusicVolume);
                OnSoundVolumeChanged?.Invoke(InternalInstance.SoundVolume); 
            }
        }
        /// <summary>
        /// Get the current volume of Music as a normalized float from 0 to 1
        /// </summary>
        public static float MusicVolume { get { return InternalInstance.MusicVolume; } }
        public static bool MusicMuted { get { return InternalInstance.MusicMuted; }
            set 
            { 
                InternalInstance.MusicMuted = value;
                OnMusicVolumeChanged?.Invoke(InternalInstance.MusicVolume);
            }
        }
        /// <summary>
        /// Get the current volume of Sounds as a normalized float from 0 to 1
        /// </summary>
        public static float SoundVolume { get { return InternalInstance.SoundVolume; } }
        public static bool SoundMuted { get { return InternalInstance.SoundMuted; } 
            set 
            { 
                InternalInstance.SoundMuted = value; 
                OnSoundVolumeChanged?.Invoke(InternalInstance.SoundVolume);
            }
        }

        public static void SetMasterVolume(float newVolume)
        {
            InternalInstance.MasterVolume = newVolume;
            OnMasterVolumeChanged?.Invoke(newVolume);
            OnMusicVolumeChanged?.Invoke(InternalInstance.MusicVolume);
            OnSoundVolumeChanged?.Invoke(InternalInstance.SoundVolume);
        }

        public static void SetMusicVolume(float newVolume)
        {
            InternalInstance.MusicVolume = newVolume;
            OnMusicVolumeChanged?.Invoke(newVolume);
        }

        public static void SetSoundVolume(float newVolume)
        {
            InternalInstance.SoundVolume = newVolume;
            OnSoundVolumeChanged?.Invoke(newVolume);
        }
        #endregion

        /// <summary>
        /// TODO: Make this more stable
        /// Ensures that the AudioManager you think you're referring to actually exists in this scene
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public void EstablishSingletonDominance()
        {
            if (Instance != this && Instance != null)
            {
                // A unique case where the Singleton exists but not in this scene
                if (Instance.gameObject.scene.name != gameObject.scene.name)
                {
                    if (Instance.gameObject.scene.name == "DontDestroyOnLoad" || gameObject.scene == null) // Previous is still here and active
                    {
                        enabled = false;
                    }
                    else
                    {
                        instance = this;
                    }
                }
                else if (!Instance.gameObject.activeInHierarchy)
                {
                    instance = this;
                }
                else if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) instance = null;
        }

        /// <summary>
        /// Called internally by AudioManager to output non-error console messages
        /// </summary>
        /// <param name="consoleOutput"></param>
        public static void DebugLog(string consoleOutput)
        {
            if (JSAMSettings.Settings)
            {
                if (JSAMSettings.Settings.DisableConsoleLogs) return;
            }
            Debug.Log("JSAM: " + consoleOutput);
        }

        public static void DebugWarning(string consoleOutput)
        {
            Debug.LogWarning("JSAM Warning: " +
                consoleOutput);
        }

        public static void DebugError(string consoleOutput)
        {
            Debug.LogError("JSAM ERROR: " +
                consoleOutput);
        }

#if UNITY_EDITOR

        /// <summary>
        /// A MonoBehaviour function called when the script is loaded or a value is changed in the inspector (Called in the editor only).
        /// </summary>
        private void OnValidate()
        {
            EstablishSingletonDominance();
            if (listener == null) FindNewListener();
            //ValidateSourcePrefab();

            if (!doneLoading) return;
        }
#endif
    }
}