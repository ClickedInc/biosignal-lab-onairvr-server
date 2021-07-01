using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using UnityEditor;

[Serializable]
public class UnityEventWithFloat : UnityEvent<float> { }

public class MotionPredictionPlayback : MonoBehaviour {
    public const float EncodingProjectionSize = 4.0f;
    public const string PrefKeyInputMotionDataFile = "kr.co.clicked.biosignal.motiondatafile";
    public const string PrefKeyCaptureOutputPath = "kr.co.clicked.biosignal.captureoutputpath";

    public struct Projection {
        public float left;
        public float right;
        public float bottom;
        public float top;

        public Projection GetOtherEyeProjection(Projection notOverfilledEye) {
            var centerBiasX = notOverfilledEye.left + notOverfilledEye.right;

            return new Projection {
                left = left - centerBiasX,
                right = right - centerBiasX,
                top = top,
                bottom = bottom
            };
        }

        public Matrix4x4 GetMatrix(float near, float far) {
            var l = left * near;
            var t = top * near;
            var r = right * near;
            var b = bottom * near;

            var result = new Matrix4x4();
            result[0, 0] = 2.0f * near / (r - l);   result[0, 1] = 0;                       result[0, 2] = (r + l) / (r - l);               result[0, 3] = 0;
            result[1, 0] = 0;                       result[1, 1] = 2.0f * near / (t - b);   result[1, 2] = (t + b) / (t - b);               result[1, 3] = 0;
            result[2, 0] = 0;                       result[2, 1] = 0;                       result[2, 2] = -(far + near) / (far - near);    result[2, 3] = -2.0f * far * near / (far - near);
            result[3, 0] = 0;                       result[3, 1] = 0;                       result[3, 2] = -1;                              result[3, 3] = 0;

            return result;
        }
    }

    public struct MotionData {
        public Vector3 leftEyePos;
        public Vector3 rightEyePos;
        public Quaternion orientation;
        public Projection projection;
    }

    public enum PlaybackState {
        Stopped,
        Playing,
        Capturing
    }

    public enum PlaybackMode : uint {
        Predict_NoTimeWarp = 0,
        Predict_TimeWarp,
        NotPredict_NoTimeWarp,
        NotPredict_TimeWarp,

        Max
    }

    private MPPSceneCamera _sceneCamera;
    private MPPPlaybackCamera _playbackCamera;
    private MPPImageCapture _imageCapture;
    private MPPUIOverlay _uiOverlay;
    private bool _runningEndOfFrameLoop = true;

    private MPPMotionDataReader _motionData;
    private float _motionDataFps;
    private int _cursor;
    private (int from, int to) _playbackRange = (0, int.MaxValue);
    private float _playbackStartRealTime;

    [SerializeField] private UnityEventWithFloat onPlayPreview;
    [SerializeField] private UnityEvent onStartCapture;
    [SerializeField] private UnityEvent onStop;
    [SerializeField] private UnityEventWithFloat onSeek;

    [HideInInspector] public bool playbackModeStartedByEditor;

    public PlaybackState playbackState { get; set; }
    public PlaybackMode playbackMode { get; private set; } = PlaybackMode.Predict_TimeWarp;

    private void Awake() {
        if (playbackModeStartedByEditor == false) {
            DestroyImmediate(gameObject);
            return;
        }

        _sceneCamera = GetComponentInChildren<MPPSceneCamera>();
        _playbackCamera = GetComponentInChildren<MPPPlaybackCamera>();
        _imageCapture = new MPPImageCapture(this);
        _uiOverlay = GetComponentInChildren<MPPUIOverlay>();

        var cameraRig = FindObjectOfType<AirVRCameraRig>();
        if (cameraRig == null) {
            throw new UnityException("[MotionPredictionPlayback] ERROR: There must exist an instance of AirVRStereoCameraRig in children.");
        }

        cameraRig.gameObject.SetActive(false);

        if (AirXRServer.isInstantiated) {
            throw new UnityException("[MotionPredictionPlayback] ERROR: MotionPredictionPlayback script must be executed before AirVRCameraRig. Please adjust script execution order in the project settings.");
        }

        if (FindObjectOfType<AudioListener>() == null) {
            _sceneCamera.gameObject.AddComponent<AudioListener>();
        }

        PlayerSettings.defaultScreenWidth = 2048;
        PlayerSettings.defaultScreenHeight = 1024;
    }

