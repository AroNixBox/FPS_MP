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
    [Command]
    private void Spawn(string objectName)
    {
        GameObject prefab = Resources.Load<GameObject>(objectName);
        if(prefab)
        {
            Instantiate(prefab, Vector3.zero, Quaternion.identity);
            Debug.Log($"Spawned {objectName}!");
        }
        else
        {
            Debug.LogError($"No prefab named {objectName} found!");
        }
    }

}
