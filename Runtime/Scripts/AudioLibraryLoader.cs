using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class AudioLibraryLoader : MonoBehaviour
    {
        [SerializeField] AudioLibrary library;

        public enum LoadBehaviour
        {
            OnStartAndDestroy,
            OnEnableAndDisable
        }

        public LoadBehaviour loadTiming = LoadBehaviour.OnStartAndDestroy;

        private void Start()
        {
            if (loadTiming == LoadBehaviour.OnStartAndDestroy)
            {
                AudioManagerInternal.Instance.LoadAudioLibrary(library);
            }
        }

        private void OnDestroy()
        {
            if (loadTiming == LoadBehaviour.OnStartAndDestroy)
            {
                AudioManagerInternal.Instance.UnloadAudioLibrary(library);
            }
        }
    }
}