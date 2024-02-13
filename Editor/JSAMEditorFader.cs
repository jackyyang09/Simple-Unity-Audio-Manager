using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JSAM.JSAMEditor
{
    public class JSAMEditorFader : System.IDisposable
    {
        BaseAudioFileObject asset;
        EditorAudioHelper helper;

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

        public JSAMEditorFader(BaseAudioFileObject _asset, EditorAudioHelper _helper)
        {
            EditorApplication.update += Update;
            asset = _asset;
            helper = _helper;
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
                helper.Source.volume = asset.relativeVolume;
                return;
            }

            // Throws an error here when replacing a AudioClip and playing it in an independent window
            var helperSource = helper.Source;
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

            helper.Source.clip = PlayingClip;
            helper.SoundHelper.PlayDebug((SoundFileObject)asset, false);

            fadeInTime = asset.fadeInDuration * helper.Source.clip.length;
            fadeOutTime = asset.fadeOutDuration * helper.Source.clip.length;
            // To prevent divisions by 0
            if (fadeInTime == 0) fadeInTime = float.Epsilon;
            if (fadeOutTime == 0) fadeOutTime = float.Epsilon;
        }
    }
}
