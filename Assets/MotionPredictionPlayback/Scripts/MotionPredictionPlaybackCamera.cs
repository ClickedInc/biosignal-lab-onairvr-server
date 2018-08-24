using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class MotionPredictionPlaybackCamera : MonoBehaviour {
    private int playbackRangeFrom = 0;
    private int playbackRangeTo = int.MaxValue;
    private int qf;
    private int captureNum;
    private string csvPath;
    private string captureOutputPath;
    private List<Dictionary<string, object>> data;
    private Camera[] playbackCameras = new Camera[2];
    private CaptureManager captureManager;
    private Camera leftPreviewCamera;
    private Camera leftCaptureCamera;
    private Transform leftTargetTextureAnchor;
    private Camera rightPreviewCamera;
    private Camera rightCaptureCamera;
    private Transform rightTargetTextureAnchor;

    public enum PlaybackState {
        Stopped,
        Playing,
        Capturing
    }
    private PlaybackState playbackState = PlaybackState.Stopped;

    public enum PlaybackMode : uint {
        NotPredict_NoTimeWarp = 0,
        NotPredict_TimeWarp,
        Predict_NoTimeWarp,
        Predict_TimeWarp,

        Max
    }
    private PlaybackMode playbackMode = PlaybackMode.Predict_TimeWarp;

    private void Awake() {
        Init();
    }

    private void Start() {
        captureManager.Init(this);
    }

    private void Update() {
        StartCoroutine(SimulationControl());
    }

    private int LatencyFrameCirculate(double latencyTime, int inputFramerate) {
        return (int)(latencyTime * inputFramerate / 1000);
    }

    private string playbackModeDescription(PlaybackMode mode) {
        switch (mode) {
            case PlaybackMode.NotPredict_NoTimeWarp:
                return "NotPredict_NoTimeWarp";
            case PlaybackMode.NotPredict_TimeWarp:
                return "NotPredict_TimeWarp";
            case PlaybackMode.Predict_NoTimeWarp:
                return "Predict_NoTimeWarp";
            case PlaybackMode.Predict_TimeWarp:
                return "Predict_TimeWarp";
        }
        Debug.Assert(false);
        return "Unknown";
    }

    private void Simulate(int num, PlaybackMode mode, bool capture) {
        bool usePredict = mode == PlaybackMode.Predict_NoTimeWarp || mode == PlaybackMode.Predict_TimeWarp;
        bool useTimeWarp = mode == PlaybackMode.NotPredict_TimeWarp || mode == PlaybackMode.Predict_TimeWarp;

        Quaternion rotationQF = Quaternion.identity;
        Quaternion rotationQH = Quaternion.identity;

        int qh = qf + LatencyFrameCirculate((double)data[qf]["prediction_time"], 120);  // assume input motion data rate is 120 fps
        if (qh >= data.Count) {
            qh = data.Count - 1;
        }

        if (parseRotateDataSetting(usePredict, qf, ref rotationQF) == false ||
            parseRotateDataSetting(false, qh, ref rotationQH) == false) {
            return;
        }

        transform.rotation = rotationQF;
        leftPreviewCamera.transform.localRotation = rightPreviewCamera.transform.localRotation = useTimeWarp ? rotationQH : rotationQF;
        leftTargetTextureAnchor.localRotation = rightTargetTextureAnchor.localRotation = rotationQF;

        if (capture) {
            foreach (var cam in playbackCameras) {
                cam.Render();
            }

            leftCaptureCamera.Render();
            rightCaptureCamera.Render();

            if (qf <= playbackRangeTo) {
                captureManager.CaptureScreenshot((double)data[qf]["timestamp"], playbackModeDescription(mode), num);
            }
        }
    }

    private IEnumerator SimulationControl() {
        yield return new WaitForEndOfFrame();

        if (playbackState == PlaybackState.Stopped) {
            yield break;
        }

        if (playbackState == PlaybackState.Playing) {
            Simulate(captureNum, playbackMode, false);
        }
        else if (playbackState == PlaybackState.Capturing) {
            for (uint mode = 0; mode < (uint)PlaybackMode.Max; mode++) {
                Simulate(captureNum, (PlaybackMode)mode, true);
            }
        }

        captureNum++;
        qf++;

        if (qf >= data.Count || qf > playbackRangeTo) {
            playbackState = PlaybackState.Stopped;
            Time.timeScale = 0.0f;

            if (PlaybackStateChanged != null) {
                PlaybackStateChanged(this, playbackState);
            }
        }
    }

    private bool parseRotateDataSetting(bool isPredict, int dataNum, ref Quaternion result) {
        if (dataNum >= data.Count) {
            return false;
        }

        string keyPrefix = isPredict ? "predicted_orientation_" : "input_orientation_";

        result.x = (float)(double)data[dataNum][keyPrefix + "x"];
        result.y = (float)(double)data[dataNum][keyPrefix + "y"];
        result.z = (float)(double)data[dataNum][keyPrefix + "z"];
        result.w = (float)(double)data[dataNum][keyPrefix + "w"];
        return true;
    }

    private void Init() {
        var playback = transform.GetComponentInParent<MotionPredictionPlayback>();

        //this.csvPath = playback.csvPath;
        //this.captureOutputPath = playback.captureOutputPath;

        playbackCameras[0] = transform.Find("LeftCam").GetComponent<Camera>();
        playbackCameras[1] = transform.Find("RightCam").GetComponent<Camera>();

        Time.captureFramerate = 60;

        GameObject captureModule = Instantiate(Resources.Load("CaptureModule") as GameObject);
        captureManager = captureModule.GetComponent<CaptureManager>();
        leftPreviewCamera = captureModule.transform.Find("LeftSide/Camera").GetComponent<Camera>();
        leftCaptureCamera = captureModule.transform.Find("LeftSide/Camera/CaptureCamera").GetComponent<Camera>();
        leftTargetTextureAnchor = captureModule.transform.Find("LeftSide/Anchor");
        rightPreviewCamera = captureModule.transform.Find("RightSide/Camera").GetComponent<Camera>();
        rightCaptureCamera = captureModule.transform.Find("RightSide/Camera/CaptureCamera").GetComponent<Camera>();
        rightTargetTextureAnchor = captureModule.transform.Find("RightSide/Anchor");

        PlayerSettings.defaultScreenWidth = 2048;
        PlayerSettings.defaultScreenHeight = 1024;

        captureModule.transform.position = Vector3.down * 1000.0f;
        leftPreviewCamera.aspect = rightPreviewCamera.aspect =
            leftCaptureCamera.aspect = rightCaptureCamera.aspect = 1.0f;

        // apply GearVR head model
        playbackCameras[0].transform.localPosition = new Vector3(-0.032f, 0.097f, 0.0805f);
        playbackCameras[1].transform.localPosition = new Vector3(0.032f, 0.097f, 0.0805f);

        Time.timeScale = 0.0f;
    }


    // handle playback & capture control from capture manager
    public delegate void PlaybackStateChangeHandler(MotionPredictionPlaybackCamera sender, PlaybackState state);
    public event PlaybackStateChangeHandler PlaybackStateChanged;

    public void SetInputMotionDataFile(string path) {
        csvPath = path;

        if (string.IsNullOrEmpty(path)) {
            return;
        }

        try {
            data = CSVReader.Read(csvPath);
        }
        catch (Exception e) {
            Debug.Assert(false, "[Motion Prediction Playback] failed to read the input motion data file: " + path);
            Debug.Assert(false, e.ToString());
        }
    }

    public void SetCaptureOutputPath(string path) {
        captureOutputPath = path;
    }

    public void SetPlaybackMode(PlaybackMode mode) {
        playbackMode = mode;
    }

    public void SetPlaybackRangeFrom(int value) {
        playbackRangeFrom = value;
    }

    public void SetPlaybackRangeTo(int value) {
        playbackRangeTo = value;
    }

    public void TogglePlay() {
        Debug.Assert(playbackState != PlaybackState.Capturing);

        if (playbackState == PlaybackState.Stopped) {
            playbackState = PlaybackState.Playing;

            qf = playbackRangeFrom;
            Time.timeScale = 1.0f;
        }
        else {
            playbackState = PlaybackState.Stopped;
            Time.timeScale = 0.0f;
        }

        if (PlaybackStateChanged != null) {
            PlaybackStateChanged(this, playbackState);
        }
    }

    public void ToggleCapture() {
        Debug.Assert(playbackState != PlaybackState.Playing);

        if (playbackState == PlaybackState.Stopped) {
            playbackState = PlaybackState.Capturing;

            captureManager.Configure(captureOutputPath, leftCaptureCamera.targetTexture);

            captureNum = 0;
            qf = playbackRangeFrom;
            Time.timeScale = 1.0f;
        }
        else {
            playbackState = PlaybackState.Stopped;
            Time.timeScale = 0.0f;
        }

        if (PlaybackStateChanged != null) {
            PlaybackStateChanged(this, playbackState);
        }
    }
}
