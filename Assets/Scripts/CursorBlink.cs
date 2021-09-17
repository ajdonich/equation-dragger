using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorBlink : MonoBehaviour
{
    public static Color onColor = new Color32(50,50,50,255);

    public float OnTime = 0.6f;
    public float OffTime = 0.5f;

    private bool isOn = true;
    private float elapsed = 0.0f;

    private void Update()
    {
        if (elapsed < (isOn ? OnTime : OffTime)) elapsed += Time.deltaTime;
        else {
            TermFactory.getTextComp(gameObject).color = (isOn ? Color.clear : CursorBlink.onColor);
            elapsed = 0.0f;
            isOn = !isOn;
        }
    }
}
