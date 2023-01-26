using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioFeedback<T> : MonoBehaviour where T : BaseAudioFileObject
    {
        [SerializeField]
        [HideInInspector]
        new protected T audio;

        [SerializeField]
        protected bool advancedMode;
    }
}