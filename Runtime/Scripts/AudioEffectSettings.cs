using UnityEngine;

namespace JSAM
{
    [System.Serializable]
    public class SpatialSoundSettings
    {
        /// <summary>
        /// Clamped between 0 and 5
        /// </summary>
        public float DopplerLevel = 1;
        public float Spread;
        public AudioRolloffMode VolumeRolloff;
        public float MinDistance = 1;
        public float MaxDistance = 500;
        public AnimationCurve RolloffCustomCurve;
        public AnimationCurve PanLevelCustomCurve;
        public AnimationCurve SpreadCustomCurve;
        public AnimationCurve ReverbZoneMixCustomCurve;
    }
}