using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("AudioManager/Audio Music Zone")]
    public class AudioMusicZone : BaseAudioMusicFeedback
    {
        [System.Serializable]
        public struct MusicZone
        {
            public Vector3 Position;
            public bool OverrideDistance;
            public Vector2 Distance;
        }

        [Tooltip("The maximum distance the listener can be at to hear the music in all zones. " +
            "The music will be at it's loudest when the listener is at this distance or closer.")]
        public float MaxDistance = 15;
        [Tooltip("The minimum distance the listener can be at to hear the music in this music zone. " +
            "Volume of the music will increase as listener approaches the minimum distance.")]
        public float MinDistance = 5;

        public bool keepPlayingWhenAway;

        public List<MusicZone> MusicZones = new List<MusicZone>();

        Transform Listener => AudioManager.AudioListener.transform;

        MusicChannelHelper helper;

        VolumeTrack track;

        private void Start()
        {
            if (keepPlayingWhenAway)
            {
                helper = AudioManager.PlayMusic(audio, null, helper);
                helper.Reserved = true;
            }

            if (!audio.volumeTrack) track = JSAMSettings.Settings.MasterTrack;
            else track = audio.volumeTrack;
        }

        private void OnDestroy()
        {
            if (AudioManagerInternal.IsQuitting) return;

            helper.Stop(true);
        }

        // Update is called once per frame
        void Update()
        {
            if (MusicZones.Count == 0) return;

            float loudest = 0;

            float min, max;

            for (int i = 0; i < MusicZones.Count; i++)
            {
                var z = MusicZones[i];
                float dist = Vector3.Distance(Listener.position, z.Position);

                if (MusicZones[i].OverrideDistance)
                {
                    min = MusicZones[i].Distance.x;
                    max = MusicZones[i].Distance.y;
                }
                else
                {
                    min = MinDistance;
                    max = MaxDistance;
                }

                if (dist <= max)
                {
                    if (!helper)
                    {
                        helper = AudioManager.PlayMusic(audio, null, helper);
                        helper.Reserved = true;
                    }

                    if (dist <= min)
                    {
                        // Set to the max volume

                        helper.AudioSource.volume = AudioManager.GetModifiedVolume(track) * audio.relativeVolume;
                        return; // Can't be beat
                    }
                    else
                    {
                        float distanceFactor = Mathf.InverseLerp(max, min, dist);
                        float newVol = AudioManager.GetModifiedVolume(track) * audio.relativeVolume * distanceFactor;
                        if (newVol > loudest) loudest = newVol;
                    }
                }
            }
            if (helper) helper.AudioSource.volume = loudest;
        }
    }
}