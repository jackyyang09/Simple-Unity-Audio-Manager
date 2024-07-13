#if JSAM_TIMELINE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace JSAM.JSAMEditor
{
    [CustomEditor(typeof(MusicPlayableAsset))]
    [CanEditMultipleObjects]
    public class MusicTimelinePlayableEditor : Editor
    {
    }

    [CustomTimelineEditor(typeof(MusicPlayableAsset))]
    public class MusicTimelineEditor : BaseTimelineEditor<MusicPlayableAsset>
    {
        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            Debug.Log(nameof(OnCreate));
            base.OnCreate(clip, track, clonedFrom);
            var asset = GetAsset(clip);
            clip.duration = asset.audio.Files[0].length;
        }

        public override void OnClipChanged(TimelineClip clip)
        {
            var asset = GetAsset(clip);
            //Debug.Log("Last Start: " + lastStart + " | " + "Start: " + clip.start);

            var length = (double)asset.audio.Files[0].samples / (double)asset.audio.Files[0].frequency;

            // Clamp logic, doesn't work very well, especially when adjusting the start
            if (lastStart != clip.start && lastEnd == clip.end)
            {
                var delta = (float)(lastStart - clip.start);
                //if (asset.startTime - delta < 0)
                //{
                //    clip.start = lastStart;
                //}
                //else
                //{
                    asset.startTime -= delta;
                    if (asset.startTime < 0) asset.startTime = 0;
                //}
            }
            // This works, but was removed since features should be consistent
            //else if (lastEnd != clip.end) 
            //{
            //    if (asset.startTime + clip.duration >= length)
            //    {
            //        clip.duration = length - asset.startTime;
            //    }
            //}

            lastStart = clip.start;
            lastEnd = clip.end;

            base.OnClipChanged(clip);
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var asset = GetAsset(clip);
            RenderAudioPreview(asset.startTime, clip, asset.audio.Files[0], region);
            base.DrawBackground(clip, region);
        }
    }
}
#endif