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
            JSAM.AudioManager.instance.CrossfadeMusic("MenuPitched", 5, false,  true);
        }
        else
        {
            JSAM.AudioManager.instance.CrossfadeMusic("Menu", 5, false, true);
        }
    }
}
