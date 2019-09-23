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
            AudioManager.instance.CrossfadeMusic("MenuPitched", 5, false, false, true);
        }
        else
        {
            AudioManager.instance.CrossfadeMusic("Menu", 5, false, false, true);
        }
    }
}
