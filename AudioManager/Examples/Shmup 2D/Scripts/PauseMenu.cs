using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    Canvas pauseMenu;

    // Start is called before the first frame update
    void Start()
    {
        pauseMenu = GetComponent<Canvas>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.enabled = !pauseMenu.enabled;
            if (pauseMenu.enabled) Time.timeScale = 0;
            else Time.timeScale = 1;
        }
    }

    public void CrossfadeMusic()
    {
        AudioManager.GetInstance().CrossfadeMusic("Main Theme", 5, true);
    }

    public void FadeMusic()
    {
        AudioManager.GetInstance().FadeMusic("Main Theme", 5, true);
    }
}
