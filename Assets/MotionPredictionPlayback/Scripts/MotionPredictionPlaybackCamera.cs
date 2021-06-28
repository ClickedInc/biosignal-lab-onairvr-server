using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;

public class MotionPredictionPlaybackCamera : MonoBehaviour {

    public class MatrixArray
    {
        public float left;
        public float right;
        public float bottom;
        public float top;

        public void ChangeElement(float left, float right, float bottom, float top)
        {
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
        }
    }

    private const float RESOLUTION = 1920f / 1080f;

    private double startTime;
    private int playbackRangeFrom = 0;
    private int playbackRangeTo = int.MaxValue;
    private int qf;
    private int captureNum;
    private string csvPath;
    private string captureOutputPath;
    private bool isMotionData;
    private bool isGetStartTime;
    private Camera[] playbackCameras = new Camera[2];
    private CaptureManager captureManager;
    private Camera leftPreviewCamera;
    private Camera leftCaptureCamera;
    private Transform leftTargetTextureAnchor;
    private Transform leftTargetTexture;
    private Camera rightPreviewCamera;
    private Camera rightCaptureCamera;
    private Transform rightTargetTextureAnchor;
    private Transform rightTargetTexture;

    public enum PlaybackState {
        Stopped,
        Playing,
        Capturing
    }
    public PlaybackState playbackState = PlaybackState.Stopped;

    public enum PlaybackMode : uint {

        Predict_NoTimeWarp = 0,
        Predict_TimeWarp,
        NotPredict_NoTimeWarp,
        NotPredict_TimeWarp,

        Max
    }
    private PlaybackMode playbackMode = PlaybackMode.Predict_NoTimeWarp;

