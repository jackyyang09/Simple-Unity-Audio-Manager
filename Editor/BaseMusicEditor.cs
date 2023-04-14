using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseMusicEditor : BaseAudioEditor<MusicFileObject>
    {
        protected override GUIContent audioDesc => new GUIContent("Music", "Sound that will be played");
        protected override List<MusicFileObject> audioLibrary => AudioManager.Instance.Library.Music;
        protected override List<AudioLibrary.CategoryToList> ctl => AudioManager.Instance.Library.musicCategoriesToList;

        protected override void DrawAudioProperty()
        {
            EditorGUILayout.LabelField("Choose some Music to Play", EditorStyles.boldLabel);

            base.DrawAudioProperty();
        }
    }
}