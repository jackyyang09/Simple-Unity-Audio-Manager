using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSAM; // Using the namespace to make things easier to type out

public class LoopPointExample : MonoBehaviour
{
    bool active;
    AudioSource sourceToTrack;

    [SerializeField]
    UnityEngine.UI.Slider progressSlider = null;

    [SerializeField]
    UnityEngine.UI.Text buttonText = null;

    private void Start()
    {
        active = true;
        sourceToTrack = GetComponent<AudioPlayerMusic>().GetAudioSource();
    }

    public void ToggleLoopPoints()
    {
        active = !active;
        AudioManager.instance.SetLoopPoints(active);
        if (active)
        {
            buttonText.text = "Disable Loop Points";
        }
        else
        {
            buttonText.text = "Enable Loop Points";
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            sourceToTrack.time -= 5;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            sourceToTrack.time += 5;
        }
        progressSlider.value = sourceToTrack.time / sourceToTrack.clip.length;
    }
}
