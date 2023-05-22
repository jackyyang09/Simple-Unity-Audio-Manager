using UnityEngine;
using System.Collections;

namespace JSAM.Example.Shmup2D
{
    public class PlayerBulletBasic : BaseBullet
    {
        protected override void TriggerEnter(Collider2D other)
        {
            // Highly recommended that you replace this if-statement with a physics check with objects on the same layer as itself
            // Set it's layer to a new "Bullet" layer
            if (!other.GetComponent<PlayerBulletBasic>())
            {
                gameObject.SetActive(false);
            }
        }
    }
}