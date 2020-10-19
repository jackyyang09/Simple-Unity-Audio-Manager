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

        AudioListener listener;

        AudioSource source;

        // Start is called before the first frame update
        new void Start()
        {
            base.Start();
            listener = AudioManager.instance.GetListenerInternal();

            source = AudioManager.instance.GetMusicSource();
        }

        // Update is called once per frame
        void Update()
        {
            float loudest = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                float dist = Vector3.Distance(listener.transform.position, positions[i]);
                if (dist <= maxDistance[i])
                {
                    if (!AudioManager.instance.IsMusicPlayingInternal(music))
                    {
                        source = AudioManager.instance.PlayMusicInternal(music);
                    }

                    if (dist <= minDistance[i])
                    {
                        // Set to the max volume
                        source.volume = AudioManager.GetTrueMusicVolume() * music.relativeVolume;
                        return; // Can't be beat
                    }
                    else
                    {
                        float distanceFactor = Mathf.InverseLerp(maxDistance[i], minDistance[i], dist);
                        float newVol = AudioManager.GetTrueMusicVolume() * music.relativeVolume * distanceFactor;
                        if (newVol > loudest) loudest = newVol;
                    }
                }
            }
            if (AudioManager.instance.IsMusicPlayingInternal(music)) source.volume = loudest;
        }
    }
}