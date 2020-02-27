using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Audio File", menuName = "AudioManager/Create New Audio File Object", order = 1)]
public class AudioFileObject : ScriptableObject
{
    public string audioName;
    public AudioClip file;
}
