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

        public bool useLoopPoints = false;

        /// <summary>
        /// If true, music will always start and end between loop points
        /// </summary>
        public bool clampBetweenLoopPoints = false;

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