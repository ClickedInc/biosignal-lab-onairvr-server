using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class MotionPredictionPlaybackCamera : MonoBehaviour {
    private const float CaptureViewScale = 2.0f;

    public enum Overfilling {
        None,
        Optimal
    }

    public class MatrixArray
    {
        public float left;
        public float right;
        public float bottom;
        public float top;

        public MatrixArray() { }

        public MatrixArray(float l, float t, float r, float b) {
            left = l;
            top = t;
            right = r;
            bottom = b;
        }

        public void ChangeElement(float left, float right, float bottom, float top)
        {
            this.left = left;
            this.right = right;
            this.bottom = bottom;
            this.top = top;
        }
    }

    private const float RESOLUTION = 2f; //1920f / 1080f;

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

    private OCSVRWorksCameraRig _foveatedRenderer;
    private RenderPerfGraph _renderPerfGraph;

    [SerializeField] private Overfilling _overfilling = Overfilling.None;
    [SerializeField] private MeshRenderer _leftVideoTexture;

    public float playbackFrom => playbackRangeFrom / 12800.0f;

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

        captureManager = FindObjectOfType<CaptureManager>();
        leftPreviewCamera = captureManager.transform.Find("LeftSide/Camera").GetComponent<Camera>();
        leftCaptureCamera = captureManager.transform.Find("LeftSide/Camera/CaptureCamera").GetComponent<Camera>();
        leftTargetTextureAnchor = captureManager.transform.Find("LeftSide/Anchor");
        leftTargetTexture = captureManager.transform.Find("LeftSide/Anchor/TargetTexture");
        rightPreviewCamera = captureManager.transform.Find("RightSide/Camera").GetComponent<Camera>();
        rightCaptureCamera = captureManager.transform.Find("RightSide/Camera/CaptureCamera").GetComponent<Camera>();
        rightTargetTextureAnchor = captureManager.transform.Find("RightSide/Anchor");
        rightTargetTexture = captureManager.transform.Find("RightSide/Anchor/TargetTexture");

        PlayerSettings.defaultScreenWidth = 2048;
        PlayerSettings.defaultScreenHeight = 1024;

        captureManager.transform.position = Vector3.down * 1000.0f;
        leftPreviewCamera.aspect = rightPreviewCamera.aspect =
        leftCaptureCamera.aspect = rightCaptureCamera.aspect = 1.0f;

        // apply GearVR head model
        playbackCameras[0].transform.localPosition = new Vector3(-0.032f, 0.097f, 0.0805f);
        playbackCameras[1].transform.localPosition = new Vector3(0.032f, 0.097f, 0.0805f);

        Time.timeScale = 0.0f;

        _foveatedRenderer = GetComponent<OCSVRWorksCameraRig>();
        _renderPerfGraph = FindObjectOfType<RenderPerfGraph>();
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

    public delegate void PlaybackSeekHandler(MotionPredictionPlaybackCamera sender, float startFrom);
    public event PlaybackSeekHandler PlaybackSeek;

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

        PlaybackSeek?.Invoke(this, playbackFrom);
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

            while (accumulatedTimeStampInterval < 0.07f)
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

        var overfilled = calcOverfilling(_overfilling, rotationQH, projectionQH, rotationQF);

        Matrix4x4 playbackCamProjectionMatrix = MatrixCalculate(
           overfilled.left * playbackCameras[0].nearClipPlane,
           overfilled.right * playbackCameras[0].nearClipPlane,
           overfilled.bottom * playbackCameras[0].nearClipPlane,
           overfilled.top * playbackCameras[0].nearClipPlane,
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

        float rpWidth = overfilled.right - overfilled.left;
        float rpHeight = overfilled.top - overfilled.bottom;

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
            (overfilled.right + overfilled.left) / 2,
            (overfilled.top + overfilled.bottom) / 2,
            CaptureViewScale
            );

        rightTargetTexture.localPosition = new Vector3(
            (overfilled.right + overfilled.left) / 2,
            (overfilled.top + overfilled.bottom) / 2,
            CaptureViewScale
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

        ////

        var originalProjHeight = projectionQH.top - projectionQH.bottom;
        var overfillWidth = overfilled.right - overfilled.left;
        var overfillHeight = overfilled.top - overfilled.bottom;
        var overfillAspect = overfillWidth / overfillHeight;

        var areaQH = (projectionQH.right - projectionQH.left) * (projectionQH.top - projectionQH.bottom);
        var areaQF = (overfilled.right - overfilled.left) * (overfilled.top - overfilled.bottom);
        var scaleAdjustment = 1.0f; // 0.9f + (areaQF / areaQH - 1.0f) * 0.4f;

        _foveatedRenderer.UpdateFoveationPattern(originalProjHeight / (overfillAspect >= 1.0f ? overfillHeight : overfillWidth) * scaleAdjustment, 1.0f);

        var leftProj = new Vector4 {
            x = overfilled.left,
            y = overfilled.top,
            z = overfilled.right,
            w = overfilled.bottom
        };
        var rightProj = new Vector4 {
            x = leftProj.x - (projectionQH.left + projectionQH.right),
            y = leftProj.y,
            z = leftProj.z - (projectionQH.left + projectionQH.right),
            w = leftProj.w
        };
        var width = leftProj.z - leftProj.x;
        var height = leftProj.y - leftProj.w;
        var scale = width / height >= 1.0f ? height : width;

        var leftGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -(leftProj.x + leftProj.z) / 2 / scale,
            y = -(leftProj.y + leftProj.w) / 2 / scale
        };
        var rightGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -(rightProj.x + rightProj.z) / 2 / scale,
            y = -(rightProj.y + rightProj.w) / 2 / scale
        };

        _foveatedRenderer.UpdateGazeLocation(leftGaze, leftGaze);

        var mat = _leftVideoTexture.sharedMaterial;
        mat.SetFloat("_InnerRadii", _foveatedRenderer.innerRadii / 1.7f * scaleAdjustment);
        mat.SetFloat("_MidRadii", _foveatedRenderer.midRadii / 1.7f * scaleAdjustment);
        mat.SetFloat("_GazeX", -(overfilled.left + overfilled.right) / 4);
        mat.SetFloat("_GazeY", -(overfilled.top + overfilled.bottom) / 4);
        mat.SetVector("_Bound", new Vector4(-rpWidth / 2, rpHeight / 2, rpWidth / 2, -rpHeight / 2));

        measureRenderPerfs(rotationQH, projectionQH, rotationQF, _foveatedRenderer.innerRadii * scaleAdjustment, _foveatedRenderer.midRadii * scaleAdjustment);
    }

    private MatrixArray calcOverfilling(Overfilling overfilling, Quaternion rotationQH, MatrixArray projectionQH, Quaternion rotationQF) {
        if (overfilling == Overfilling.Optimal) {
            var q_delta = Quaternion.Inverse(rotationQF) * rotationQH;

            var p_lt = q_delta * new Vector3(projectionQH.left, projectionQH.top, 1.0f); p_lt /= p_lt.z;
            var p_rt = q_delta * new Vector3(projectionQH.right, projectionQH.top, 1.0f); p_rt /= p_rt.z;
            var p_rb = q_delta * new Vector3(projectionQH.right, projectionQH.bottom, 1.0f); p_rb /= p_rb.z;
            var p_lb = q_delta * new Vector3(projectionQH.left, projectionQH.bottom, 1.0f); p_lb /= p_lb.z;

            var p_l = Mathf.Min(p_lt.x, p_rt.x, p_rb.x, p_lb.x);
            var p_t = Mathf.Max(p_lt.y, p_rt.y, p_rb.y, p_lb.y);
            var p_r = Mathf.Max(p_lt.x, p_rt.x, p_rb.x, p_lb.x);
            var p_b = Mathf.Min(p_lt.y, p_rt.y, p_rb.y, p_lb.y);

            return new MatrixArray(Mathf.Min(p_l, projectionQH.left),
                                   Mathf.Max(p_t, projectionQH.top),
                                   Mathf.Max(p_r, projectionQH.right),
                                   Mathf.Min(p_b, projectionQH.bottom));
        }
        else {
            return projectionQH;
        }
    }

    private void measureRenderPerfs(Quaternion rotationQH, MatrixArray projectionQH, Quaternion rotationQF, float innerRadii, float midRadii) {
        var optimal = calcOverfilling(Overfilling.Optimal, rotationQH, projectionQH, rotationQF);

        var radius = (projectionQH.right - projectionQH.left) / 2;
        var innerArea = calcRadiiArea(optimal, radius * innerRadii);
        var midArea = calcRadiiArea(optimal, radius * midRadii);

        var ideal_area = (projectionQH.right - projectionQH.left) * (projectionQH.top - projectionQH.bottom);
        var overfilled_area = (optimal.right - optimal.left) * (optimal.top - optimal.bottom);
        var foveated_area = innerArea + (midArea - innerArea) / 4 + (overfilled_area - midArea) / 16;

        _renderPerfGraph?.AddMeasurement(ideal_area, overfilled_area, foveated_area);
    }

    private float calcRadiiArea(MatrixArray projection, float radius) {
        var overflow_l = calcOverflowedSideArea(radius, -projection.left);
        var overflow_t = calcOverflowedSideArea(radius, projection.top);
        var overflow_r = calcOverflowedSideArea(radius, projection.right);
        var overflow_b = calcOverflowedSideArea(radius, -projection.bottom);

        var overflow_lt = calcOverflowedCornerArea(radius, -projection.left, projection.top);
        var overflow_rt = calcOverflowedCornerArea(radius, projection.right, projection.top);
        var overflow_rb = calcOverflowedCornerArea(radius, projection.right, -projection.bottom);
        var overflow_lb = calcOverflowedCornerArea(radius, -projection.left, -projection.bottom);

        return Mathf.PI * radius * radius - (overflow_l + overflow_t + overflow_r + overflow_b) + (overflow_lt + overflow_rt + overflow_rb + overflow_lb);
     }

    private float calcOverflowedSideArea(float radius, float side) {
        var cut = Mathf.Abs(side);
        if (cut >= radius) { return 0; }

        var halfAngle = Mathf.Acos(cut / radius);
        var sector =  halfAngle * radius * radius;
        var segment = sector - cut * radius * Mathf.Sin(halfAngle);

        if (side >= 0) {
            return segment;
        }
        else {
            return Mathf.PI * radius * radius - segment;
        }
    }

    private float calcOverflowedCornerArea(float radius, float side, float top) {
        if (new Vector2(side, top).magnitude >= radius) { return 0; }

        var side_segment = calcOverflowedSideArea(radius, Mathf.Abs(side));
        var top_cross = Mathf.Sqrt(radius * radius - top * top);
        var top_cross_segment = calcOverflowedSideArea(radius, top_cross);

        var corner_area = (side_segment - top_cross_segment - 2 * Mathf.Abs(top) * (top_cross - Mathf.Abs(side))) / 2;

        if (side < 0 && top < 0) {
            return Mathf.PI * radius * radius - corner_area;
        }
        else if (side < 0) {
            return top_cross_segment - corner_area;
        }
        else if (top < 0) {
            return side_segment - corner_area;
        }
        else {
            return corner_area;
        }
    }
}
