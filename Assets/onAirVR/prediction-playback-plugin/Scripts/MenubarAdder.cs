using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MenubarAdder : MonoBehaviour {

    public static bool isCapturemode;

    [MenuItem("ExportPlugin/Export composite_Plugin!!")]
    public static void Exporting()
    {
        string prefabfilename = "Assets/prediction_playback-plugin";

        string exportPath = EditorUtility.SaveFilePanel("Export prediction-playback-plugin", "", "prediction-playback-plugin", "unitypackage");

        AssetDatabase.ExportPackage(prefabfilename, exportPath,
        ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);

        EditorUtility.DisplayDialog("Congratulation!", "The package is exported successfully.", "OK.");
    }

    [MenuItem("onAirVR/Select Play Mode")]
    public static void UsePlayMode()
    {
        GameObject playbackCamera = GameObject.Find("PlaybackCamera(Clone)");
        GameObject samplePlayer = GameObject.Find("AirVRSamplePlayer");
        GameObject airVRCameraRig = GameObject.Find("AirVRCameraRig");
        
        if (playbackCamera != null)
        {
            airVRCameraRig.GetComponent<AirVRStereoCameraRig>().enabled = true;
            airVRCameraRig.transform.position = samplePlayer.transform.position;
            Object.DestroyImmediate(playbackCamera);
        }

        EditorApplication.isPlaying = true;
    }

    [MenuItem("onAirVR/Select Capture Mode")]
    public static void UseCaptureMode()
    {
        GameObject playbackCamera = GameObject.Find("PlaybackCamera(Clone)");
        GameObject airVRCameraRig = GameObject.Find("AirVRCameraRig");
        GameObject samplePlayer = GameObject.Find("AirVRSamplePlayer");

        if (playbackCamera == null)
        {
            airVRCameraRig.GetComponent<AirVRStereoCameraRig>().enabled = false;
            playbackCamera = Instantiate(Resources.Load("PlaybackCamera") as GameObject);
            playbackCamera.transform.parent = samplePlayer.transform;
            playbackCamera.transform.position = samplePlayer.transform.position;
        }

        EditorApplication.isPlaying = true;
    }

}
