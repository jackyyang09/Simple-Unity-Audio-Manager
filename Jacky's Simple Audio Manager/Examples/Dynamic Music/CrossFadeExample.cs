using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossFadeExample : MonoBehaviour
{
    bool pitched = false;

    public void UseCrossFade()
    {
        pitched = !pitched;
        if (pitched)
        {
            JSAM.AudioManager.instance.CrossfadeMusic(JSAM.MusicExampleCrossfade.MenuPitched, 5, JSAM.LoopMode.LoopWithLoopPoints,  true);
        }
        else
        {
            JSAM.AudioManager.instance.CrossfadeMusic(JSAM.MusicExampleCrossfade.Menu, 5, JSAM.LoopMode.LoopWithLoopPoints, true);
        }
    }
}
