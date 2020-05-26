using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    /// <summary>
    /// A lot of code by this cool dude:
    /// https://www.reddit.com/r/Unity3D/comments/8k7w7v/unity_simple_mouselook/
    /// </summary>
    public class FPSMouseLook : MonoBehaviour
    {
        [SerializeField]
        public float speed = 3;

        float rotationX = 0;
        float rotationY = 0;

        Transform stand;

        private void Start()
        {
            stand = transform.parent;
        }

        // Update is called once per frame
        void Update()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                rotationY += Input.GetAxis("Mouse X") * speed;
                rotationX += -Input.GetAxis("Mouse Y") * speed;

                rotationX = Mathf.Clamp(rotationX, -90, 90);

                stand.localEulerAngles = new Vector3(0, rotationY, 0);
                transform.localEulerAngles = new Vector3(rotationX, 0, 0);
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}