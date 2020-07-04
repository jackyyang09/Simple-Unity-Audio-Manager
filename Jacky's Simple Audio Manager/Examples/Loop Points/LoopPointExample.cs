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
        if (sourceToTrack == null)
        {
            sourceToTrack = GetComponent<AudioPlayerMusic>().GetAudioSource();
        }
        if (sourceToTrack != null)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                sourceToTrack.time = Mathf.Clamp(sourceToTrack.time - 5f, 0, sourceToTrack.clip.length - 0.01f);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                sourceToTrack.time = Mathf.Clamp(sourceToTrack.time + 5f, 0, sourceToTrack.clip.length - 0.01f);
            }
            progressSlider.value = sourceToTrack.time / sourceToTrack.clip.length;
        }
    }
}
