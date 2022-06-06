using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeScaleUI : MonoBehaviour
{
    [SerializeField]
    float timeIncrement = 0.25f;

    [SerializeField]
    Slider uiSlider = null;

    [SerializeField]
    Text uiText = null;

    //// Start is called before the first frame update
    void Start()
    {
        UpdateUI(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Time.timeScale = Mathf.Clamp(Time.timeScale + timeIncrement, 0, 2);
            UpdateUI();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Time.timeScale = Mathf.Clamp(Time.timeScale - timeIncrement, 0, 2);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        uiSlider.value = Mathf.InverseLerp(0, 2, Time.timeScale);
        uiText.text = "TimeScale: " + Time.timeScale;
    }
}
