using UnityEngine;

namespace JSAM
{
    public class VolumeListener : MonoBehaviour
    {
        [SerializeField] [Range(0, 1)] protected float relativeVolume = 1;
        public float RelativeVolume => relativeVolume;

        [SerializeField] protected VolumeTrack track;
        protected VolumeTrack subscribedTrack;

        [SerializeField] protected AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        protected void SubscribeToVolumeEvents()
        {
            if (track == null) subscribedTrack = JSAMSettings.Settings.MasterTrack;
            else subscribedTrack = track;

            AudioManager.OnVolumeChanged[subscribedTrack] += OnUpdateVolume;
        }

        protected void UnsubscribeFromVolumeEvents()
        {
            if (!subscribedTrack) return;

            AudioManager.OnVolumeChanged[subscribedTrack] -= OnUpdateVolume;

            subscribedTrack = null;
        }

        protected void OnUpdateVolume(float channelVolume, float realVolume)
        {
            audioSource.volume = realVolume * relativeVolume;
        }

        protected void ForceUpdateVolume()
        {
            OnUpdateVolume(AudioManager.GetVolume(subscribedTrack), AudioManager.GetModifiedVolume(subscribedTrack));
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
            if (subscribedTrack != track)
            {
                UnsubscribeFromVolumeEvents();
                SubscribeToVolumeEvents();
            }

            ForceUpdateVolume();
        }
#endif
    }
}
