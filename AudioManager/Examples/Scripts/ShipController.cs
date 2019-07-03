using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 8;

    [SerializeField]
    float focusSpeed = 3;

    Rigidbody2D rb;

    AudioManager am;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        am = AudioManager.GetInstance();
    }

    // Update is called once per frame
    void Update()
    {
        float speed = (Input.GetKey(KeyCode.LeftShift)) ? focusSpeed : moveSpeed;

        Vector2 input = Vector2.zero;

        if (Input.GetKey(KeyCode.UpArrow))
        {
            input += Vector2.up;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            input += Vector2.left;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            input += Vector2.right;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            input += Vector2.down;
        }

        rb.MovePosition((Vector2)transform.position + input * speed * Time.deltaTime);

        if (Input.GetKey(KeyCode.Z))
        {
            if (!am.IsSoundPlaying("Shooting", transform))
            {
                am.PlaySoundLoop("Shooting", transform);
            }
        }
        else
        {
            am.StopSoundLoop("Shooting");
        }
    }
}
