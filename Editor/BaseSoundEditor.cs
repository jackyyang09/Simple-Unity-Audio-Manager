using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseSoundEditor : BaseAudioEditor<SoundFileObject>
    {
        protected override GUIContent audioDesc => new GUIContent("Sound", "Sound that will be played");
        protected override List<SoundFileObject> audioLibrary => AudioManager.Instance.Library.Sounds;
        protected override List<AudioLibrary.CategoryToList> ctl => AudioManager.Instance.Library.soundCategoriesToList;
        
        protected override void DrawAudioProperty()
        {
            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            base.DrawAudioProperty();
        }
    }
}