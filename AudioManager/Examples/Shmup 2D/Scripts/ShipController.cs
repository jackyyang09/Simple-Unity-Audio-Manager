using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField]
    float moveSpeed = 8;

    [SerializeField]
    float focusSpeed = 3;

    /// <summary>
    /// Time between each shot
    /// </summary>
    [SerializeField]
    [Tooltip("Time between each shot")]
    float shotCooldown = 0.15f;
    bool canShoot = true;

    [SerializeField]
    ObjectPool bulletPool;

    [SerializeField]
    Transform bulletSpawnZone;

    Rigidbody2D rb;

    AudioManager am;

    Animator anim;

    SpriteRenderer hitBox;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        am = AudioManager.GetInstance();
        anim = GetComponentInChildren<Animator>();
        hitBox = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        float speed = (Input.GetKey(KeyCode.LeftShift)) ? focusSpeed : moveSpeed;
        hitBox.enabled = (Input.GetKey(KeyCode.LeftShift));

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

        if (Time.timeScale > 0)
        {
            anim.SetBool("left", Input.GetKey(KeyCode.LeftArrow));
            anim.SetBool("right", Input.GetKey(KeyCode.RightArrow));

            if (Input.GetKey(KeyCode.Z) && canShoot)
            {
                GameObject bullet = bulletPool.GetObject();

                bullet.transform.position = transform.position;
                bullet.SetActive(true);

                if (!am.IsSoundPlaying("Shooting", transform))
                {
                    am.PlaySoundLoop("Shooting", transform);
                }

                StartCoroutine(ShotCooldown());
            }
            else if (!Input.GetKey(KeyCode.Z))
            {
                am.StopSoundLoop("Shooting");
            }
        }
    }

    IEnumerator ShotCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(shotCooldown);
        canShoot = true;
    }
}
