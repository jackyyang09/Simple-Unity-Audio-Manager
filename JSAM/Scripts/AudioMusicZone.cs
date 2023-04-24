using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("AudioManager/Audio Music Zone")]
    public class AudioMusicZone : BaseAudioMusicFeedback
    {
        [System.Serializable]
        public class MusicZone
        {
            public Vector3 Position;
            public float MaxDistance;
            public float MinDistance;
        }

        public bool keepPlayingWhenAway;

        public List<MusicZone> MusicZones = new List<MusicZone>();

        Transform Listener => AudioManager.AudioListener.transform;

        MusicChannelHelper helper;

        private void Start()
        {
            if (keepPlayingWhenAway)
            {
                helper = AudioManager.PlayMusic(audio, null, helper);
                helper.Reserved = true;
            }
        }

        private void OnDestroy()
        {
            if (helper)
            {
                helper.Stop(true);
            }
        }

        // Update is called once per frame
        void Update()
        {
            float loudest = 0;
            for (int i = 0; i < MusicZones.Count; i++)
            {
                var z = MusicZones[i];
                float dist = Vector3.Distance(Listener.position, z.Position);
                if (dist <= z.MaxDistance)
                {
                    if (!helper)
                    {
                        helper = AudioManager.PlayMusic(audio, null, helper);
                        helper.Reserved = true;
                    }

                    if (dist <= z.MinDistance)
                    {
                        // Set to the max volume
                        helper.AudioSource.volume = AudioManager.MusicVolume * audio.relativeVolume;
                        return; // Can't be beat
                    }
                    else
                    {
                        float distanceFactor = Mathf.InverseLerp(z.MaxDistance, z.MinDistance, dist);
                        float newVol = AudioManager.MusicVolume * audio.relativeVolume * distanceFactor;
                        if (newVol > loudest) loudest = newVol;
                    }
                }
            }
            if (helper) helper.AudioSource.volume = loudest;
        }
    }
}