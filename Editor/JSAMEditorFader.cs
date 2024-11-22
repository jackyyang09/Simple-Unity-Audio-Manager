using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JSAM.JSAMEditor
{
    public class JSAMEditorFader : System.IDisposable
    {
        BaseAudioFileObject asset;
        public AudioSource AudioSource;

        AudioClip PlayingClip
        {
            get
            {
                return playingClip;
            }
            set
            {
                playingClip = value;
            }
        }
        AudioClip playingClip;

        public JSAMEditorFader(BaseAudioFileObject _asset)
        {
            EditorApplication.update += Update;
            asset = _asset;
        }

        public void Dispose()
        {
            EditorApplication.update -= Update;
        }

        /// <summary>
        /// Can't use co-routines, so this is the alternative
        /// </summary>
        /// <param name="asset"></param>
        void Update()
        {
            if (!AudioSource) return;

            if (!asset.fadeInOut)
            {
                AudioSource.volume = asset.relativeVolume;
                return;
            }

            if (AudioSource.isPlaying)
            {
                var fadeInTime = asset.fadeInDuration * PlayingClip.length;
                var fadeOutTime = asset.fadeOutDuration * PlayingClip.length;

                if (AudioSource.time < PlayingClip.length - fadeOutTime)
                {
                    if (fadeInTime > 0)
                    {
                        AudioSource.volume = Mathf.Lerp(0, asset.relativeVolume, AudioSource.time / fadeInTime);
                    }
                }
                else
                {
                    if (fadeOutTime > 0)
                    {
                        AudioSource.volume = Mathf.Lerp(0, asset.relativeVolume, (playingClip.length - AudioSource.time) / fadeOutTime);
                    }
                }
            }
        }

        public void StartFading(AudioClip audioClip, BaseAudioFileObject newAsset)
        {
            PlayingClip = audioClip;
            if (newAsset != null) asset = newAsset;
        }
    }
}
