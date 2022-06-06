using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public abstract class BaseAudioFeedback : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        protected JSAMSoundFileObject sound;
    }
}