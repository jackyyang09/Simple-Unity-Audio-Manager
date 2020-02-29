using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

namespace JSAM
{
    public class FPSAnimator : MonoBehaviour
    {
        [SerializeField]
        int clipSize = 6;
        int bullets;

        Animator anim;
        // Start is called before the first frame update
        void Start()
        {
            anim = GetComponent<Animator>();
            bullets = 6;
        }

        // Update is called once per frame
        void Update()
        {
            switch (bullets)
            {
                case 1:
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        AudioManager.instance.PlaySoundOnce("Gunshot", transform, Priority.Default, Pitch.Low);
                        anim.SetTrigger("FireFinal");
                        bullets--;
                    }
                    break;
                default:
                    if (Input.GetKeyDown(KeyCode.Mouse0) && bullets > 1)
                    {
                        AudioManager.instance.PlaySoundOnce("Gunshot", transform, Priority.Default, Pitch.Low);
                        anim.SetTrigger("Fire");
                        bullets--;
                    }
                    else if (Input.GetKeyDown(KeyCode.R))
                    {
                        anim.SetTrigger("Reload");
                        Invoke("ReloadBullets", 3);
                    }
                    break;
            }
        }

        public void ReloadBullets()
        {
            bullets = 6;
        }
    }
}