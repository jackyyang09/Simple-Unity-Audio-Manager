using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

namespace JSAM
{
    public enum MovementStates
    {
        Idle,
        Walking,
        Running
    }

    public class FPSWalker : MonoBehaviour
    {
        [SerializeField]
        float moveSpeed = 5;

        [SerializeField]
        float runSpeedMultiplier = 3;

        [SerializeField]
        Vector3 gravity = new Vector3(0, -9.81f, 0);

        [SerializeField]
        MovementStates moveState;

        CharacterController controller;

        Transform stand;

        AudioManager am;

        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<CharacterController>();
            stand = Camera.main.transform;

            am = AudioManager.instance;
        }

        // Update is called once per frame
        void Update()
        {
            float theSpeed = moveSpeed;

            Vector3 movement = Vector3.zero;

            moveState = MovementStates.Idle;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                theSpeed *= runSpeedMultiplier;
                moveState = MovementStates.Running;
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

            if (movement.magnitude > 0 && moveState != MovementStates.Running) moveState = MovementStates.Walking;

            controller.Move((movement + gravity) * Time.deltaTime);

            PlayMovementSound();
        }

        public void PlayMovementSound()
        {
            switch (moveState)
            {
                case MovementStates.Idle:
                    am.StopSoundLoop("Walk", true, transform);
                    am.StopSoundLoop("Running", true, transform);
                    break;
                case MovementStates.Walking:
                    am.StopSoundLoop("Running", true, transform);
                    if (!am.IsSoundLooping("Walk"))
                    {
                        am.PlaySoundLoop("Walk", transform, false, Priority.Default);
                    }
                    break;
                case MovementStates.Running:
                    am.StopSoundLoop("Walk", true, transform);
                    if (!am.IsSoundLooping("Running"))
                    {
                        am.PlaySoundLoop("Running", transform, false, Priority.Default);
                    }
                    break;
            }
        }

        public Vector3 Gravity()
        {
            return gravity;
        }
    }
}