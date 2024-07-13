#if JSAM_TIMELINE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace JSAM
{
    [TrackClipType(typeof(SoundPlayableAsset))]
    public class SoundControlTrack : TrackAsset
    {
    }
}
#endif