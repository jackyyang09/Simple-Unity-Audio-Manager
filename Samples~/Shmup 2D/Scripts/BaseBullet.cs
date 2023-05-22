using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBullet : MonoBehaviour
{
    [SerializeField] protected float bulletSpeed = 50;

    [SerializeField] protected float lifeTime = 1;
    protected float lifeTimer;

    protected bool isAlive;

    private void OnEnable()
    {
        isAlive = true;
        StartCoroutine(Move());
        StartCoroutine(TickLifeTimer());
    }

    private void Update()
    {
        if (!isAlive)
        {
            gameObject.SetActive(false);
        }
    }

    protected IEnumerator Move()
    {
        while (isAlive)
        {
            transform.position += (transform.up * bulletSpeed * Time.deltaTime);
            yield return null;
        }
    }

    protected IEnumerator TickLifeTimer()
    {
        lifeTimer = lifeTime;
        while (isAlive)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0)
            {
                isAlive = false;
            }
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TriggerEnter(other);
    }

    protected abstract void TriggerEnter(Collider2D other);
}
