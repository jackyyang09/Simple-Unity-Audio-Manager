using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM.Example.FirstPerson3D
{
    public class RandomForceEmitter : MonoBehaviour
    {
        [SerializeField]
        float upWardsForce = 1.0f;

        [SerializeField]
        float jumpCooldown = 2.5f;

        Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Start is called before the first frame update
        void Start()
        {
            InvokeRepeating("AddForceForNoReason", 0, jumpCooldown);
        }

        void AddForceForNoReason()
        {
            rb.AddForce(transform.up * upWardsForce);
        }
    }
}