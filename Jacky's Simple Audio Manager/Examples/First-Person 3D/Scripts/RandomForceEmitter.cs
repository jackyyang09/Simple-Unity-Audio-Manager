using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class RandomForceEmitter : MonoBehaviour
    {
        [SerializeField]
        float upWardsForce;

        Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Start is called before the first frame update
        void Start()
        {
            InvokeRepeating("AddForceForNoReason", 0, 2.5f);
        }

        void AddForceForNoReason()
        {
            rb.AddForce(transform.up * upWardsForce);
        }
    }
}