﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using UnityEditor;

[Serializable]
public class UnityEventWithFloat : UnityEvent<float> { }

public static class MPPUtils {
    public static double FlicksToSecond(double flicks) => flicks / 705600000.0;

    public static MPPProjection calcOptimalOverfilling(Quaternion headRotation, Quaternion frameRotation, MPPProjection eyeProjection) {
        var q_delta = Quaternion.Inverse(frameRotation) * headRotation;

        var p_lt = q_delta * new Vector3(eyeProjection.left, eyeProjection.top, 1.0f); p_lt /= p_lt.z;
        var p_rt = q_delta * new Vector3(eyeProjection.right, eyeProjection.top, 1.0f); p_rt /= p_rt.z;
        var p_rb = q_delta * new Vector3(eyeProjection.right, eyeProjection.bottom, 1.0f); p_rb /= p_rb.z;
        var p_lb = q_delta * new Vector3(eyeProjection.left, eyeProjection.bottom, 1.0f); p_lb /= p_lb.z;

        var p_l = Mathf.Min(p_lt.x, p_rt.x, p_rb.x, p_lb.x);
        var p_t = Mathf.Max(p_lt.y, p_rt.y, p_rb.y, p_lb.y);
        var p_r = Mathf.Max(p_lt.x, p_rt.x, p_rb.x, p_lb.x);
        var p_b = Mathf.Min(p_lt.y, p_rt.y, p_rb.y, p_lb.y);

        return new MPPProjection {
            left = Mathf.Min(p_l, eyeProjection.left),
            top = Mathf.Max(p_t, eyeProjection.top),
            right = Mathf.Max(p_r, eyeProjection.right),
            bottom = Mathf.Min(p_b, eyeProjection.bottom)
        };
    }
}

public struct MPPProjection {
    public static MPPProjection FromRect(Rect rect) {
        return new MPPProjection {
            left = rect.xMin,
            top = rect.yMax,
            right = rect.xMax,
            bottom = rect.yMin
        };
    }

    public float left;
    public float right;
    public float bottom;
    public float top;

    public float width => right - left;
    public float height => top - bottom;
    public float aspect => width / height;
    public Vector2 center => new Vector2((left + right) / 2, (top + bottom) / 2);

    public MPPProjection GetOtherEyeProjection(MPPProjection notOverfilledEye) {
        var centerBiasX = notOverfilledEye.left + notOverfilledEye.right;

        return new MPPProjection {
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
        result[0, 0] = 2.0f * near / (r - l); result[0, 1] = 0; result[0, 2] = (r + l) / (r - l); result[0, 3] = 0;
        result[1, 0] = 0; result[1, 1] = 2.0f * near / (t - b); result[1, 2] = (t + b) / (t - b); result[1, 3] = 0;
        result[2, 0] = 0; result[2, 1] = 0; result[2, 2] = -(far + near) / (far - near); result[2, 3] = -2.0f * far * near / (far - near);
        result[3, 0] = 0; result[3, 1] = 0; result[3, 2] = -1; result[3, 3] = 0;

        return result;
    }
}

public struct MPPMotionData {
    public Vector3 leftEyePos;
    public Vector3 rightEyePos;
    public Quaternion orientation;
    public MPPProjection projection;
    public float foveationInnerRadius;
    public float foveationMiddleRadius;
}

public class MotionPredictionPlayback : MonoBehaviour {
    public readonly static Vector2 EncodingProjectionSize = new Vector2(4.0f, 4.0f);
    public const string PrefKeyInputMotionDataFile = "kr.co.clicked.biosignal.motiondatafile";
    public const string PrefKeyCaptureOutputPath = "kr.co.clicked.biosignal.captureoutputpath";

    public enum PlaybackState {
        Stopped,
        Playing,
        Capturing,
        Realtime
    }

    public enum PlaybackMode : uint {
        Predict_NoTimeWarp = 0,
        Predict_TimeWarp,
        NotPredict_NoTimeWarp,
        NotPredict_TimeWarp,

