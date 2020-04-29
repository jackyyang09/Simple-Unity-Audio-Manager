using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

namespace JSAM
{
    public class FPSAnimator : MonoBehaviour
    {
        enum ShooterStates
        {
            Idle,
            Shooting,
            Reloading
        }

        [SerializeField]
        ShooterStates currentState;

        [SerializeField]
        int magSize = 30;
        int bullets;

        [SerializeField]
        float timeBetweenShots;
        [SerializeField]
        bool canShoot;

        [SerializeField]
        float aimDownSightsTime;
        float adsProgress;

        bool reloading;

        Animator anim;

        // Start is called before the first frame update
        void Start()
        {
            anim = GetComponent<Animator>();
            bullets = magSize;
            canShoot = true;
        }

        // Update is called once per frame
        void Update()
        {
            switch (currentState)
            {
                case ShooterStates.Idle:
                case ShooterStates.Shooting:
                    if (canShoot)
                    {
                        if (Input.GetKey(KeyCode.Mouse0))
                        {
                            if (bullets > 1)
                            {
                                AudioManager.instance.PlaySoundOnce(SoundsExample3D.Gunshot, transform, Priority.Default, Pitch.Low);
                                StartCoroutine(ShootDelay());
                                anim.SetTrigger("Fire");
                                bullets--;
                            }
                            else if (bullets == 1)
                            {
                                anim.SetTrigger("FireFinal");
                                StartCoroutine(ShootDelay());
                                bullets--;
                                AudioManager.instance.PlaySoundOnce(SoundsExample3D.AKDryFire, transform, Priority.Default, Pitch.Low);
                            }
                        }
                        else if (Input.GetKeyUp(KeyCode.Mouse0))
                        {
                            anim.SetTrigger("FireStop");
                        }
                        
                    }
                    if (Input.GetMouseButton(1))
                    {
                        adsProgress = Mathf.Clamp(adsProgress + Time.deltaTime, 0, aimDownSightsTime);
                    }
                    if (Input.GetKeyDown(KeyCode.R) && adsProgress == 0)
                    {
                        anim.SetInteger("Ammo", bullets);
                        anim.SetTrigger("Reload");
                        canShoot = false;
                        currentState = ShooterStates.Reloading;
                    }
                    break;
                case ShooterStates.Reloading:
                    break;
                    
            }
            if (!Input.GetMouseButton(1))
            {
                adsProgress = Mathf.Clamp(adsProgress - Time.deltaTime, 0, aimDownSightsTime);
            }
            anim.SetFloat("AimDownSights", adsProgress / aimDownSightsTime);
        }

        IEnumerator ShootDelay()
        {
            canShoot = false;
            yield return new WaitForSeconds(timeBetweenShots);
            canShoot = true;
        }

        public void ReloadBullets()
        {
            bullets = magSize;
            canShoot = true;
            currentState = ShooterStates.Idle;
        }
    }
}