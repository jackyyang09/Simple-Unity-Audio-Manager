using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Audio File", menuName = "AudioManager/New Audio File Object", order = 1)]
    public class AudioFileObject : ScriptableObject
    {
        [Header("Attach audio file here to use")]
        [SerializeField]
        public AudioClip file;

        [Header("Attach audio files here to use")]
        [SerializeField]
        public List<AudioClip> files = new List<AudioClip>();

        [HideInInspector]
        public bool useLibrary;

        [SerializeField]
        [Tooltip("If true, playback will be affected based on distance and direction from listener")]
        public bool spatialize;

        [Tooltip("Will this sound loop or will it only play once when you play it?")]
        [SerializeField]
        public bool loopSound = false;

        [Tooltip("If there are several sounds playing at once, sounds with higher priority will be culled by Unity's sound system later than sounds with lower priority.")]
        [SerializeField]
        public Priority priority = Priority.Default;

        [Tooltip("How much random variance to the sound's frequency will be applied when this sound is played. Keep at Very Low for best results.")]
        [SerializeField]
        public Pitch pitchShift = Pitch.VeryLow;

        [Tooltip("Adds a delay in seconds before this sound is played")]
        [SerializeField]
        public float delay = 0;

        [Tooltip("If true, will ignore the \"Time Scaled Sounds\" parameter in AudioManager")]
        [SerializeField]
        public bool ignoreTimeScale = false;

        public AudioClip GetFile()
        {
            return file;
        }

        public List<AudioClip> GetFiles()
        {
            return files;
        }

        public bool IsLibraryEmpty()
        {
            foreach (AudioClip a in files)
            {
                if (a != null)
                {
                    return false;
                }
            }
            return true;
        }

        public bool HasAudioClip(AudioClip a)
        {
            return file == a || files.Contains(a);
        }

        public bool UsingLibrary()
        {
            return useLibrary;
        }
    }
}