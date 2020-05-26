using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Audio Music File", menuName = "AudioManager/New Audio File Music Object", order = 1)]
    public class AudioFileMusicObject : AudioFileObject
    {
        /// <summary>
        /// If true, music will always start and end between loop points
        /// </summary>
        [HideInInspector]
        [Tooltip("If true, music will always start and end between loop points")]
        public bool clampToLoopPoints = false;

        [Tooltip("Standard looping disregards all loop point logic and will make the music loop from start to end, " + "\"Loop with Loop Points\" enables loop point use and makes the music start from the start point upon reaching the end")]
        [SerializeField]
        public LoopMode loopMode = LoopMode.Looping;

        /// <summary>
        /// Starting loop point, stored as time for accuracy sake, converted to samples in back-end
        /// </summary>
        [HideInInspector] public float loopStart;
        /// <summary>
        /// Ending loop point, stored as time for accuracy sake, converted to samples in back-end
        /// </summary>
        [HideInInspector] public float loopEnd;

        [HideInInspector] public int bpm = 120;

        AudioClip cachedFile;

#if UNITY_EDITOR

        string fileExtension = "";

        /// <summary>
        /// Returns true if this AudioFile houses a .WAV
        /// </summary>
        /// <returns></returns>
        public bool IsWavFile()
        {
            if (cachedFile != file)
            {
                string filePath = UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets(file.name)[0]);
                string trueFilePath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("/") + 1) + filePath;
                fileExtension = trueFilePath.Substring(trueFilePath.Length - 4);
                cachedFile = file;
            }
            return fileExtension == ".wav";
        }
#endif
    }
}