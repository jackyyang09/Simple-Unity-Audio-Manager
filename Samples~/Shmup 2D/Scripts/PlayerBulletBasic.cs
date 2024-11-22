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
            if (other.attachedRigidbody)
            {
#if UNITY_2019_2_OR_NEWER
                if (other.attachedRigidbody.TryGetComponent(out BaseBullet b)) return;
#else
                if (other.attachedRigidbody.GetComponent<BaseBullet>()) return;
#endif
            }
            //gameObject.SetActive(false);
        }
    }
}