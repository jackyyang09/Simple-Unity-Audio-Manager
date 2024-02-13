using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM.JSAMEditor
{
    public class EditorAudioHelper : System.IDisposable
    {
        GameObject gameObject;
        AudioSource audioSource;
        public AudioSource Source => audioSource;

        SoundChannelHelper soundHelper;
        public SoundChannelHelper SoundHelper => soundHelper;
        MusicChannelHelper musicHelper;
        public MusicChannelHelper MusicHelper => musicHelper;

        public AudioClip Clip
        {
            get => audioSource.clip;
            set => audioSource.clip = value;
        }

        public EditorAudioHelper(AudioClip clip)
        {
            CreateHelper(clip);
        }

        void CreateHelper(AudioClip clip)
        {
            gameObject = new GameObject("JSAM Audio Helper");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = clip;

            soundHelper = gameObject.AddComponent<SoundChannelHelper>();
            musicHelper = gameObject.AddComponent<MusicChannelHelper>();
            UnityEngine.Audio.AudioMixerGroup sg = null;
            UnityEngine.Audio.AudioMixerGroup mg = null;
            if (JSAMSettings.Settings)
            {
                sg = JSAMSettings.Settings.SoundGroup;
                mg = JSAMSettings.Settings.MusicGroup;
            }
            soundHelper.Init(sg);
            musicHelper.Init(mg);
            //gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.hideFlags = HideFlags.DontSave;
        }

        public void Dispose()
        {
            if (audioSource)
            {
                audioSource.Stop();
            }
            Object.DestroyImmediate(gameObject);
        }
    }
}