using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

/// <summary>
/// Control script for a generic scrolling shooter game
/// </summary>
public class ShipController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Speed at which player moves")]
    float moveSpeed = 8;

    [SerializeField]
    [Tooltip("Speed at which player moves when focused")]
    float focusSpeed = 3;

    /// <summary>
    /// Time between each shot
    /// </summary>
    [SerializeField]
    [Tooltip("Time between each shot")]
    float shotCooldown = 0.15f;
    bool canShoot = true;

    [SerializeField]
    Transform bulletSpawnZone = null;

    [Header("Object References")]

    [SerializeField]
    ObjectPool bulletPool = null;

    Rigidbody2D rb;

    Animator anim;

    SpriteRenderer hitBox;

    void Awake()
    {
        // Set object references
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        hitBox = transform.GetChild(0).GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Speed and hitbox visibility changes depending on if we're holding the shift key or not
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

        // Moves player based on input
        rb.MovePosition((Vector2)transform.position + input.normalized * speed * Time.fixedDeltaTime);

        // Only do the following if the game is not paused
        if (Time.timeScale > 0)
        {
            // Change animation state depending on input
            anim.SetBool("left", Input.GetKey(KeyCode.LeftArrow));
            anim.SetBool("right", Input.GetKey(KeyCode.RightArrow));

            if (Input.GetKey(KeyCode.Z) && canShoot)
            {
                // Get an inactive bullet from the player bullet pool
                GameObject bullet = bulletPool.GetObject();

                // Place the bullet near the player and enable it
                bullet.transform.position = bulletSpawnZone.position;
                bullet.SetActive(true);

                // Have AudioManager loop the "Shooting" sound if it's not looping already
                if (!AudioManager.IsSoundLooping(SoundsExample2D.Shooting))
                {   
                    AudioManager.PlaySoundLoop(SoundsExample2D.Shooting, transform);
                }

                StartCoroutine(ShotCooldown());
            }
            else if (!Input.GetKey(KeyCode.Z) && AudioManager.IsSoundLooping(SoundsExample2D.Shooting))
            {
                AudioManager.StopSoundLoop(SoundsExample2D.Shooting, transform);
            }
        }
    }

    /// <summary>
    /// Disable ability to shoot for a set amount of time
    /// </summary>
    /// <returns></returns>
    IEnumerator ShotCooldown()
    {
        canShoot = false;
        yield return new WaitForSeconds(shotCooldown);
        canShoot = true;
    }
}
