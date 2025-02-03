using UnityEngine;
using UnityEngine.Audio;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Volume Track", menuName = "AudioManager/Volume Track")]
    public partial class VolumeTrack : ScriptableObject
    {
        /// <summary>
        /// Audio File Objects that play through this Track will use this Mixer Group unless overriden
        /// </summary>
        [Tooltip("Audio File Objects that play through this Track will use this Mixer Group unless overriden")]
        public AudioMixerGroup DefaultMixerGroup;
        /// <summary>
        /// When no existing preference is found, will set the Audio Track to this value
        /// </summary>
        [Tooltip("When no existing preference is found, will set the Audio Track to this value")]
        public float DefaultVolume = 1;
        /// <summary>
        /// If left empty, will use the name of this asset + VOL
        /// </summary>
        [Tooltip("If left empty, will use the name of this asset + VOL")]
        public string PlayerPrefsVolumeKey;
        public string VolumeKey
        {
            get
            {
                if (string.IsNullOrEmpty(PlayerPrefsVolumeKey))
                {
                    return name + "VOL";
                }
                return PlayerPrefsVolumeKey;
            }
        }

        /// <summary>
        /// If left empty, will use the name of this asset + MUTE
        /// </summary>
        [Tooltip("If left empty, will use the name of this asset + MUTE")]
        public string PlayerPrefsMutedKey;
        public string MutedKey
        {
            get
            {
                if (string.IsNullOrEmpty(PlayerPrefsMutedKey))
                {
                    return name + "MUTE";
                }
                return PlayerPrefsMutedKey;
            }
        }
    }

    public static class VolumeTrackExtensions
    {
        public static void SetVolume(this VolumeTrack track, float volume)
        {
            AudioManager.SetVolume(track, volume);
        }

        public static void SetMuted(this VolumeTrack track, bool muted)
        {
            AudioManager.SetMute(track, muted);
        }

        public static bool IsMuted(this VolumeTrack track)
        {
            return AudioManager.IsMuted(track);
        }
    }
} 