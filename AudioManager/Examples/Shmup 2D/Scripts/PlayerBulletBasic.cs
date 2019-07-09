using UnityEngine;
using System.Collections;

public class PlayerBulletBasic : MonoBehaviour {

    [SerializeField]
    float bulletSpeed = 50;
    float speed;
    bool isAlive;

    [SerializeField]
    float maxLifeTime = 1;
    float lifeTime;

	void OnEnable () {
        isAlive = true;
        speed = bulletSpeed;
        lifeTime = maxLifeTime;
        StartCoroutine("Move");
        StartCoroutine("DeathTimer");
	}
	
    IEnumerator Move() {
        do {
            transform.Translate(Vector2.up * speed * Time.deltaTime);
            yield return null;
        } while (isAlive);
    }

    IEnumerator DeathTimer() {
        do {
            lifeTime -= Time.deltaTime;

            if (lifeTime <= 0) {
                isAlive = false;
            }
            yield return null;
        } while (isAlive);

        gameObject.SetActive(false);
    }

    public void SetSpeed(float s)
    {
        speed = s;
    }

    public void SetLifeTime(float time)
    {
        lifeTime = time;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Highly recommended that you replace this if-statement with a physics check with objects on the same layer as itself
        // Set it's layer to a new "Bullet" layer
        if (!other.GetComponent<PlayerBulletBasic>())
        {
            gameObject.SetActive(false);
        }
    }
}