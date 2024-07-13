using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace JSAM
{
    public class MusicPlayableAsset : PlayableAsset
    {
        public MusicFileObject audio;
        public float volume = 1;
        public double startTime;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MusicPlayableBehaviour>.Create(graph);

            var behaviour = playable.GetBehaviour();
            behaviour.Audio = audio;
            behaviour.Volume = volume;
            behaviour.StartTime = startTime;

            return playable;
        }
    }
}