    private void Start() {
        if (playbackModeStartedByEditor == false) { return; }

        StartCoroutine(runEndOfFrameLoop());

        loadMotionData(PlayerPrefs.GetString(PrefKeyInputMotionDataFile, "Assets/onAirXR/MotionPredictionPlayback/sample.csv"));
        setCaptureOutputPath(PlayerPrefs.GetString(PrefKeyCaptureOutputPath, Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOutput")));

        stopUnityTime();
    }

    private void OnDestroy() {
        if (playbackModeStartedByEditor == false) { return; }

        _runningEndOfFrameLoop = false;
    }

    private void loadMotionData(string path) {
        try {
            _motionData = new MPPMotionDataReader(path);

            var startts = flicksToSecond((double)_motionData.Read(0)["timestamp"]);
            var lastts = flicksToSecond((double)_motionData.Read(_motionData.count - 1)["timestamp"]);

            _motionDataFps = _motionData.count / (float)(lastts - startts);

            _uiOverlay.NotifyMotionDataLoaded(_motionData);

            Debug.LogFormat("[MotionPredictionPlayback] motion data loaded: length = {0}s, fps = {1}", lastts - startts, _motionDataFps);
        }
        catch (Exception e) {
            Debug.LogErrorFormat("[ERROR] failed to load motion data: {0}", e.StackTrace);
            _motionData = null;
        }
    }

    private void setCaptureOutputPath(string path) {
        _imageCapture.outputPath = path;

        _uiOverlay.NotifyCaptureOutputPathSet(path);
    }

    private IEnumerator runEndOfFrameLoop() {
        while (_runningEndOfFrameLoop) {
            yield return new WaitForEndOfFrame();
            if (playbackState == PlaybackState.Stopped) { continue; }

            switch (playbackState) {
                case PlaybackState.Playing:
                    _cursor = calcCurrentCursorBasedOnRealTime(_cursor, _playbackStartRealTime);

                    if (doesCursorReachToEnd(_cursor)) {
                        transitPlaybackStateTo(PlaybackState.Stopped);
                    }
                    else {
                        updateCameras(playbackMode);
                    }
                    break;
                case PlaybackState.Capturing:
                    if (doesCursorReachToEnd(_cursor)) {
                        transitPlaybackStateTo(PlaybackState.Stopped);
                    }
                    else {
                        updateCameras(playbackMode);

                        _sceneCamera.Render();
                        _playbackCamera.RenderToCapture();

                        _imageCapture.Capture(flicksToSecond((double)_motionData.Read(_cursor)["timestamp"]), playbackMode);
                        
                        _cursor++;
                    }
                    break;
            }
        }
    }

    private int calcCurrentCursorBasedOnRealTime(int lastCursor, float startRealTime) {
        var elapsedRealTime = Time.realtimeSinceStartup - startRealTime;
        var startts = (double)_motionData.Read(_playbackRange.from)["timestamp"];

        while (flicksToSecond((double)_motionData.Read(lastCursor + 1)["timestamp"] - startts) < elapsedRealTime) {
            lastCursor++;
        }
        return lastCursor;
    }

    private void transitPlaybackStateTo(PlaybackState next) {
        if (playbackState == next) { return; }

        var prev = playbackState;
        playbackState = next;

        switch (next) {
            case PlaybackState.Playing:
                _playbackStartRealTime = Time.realtimeSinceStartup;
                _cursor = _playbackRange.from;

                playUnityTime();

                onPlayPreview?.Invoke(_motionDataFps);
                break;
            case PlaybackState.Capturing:
                _playbackStartRealTime = Time.realtimeSinceStartup;
                _cursor = _playbackRange.from;
                
                _imageCapture.Prepare(_playbackCamera.leftCaptureCamera.targetTexture);

                playUnityTime();

                onStartCapture?.Invoke();
                break;
            case PlaybackState.Stopped:
                stopUnityTime();

                onStop?.Invoke();
                break;
        }
    }

    private bool doesCursorReachToEnd(int cursor) {
        return cursor >= _motionData.count || cursor >= _playbackRange.to;
    }

    private void updateCameras(PlaybackMode mode) {
        var usePredict = mode == PlaybackMode.Predict_NoTimeWarp || mode == PlaybackMode.Predict_TimeWarp;
        var useTimewarp = mode == PlaybackMode.NotPredict_TimeWarp || mode == PlaybackMode.Predict_TimeWarp;

        var cursorForHead = _cursor;
        if (_motionData.type == MPPMotionDataReader.Type.Raw) {
            if (doesCursorReachToEnd(cursorForHead + 1)) { return; }

            var dataForFrame = _motionData.Read(_cursor);
            var predictionTime = (double)dataForFrame["prediction_time"] / 1000.0;
            var framets = flicksToSecond((double)dataForFrame["timestamp"]);

            var headts = flicksToSecond((double)_motionData.Read(cursorForHead + 1)["timestamp"]);
            while (headts - framets < predictionTime) {
                if (doesCursorReachToEnd(cursorForHead + 1)) { return; }

                cursorForHead++;
                headts = flicksToSecond((double)_motionData.Read(cursorForHead + 1)["timestamp"]);
            }
        }

        var frameMotion = new MotionData();
        var headMotion = new MotionData();
        if (readMotionData(usePredict, _cursor, ref frameMotion) == false ||
            readMotionData(false, cursorForHead, ref headMotion) == false) { return; }

        _sceneCamera.Apply(frameMotion, headMotion, EncodingProjectionSize);
        _playbackCamera.Apply(frameMotion, headMotion, useTimewarp, EncodingProjectionSize);
    }

    private bool readMotionData(bool usePredict, int cursor, ref MotionData motionData) {
        if (cursor >= _motionData.count) { return false; }

        var leftEyePosPrefix = usePredict ? "predicted_left_eye_position_" : "input_left_eye_position_";
        var rightEyePosPrefix = usePredict ? "predicted_right_eye_position_" : "input_right_eye_position_";
        var orientationPrefix = usePredict ? "predicted_head_orientation_" : "input_head_orientation_";
        var projectionPrefix = usePredict ? "predicted_camera_projection_" : "input_camera_projection_";

        var data = _motionData.Read(cursor);

        motionData = new MotionData {
            leftEyePos = new Vector3((float)(double)data[leftEyePosPrefix + "x"],
                                     (float)(double)data[leftEyePosPrefix + "y"],
                                     (float)(double)data[leftEyePosPrefix + "z"]),
            rightEyePos = new Vector3((float)(double)data[rightEyePosPrefix + "x"],
                                      (float)(double)data[rightEyePosPrefix + "y"],
                                      (float)(double)data[rightEyePosPrefix + "z"]),
            orientation = new Quaternion(-(float)(double)data[orientationPrefix + "x"],
                                         -(float)(double)data[orientationPrefix + "y"],
                                         (float)(double)data[orientationPrefix + "z"],
                                         (float)(double)data[orientationPrefix + "w"]),
            projection = new Projection { left = (float)(double)data[projectionPrefix + "left"],
                                          top = (float)(double)data[projectionPrefix + "top"],
                                          right = (float)(double)data[projectionPrefix + "right"],
                                          bottom = (float)(double)data[projectionPrefix + "bottom"] }
        };
        return true;
    }

    // for MPPImageCapture
    public void OnImageCaptured(MPPImageCapture capture, int seqnum) {
        onSeek?.Invoke(seqnum / _motionDataFps);
    }

    // handle ui events
    public void OnLoadInputMotionDataFile(string path) {
        loadMotionData(path);

        PlayerPrefs.SetString(PrefKeyInputMotionDataFile, path);
    }

    public void OnSetCaptureOutputPath(string path) {
        setCaptureOutputPath(path);

        PlayerPrefs.SetString(PrefKeyCaptureOutputPath, path);
    }

    public void OnSetPlaybackMode(PlaybackMode mode) {
        playbackMode = mode;
    }

    public void OnSetPlaybackRangeFrom(int value) {
        _playbackRange.from = value;
    }

    public void OnSetPlaybackRangeTo(int value) {
        _playbackRange.to = value;
    }

    public void OnTogglePlay() {
        if (playbackState == PlaybackState.Capturing) { return; }

        transitPlaybackStateTo(playbackState == PlaybackState.Playing ? PlaybackState.Stopped : PlaybackState.Playing);
    }

    public void OnToggleCapture() {
        if (playbackState == PlaybackState.Playing) { return; }

        transitPlaybackStateTo(playbackState == PlaybackState.Capturing ? PlaybackState.Stopped : PlaybackState.Capturing);
    }

    private void playUnityTime() {
        Time.timeScale = 1.0f;
    }

    private void stopUnityTime() {
        Time.timeScale = 0.0f;
    }
    
    private double flicksToSecond(double flicks) => flicks / 705600000;
}
