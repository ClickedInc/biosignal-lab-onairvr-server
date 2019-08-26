using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

    private double updateTimeInterval;
    private double lastUpdateTime;
    private int playbackRangeFrom = 0;
    private int playbackRangeTo = int.MaxValue;
    private int qf;
    private int captureNum;
    private string csvPath;
    private string captureOutputPath;
    private bool isMotionData;
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
    private PlaybackState playbackState = PlaybackState.Stopped;

    public enum PlaybackMode : uint {

        Predict_NoTimeWarp = 0,
        Predict_TimeWarp,
        NotPredict_NoTimeWarp,
        NotPredict_TimeWarp,

        Max
    }
    private PlaybackMode playbackMode = PlaybackMode.Predict_TimeWarp;

    private void Awake() {
        var playback = transform.GetComponentInParent<MotionPredictionPlayback>();

        playbackCameras[0] = transform.Find("LeftCam").GetComponent<Camera>();
        playbackCameras[1] = transform.Find("RightCam").GetComponent<Camera>();

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

        lastUpdateTime = Time.realtimeSinceStartup;
    }

    private void Update() {
        if (playbackState != PlaybackState.Playing)
            return;

        double timeStampInterval = (FlicksToSecond((double)CSVReader.ReadLine(qf + 1)["timestamp"]) - FlicksToSecond((double)CSVReader.ReadLine(qf)["timestamp"]));

        updateTimeInterval += Time.realtimeSinceStartup - lastUpdateTime;
        lastUpdateTime = Time.realtimeSinceStartup;

        if (timeStampInterval <= updateTimeInterval)
        {
            double timeStampInterval2 = (FlicksToSecond((double)CSVReader.ReadLine(qf + 2)["timestamp"]) - FlicksToSecond((double)CSVReader.ReadLine(qf)["timestamp"]));

            double t1 = Math.Abs(updateTimeInterval - timeStampInterval);
            double t2 = Math.Abs(updateTimeInterval - timeStampInterval2);

            if (t1 > t2)
            {
                qf += 1;
            }

            updateTimeInterval = 0;

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
        }

        if (PlaybackStateChanged != null)
        {
            PlaybackStateChanged(this, playbackState);
        }
    }

    public void SetInputMotionDataFile(string path)
    {
        csvPath = path;

        if (string.IsNullOrEmpty(path))
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

    private float FlicksToSecond(double flicks)
    {
        return (float)flicks * 1 / 705600000;
    }

    private void CheckMatricData()
    {
        bool result = CSVReader.GetExistKey("prediction_time");

        isMotionData = result;
    }

    private void CheckTimestampAndSetMotionDataFps()
    {
        int timeStampNum = 0;

        double firstTimeStamp = FlicksToSecond((double)CSVReader.ReadLine(0)["timestamp"]);
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

        if (getMotionData(usePredict, qf, ref rotationQF, ref projectionQF) == false ||
            getMotionData(false, qh, ref rotationQH, ref projectionQH) == false) {
            return;
        }

        modifyCameraProjectionAndTexture(rotationQF, projectionQF, rotationQH, projectionQH, useTimeWarp);

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

            PlaybackStateChanged(this, playbackState);
        }
    }

    private bool getMotionData(bool isPredict, int dataNum, ref Quaternion orientation, ref MatrixArray projection) {
        if (dataNum >= CSVReader.lineLength - 1) {
            return false;
        }

        string orientationPrefix = isPredict ? "predicted_orientation_" : "input_orientation_";
        string projectionPreix = isPredict ? "predicted_projection_" : "input_projection_";

        orientation.x = -(float)(double)CSVReader.ReadLine(dataNum)[orientationPrefix + "x"];
        orientation.y = -(float)(double)CSVReader.ReadLine(dataNum)[orientationPrefix + "y"];
        orientation.z = (float)(double)CSVReader.ReadLine(dataNum)[orientationPrefix + "z"];
        orientation.w = (float)(double)CSVReader.ReadLine(dataNum)[orientationPrefix + "w"];

        projection.ChangeElement(
            (float)(double)CSVReader.ReadLine(dataNum)[projectionPreix + "left"],
            (float)(double)CSVReader.ReadLine(dataNum)[projectionPreix + "right"],
            (float)(double)CSVReader.ReadLine(dataNum)[projectionPreix + "bottom"],
            (float)(double)CSVReader.ReadLine(dataNum)[projectionPreix + "top"]
        );
        return true;
    }

    private void modifyCameraProjectionAndTexture(Quaternion rotationQF, MatrixArray projectionQF, Quaternion rotationQH, MatrixArray projectionQH, bool useTimeWarp)
    {
        transform.rotation = rotationQF;
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
            0.5f - (rpWidth / epHeight) / 2,
            rpWidth / epWidth,
            rpHeight / epHeight
            );

        playbackCameras[1].rect = new Rect(
            0.5f - (rpWidth / epWidth) / 2,
            0.5f - (rpWidth / epHeight) / 2,
            rpWidth / epWidth,
            rpHeight / epHeight
            );

        playbackCameras[0].targetTexture.Release();
        playbackCameras[1].targetTexture.Release();
    }

}
