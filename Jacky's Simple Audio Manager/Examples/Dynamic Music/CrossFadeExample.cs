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
            JSAM.AudioManager.CrossfadeMusic(JSAM.MusicExampleCrossfade.MenuPitched, 5, true);
        }
        else
        {
            JSAM.AudioManager.CrossfadeMusic(JSAM.MusicExampleCrossfade.Menu, 5, true);
        }
    }
}
