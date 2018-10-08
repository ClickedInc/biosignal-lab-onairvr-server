using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventWifhFloat : UnityEvent<float> { }

public class MotionPredictionPlayback : MonoBehaviour {
    //public string csvPath = "Assets/MotionPredictionPlayback/test.csv";
    //public string captureOutputPath = "CaptureOutput";

    private MotionPredictionPlaybackCamera _playbackCamera;

    public UnityEventWifhFloat onPlayPreview;
    public UnityEvent onStartCapture;
    public UnityEvent onStop;
    public UnityEventWifhFloat onSeek;

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

        if (FindObjectOfType<AudioListener>() == null) {
            playbackCamera.AddComponent<AudioListener>();
        }

        _playbackCamera = playbackCamera.GetComponent<MotionPredictionPlaybackCamera>();
    }

    private void Start() {
        if (playbackModeStartedByEditor == false) {
            return;
        }

        _playbackCamera.PlaybackStateChanged += playbackStateChanged;
        _playbackCamera.PlaybackCaptured += playbackCaptured;
    }

    private void OnDestroy() {
        if (playbackModeStartedByEditor == false) {
            return;
        }

        _playbackCamera.PlaybackStateChanged -= playbackStateChanged;
        _playbackCamera.PlaybackCaptured -= playbackCaptured;
    }

    private void playbackStateChanged(MotionPredictionPlaybackCamera sender, MotionPredictionPlaybackCamera.PlaybackState state) {
        if (state == MotionPredictionPlaybackCamera.PlaybackState.Playing) {
            onPlayPreview.Invoke(_playbackCamera.motionDataFps);
        }
        else if (state == MotionPredictionPlaybackCamera.PlaybackState.Capturing) {
            onStartCapture.Invoke();
        }
        else {
            onStop.Invoke();
        }
    }

    private void playbackCaptured(MotionPredictionPlaybackCamera sender, int frame) {
        onSeek.Invoke(frame / _playbackCamera.motionDataFps);
    }
}