using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    [AddComponentMenu("AudioManager/Audio Music Zone")]
    public class AudioMusicZone : BaseAudioMusicFeedback
    {
        public List<Vector3> positions = new List<Vector3>();
        public List<float> maxDistance = new List<float>();
        public List<float> minDistance = new List<float>();

        JSAMMusicChannelHelper helper;

        // Update is called once per frame
        void Update()
        {
            float loudest = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                float dist = Vector3.Distance(AudioManager.AudioListener.transform.position, positions[i]);
                if (dist <= maxDistance[i])
                {
                    if (!AudioManager.IsMusicPlaying(music))
                    {
                        helper = AudioManager.PlayMusic(music);
                    }

                    if (dist <= minDistance[i])
                    {
                        // Set to the max volume
                        helper.AudioSource.volume = AudioManager.MusicVolume * music.relativeVolume;
                        return; // Can't be beat
                    }
                    else
                    {
                        float distanceFactor = Mathf.InverseLerp(maxDistance[i], minDistance[i], dist);
                        float newVol = AudioManager.MusicVolume * music.relativeVolume * distanceFactor;
                        if (newVol > loudest) loudest = newVol;
                    }
                }
            }
            if (AudioManager.IsMusicPlaying(music)) helper.AudioSource.volume = loudest;
        }
    }
}