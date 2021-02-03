using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New AudioManager Settings", menuName = "AudioManager/New AudioManager Settings Asset", order = 1)]
    public class AudioManagerSettings : ScriptableObject
    {
        [Tooltip("If true, enables 3D spatialized audio for all sound effects, does not effect music")]
        [SerializeField] bool spatialSound = true;
        public bool Spatialize
        {
            get
            {
                return spatialSound;
            }
        }

        /// <summary>
        /// Number of Audio Sources to be created on start
        /// </summary>
        [Tooltip("Number of Audio Sources to be created on start")]
        [SerializeField] int startingAudioSources = 16;
        public int StartingAudioSources
        {
            get
            {
                return startingAudioSources;
            }
        }

        /// <summary>
        /// If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings
        /// </summary>
        [Tooltip("If true, AudioManager no longer prints info to the console. Does not affect AudioManager errors/warnings")]
        [SerializeField] bool disableConsoleLogs = false;
        public bool DisableConsoleLogs
        {
            get
            {
                return disableConsoleLogs;
            }
        }

        /// <summary>
        /// If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced
        /// </summary>
        [Tooltip("If true, keeps AudioManager alive through scene loads. You're recommended to disable this if your AudioManager is instanced")]
        [SerializeField] bool dontDestroyOnLoad = true;
        public new bool DontDestroyOnLoad
        {
            get
            {
                return dontDestroyOnLoad;
            }
        }

        /// <summary>
        /// If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled
        /// </summary>
        [Tooltip("If true, adds more Audio Sources automatically if you exceed the starting count, you are recommended to keep this enabled")]
        [SerializeField] bool dynamicSourceAllocation = true;
        public bool DynamicSourceAllocation
        {
            get
            {
                return dynamicSourceAllocation;
            }
        }

        /// <summary>
        /// If true, stops all sounds when you load a scene
        /// </summary>
        [Tooltip("If true, stops all sounds when you load a scene")]
        [SerializeField] bool stopSoundsOnSceneLoad = false;
        public bool StopSoundsOnSceneLoad
        {
            get
            {
                return stopSoundsOnSceneLoad;
            }
        }

        [Tooltip("Use if spatialized sounds are spatializing late when playing in-editor, known to happen with the Oculus SDK")]
        [SerializeField] bool spatializeLateUpdate = false;
        public bool SpatializeOnLateUpdate
        {
            get
            {
                return spatializeLateUpdate;
            }
        }

        [Tooltip("Changes the pitch of sounds according to Time.timeScale. When Time.timeScale is set to 0, pauses all sounds instead")]
        [SerializeField] bool timeScaledSounds = true;
        public bool TimeScaledSounds
        {
            get
            {
                return timeScaledSounds;
            }
        }
    }
}