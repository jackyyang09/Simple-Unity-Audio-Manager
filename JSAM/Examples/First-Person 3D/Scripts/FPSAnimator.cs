using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM.Example.FirstPerson3D
{
    public class FPSAnimator : MonoBehaviour
    {
        enum ShooterStates
        {
            Idle,
            Shooting,
            Reloading,
            Running
        }

        [Header("Explore me for examples of playing basic sounds!")]
        [SerializeField]
        ShooterStates currentState = ShooterStates.Idle;

        [SerializeField]
        int magSize = 30;
        int bullets = 0;

        [SerializeField]
        float timeBetweenShots = 1;
        [SerializeField]
        bool canShoot = true;

        [SerializeField]
        float aimDownSightsTime = 1;
        float adsProgress = 0;

        [SerializeField]
        UnityEngine.UI.Text ammoText = null;

        [Header("Example of AudioEvents being used when player crouches")]
        [SerializeField]
        UnityEngine.Events.UnityEvent onCrouch = null;

        bool reloading;

        Animator anim;
        FPSWalker walker;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            walker = GetComponentInParent<FPSWalker>();
        }

        // Start is called before the first frame update
        void Start()
        {
            bullets = magSize;
            canShoot = true;
            ammoText.text = bullets.ToString(); 
        }

        // Update is called once per frame
        void Update()
        {
            if (currentState == ShooterStates.Idle || currentState == ShooterStates.Running)
            {
                if (walker.CurrentState == MovementStates.Running)
                {
                    anim.SetBool("Sprint", true);
                    currentState = ShooterStates.Running;
                }
                else
                {
                    anim.SetBool("Sprint", false);
                    currentState = ShooterStates.Idle;
                }
            }
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
                                AudioManager.PlaySound(FPS3DSounds.Gunshot);
                                StartCoroutine(ShootDelay());
                                anim.SetTrigger("Fire");
                                bullets--;
                                ammoText.text = bullets.ToString();
                            }
                            else if (bullets == 1)
                            {
                                anim.SetTrigger("FireFinal");
                                StartCoroutine(ShootDelay());
                                bullets--;
                                ammoText.text = bullets.ToString();
                                AudioManager.PlaySound(FPS3DSounds.AKDryFire);
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

        public void InvokeOnCrouch(bool crouching)
        {
            anim.SetBool("Crouch", crouching);
            onCrouch.Invoke();
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
            ammoText.text = bullets.ToString();
        }
    }
}