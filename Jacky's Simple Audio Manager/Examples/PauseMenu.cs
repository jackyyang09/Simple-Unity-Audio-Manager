using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM;

public class PauseMenu : MonoBehaviour
{
    /// <summary>
    /// Button used to toggle the pause menu, incompatible with Unity's new input manager
    /// </summary>
    [Tooltip("Button used to toggle the pause menu, incompatible with Unity's new input manager")]
    [SerializeField]
    KeyCode toggleButton = KeyCode.Escape;

    Canvas pauseMenu;

    // Start is called before the first frame update
    void Awake()
    {
        pauseMenu = GetComponent<Canvas>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleButton))
        {
            pauseMenu.enabled = !pauseMenu.enabled;
            if (pauseMenu.enabled)
            {
                Time.timeScale = 0;
            }
            else Time.timeScale = 1;
        }

        if (pauseMenu.enabled)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
            {
                Time.timeScale = 0;
                // Sometimes the user has custom cursor locking code
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    /// <summary>
    /// Sample method that gets called from the sample button in the Pause Menu hierarchy
    /// </summary>
    public void FadeMusic()
    {
        //AudioManager.instance.FadeMusic("Main Theme Combined", 5);
    }

    public void UpdateSoundVolume(UnityEngine.UI.Slider uiElement)
    {
        AudioManager.SetSoundVolume(uiElement.value);
    }

    public void UpdateMusicVolume(UnityEngine.UI.Slider uiElement)
    {
        AudioManager.SetMusicVolume(uiElement.value);
    }
}
