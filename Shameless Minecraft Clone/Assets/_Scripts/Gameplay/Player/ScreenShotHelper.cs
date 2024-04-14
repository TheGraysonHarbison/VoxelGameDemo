using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class ScreenShotHelper : MonoBehaviour
{
    public string pathToFolder = "Assets/Textures/ScreenCaptures";

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F2))
        {
            string time = System.DateTime.Now.ToString().ReplaceInvalidFileNameCharacters();
            time.Replace("/", "-");
            time.Replace("\\", "-");

            string path = pathToFolder + "/Capture" + time + ".png";
            ScreenCapture.CaptureScreenshot(path);

            print("Got Capture At" + path);
        }
#endif
    }
}
