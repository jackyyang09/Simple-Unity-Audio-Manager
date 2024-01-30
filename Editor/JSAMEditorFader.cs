using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JSAM.JSAMEditor
{
    public class JSAMEditorFader : System.IDisposable
    {
        BaseAudioFileObject asset;

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
        float fadeInTime, fadeOutTime;

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
            if (!asset.fadeInOut)
            {
                AudioPlaybackToolEditor.helperSource.volume = asset.relativeVolume;
                return;
            }

            // Throws an error here when replacing a AudioClip and playing it in an independent window
            var helperSource = AudioPlaybackToolEditor.helperSource;
            if (helperSource.isPlaying)
            {
                if (helperSource.time < PlayingClip.length - fadeOutTime)
                {
                    if (fadeInTime == float.Epsilon)
                    {
                        helperSource.volume = asset.relativeVolume;
                    }
                    else
                    {
                        helperSource.volume = Mathf.Lerp(0, asset.relativeVolume, helperSource.time / fadeInTime);
                    }
                }
                else
                {
                    if (fadeOutTime == float.Epsilon)
                    {
                        helperSource.volume = asset.relativeVolume;
                    }
                    else
                    {
                        helperSource.volume = Mathf.Lerp(0, asset.relativeVolume, (playingClip.length - helperSource.time) / fadeOutTime);
                    }
                }
            }
        }

        public void StartFading(AudioClip audioClip, SoundFileObject newAsset)
        {
            PlayingClip = audioClip;
            if (newAsset != null) asset = newAsset;

            AudioPlaybackToolEditor.helperSource.clip = PlayingClip;
            AudioPlaybackToolEditor.soundHelper.PlayDebug((SoundFileObject)asset, false);

            fadeInTime = asset.fadeInDuration * AudioPlaybackToolEditor.helperSource.clip.length;
            fadeOutTime = asset.fadeOutDuration * AudioPlaybackToolEditor.helperSource.clip.length;
            // To prevent divisions by 0
            if (fadeInTime == 0) fadeInTime = float.Epsilon;
            if (fadeOutTime == 0) fadeOutTime = float.Epsilon;
        }
    }
}
