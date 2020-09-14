using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public static class JSAMExtensions
    {
        public static T Clamp<T>(this T val, T min, T max) where T : System.IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        /// <summary>
        /// Extension method to check if a layer is in a layer mask
        /// With help from these lads https://answers.unity.com/questions/50279/check-if-layer-is-in-layermask.html
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static bool Contains(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static float InverseLerpUnclamped(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }
    }
}