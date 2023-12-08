//#define PrintStuff
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace JSAM
{
    public class MusicPlayableBehaviour : BaseJSAMPlayableBehaviour<MusicFileObject>
    {
        protected override void InitAudioHelper()
        {
            if (!helperObject.TryForComponent(out helper))
            {
                helper = helperObject.AddComponent<MusicChannelHelper>();
            }
        }

        protected override BaseAudioChannelHelper<MusicFileObject> Helper
        {
            get
            {
                if (!helper)
                {
                    helper = base.Helper;

                    if (JSAMSettings.Settings)
                    {
                        var mg = JSAMSettings.Settings.MusicGroup;
                        helper.Init(mg);
                    }
                }

                return helper as MusicChannelHelper;
            }
        }

        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);
#if PrintStuff
            Debug.Log(nameof(OnGraphStart) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif
        }

        public override void OnGraphStop(Playable playable)
        {
            base.OnGraphStop(playable);
#if PrintStuff
            Debug.Log(nameof(OnGraphStop) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            base.OnPlayableDestroy(playable);
#if PrintStuff
            Debug.Log(nameof(OnPlayableDestroy));
#endif
            GameObject.DestroyImmediate(helperObject);
            SOURCES--;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            base.OnBehaviourPlay(playable, info);
            //AudioPlaybackToolEditor.soundHelper.PlayDebug(sound);
#if PrintStuff
            Debug.Log(nameof(OnBehaviourPlay) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() + 
                " | Seek Occurred: " + info.seekOccurred +
                " | PlayState: " + info.effectivePlayState +
                " | Evaluation: " + info.evaluationType +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif
            if (Helper)
            {
                if (helperSource.clip)
                {
                    UpdateTime(playable);
                }
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            base.OnBehaviourPause(playable, info);
            switch (info.effectivePlayState)
            {
                case PlayState.Paused: // Clip ended
                    Helper.AudioSource.Pause();
                    break;
                case PlayState.Playing: // Just paused in-editor
                    Helper.AudioSource.Pause();
                    break;
                case PlayState.Delayed:
                    break;
            }
#if PrintStuff
            Debug.Log(nameof(OnBehaviourPause) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Seek Occurred: " + info.seekOccurred +
                " | PlayState: " + info.effectivePlayState +
                " | Evaluation: " + info.evaluationType +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif
        }

        public override void OnPlayableCreate(Playable playable)
        {
            base.OnPlayableCreate(playable);
#if PrintStuff
            Debug.Log(nameof(OnPlayableCreate) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif
        }

        public override void PrepareData(Playable playable, FrameData info)
        {
            base.PrepareData(playable, info);
#if PrintStuff
            Debug.Log(nameof(PrepareData) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Seek Occurred: " + info.seekOccurred +
                " | PlayState: " + info.effectivePlayState +
                " | Evaluation: " + info.evaluationType +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif

            if (Application.isPlaying)
            {
                //if (!AudioManager.IsSoundPlaying(sound))
                //{
                //    helper = AudioManager.PlaySound(sound);
                //}
            }
#if UNITY_EDITOR
            else
            {

            }
#endif
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            base.PrepareFrame(playable, info);

#if PrintStuff
            Debug.Log(nameof(PrepareFrame) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Seek Occurred: " + info.seekOccurred +
                " | PlayState: " + info.effectivePlayState +
                " | Evaluation: " + info.evaluationType +
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif

            if (Helper)
            {
                if (!playable.GetGraph().IsPlaying() && helperSource.clip)
                {
                    if (helperSource.isPlaying)
                    {
                        helperSource.Pause();
                    }
                    UpdateTime(playable);
                }
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            base.ProcessFrame(playable, info, playerData);
#if PrintStuff
            Debug.Log(nameof(ProcessFrame) +
                " | Is Playing: " + playable.GetGraph().IsPlaying() +
                " | Seek Occurred: " + info.seekOccurred +
                " | PlayState: " + info.effectivePlayState +
                " | Evaluation: " + info.evaluationType + 
                " | Traversal Mode: " + playable.GetTraversalMode() +
                " | Play State: " + playable.GetPlayState());
#endif
            if (Application.isPlaying)
            {
                //if (AudioManager.IsSoundPlaying(sound))
                {
                    //helper.AudioSource.time = playable.GetDuration();
                }
            }
            else
            {
                if (playable.GetGraph().IsPlaying())
                {
                    if (helperSource.clip != Audio.Files[0])
                    {
                        helperSource.clip = Audio.Files[0];
                        UpdateTime(playable);
                    }
#if UNITY_EDITOR
                    if (!helperSource.isPlaying)
                    {
                        Helper.PlayDebug(Audio);
                    }
#endif
                }
            }
            Helper.AudioSource.volume = Volume;
        }
    }
}