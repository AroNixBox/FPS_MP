using System;
using UnityEngine;

namespace SpectrumConsole
{
    public class CommandLibrary : MonoBehaviour
    {
        [SerializeField] private GameObject ConsoleUI;
        private bool _isActive;

        [Command]
        private void TakeScreenshot(string filename)
        {
            HandleActiveStateConsole();
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
        [Command]
        private void SetTimeScale(string value)
        {
            if(float.TryParse(value, out float result))
            {
                Time.timeScale = result;
                Debug.Log($"Time scale set to {result}");
            }
            else
            {
                Debug.LogError("Invalid time scale value.");
            }
        }
        [Command]
        private void ShowSystemInfo()
        {
            try
            {
                Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
                Debug.Log($"CPU: {SystemInfo.processorType}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    
        [Command]
        private void LoadScene(string sceneName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        [Command]
        private void SetFPS(float fps)
        {
            if (fps == -1.0f)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
            }
            else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (int)fps;
            }

            Debug.Log($"FPS limit set to: {(fps == -1.0f ? "Unlimited" : fps.ToString())}");
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.End))
            {
                HandleActiveStateConsole();
            }
        
        }

        private void HandleActiveStateConsole()
        {
            _isActive = !_isActive;
            ConsoleUI.SetActive(_isActive);
        }
    }
}
