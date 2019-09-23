using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoopPointExample : MonoBehaviour
{
    bool active;

    public void ToggleLoopPoints()
    {
        active = !active;
        AudioManager.instance.SetLoopPoints(active);
    }
}
