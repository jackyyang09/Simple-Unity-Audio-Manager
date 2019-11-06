using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

public class FPSWalker : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 5;

    [SerializeField]
    float runSpeedMultiplier = 3;

    [SerializeField]
    Vector3 gravity = new Vector3(0, -9.81f, 0);

    CharacterController controller;

    Transform stand;

    AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        stand = transform.GetChild(0);

        am = AudioManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        float theSpeed = moveSpeed;

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftShift))
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

        controller.Move((movement + gravity) * Time.deltaTime);

        if (movement.magnitude > 0)
        {
            if (!am.IsSoundLooping("Walk"))
            {
                am.PlaySoundLoop("Walk", transform, false, Priority.Default);
            }
        }
        else
        {
            am.StopSoundLoop("Walk", true, transform);
        }
    }

    public Vector3 Gravity()
    {
        return gravity;
    }
}
