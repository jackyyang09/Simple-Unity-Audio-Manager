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

        public static bool IsNullEmptyOrWhiteSpace(this string input)
        {
            return string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input);
        }

        public static bool TryForComponent<T>(this Component obj, out T comp) where T : Component
        {
#if UNITY_2019_4_OR_NEWER
            return obj.TryGetComponent(out comp);
#else
            comp = GetComponent<AudioChorusFilter>();
            return comp != null;
#endif
        }

        /// <summary>
        /// Helpful method by Stack Overflow user ata
        /// https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ConvertToAlphanumeric(this string input)
        {
            char[] arr = input.ToCharArray();

            arr = System.Array.FindAll<char>(arr, (c => (char.IsLetterOrDigit(c)
            || c == '_')));

            // If the first index is a number
            while (char.IsDigit(arr[0]))
            {
                List<char> newArray = new List<char>();
                newArray = new List<char>(arr);
                newArray.RemoveAt(0);
                arr = newArray.ToArray();
                if (arr.Length == 0) break; // No valid characters to use, returning empty
            }

            return new string(arr);
        }
    }
}