using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

namespace JSAM
{
    public class JSAMSettings : ScriptableObject
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

        [Tooltip("Number of Music Channels to be created on start")]
        [SerializeField] int startingMusicChannels = 3;
        public int StartingMusicChannels { get { return startingMusicChannels; } }

        [Tooltip("If the maxDistance property of an Audio File Object is left at 0, then this value will be used as a substitute.")]
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

        [Tooltip("The AudioManager will instantiate this prefab during runtime to play sounds from. If null, will use default AudioSource settings.")]
        [SerializeField] SoundChannelHelper soundChannelPrefabOverride;
        public GameObject SoundChannelPrefab => soundChannelPrefabOverride == null ? null : soundChannelPrefabOverride.gameObject;
        [Tooltip("The AudioManager will instantiate this prefab during runtime to play music from. If null, will use default AudioSource settings.")]
        [SerializeField] MusicChannelHelper musicChannelPrefabOverride;
        public GameObject MusicChannelPrefab => musicChannelPrefabOverride == null ? null : musicChannelPrefabOverride.gameObject;

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
            /// Less performance overhead, but will clutter your object hierarchies during runtime
            /// </summary>
            Parented
        }

        [Tooltip("Default - Audio Channels track their targets in World Space every update.\n\n" +
            "FixedUpdate - Audio channels track their targets in FixedUpdate. Good for targets that move during FixedUpdate.\n\n" +
            "LateUpdate - Same as FixedUpdate but in LateUpdate instead.\n\n" +
            "Parented - Audio channels are parented in the hierarchy to their targets. " +
            "Slightly less performance overhead, but will clutter your object hierarchies during runtime.")]
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

        [Tooltip("The font size used when rendering \"quick reference guides\" in JSAM editor windows")]
        [SerializeField] int quickReferenceFontSize = 10;
        public int QuickReferenceFontSize
        {
            get
            {
                return quickReferenceFontSize;
            }
        }

        static JSAMSettings settings;
        public static JSAMSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    var asset = Resources.Load(nameof(JSAMSettings));
                    settings = asset as JSAMSettings;
#if UNITY_EDITOR
                    if (settings == null) TryCreateNewSettingsAsset();
#endif
                }
                return settings;
            }
        }

#if UNITY_EDITOR
        static readonly string SETTINGS_PATH = "Assets/Settings/Resources/" + nameof(JSAMSettings) + ".asset";

        public static void TryCreateNewSettingsAsset()
        {
            if (!EditorUtility.DisplayDialog(
                "JSAM First Time Setup",
                "In order to function, JSAM needs a place to store settings. By default, a " +
                "Settings asset will be created at Assets/Settings/Resources/, but you may move it " +
                "elsewhere, so long as it's in a Resources folder.\n " +
                "Moving it out of the Resources folder will prompt this message to appear again erroneously!",
                "Ok Create It.", "Not Yet!")) return;

            var asset = CreateInstance<JSAMSettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Resources")) AssetDatabase.CreateFolder("Assets/Settings", "Resources");
            AssetDatabase.CreateAsset(asset, SETTINGS_PATH);
            asset.Reset();

            settings = asset;
        }

        static SerializedObject serializedObject;
        public static SerializedObject SerializedObject
        {
            get
            {
                if (serializedObject == null)
                {
                    serializedObject = new SerializedObject(Settings);
                    return serializedObject;
                }
                return serializedObject;
            }
        }
#endif

        public void Reset()
        {
            //packagePath = JSAMEditorHelper.GetAudioManagerPath;
            //packagePath = packagePath.Remove(packagePath.IndexOf("/Scripts/AudioManager.cs"));
            //presetsPath = packagePath + "/Presets";
        }
    }
}