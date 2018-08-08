using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CaptureManager : MonoBehaviour {

    public static int captureNum;

    public static string ScreenShotName(float time,string message,string path,int num)
    {
        string filename = string.Format("{0}_{1}_{2}.png",
                                        num,
                                        time,
                                        message
                                        );
        return Path.Combine(path, filename);
    }

    public void CaptureScreenshot(float time, string path, string message, int num)
    {
        if (path != string.Empty)
        {
            System.IO.Directory.CreateDirectory(path);
        }

        Texture2D screenImage = new Texture2D(Screen.width, Screen.height);
        screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenImage.Apply();
        byte[] imageBytes = screenImage.EncodeToPNG();

        System.IO.File.WriteAllBytes(ScreenShotName(time, message, path, num), imageBytes);

        Destroy(screenImage);
    }
}
