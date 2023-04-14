using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM.Example.ExtraTips
{
    public class RepeatingButton : MonoBehaviour
    {
        [SerializeField] SoundFileObject soundToPlay;

        bool buttonDown;

        private void Update()
        {
            if (buttonDown)
            {
                soundToPlay.Play();
            }
        }

        public void ButtonDown()
        {
            buttonDown = true;
        }

        public void ButtonUp()
        {
            buttonDown = false;
        }
    }
}