        Max
    }

    private AirXRPredictiveCameraRig _predictiveCameraRig;
    private MPPSceneCamera _sceneCamera;
    private MPPPlaybackCamera _playbackCamera;
    private MPPImageCapture _imageCapture;
    private MPPUIOverlay _uiOverlay;
    private MPPRenderPerfGraph _renderPerfGraph;
    private bool _runningEndOfFrameLoop = true;

    private MPPMotionDataProvider _motionData;

    [SerializeField] private UnityEventWithFloat onPlayPreview;
    [SerializeField] private UnityEvent onStartCapture;
    [SerializeField] private UnityEvent onStop;
    [SerializeField] private UnityEventWithFloat onSeek;

    [HideInInspector] public bool playbackModeStartedByEditor = false;

    public AirXRServerSettings settings { get; private set; }
    public PlaybackState playbackState { get; set; }
    public PlaybackMode playbackMode { get; private set; } = PlaybackMode.Predict_TimeWarp;

    private void Awake() {
        _predictiveCameraRig = FindObjectOfType<AirXRPredictiveCameraRig>();
        if (_predictiveCameraRig == null) {
            throw new UnityException("[MotionPredictionPlayback] ERROR: There must exist an instance of AirVRStereoCameraRig in children.");
        }

        settings = Resources.Load<AirXRServerSettings>("AirXRServerSettings");
        if (settings == null) {
            settings = ScriptableObject.CreateInstance<AirXRServerSettings>();
        }

        _sceneCamera = GetComponentInChildren<MPPSceneCamera>();
        _playbackCamera = GetComponentInChildren<MPPPlaybackCamera>();
        _uiOverlay = GetComponentInChildren<MPPUIOverlay>();
        _renderPerfGraph = GetComponentInChildren<MPPRenderPerfGraph>();

        if (playbackModeStartedByEditor) {
            _imageCapture = new MPPImageCapture(this);

            _predictiveCameraRig.gameObject.SetActive(false);

            if (AirXRServer.isInstantiated) {
                throw new UnityException("[MotionPredictionPlayback] ERROR: MotionPredictionPlayback script must be executed before AirVRCameraRig. Please adjust script execution order in the project settings.");
            }

            if (FindObjectOfType<AudioListener>() == null) {
                _sceneCamera.gameObject.AddComponent<AudioListener>();
            }
        }
    }

