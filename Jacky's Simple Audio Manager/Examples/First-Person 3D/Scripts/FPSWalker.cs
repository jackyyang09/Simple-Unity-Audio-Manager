using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public enum MovementStates
    {
        Idle,
        Walking,
        Running,
    }

    public class FPSWalker : MonoBehaviour
    {
        [Header("Explore me for examples of sound looping!")]
        [SerializeField]
        float moveSpeed = 5;

        [SerializeField]
        float runSpeedMultiplier = 3;
        [SerializeField]
        float crouchSpeedMultiplier = 0.75f;

        [SerializeField]
        Vector3 gravity = new Vector3(0, -9.81f, 0);

        [SerializeField]
        MovementStates moveState;

        [SerializeField]
        bool crouching;
        bool canToggleCrouch = true;

        CharacterController controller;

        [SerializeField]
        Transform stand = null;

        FPSAnimator animator = null;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponentInChildren<FPSAnimator>();
        }

        // Update is called once per frame
        void Update()
        {
            float theSpeed = moveSpeed;

            Vector3 movement = Vector3.zero;

            if (crouching)
            {
                theSpeed *= crouchSpeedMultiplier;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                theSpeed *= runSpeedMultiplier;
            }

            if (Input.GetKey(KeyCode.W))
            {
                movement += stand.transform.forward * theSpeed;
            }
            if (Input.GetKey(KeyCode.S))
            {
                movement -= stand.transform.forward * theSpeed;
            }
            if (Input.GetKey(KeyCode.A))
            {
                movement -= stand.transform.right * theSpeed;
            }
            if (Input.GetKey(KeyCode.D))
            {
                movement += stand.transform.right * theSpeed;
            }

            if (Input.GetKeyDown(KeyCode.C) && canToggleCrouch)
            {
                StartCoroutine(CrouchCooldown());
            }
            // Un-crouch just by sprinting
            else if (crouching && Input.GetKey(KeyCode.LeftShift) && canToggleCrouch)
            {
                StartCoroutine(CrouchCooldown());
            }

            if (Input.GetKey(KeyCode.LeftShift) && moveState == MovementStates.Walking) moveState = MovementStates.Running;
            else if (!Input.GetKey(KeyCode.LeftShift) && movement.magnitude > 0) moveState = MovementStates.Walking;
            else if (movement.magnitude == 0) moveState = MovementStates.Idle;

            controller.Move((movement + gravity) * Time.deltaTime);

            PlayMovementSound();
        }

        public void PlayMovementSound()
        {
            switch (moveState)
            {
                case MovementStates.Idle:
                    if (AudioManager.IsSoundLooping(SoundsExample3D.Walk))
                    {
                        AudioManager.StopSoundLoop(SoundsExample3D.Walk, transform, true);
                    }
                    if (AudioManager.IsSoundLooping(SoundsExample3D.Running))
                    {
                        AudioManager.StopSoundLoop(SoundsExample3D.Running, transform, true);
                    }
                    break;
                case MovementStates.Walking:
                    if (AudioManager.IsSoundLooping(SoundsExample3D.Running))
                    {
                        AudioManager.StopSoundLoop(SoundsExample3D.Running, transform, true);
                    }
                    if (!AudioManager.IsSoundLooping(SoundsExample3D.Walk))
                    {
                        AudioManager.PlaySoundLoop(SoundsExample3D.Walk, transform);
                    }
                    break;
                case MovementStates.Running:
                    if (AudioManager.IsSoundLooping(SoundsExample3D.Walk))
                    {
                        AudioManager.StopSoundLoop(SoundsExample3D.Walk, transform, true);
                    }
                    if (!AudioManager.IsSoundLooping(SoundsExample3D.Running))
                    {
                        AudioManager.PlaySoundLoop(SoundsExample3D.Running, transform);
                    }
                    break;
            }
        }

        IEnumerator CrouchCooldown()
        {
            crouching = !crouching;
            animator.InvokeOnCrouch(crouching);
            canToggleCrouch = false;
            yield return new WaitForSeconds(0.15f);
            canToggleCrouch = true;
        }

        public MovementStates GetCurrentState()
        {
            return moveState;
        }

        public Vector3 Gravity()
        {
            return gravity;
        }
    }
}