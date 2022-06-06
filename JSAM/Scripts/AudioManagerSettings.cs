using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New AudioManager Settings", menuName = "AudioManager/New AudioManager Settings Asset", order = 1)]
    public class AudioManagerSettings : ScriptableObject
    {
        [Tooltip("If true, enables 3D spatialized audio for all sound effects, does not effect music")]
        [SerializeField] bool spatialSound = true;
        public bool Spatialize { get { return spatialSound; } }

        /// <summary>
        /// Number of Sound Channels to be created on start
        /// </summary>
        [Tooltip("Number of Sound Channels to be created on start")]
        [SerializeField] int startingSoundChannels = 16;
        public int StartingSoundChannels { get { return startingSoundChannels; } }

        [SerializeField] int startingMusicChannels = 3;
        public int StartingMusicChannels { get { return startingMusicChannels; } }

        [SerializeField] float defaultSoundMaxDistance = 7;
        public float DefaultSoundMaxDistance { get { return defaultSoundMaxDistance; } }

        /// <summary>
        /// If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings
        /// </summary>
        [Tooltip("If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings")]
        [SerializeField] bool disableConsoleLogs = false;
        public bool DisableConsoleLogs { get { return disableConsoleLogs; } }

        /// <summary>
        /// If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced
        /// </summary>
        [Tooltip("If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced")]
        [SerializeField] bool dontDestroyOnLoad = true;
        public new bool DontDestroyOnLoad { get { return dontDestroyOnLoad; } }

        /// <summary>
        /// If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled
        /// </summary>
        [Tooltip("If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled")]
        [SerializeField] bool dynamicSourceAllocation = true;
        public bool DynamicSourceAllocation { get { return dynamicSourceAllocation; } }

        /// <summary>
        /// If true, stops all sounds when you load a scene
        /// </summary>
        [Tooltip("If true, stops all sounds when you load a scene")]
        [SerializeField] bool stopSoundsOnSceneLoad = false;
        public bool StopSoundsOnSceneLoad { get { return stopSoundsOnSceneLoad; } }

        [Tooltip("Use if spatialized sounds are spatializing late when playing in-editor, known to happen with the Oculus SDK")]
        [SerializeField] bool spatializeLateUpdate = false;
        public bool SpatializeOnLateUpdate { get { return spatializeLateUpdate; } }

        /// <summary>
        /// Specifies how your audio channels will follow their targets in 3D space during runtime. 
        /// Only applies if you have Spatial Sound enabled
        /// </summary>
        public enum SpatializeUpdateMode
        {
            /// <summary>
            /// Audio channels track their targets in world space every Update.
            /// </summary>
            Default,
            /// <summary>
            /// Audio channels track their targets in FixedUpdate. 
            /// Good for targets that move during FixedUpdate
            /// </summary>
            FixedUpdate,
            /// <summary>
            /// Audio channels track their targets in LateUpdate. 
            /// Good for targets that move during LateUpdate
            /// </summary>
            LateUpdate,
            /// <summary>
            /// Audio channels are parented in the hierarchy to their targets. 
            /// Less performance overhead, but will clutter your object hierarchies during playback
            /// </summary>
            Parented
        }

        [SerializeField] SpatializeUpdateMode spatializationMode;
        public SpatializeUpdateMode SpatializationMode { get { return spatializationMode; } }

        [Tooltip("Changes the pitch of sounds according to Time.timeScale. When Time.timeScale is set to 0, pauses all sounds instead")]
        [SerializeField] bool timeScaledSounds = true;
        public bool TimeScaledSounds { get { return timeScaledSounds; } }

        [SerializeField] AudioMixer mixer = null;
        public AudioMixer Mixer { get { return mixer; } }

        [SerializeField] AudioMixerGroup masterGroup = null;
        public AudioMixerGroup MasterGroup { get { return masterGroup; } }
        [SerializeField] string masterVolumeParam = "MasterVolume";
        public string MasterVolumePararm { get { return masterVolumeParam; } }

        [SerializeField] AudioMixerGroup musicGroup = null;
        public AudioMixerGroup MusicGroup { get { return musicGroup; } }
        [SerializeField] string musicVolumeParam = "MusicVolume";
        public string MusicVolumePararm { get { return musicVolumeParam; } }

        [SerializeField] AudioMixerGroup soundGroup = null;
        public AudioMixerGroup SoundGroup { get { return soundGroup; } }
        [SerializeField] string soundVolumeParam = "SoundVolume";
        public string SoundVolumePararm { get { return soundVolumeParam; } }

        [SerializeField] AudioMixerGroup voiceGroup = null;
        public AudioMixerGroup VoiceGroup { get { return voiceGroup; } }
        [SerializeField] string voiceVolumeParam = "VoiceVolume";
        public string VoiceVolumePararm { get { return voiceVolumeParam; } }

        [Tooltip("If true, will save volume settings into PlayerPrefs and automatically loads previous volume settings on play. ")]
        [SerializeField] bool saveVolumeToPlayerPrefs = true;
        public bool SaveVolumeToPlayerPrefs { get { return saveVolumeToPlayerPrefs; } }

        [SerializeField] string masterVolumeKey = "JSAM_MASTER_VOL";
        [SerializeField] string masterMutedKey = "JSAM_MASTER_MUTE";
        public string MasterVolumeKey { get { return masterVolumeKey; } }
        public string MasterMutedKey { get { return masterMutedKey; } }
        [SerializeField] string musicVolumeKey = "JSAM_MUSIC_VOL";
        [SerializeField] string musicMutedKey = "JSAM_MUSIC_MUTE";
        public string MusicVolumeKey { get { return musicVolumeKey; } }
        public string MusicMutedKey { get { return musicMutedKey; } }
        [SerializeField] string soundVolumeKey = "JSAM_SOUND_VOL";
        [SerializeField] string soundMutedKey = "JSAM_SOUND_MUTE";
        public string SoundVolumeKey { get { return soundVolumeKey; } }
        public string SoundMutedKey { get { return soundMutedKey; } }
    }
}