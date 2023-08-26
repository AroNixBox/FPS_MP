using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandLibrary : MonoBehaviour
{
    [Command]
    private void TakeScreenshot(string filename)
    {
        ScreenCapture.CaptureScreenshot(filename + ".png");
        Debug.Log($"Screenshot saved as {filename}.png");
    }
}
