using UnityEngine;

namespace JSAM
{
    public static class JSAMCompatibility
    {
        public static T FindObjectOfType<T>() where T : Behaviour
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
    }
}