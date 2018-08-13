using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionPredictionPlayback : MonoBehaviour {
    //public string csvPath = "Assets/MotionPredictionPlayback/test.csv";
    //public string captureOutputPath = "CaptureOutput";

    [HideInInspector]
    public bool playbackModeStartedByEditor;

    private void Awake() {
        if (playbackModeStartedByEditor == false) {
            return;
        }

        var cameraRig = GetComponentInChildren<AirVRStereoCameraRig>();
        if (cameraRig == null) {
            throw new UnityException("[MotionPredictionPlayback] ERROR: There must exist an instance of AirVRStereoCameraRig in children.");
        }

        cameraRig.gameObject.SetActive(false);

        if (AirVRServer.isInstantiated) {
            throw new UnityException("[MotionPredictionPlayback] ERROR: MotionPredictionPlayback script must be executed before AirVRCameraRig. Please adjust script execution order in the project settings.");
        }

        var playbackCamera = Instantiate(Resources.Load("PlaybackCamera") as GameObject, cameraRig.transform.parent);
        playbackCamera.transform.position = cameraRig.transform.position;
    }
}
