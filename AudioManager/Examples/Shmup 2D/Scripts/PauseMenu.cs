using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    Canvas pauseMenu;

    // Start is called before the first frame update
    void Awake()
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

    public void FadeMusic()
    {
        AudioManager.instance.FadeMusic("Main Theme Combined", 5, true);
    }

    public void UpdateSoundVolume(UnityEngine.UI.Slider uiElement)
    {
        AudioManager.instance.SetSoundVolume(uiElement.value);
    }

    public void UpdateMusicVolume(UnityEngine.UI.Slider uiElement)
    {
        AudioManager.instance.SetMusicVolume(uiElement.value);
    }
}
