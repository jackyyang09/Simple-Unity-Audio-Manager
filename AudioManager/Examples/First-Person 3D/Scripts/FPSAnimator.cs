using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSAnimator : MonoBehaviour
{
    [SerializeField]
    int clipSize = 6;
    int bullets;

    Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        bullets = 6;
    }

    // Update is called once per frame
    void Update()
    {
        switch (bullets)
        {
            case 1:
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    anim.SetTrigger("FireFinal");
                }
                break;
            default:
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    anim.SetTrigger("Fire");
                }
                else if (Input.GetKeyDown(KeyCode.R))
                {
                    anim.SetTrigger("Reload");
                }
                break;
        }
    }

    public void ReloadBullets()
    {
        bullets = 6;
    }
}
