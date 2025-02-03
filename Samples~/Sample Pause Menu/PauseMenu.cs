using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JSAM
{
    public class PauseMenu : MonoBehaviour
    {
        /// <summary>
        /// Button used to toggle the pause menu, incompatible with Unity's new input manager
        /// </summary>
        [Tooltip("Button used to toggle the pause menu, incompatible with Unity's new input manager")]
        [SerializeField]
        KeyCode toggleButton = KeyCode.Escape;

        [SerializeField] VolumeTrack musicTrack;
        [SerializeField] VolumeTrack soundTrack;

        [SerializeField] Canvas pauseMenu;

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

        // Example API for manipulating volume
        // Both the below calls do the same thing, it's up to you whether you prefer to call the AudioManager
        // or make use of Extension methods

        public void SetMusicVolume(float value)
        {
            AudioManager.SetVolume(musicTrack, value);
        }

        public void SetSoundVolume(float value)
        {
            soundTrack.SetVolume(value);
        }

        public void ToggleMusicMute()
        {
            AudioManager.SetMute(musicTrack, !AudioManager.IsMuted(musicTrack));
        }

        public void ToggleSoundMute()
        {
            soundTrack.SetMuted(!soundTrack.IsMuted());
        }
    }
}