using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Still being drafted, come back later!
/// </summary>
namespace JSAM
{
    [CreateAssetMenu(fileName = "New AudioManager Settings", menuName = "AudioManager/New AudioManager Settings Asset", order = 1)]
    public class AudioManagerSettings : ScriptableObject
    {
        [HideInInspector] public string sceneSoundEnumName = "Sounds";
        [HideInInspector] public System.Type sceneSoundEnumType = null;

        [HideInInspector] public string sceneMusicEnumName = "Music";
        [HideInInspector] public System.Type sceneMusicEnumType = null;

        [HideInInspector] public float masterVolume = 1;
        [HideInInspector] public bool masterMuted = false;

        [HideInInspector] public float soundVolume = 1;
        [HideInInspector] public bool soundMuted = false;

        [HideInInspector] public float musicVolume = 1;
        [HideInInspector] public bool musicMuted = false;

        [HideInInspector] public bool spatialSound = true;

        private void OnEnable()
        {
        }
    }
}