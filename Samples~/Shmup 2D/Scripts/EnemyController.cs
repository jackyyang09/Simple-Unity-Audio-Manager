using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM.Example.Shmup2D
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] float rotateSpeed;

        [SerializeField] int spawnAmount;

        [SerializeField] float bulletDelay;
        [SerializeField] float spawnDelay;

        [SerializeField] ObjectPool pool;

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(SpawnBehaviour());
        }

        // Update is called once per frame
        void Update()
        {
            transform.eulerAngles += new Vector3(0, 0, -rotateSpeed * Time.deltaTime);
        }

        IEnumerator SpawnBehaviour()
        {
            while (true)
            {
                var angle = 360f / (float)spawnAmount;
                for (int i = 0; i < spawnAmount; i++)
                {
                    var bullet = pool.GetObject();
                    if (bullet)
                    {
                        bullet.transform.position = transform.position;
                        bullet.transform.localEulerAngles = transform.eulerAngles + new Vector3(0, 0, i * angle);
                        bullet.SetActive(true);
                        AudioManager.PlaySound(Shmup2DSounds.EnemyShot);
                    }
                    yield return new WaitForSeconds(bulletDelay);
                }
                yield return new WaitForSeconds(spawnDelay);
            }
        }
    }
}