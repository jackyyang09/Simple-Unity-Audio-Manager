using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace JSAM.JSAMEditor
{
    public abstract class BaseSoundEditor : BaseAudioEditor<SoundFileObject>
    {
        protected override GUIContent audioDesc => new GUIContent("Sound", "Sound that will be played");
        protected override List<SoundFileObject> GetListFromLibrary(AudioLibrary l) => l.Sounds;
        protected override List<AudioLibrary.CategoryToList> GetCTLFromLibrary(AudioLibrary l) => l.soundCategoriesToList;

        protected override void DrawAudioProperty()
        {
            EditorGUILayout.LabelField("Choose a Sound to Play", EditorStyles.boldLabel);

            base.DrawAudioProperty();
        }
    }
}