using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{


    public GameObject checkImage;
    private bool pressed=true;
    public bool Pressed
    {
        get { return pressed; }
        set { pressed = value; }
    }
    public void Press()
    {
        pressed = !pressed;
        if (pressed) checkImage.SetActive(true); else checkImage.SetActive(false);
    }

}
