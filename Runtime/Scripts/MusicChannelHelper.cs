using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(AudioSource))]
    public class MusicChannelHelper : BaseAudioChannelHelper<MusicFileObject>
    {
        protected override VolumeChannel DefaultChannel => VolumeChannel.Music;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (audioFile)
            {
                if (audioFile.maxPlayingInstances > 0)
                {
                    AudioManager.InternalInstance.RemovePlayingMusic(audioFile, this);
                }
            }
        }

        public override AudioSource Play(MusicFileObject file)
        {
            if (file == null)
            {
                AudioManager.DebugWarning("Attempted to play a non-existent JSAM Music File Object!");
                return AudioSource;
            }

            AudioSource.pitch = 1;

            if (file.loopMode == LoopMode.NoLooping)
            {
                AudioSource.loop = false;
            }
            else
            {
                AudioSource.loop = true;
            }

            return base.Play(file);
        }

        public override void Stop(bool stopInstantly = true)
        {
            base.Stop(stopInstantly);
            if (stopInstantly)
            {
                AudioSource.Stop();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="fadeTime">Fade-out time in seconds</param>
        /// <returns></returns>
        protected override IEnumerator FadeOut(float fadeTime)
        {
            if (fadeTime != 0)
            {
                float startingVolume = AudioSource.volume;
                float timer = 0;
                while (timer < fadeTime)
                {
                    if (audioFile.ignoreTimeScale) timer += Time.unscaledDeltaTime;
                    else timer += Time.deltaTime;

                    AudioSource.volume = Mathf.Lerp(startingVolume, 0, timer / fadeTime);
                    yield return null;
                }
                AudioSource.Stop();
            }
        }

#if UNITY_EDITOR
        public void PlayDebug(MusicFileObject file, bool dontReset)
        {
            if (!dontReset)
            {
                AudioSource.Stop();
            }
            audioFile = file;
            AudioSource.clip = file.Files[0];
            AudioSource.timeSamples = (int)Mathf.Clamp((float)AudioSource.timeSamples, 0, (float)AudioSource.clip.samples - 1);
            AudioSource.pitch = 1;
            AudioSource.volume = file.relativeVolume;

            base.PlayDebug(dontReset);
        }
#endif
    }
}