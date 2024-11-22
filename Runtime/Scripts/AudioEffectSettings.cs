using UnityEngine;

namespace JSAM
{
    [System.Serializable]
    public class SpatialSoundSettings
    {
        /// <summary>
        /// Clamped between 0 and 5
        /// </summary>
        public float DopplerLevel;
        public AnimationCurve Spread;
        public AudioRolloffMode VolumeRolloff;
        public float MinDistance;
        public float MaxDistance;
    }
}