    private void Start() {
        StartCoroutine(runEndOfFrameLoop());

        if (playbackModeStartedByEditor) {
            loadMotionDataFile(PlayerPrefs.GetString(PrefKeyInputMotionDataFile, "Assets/onAirXR/MotionPredictionPlayback/sample.csv"));
            setCaptureOutputPath(PlayerPrefs.GetString(PrefKeyCaptureOutputPath, Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOutput")));

            stopUnityTime();
        }
        else {
            loadLiveMotionDataProvider();
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            _uiOverlay.gameObject.SetActive(!_uiOverlay.gameObject.activeSelf);
        }
    }

    private void OnDestroy() {
        _runningEndOfFrameLoop = false;
    }

    private void loadMotionDataFile(string path) {
        try {
            var window = (0, int.MaxValue);
            if (_motionData != null && _motionData is MPPMotionDataFile) {
                window = (_motionData as MPPMotionDataFile).window;
            }

            _motionData = new MPPMotionDataFile(path);
            (_motionData as MPPMotionDataFile).window = window;

            _uiOverlay.NotifyMotionDataProviderLoaded(_motionData);
        }
        catch (Exception e) {
            Debug.LogErrorFormat("[ERROR] failed to load motion data: {0}", e.StackTrace);
            _motionData = null;
        }
    }

    private void loadLiveMotionDataProvider() {
        _motionData = _predictiveCameraRig.liveMotionProvider;

        transitPlaybackStateTo(PlaybackState.Realtime);
        _uiOverlay.NotifyMotionDataProviderLoaded(_motionData);
    }

    private void setCaptureOutputPath(string path) {
        _imageCapture.outputPath = path;

        _uiOverlay.NotifyCaptureOutputPathSet(path);
    }

    private IEnumerator runEndOfFrameLoop() {
        while (_runningEndOfFrameLoop) {
            yield return new WaitForEndOfFrame();

            if (playbackState == PlaybackState.Stopped || EditorApplication.isPaused) { continue; }

            switch (playbackState) {
                case PlaybackState.Playing:
                    _motionData.AdvanceToNext(true);

                    if (_motionData.reachedToEnd) {
                        transitPlaybackStateTo(PlaybackState.Stopped);
                        break;
                    }

                    updateCameras(playbackMode);
                    break;
                case PlaybackState.Capturing:
                    if (_motionData.reachedToEnd) {
                        transitPlaybackStateTo(PlaybackState.Stopped);
                        break;
                    }

                    updateCameras(playbackMode);

                    _sceneCamera.Render();
                    _playbackCamera.RenderToCapture();

                    _imageCapture.Capture(_motionData.currentTimestamp, playbackMode);

                    _motionData.AdvanceToNext(false);
                    break;
                case PlaybackState.Realtime:
                    _motionData.AdvanceToNext(true);

                    updateCameras(playbackMode);
                    break;
            }
        }
    }

    private void transitPlaybackStateTo(PlaybackState next) {
        if (playbackState == next) { return; }

        var prev = playbackState;
        playbackState = next;

        switch (next) {
            case PlaybackState.Playing: {
                if (!(_motionData is MPPMotionDataFile motionData)) { break; }

                motionData.SeekToStart();
                playUnityTime();

                onPlayPreview?.Invoke(motionData.fps);
                break;
            }
            case PlaybackState.Capturing: {
                if (!(_motionData is MPPMotionDataFile motionData)) { break; }

                motionData.SeekToStart();

                _imageCapture.Prepare(_playbackCamera.leftCaptureCamera.targetTexture);
                playUnityTime();

                onStartCapture?.Invoke();
                break;
            }
            case PlaybackState.Stopped:
                stopUnityTime();

                onStop?.Invoke();
                break;
        }
    }

    private void updateCameras(PlaybackMode mode) {
        var usePredict = mode == PlaybackMode.Predict_NoTimeWarp || mode == PlaybackMode.Predict_TimeWarp;
        var useTimewarp = mode == PlaybackMode.NotPredict_TimeWarp || mode == PlaybackMode.Predict_TimeWarp;

        var motionFrame = new MPPMotionData();
        var motionHead = new MPPMotionData();
        if (_motionData.GetCurrentMotion(usePredict, settings.OverfillingMode, ref motionFrame, ref motionHead) == false) { return; }

        //var offsetX = Mathf.Sin(Time.realtimeSinceStartup) / 2;
        //var offsetY = Mathf.Cos(Time.realtimeSinceStartup) / 2;
        //motionFrame.projection = new MPPProjection { left = -1.0f + offsetX, top = 1.0f + offsetY, right = 1.0f + offsetX, bottom = -1.0f + offsetY };

        _sceneCamera.Apply(motionFrame, motionHead, EncodingProjectionSize);
        _playbackCamera.Apply(motionFrame, motionHead, useTimewarp, EncodingProjectionSize);

        _renderPerfGraph?.AddPoint(motionHead, motionFrame);
    }

    // for MPPImageCapture
    public void OnImageCaptured(MPPImageCapture capture, int seqnum) {
        if (!(_motionData is MPPMotionDataFile motionData)) { return; }

        onSeek?.Invoke(seqnum / motionData.fps);
    }

    // handle ui events
    public void OnLoadInputMotionDataFile(string path) {
        loadMotionDataFile(path);

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
        if (!(_motionData is MPPMotionDataFile motionData)) { return; }

        motionData.window = (value, motionData.window.to);
    }

    public void OnSetPlaybackRangeTo(int value) {
        if (!(_motionData is MPPMotionDataFile motionData)) { return; }

        motionData.window = (motionData.window.from, value);
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
}
