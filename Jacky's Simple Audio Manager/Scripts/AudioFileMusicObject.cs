using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [CreateAssetMenu(fileName = "New Audio Music File", menuName = "AudioManager/New Audio File Music Object", order = 1)]
    public class AudioFileMusicObject : AudioFileObject
    {
        public enum LoopPointTool
        {
            Slider,
            TimeInput,
            TimeSamplesInput,
            BPMInput//WithBeats,
                    //BPMInputWithBars
        }

        [Tooltip("Enable this option if you want your music to loop between specific parts rather than from the very beginning to very end")]
        public bool useLoopPoints = false;

        /// <summary>
        /// If true, music will always start and end between loop points
        /// </summary>
        [HideInInspector]
        [Tooltip("If true, music will always start and end between loop points")]
        public bool clampBetweenLoopPoints = false;

        [Tooltip("Standard looping disregards all loop point logic, loop point use is enabled in the audio music file")]
        [SerializeField]
        LoopMode loopMode = LoopMode.Looping;

        /// <summary>
        /// Starting loop point, stored as time for accuracy sake, converted to samples in backend
        /// </summary>
        [HideInInspector] public float loopStart;
        /// <summary>
        /// Ending loop point, stored as time for accuracy sake, converted to samples in backend
        /// </summary>
        [HideInInspector] public float loopEnd;

        [HideInInspector] public LoopPointTool loopPointInputMode;

        [HideInInspector] public int bpm = 120;
    }
}