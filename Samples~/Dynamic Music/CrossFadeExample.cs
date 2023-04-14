using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM.Example
{
    public class CrossFadeExample : MonoBehaviour
    {
        bool pitched = false;

        public void UseCrossFade()
        {
            pitched = !pitched;
            var time = AudioManager.FadeMainMusicOut(5).AudioSource.time;
            if (pitched)
            {
                AudioManager.FadeMusicIn(DynamicMusicMusic.MenuPitched, 5, true).AudioSource.time = time;
            }
            else
            {
                AudioManager.FadeMusicIn(DynamicMusicMusic.Menu, 5, true).AudioSource.time = time;
            }
        }
    }
}