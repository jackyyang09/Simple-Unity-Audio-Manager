using UnityEngine;

namespace JSAM
{
    public class VolumeListener : MonoBehaviour
    {
        [SerializeField] [Range(0, 1)] protected float relativeVolume = 1;
        public float RelativeVolume => relativeVolume;

        [SerializeField] protected VolumeChannel volumeChannel = VolumeChannel.Sound;
        protected VolumeChannel subscribedChannel;

        [SerializeField] protected AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        protected void SubscribeToVolumeEvents()
        {
            switch (volumeChannel)
            {
                case VolumeChannel.Music:
                    AudioManager.OnMusicVolumeChanged += OnUpdateVolume;
                    break;
                case VolumeChannel.Sound:
                    AudioManager.OnSoundVolumeChanged += OnUpdateVolume;
                    break;
                case VolumeChannel.Voice:
                    AudioManager.OnVoiceVolumeChanged += OnUpdateVolume;
                    break;
            }
        }

        protected void UnsubscribeFromAudioEvents()
        {
            switch (volumeChannel)
            {
                case VolumeChannel.Music:
                    AudioManager.OnMusicVolumeChanged -= OnUpdateVolume;
                    break;
                case VolumeChannel.Sound:
                    AudioManager.OnSoundVolumeChanged -= OnUpdateVolume;
                    break;
                case VolumeChannel.Voice:
                    AudioManager.OnVoiceVolumeChanged -= OnUpdateVolume;
                    break;
            }
        }

        protected void OnUpdateVolume(float channelVolume, float realVolume)
        {
            audioSource.volume = realVolume * relativeVolume;
        }

        protected void ForceUpdateVolume()
        {
            switch (subscribedChannel)
            {
                case VolumeChannel.Music:
                    OnUpdateVolume(AudioManagerInternal.Instance.MusicVolume, AudioManagerInternal.Instance.ModifiedMusicVolume);
                    break;
                case VolumeChannel.Sound:
                    OnUpdateVolume(AudioManagerInternal.Instance.SoundVolume, AudioManagerInternal.Instance.ModifiedSoundVolume);
                    break;
                case VolumeChannel.Voice:
                    OnUpdateVolume(AudioManagerInternal.Instance.VoiceVolume, AudioManagerInternal.Instance.ModifiedVoiceVolume);
                    break;
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += EditorUpdateVolume;
            }
            else
            {
                if (audioSource) return;

                UnityEditor.Undo.RecordObject(this, $"{GetType()}: Setting up");
                audioSource = GetComponent<AudioSource>();
            }
        }

        protected void EditorUpdateVolume()
        {
            UnityEditor.EditorApplication.delayCall -= EditorUpdateVolume;

            if (subscribedChannel != volumeChannel)
            {
                UnsubscribeFromAudioEvents();
                SubscribeToVolumeEvents();
                subscribedChannel = volumeChannel;
            }

            ForceUpdateVolume();
        }
#endif
    }
}