    private void Awake() {
        var playback = transform.GetComponentInParent<MotionPredictionPlayback>();

        playbackCameras[0] = transform.Find("TrackingSpace/LeftEyeAnchor").GetComponent<Camera>();
        playbackCameras[1] = transform.Find("TrackingSpace/RightEyeAnchor").GetComponent<Camera>();

        Time.captureFramerate = 60;

        GameObject captureModule = Instantiate(Resources.Load("CaptureModule") as GameObject);
        captureManager = captureModule.GetComponent<CaptureManager>();
        leftPreviewCamera = captureModule.transform.Find("LeftSide/Camera").GetComponent<Camera>();
        leftCaptureCamera = captureModule.transform.Find("LeftSide/Camera/CaptureCamera").GetComponent<Camera>();
        leftTargetTextureAnchor = captureModule.transform.Find("LeftSide/Anchor");
        leftTargetTexture = captureModule.transform.Find("LeftSide/Anchor/TargetTexture");
        rightPreviewCamera = captureModule.transform.Find("RightSide/Camera").GetComponent<Camera>();
        rightCaptureCamera = captureModule.transform.Find("RightSide/Camera/CaptureCamera").GetComponent<Camera>();
        rightTargetTextureAnchor = captureModule.transform.Find("RightSide/Anchor");
        rightTargetTexture = captureModule.transform.Find("RightSide/Anchor/TargetTexture");

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

    private void Start() {
        captureManager.Init(this);
    }

    private void Update() {
        if (playbackState == PlaybackState.Stopped)
            return;

        if (!isGetStartTime)
        {
            startTime = Time.realtimeSinceStartup;
            isGetStartTime = true;
        }

        if (playbackState == PlaybackState.Playing) {
            double updateTimeInterval = Time.realtimeSinceStartup - startTime;
            double timestampStart = (double)CSVReader.ReadLine(playbackRangeFrom)["timestamp"];

            int count = 0;
            double nextTimestampInterval = FlicksToSecond((double)CSVReader.ReadLine(qf + 1)["timestamp"]) - FlicksToSecond(timestampStart);
            while (nextTimestampInterval <= updateTimeInterval) {
                nextTimestampInterval = FlicksToSecond((double)CSVReader.ReadLine(qf + (++count + 1))["timestamp"]) - FlicksToSecond(timestampStart);
            }

            if (count > 0) {
                qf += count;
                StartCoroutine(SimulationControl());
            }
        }
        else if (playbackState == PlaybackState.Capturing)
        {
            StartCoroutine(SimulationControl());
        }
    }

    public float MotionDataFps { get; set; } // assume input motion data rate is 120 fps

    // handle playback & capture control from capture manager
    public delegate void PlaybackStateChangeHandler(MotionPredictionPlaybackCamera sender, PlaybackState state);
    public event PlaybackStateChangeHandler PlaybackStateChanged;

    public delegate void PlaybackCaptureHandler(MotionPredictionPlaybackCamera sender, int frame);
    public event PlaybackCaptureHandler PlaybackCaptured;

    public void ToggleCapture()
    {
        Debug.Assert(playbackState != PlaybackState.Playing);

        if (playbackState == PlaybackState.Stopped)
        {
            playbackState = PlaybackState.Capturing;

            captureManager.Configure(captureOutputPath, leftCaptureCamera.targetTexture);

            captureNum = 0;
            qf = playbackRangeFrom;
            Time.timeScale = 1.0f;
        }
        else
        {
            playbackState = PlaybackState.Stopped;
            Time.timeScale = 0.0f;
            isGetStartTime = false;
        }

        if (PlaybackStateChanged != null)
        {
            PlaybackStateChanged(this, playbackState);
        }
    }

    public void SetInputMotionDataFile(string path)
    {
        csvPath = path;

        if (string.IsNullOrEmpty(path) || File.Exists(path) == false)
        {
            return;
        }

        CSVReader.Init(csvPath);

        CheckTimestampAndSetMotionDataFps();
        CheckMatricData();

        captureManager.SetPlaybackMode(isMotionData);
    }

    public void SetCaptureOutputPath(string path)
    {
        captureOutputPath = path;
    }

    public void SetPlaybackMode(PlaybackMode mode)
    {
        playbackMode = mode;
    }

    public void SetPlaybackRangeFrom(int value)
    {
        playbackRangeFrom = value;
    }

    public void SetPlaybackRangeTo(int value)
    {
        playbackRangeTo = value;
    }

    public void TogglePlay()
    {
        Debug.Assert(playbackState != PlaybackState.Capturing);

        if (playbackState == PlaybackState.Stopped)
        {
            playbackState = PlaybackState.Playing;

            qf = playbackRangeFrom;
            Time.timeScale = 1.0f;
        }
        else
        {
            playbackState = PlaybackState.Stopped;
            Time.timeScale = 0.0f;
            isGetStartTime = false;
        }

        if (PlaybackStateChanged != null)
        {
            PlaybackStateChanged(this, playbackState);
        }
    }

    static Matrix4x4 MatrixCalculate(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
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

    private double FlicksToSecond(double flicks)
    {
        return flicks / 705600000;
    }

    private void CheckMatricData()
    {
        bool result = CSVReader.GetExistKey("prediction_time");

        isMotionData = result;
    }

    private void CheckTimestampAndSetMotionDataFps()
    {
        int timeStampNum = 0;

        double firstTimeStamp = FlicksToSecond((double)CSVReader.ReadLine(1)["timestamp"]);
        double indexTimeStamp = 0;

        while (indexTimeStamp - firstTimeStamp < 10.0f)
        {
            indexTimeStamp = FlicksToSecond((double)CSVReader.ReadLine(timeStampNum)["timestamp"]);

            timeStampNum++;
        }

        if (timeStampNum >= 700)
        {
            MotionDataFps = 120.0f;
        }
        else
        {
            MotionDataFps = 60.0f;
        }
    }

    private void Simulate(int num, PlaybackMode mode, bool capture) {
        bool usePredict = mode == PlaybackMode.Predict_NoTimeWarp || mode == PlaybackMode.Predict_TimeWarp;
        bool useTimeWarp = mode == PlaybackMode.NotPredict_TimeWarp || mode == PlaybackMode.Predict_TimeWarp;

        var leftEyePosQF = Vector3.zero;
        var rightEyePosQF = Vector3.zero;
        var leftEyePosQH = Vector3.zero;
        var rightEyePosQH = Vector3.zero;
        Quaternion rotationQF = Quaternion.identity;
        MatrixArray projectionQF = new MatrixArray();
        Quaternion rotationQH = Quaternion.identity;
        MatrixArray projectionQH = new MatrixArray();

        int qh = 0;

        if (isMotionData) {
            double currentTimeStamp = FlicksToSecond((double)CSVReader.ReadLine(qf)["timestamp"]);
            double accumulatedTimeStampInterval = 0;
            int frame = 0;

            while (accumulatedTimeStampInterval < 0.1f)
            {
                if (qf + frame >= CSVReader.lineLength - 3)
                    return;

                frame++;

                accumulatedTimeStampInterval = FlicksToSecond((double)CSVReader.ReadLine(qf + frame)["timestamp"]) - currentTimeStamp;
            }
            qh = qf + frame;
        }
        else {
            qh = qf;
        }

        if (getMotionData(usePredict, qf, ref leftEyePosQF, ref rightEyePosQF, ref rotationQF, ref projectionQF) == false ||
            getMotionData(false, qh, ref leftEyePosQH, ref rightEyePosQH, ref rotationQH, ref projectionQH) == false) {
            return;
        }

        modifyCameraProjectionAndTexture(leftEyePosQF, rightEyePosQF, rotationQF, projectionQF, rotationQH, projectionQH, useTimeWarp);

        if (capture) {
            foreach (var cam in playbackCameras) {
                cam.Render();
            }

            leftCaptureCamera.Render();
            rightCaptureCamera.Render();

            if (qf <= playbackRangeTo) {
                captureManager.CaptureScreenshot((double)CSVReader.ReadLine(qf)["timestamp"], playbackModeDescription(mode), num);
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
            Simulate(captureNum, playbackMode, true);

            //for (uint mode = 0; mode < (uint)PlaybackMode.Max; mode++) {
            //    Simulate(captureNum, (PlaybackMode)mode, true);
            //}

            if (PlaybackCaptured != null) {
                PlaybackCaptured(this, captureNum);
            }
        }

        captureNum++;
        qf++;

        if (qf >= CSVReader.lineLength - 4 || qf > playbackRangeTo) {
            playbackState = PlaybackState.Stopped;
            Time.timeScale = 0.0f;
            isGetStartTime = false;

            PlaybackStateChanged(this, playbackState);
        }
    }

    private bool getMotionData(bool isPredict, int dataNum, ref Vector3 leftEyePos, ref Vector3 rightEyePos, ref Quaternion orientation, ref MatrixArray projection) {
        if (dataNum >= CSVReader.lineLength - 1) {
            return false;
        }

        string leftEyePosPrefix = isPredict ? "predicted_left_eye_position_" : "input_left_eye_position_";
        string rightEyePosPrefix = isPredict ? "predicted_right_eye_position_" : "input_right_eye_position_";
        string orientationPrefix = isPredict ? "predicted_head_orientation_" : "input_head_orientation_";
        string projectionPreix = isPredict ? "predicted_camera_projection_" : "input_camera_projection_";

        var line = CSVReader.ReadLine(dataNum);

        leftEyePos = new Vector3((float)(double)line[leftEyePosPrefix + "x"],
                                 (float)(double)line[leftEyePosPrefix + "y"],
                                 (float)(double)line[leftEyePosPrefix + "z"]);
        rightEyePos = new Vector3((float)(double)line[rightEyePosPrefix + "x"],
                                  (float)(double)line[rightEyePosPrefix + "y"],
                                  (float)(double)line[rightEyePosPrefix + "z"]);

        orientation.x = -(float)(double)line[orientationPrefix + "x"];
        orientation.y = -(float)(double)line[orientationPrefix + "y"];
        orientation.z = (float)(double)line[orientationPrefix + "z"];
        orientation.w = (float)(double)line[orientationPrefix + "w"];

        projection.ChangeElement((float)(double)line[projectionPreix + "left"],
                                 (float)(double)line[projectionPreix + "right"],
                                 (float)(double)line[projectionPreix + "bottom"],
                                 (float)(double)line[projectionPreix + "top"]
        );
        return true;
    }

    private void modifyCameraProjectionAndTexture(Vector3 leftEyePosQF, Vector3 rightEyePosQF, Quaternion rotationQF, MatrixArray projectionQF, Quaternion rotationQH, MatrixArray projectionQH, bool useTimeWarp)
    {
        playbackCameras[0].transform.localPosition = leftEyePosQF;
        playbackCameras[1].transform.localPosition = rightEyePosQF;
        playbackCameras[0].transform.localRotation = playbackCameras[1].transform.localRotation = rotationQF;

        leftPreviewCamera.transform.localRotation = rightPreviewCamera.transform.localRotation = useTimeWarp ? rotationQH : rotationQF;
        leftTargetTextureAnchor.localRotation = rightTargetTextureAnchor.localRotation = rotationQF;

        Matrix4x4 playbackCamProjectionMatrix = MatrixCalculate(
           projectionQF.left * playbackCameras[0].nearClipPlane,
           projectionQF.right * playbackCameras[0].nearClipPlane,
           projectionQF.bottom * playbackCameras[0].nearClipPlane,
           projectionQF.top * playbackCameras[0].nearClipPlane,
           playbackCameras[0].nearClipPlane,
           playbackCameras[0].farClipPlane
           );

        Matrix4x4 timewarpCamProjectionMatrix = MatrixCalculate(
           projectionQH.left * leftPreviewCamera.nearClipPlane,
           projectionQH.right * leftPreviewCamera.nearClipPlane,
           projectionQH.bottom * leftPreviewCamera.nearClipPlane,
           projectionQH.top * leftPreviewCamera.nearClipPlane,
           leftPreviewCamera.nearClipPlane,
           leftPreviewCamera.farClipPlane
           );

        playbackCameras[0].projectionMatrix = playbackCamProjectionMatrix;
        playbackCameras[1].projectionMatrix = playbackCamProjectionMatrix;

        leftPreviewCamera.projectionMatrix = timewarpCamProjectionMatrix;
        leftCaptureCamera.projectionMatrix = timewarpCamProjectionMatrix;

        rightPreviewCamera.projectionMatrix = timewarpCamProjectionMatrix;
        rightCaptureCamera.projectionMatrix = timewarpCamProjectionMatrix;

        Vector2 center = new Vector2(projectionQF.left + projectionQF.right / 2, projectionQF.top + projectionQF.bottom / 2);

        float rpWidth = projectionQF.right - projectionQF.left;
        float rpHeight = projectionQF.top - projectionQF.bottom;

        float epWidth = 2 * RESOLUTION;
        float epHeight = 2 * RESOLUTION;

        leftTargetTexture.localScale = new Vector3(
            epWidth,
            epHeight,
            1
            );

        rightTargetTexture.localScale = new Vector3(
            epWidth,
            epHeight,
            1
            );

        leftTargetTexture.localPosition = new Vector3(
            (projectionQF.right + projectionQF.left) / 2,
            (projectionQF.top + projectionQF.bottom) / 2,
            1.0f
            );

        rightTargetTexture.localPosition = new Vector3(
            (projectionQF.right + projectionQF.left) / 2,
            (projectionQF.top + projectionQF.bottom) / 2,
            1.0f
            );

        playbackCameras[0].rect = new Rect(
            0.5f - (rpWidth / epWidth) / 2,
            0.5f - (rpHeight / epHeight) / 2,
            rpWidth / epWidth,
            rpHeight / epHeight
            );

        playbackCameras[1].rect = new Rect(
            0.5f - (rpWidth / epWidth) / 2,
            0.5f - (rpHeight / epHeight) / 2,
            rpWidth / epWidth,
            rpHeight / epHeight
            );

        playbackCameras[0].targetTexture.Release();
        playbackCameras[1].targetTexture.Release();
    }

}
