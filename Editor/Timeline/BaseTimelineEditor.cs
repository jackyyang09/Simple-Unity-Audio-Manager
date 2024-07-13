#if JSAM_TIMELINE
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace JSAM.JSAMEditor
{
    public class BaseTimelineEditor<T> : ClipEditor where T : PlayableAsset
    {
        protected double lastStart;
        protected double lastEnd;

        protected GUIContent leftArrow;
        protected GUIContent LeftArrow
        {
            get
            {
                if (leftArrow == null)
                {
                    leftArrow = EditorGUIUtility.IconContent("d_scrollleft");
                }
                return leftArrow;
            }
        }

        protected GUIContent rightArrow;
        protected GUIContent RightArrow
        {
            get
            {
                if (rightArrow == null)
                {
                    rightArrow = EditorGUIUtility.IconContent("d_scrollright");
                }
                return rightArrow;
            }
        }

        protected Texture2D previewTex;

        protected T GetAsset(TimelineClip clip) => clip.asset as T;

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var cdo = base.GetClipOptions(clip);
            cdo.displayClipName = false;
            cdo.highlightColor = new Color(0.99f, 0.52f, 0);
            return cdo;
        }

        protected void RenderAudioPreview(double startTime, TimelineClip clip, AudioClip audio, ClipBackgroundRegion region)
        {
            if (!previewTex)
            {
                previewTex = AudioPlaybackToolEditor.RenderStaticPreview(
                    audio, region.position, 1, new Color(0.13f, 0.16f, 0.25f),
                    new Color(0.25f, 0.266f, 0.29f));

                byte[] _bytes = previewTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(@"C:\Users\jacky\Documents\Unity Projects\Simple-Unity-Audio-Manager\Assets\test.png", _bytes);
            }

            EditorGUI.DrawPreviewTexture(region.position, previewTex);

            if (startTime > 0)
            {
                EditorGUI.LabelField(region.position, LeftArrow);
            }

            if (startTime + clip.duration < audio.length)
            {
                EditorGUI.LabelField(region.position, RightArrow, EditorStyles.label.ApplyTextAnchor(TextAnchor.MiddleRight));
            }

            var style = new GUIStyle(EditorStyles.label)
                .ApplyTextAnchor(TextAnchor.MiddleCenter)
                .SetFontSize(10);

            var name = clip.displayName;

            // Create Shadow
            var r = region.position;
            r.center += new Vector2(1.25f, 1.25f);
            EditorGUI.LabelField(r, name, style.SetTextColor(Color.black));

            // Actual Text
            EditorGUI.LabelField(region.position, name, style.SetTextColor(Color.white));
        }
    }
}
#endif