using System;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    /// <summary>
    /// AudioManager singleton that manages all audio in the game
    /// </summary>
    [DefaultExecutionOrder(1)]
    [DisallowMultipleComponent]
    public partial class AudioManager : MonoBehaviour
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
                    instance = JSAMCompatibility.FindObjectOfType<AudioManager>();
                    if (instance == null)
                    {
                        if (!isQuitting && Application.isPlaying)
                        {
                            DebugError("No AudioManager found in scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                        }
                    }
                }
                return instance;
            }
        }

        [Tooltip("The Audio Librarys that should be loaded on start")]
        [SerializeField] AudioLibrary[] preloadedLibraries = new AudioLibrary[0];
        /// <summary>
        /// The Audio Librarys that should be loaded on start
        /// </summary>
        public AudioLibrary[] PreloadedLibraries => preloadedLibraries;

        static AudioListener listener;
        public static AudioListener AudioListener
        {
            get
            {
                if (!listener)
                {
                    listener = JSAMCompatibility.FindObjectOfType<AudioListener>();
                }
                return listener;
            }
        }

        bool doneLoading;

        bool initialized = false;
        /// <summary>
        /// True if AudioManager finishes setting up
        /// </summary>
        public bool Initialized => initialized;

        public static MusicChannelHelper MainMusicHelper { get => InternalInstance.MainMusic; }
        public static MusicFileObject MainMusic { get { return MainMusicHelper.AudioFile; } }

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
        public static Action<SoundChannelHelper, SoundFileObject> OnSoundPlayed;
        public static Action<SoundChannelHelper, SoundFileObject> OnVoicePlayed;
        public static Action<MusicChannelHelper, MusicFileObject> OnMusicPlayed;

        /// <summary>
        /// <para>Invoked when the volume of a track is changed. Is also invoked when the volume of the Master Track is changed.</para>
        /// <para>You can subscribe to OnVolumeChanged events like so:</para>
        /// <para>public VolumeTrack m_TrackReference;</para>
        /// <para>OnVolumeChanged[m_TrackReference] += OnVolumeChanged;</para>
        /// <para>float channelVolume - The new volume level of this specific channel, useful for sliders exposed to the User</para>
        /// <para>float realVolume - The new, real volume level of Audio playing in this channel, should be applied to your AudioSource (and optionally multiplied by your AudioClip's specific volume</para>
        /// </summary>
        public static Dictionary<VolumeTrack, Action<float, float>> OnVolumeChanged;
        /// <summary>
        /// Invoked when the volume of any track is changed.
        /// <para>VolumeTrack track - The track whose volume was just changed</para>
        /// <para>float channelVolume - The new volume level of this specific channel, useful for sliders exposed to the User</para>
        /// <para>float realVolume - The new, real volume level of Audio playing in this channel, should be applied to your AudioSource (and optionally multiplied by your AudioClip's specific volume</para>
        /// </summary>
        public static Action<VolumeTrack, float, float> OnAnyVolumeChanged;
        #endregion

        [RuntimeInitializeOnLoadMethod]
        static void RunOnStart()
        {
            OnAudioManagerInitialized = null;
            OnSoundPlayed = null;
            OnVoicePlayed = null;
            OnMusicPlayed = null;

            OnVolumeChanged = new Dictionary<VolumeTrack, Action<float, float>>();

            foreach (var track in JSAMSettings.Settings.AllTracks)
            {
                OnVolumeChanged[track] = default;
            }
        }

        // Use this for initialization
        void Awake()
        {
            if (JSAMSettings.Settings.DontDestroyOnLoad)
            {
                gameObject.transform.SetParent(null, true); 
                DontDestroyOnLoad(gameObject);
            }

            if (!TryDesignateSingleton())
            {
                Destroy(gameObject);
                return;
            }

            if (!initialized)
            {
                doneLoading = true;
            }

            InternalInstance.Initialize();

            foreach (var library in preloadedLibraries)
            {
                InternalInstance.LoadAudioLibrary(library);
            }

            initialized = true;
        }

        static SoundFileObject SoundFileFromEnum<T>(T e) where T : Enum
        {
            return InternalInstance.AudioFileFromEnum(e) as SoundFileObject;
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
            return InternalInstance.PlaySoundInternal(SoundFileFromEnum(sound), transform, helper);
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
            return InternalInstance.PlaySoundInternal(SoundFileFromEnum(sound), position, helper);
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
        public static SoundChannelHelper StopSound<T>(T sound, Transform transform = null, bool stopInstantly = true) where T : Enum =>
            InternalInstance.StopSoundInternal(SoundFileFromEnum(sound), transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSound{T}(T, Transform)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The sound to be stopped</param>
        /// <param name="position">The sound's playback position in world space. 
        /// Passing this property will limit playback stopping 
        /// to only the sound playing at this specific position</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static SoundChannelHelper StopSound<T>(T sound, Vector3 position, bool stopInstantly = true) where T : Enum => 
            InternalInstance.StopSoundInternal(SoundFileFromEnum(sound), position, stopInstantly);

        /// <summary>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to play directly</param>
        /// <param name="transform">Optional: If the sound was initially passed a reference to 
        /// a transform in PlaySound, passing the same Transform reference will stop that specific playing instance</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static SoundChannelHelper StopSound(SoundFileObject sound, Transform transform = null, bool stopInstantly = true) =>
            InternalInstance.StopSoundInternal(sound, transform, stopInstantly);

        /// <summary>
        /// <inheritdoc cref="StopSound{T}(T, Transform)"/>
        /// </summary>
        /// <param name="sound">A reference to the Sound File asset to play directly</param>
        /// <param name="position">The sound's playback position in world space. 
        /// Passing this property will limit playback stopping 
        /// to only the sound playing at this specific position</param>
        /// <param name="stopInstantly">Optional: If true, stop the sound immediately, you may want to leave this false for looping sounds</param>
        public static SoundChannelHelper StopSound(SoundFileObject sound, Vector3 position, bool stopInstantly = true) => 
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
            InternalInstance.StopSoundIfPlayingInternal(SoundFileFromEnum(sound), transform, stopInstantly);

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
            InternalInstance.StopSoundIfPlayingInternal(SoundFileFromEnum(sound), position, stopInstantly);

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
            InternalInstance.IsSoundPlayingInternal(SoundFileFromEnum(sound), transform);

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sound">The enum value for the sound in question. Check AudioManager to see what Enum you should use</param>
        /// <param name="position">Only return true if the sound is played at this position</param>
        /// <returns><inheritdoc cref="IsSoundPlaying{T}(T, Transform)" path="/returns"/></returns>
        public static bool IsSoundPlaying<T>(T sound, Vector3 position) where T : Enum =>
            InternalInstance.IsSoundPlayingInternal(SoundFileFromEnum(sound), position);

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
            InternalInstance.TryGetPlayingSound(SoundFileFromEnum(sound), out helper);

        /// <summary>
        /// Very similar use case as TryGetComponent
        /// </summary>
        /// <param name="sound">The enum of the music in question, check AudioManager to see what enums you can use</param>
        /// <param name="helper">This helper reference will be given a value if the method returns true</param>
        /// <returns>The first Sound Helper that's currently playing the specified music</returns>
        public static bool TryGetPlayingSound(SoundFileObject sound, out SoundChannelHelper helper) =>
            InternalInstance.TryGetPlayingSound(sound, out helper);

        #endregion

        static MusicFileObject MusicFileFromEnum<T>(T e) where T : Enum
        {
            return InternalInstance.AudioFileFromEnum(e) as MusicFileObject;
        }

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
            return InternalInstance.PlayMusicInternal(MusicFileFromEnum(music), isMainMusic);
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
            return InternalInstance.PlayMusicInternal(MusicFileFromEnum(music), transform, helper);
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
            return InternalInstance.PlayMusicInternal(MusicFileFromEnum(music), position, helper);
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
        /// Plays and Fades-In a new Music File.
        /// <para>Refrain from fading Music Files with "Fade In Out" enabled as Fade behaviour will conflict</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="fadeInTime">Amount of time in seconds the fade will last</param>
        /// <param name="isMainmusic">If true, defines the music as the "Main music"</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMusicIn<T>(T music, float fadeInTime, bool isMainmusic = false) where T : Enum
        {
            return InternalInstance.FadeMusicInInternal(MusicFileFromEnum(music), fadeInTime, isMainmusic);
        }

        /// <summary>
        /// <inheritdoc cref="FadeMusicIn{T}(T, float, bool)"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music">Enum value for the music to be played. You can find this in the AudioLibrary</param>
        /// <param name="fadeInTime">Amount of time in seconds the fade will last</param>
        /// <param name="isMainmusic">If true, defines the music as the "Main music"</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMusicIn(MusicFileObject music, float fadeInTime, bool isMainmusic = false)
        {
            return InternalInstance.FadeMusicInInternal(music, fadeInTime, isMainmusic);
        }

        /// <summary>
        /// Fades out the currently designated "Main Music". Main Music is moved off the position of "Main"
        /// to make way for new music. 
        /// <para>Refrain from fading Music Files with "Fade In Out" enabled as Fade behaviour will conflict</para>
        /// </summary>
        /// <param name="fadeOutTime">Amount of time in seconds the fade will last</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMainMusicOut(float fadeOutTime)
        {
            return InternalInstance.FadeMainMusicOutInternal(fadeOutTime);
        }

        /// <summary>
        /// Fades-out Music if it is currently playing
        /// <para>Refrain from fading Music Files with "Fade In Out" enabled as Fade behaviour will conflict</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="music"></param>
        /// <param name="fadeOutTime">Amount of time in seconds the fade will last</param>
        /// <returns></returns>
        public static MusicChannelHelper FadeMusicOut<T>(T music, float fadeOutTime) where T : Enum
        {
            return InternalInstance.FadeMusicOutInternal(MusicFileFromEnum(music), fadeOutTime);
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
            InternalInstance.IsMusicPlayingInternal(MusicFileFromEnum(music));

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
            InternalInstance.TryGetPlayingMusic(MusicFileFromEnum(music), out helper);

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
        /// Stops all playing music maintained by AudioManager
        /// </summary>
        public static void StopAllMusic(bool stopInstantly = true) =>
            InternalInstance.StopAllMusicInternal(stopInstantly);

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
            return InternalInstance.StopMusicInternal(MusicFileFromEnum(music), transform, stopInstantly);
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
            return InternalInstance.StopMusicInternal(MusicFileFromEnum(music), position, stopInstantly);
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
            return InternalInstance.StopMusicIfPlayingInternal(MusicFileFromEnum(music), transform, stopInstantly);
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
            InternalInstance.StopMusicIfPlayingInternal(MusicFileFromEnum(music), position, stopInstantly);

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
        /// Returns the volume of the Master Track
        /// </summary>
        /// <returns></returns>
        public static float GetVolume() => GetVolume(JSAMSettings.Settings.MasterTrack);
        /// <summary>
        /// Returns the volume of the provided Track
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public static float GetVolume(VolumeTrack track)
        {
            var data = InternalInstance.GetVolumeData(track);
            return Convert.ToInt16(!data.Muted) * data.Volume;
        }
        /// <summary>
        /// Returns the real volume of the provided track if it was to be applied to an arbitrary AudioSource
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public static float GetModifiedVolume(VolumeTrack track) => GetVolume() * GetVolume(track);

        /// <summary>
        /// Sets the volume of the "Master" track. 
        /// Basic way of controlling the game volume 
        /// </summary>
        /// <param name="volume">The game volume from 0 to 1</param>
        public static void SetVolume(float volume)
        {
            SetVolume(JSAMSettings.Settings.MasterTrack, volume);
        }

        /// <summary>
        /// Changes the volume of a given VolumeTrack
        /// </summary>
        /// <param name="track"></param>
        /// <param name="volume"></param>
        public static void SetVolume(VolumeTrack track, float volume)
        {
            volume = Mathf.Clamp01(volume);

            var data = internalInstance.GetVolumeData(track);

            if (data.Volume == volume) return;

            data.Volume = volume;
            internalInstance.SetVolumeData(track, data);

            InvokeVolumeEvents(track, data);
        }

        /// <summary>
        /// Returns true if the Master Track is muted
        /// </summary>
        /// <returns></returns>
        public static bool IsMuted() => IsMuted(JSAMSettings.Settings.MasterTrack);
        /// <summary>
        /// Returns true if the provided VolumeTrack is muted
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public static bool IsMuted(VolumeTrack track) => InternalInstance.GetVolumeData(track).Muted;

        /// <summary>
        /// Sets the mute state of the "Master" track. 
        /// Basic method of setting the game audio's mute state
        /// </summary>
        /// <param name="muted"></param>
        public static void SetMute(bool muted)
        {
            SetMute(JSAMSettings.Settings.MasterTrack, muted);
        }

        /// <summary>
        /// Sets the mute state of a given VolumeTrack. 
        /// </summary>
        /// <param name="track"></param>
        /// <param name="muted"></param>
        public static void SetMute(VolumeTrack track, bool muted)
        {
            var data = internalInstance.GetVolumeData(track);

            if (data.Muted == muted) return;

            data.Muted = muted;
            internalInstance.SetVolumeData(track, data);

            InvokeVolumeEvents(track, data);
        }

        static void InvokeVolumeEvents(VolumeTrack track, AudioManagerInternal.VolumeData trackData)
        {
            var realVolume = GetModifiedVolume(track);
            OnVolumeChanged[track]?.Invoke(trackData.Volume, realVolume);
            OnAnyVolumeChanged?.Invoke(track, trackData.Volume, trackData.Volume);

            if (track != JSAMSettings.Settings.MasterTrack) return;

            var masterTrack = JSAMSettings.Settings.MasterTrack;
            var masterData = internalInstance.GetVolumeData(masterTrack);
            foreach (var t in JSAMSettings.Settings.Tracks)
            {
                if (t == track) continue;
                var data = internalInstance.GetVolumeData(t);
                realVolume = GetModifiedVolume(t);
                OnVolumeChanged[t]?.Invoke(data.Volume, realVolume);
                OnAnyVolumeChanged?.Invoke(t, data.Volume, realVolume);
            }
        }
        #endregion

        /// <summary>
        /// Ensures that the AudioManager you think you're referring to actually exists in this scene
        /// </summary>
        public bool TryDesignateSingleton()
        {
            if (!JSAMSettings.Settings.EstablishSingletonDominance)
            {
                return true;
            }
            
            if (Instance != this && Instance != null)
            {
                // A unique case where the Singleton exists but not in this scene
                if (Instance.gameObject.scene.name != gameObject.scene.name)
                {
                    if (Instance.gameObject.scene.name == "DontDestroyOnLoad" || gameObject.scene == null) // Previous is still here and active
                    {
                        return false;
                    }
                }
                else if (!Instance.gameObject.activeInHierarchy)
                {
                    return true;
                }
                return false;
            }

            return true;
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
        private void OnValidate()
        {
            // Don't go any further if you're in a prefab
            UnityEditor.SceneManagement.PrefabStage currentStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (currentStage != null)
            {
                return;
            }

            //TryDesignateSingleton();
            //ValidateSourcePrefab();

            if (!doneLoading) return;
        }
#endif
    }
}