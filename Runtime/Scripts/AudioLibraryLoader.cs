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

        void Load() => AudioManagerInternal.Instance.LoadAudioLibrary(library);
        void Unload() => AudioManagerInternal.Instance.UnloadAudioLibrary(library);

        private void OnEnable()
        {
            if (loadTiming == LoadBehaviour.OnEnableAndDisable)
            {
                Load();
            }
        }

        private void OnDisable()
        {
            if (loadTiming == LoadBehaviour.OnEnableAndDisable)
            {
                Unload();
            }
        }

        private void Start()
        {
            if (loadTiming == LoadBehaviour.OnStartAndDestroy)
            {
                Load();
            }
        }

        private void OnDestroy()
        {
            if (loadTiming == LoadBehaviour.OnStartAndDestroy)
            {
                Unload();
            }
        }
    }
}