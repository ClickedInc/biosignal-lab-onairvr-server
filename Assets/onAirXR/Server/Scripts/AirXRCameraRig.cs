/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Assertions;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
public abstract class AirXRCameraRig : MonoBehaviour {
    private AirXRClientConfig _config;
    private bool _mediaStreamJustStopped;
    private int _viewNumber;
    private bool _encodeVideoFrameRequested;
    private AudioListener _audioListener;
    private AirXRServerAudioOutputRouter _audioOutputRouter;
    private AirXRPredictiveCameraRig _predictiveCameraRig;
    private CameraEventEmitter _cameraEventEmitter;

    [SerializeField] private bool _sendAudio = true;
    [SerializeField] private AirXRServerAudioOutputRouter.Input _audioInput = AirXRServerAudioOutputRouter.Input.AudioListener;
    [SerializeField] private AudioMixer _targetAudioMixer = null;
    [SerializeField] private string _exposedRendererIDParameterName = null;

    private void enableCameras() {
        foreach (Camera cam in cameras) {
            cam.enabled = true;
        }
    }

    private void disableCameras() {
        foreach (Camera cam in cameras) {
            cam.enabled = false;
        }
    }

    private void initializeCamerasForMediaStream() {
        Assert.IsNotNull(_config);

        setupCamerasOnBound(_config);
    }

    private void startToRenderCamerasForMediaStream() {
        enableCameras();
        onStartRender();

        _mediaStreamJustStopped = false;
        StartCoroutine(CallPluginEndOfFrame());
    }

    private Transform findDirectChildByName(Transform parent, string name) {
        Transform[] xforms = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform xform in xforms) {
            if (xform.parent == parent && xform.gameObject.name.Equals(name)) {
                return xform;
            }
        }
        return null;
    }

    private void Awake() {
        ensureGameObjectIntegrity(false);
        if (Application.isPlaying == false) { return; }

        AirXRServer.LoadOnce();

        disableCameras();
        AirXRCameraRigManager.managerOnCurrentScene.RegisterCameraRig(this);
        AirXRCameraRigManager.managerOnCurrentScene.eventDispatcher.MessageReceived += onAirXRMessageReceived;

        playerID = AXRServerPlugin.InvalidPlayerID;

        inputStream = new AirXRServerInputStream(this);

        if (_sendAudio) {
            _audioOutputRouter = headAnchor.gameObject.AddComponent<AirXRServerAudioOutputRouter>();
            _audioOutputRouter.input = _audioInput;
            _audioOutputRouter.targetAudioMixer = _targetAudioMixer;
            _audioOutputRouter.exposedRendererIDParameterName = _exposedRendererIDParameterName;
            _audioOutputRouter.targetCameraRig = this;

            if (_audioInput == AirXRServerAudioOutputRouter.Input.AudioListener) {
                _audioOutputRouter.output = AirXRServerAudioOutputRouter.Output.All;

                _audioListener = headAnchor.gameObject.AddComponent<AudioListener>();
            }
            else {
                _audioOutputRouter.output = AirXRServerAudioOutputRouter.Output.One;
            }
        }

        if (_audioOutputRouter) {
            _audioOutputRouter.enabled = false;
        }
        //if (_audioListener) {
        //    _audioListener.enabled = false;
        //}

        _predictiveCameraRig = GetComponent<AirXRPredictiveCameraRig>();
        _cameraEventEmitter = cameras[0].gameObject.AddComponent<CameraEventEmitter>();

        onAwake();
    }

    private void Start() {
        if (Application.isPlaying == false) {
            ensureGameObjectIntegrity(true);
            return;
        }

        onStart();
    }

    private void Update() {
        if (Application.isPlaying == false) {
            ensureGameObjectIntegrity(true);
            return;
        }
    }

    // Events called by AirXRCameraRigManager to guarantee the update execution order
    internal void OnUpdate() {
        inputStream.UpdateInputFrame();

        if (_audioOutputRouter) {
            _audioOutputRouter.enabled = isActive;
        }
        //if (_audioListener) {
        //    _audioListener.enabled = isActive;
        //}
    }

    internal void OnLateUpdate() {
        if (mediaStream != null && _mediaStreamJustStopped == false && _encodeVideoFrameRequested) {
            Assert.IsTrue(isBoundToClient);

            var bypassPrediction = _predictiveCameraRig?.bypassPrediction ?? true;

            var timestamp = bypassPrediction == false && _predictiveCameraRig != null ? _predictiveCameraRig.predictedMotionProvider.timestamp :
                                                                                        inputStream.inputRecvTimestamp;
            var leftEyePose = Pose.identity;
            var rightEyePose = Pose.identity;
            var leftProjection = Rect.MinMaxRect(-1, -1, 1, 1);
            var rightProjection = Rect.MinMaxRect(-1, -1, 1, 1);

            if (bypassPrediction) {
                leftEyePose = inputStream.GetPose((byte)AXRInputDeviceID.HeadTracker, (byte)AXRHeadTrackerControl.Pose);
            }
            else {
                leftEyePose = _predictiveCameraRig?.predictedMotionProvider.leftEye ?? Pose.identity;
                rightEyePose = _predictiveCameraRig?.predictedMotionProvider.rightEye ?? Pose.identity;
                leftProjection = _predictiveCameraRig?.predictedMotionProvider.leftProjection ?? leftProjection;
                rightProjection = _predictiveCameraRig?.predictedMotionProvider.rightProjection ?? rightProjection;
            }

            var encodingProjSize = _config.GetEncodingProjectionSize();
            var leftEncodingProjection = Rect.MinMaxRect(-encodingProjSize.width / 2 + leftProjection.center.x,
                                                         -encodingProjSize.height / 2 + leftProjection.center.y,
                                                         encodingProjSize.width / 2 + leftProjection.center.x,
                                                         encodingProjSize.height / 2 + leftProjection.center.y);
            var rightEncodingProjection = Rect.MinMaxRect(-encodingProjSize.width / 2 + rightProjection.center.x,
                                                          -encodingProjSize.height / 2 + rightProjection.center.y,
                                                          encodingProjSize.width / 2 + rightProjection.center.x,
                                                          encodingProjSize.height / 2 + rightProjection.center.y);

            var leftRenderProjection = makeSafeRenderProjection(
                Rect.MinMaxRect(Mathf.Max(leftEncodingProjection.xMin, leftProjection.xMin),
                                Mathf.Max(leftEncodingProjection.yMin, leftProjection.yMin),
                                Mathf.Min(leftEncodingProjection.xMax, leftProjection.xMax),
                                Mathf.Min(leftEncodingProjection.yMax, leftProjection.yMax))
            );
            var rightRenderProjection = makeSafeRenderProjection(
                Rect.MinMaxRect(Mathf.Max(rightEncodingProjection.xMin, rightProjection.xMin),
                                Mathf.Max(rightEncodingProjection.yMin, rightProjection.yMin),
                                Mathf.Min(rightEncodingProjection.xMax, rightProjection.xMax),
                                Mathf.Min(rightEncodingProjection.yMax, rightProjection.yMax))
            );

            AXRServerPlugin.GetViewNumber(playerID, 
                                          timestamp, 
                                          (int)(_predictiveCameraRig?.predictedMotionProvider.predictionTime ?? 0), 
                                          leftEyePose.rotation, 
                                          leftRenderProjection,
                                          rightRenderProjection,
                                          leftEncodingProjection, 
                                          rightEncodingProjection,
                                          out _viewNumber);

            if (bypassPrediction) {
                updateCameraTransforms(_config, leftEyePose.position, leftEyePose.rotation);
            }
            else {
                updateCameraTransforms(_config, leftEyePose, rightEyePose);
            }

            updateCameraProjection(_config, leftRenderProjection, rightRenderProjection, leftEncodingProjection, rightEncodingProjection);
            updateControllerTransforms(_config, _predictiveCameraRig?.predictedMotionProvider, bypassPrediction);

            mediaStream.GetNextFramebufferTexturesAsRenderTargets(cameras);

            _cameraEventEmitter.UpdatePerFrame(_viewNumber);
        }
        inputStream.UpdateSenders();
    }

    private void OnDestroy() {
        if (Application.isPlaying == false) {
            return;
        }

        if (AirXRCameraRigManager.CheckIfExistManagerOnCurrentScene()) {
            AirXRCameraRigManager.managerOnCurrentScene.eventDispatcher.MessageReceived -= onAirXRMessageReceived;
            AirXRCameraRigManager.managerOnCurrentScene.UnregisterCameraRig(this);
        }
    }

    private Rect makeSafeRenderProjection(Rect projection) {
        if (projection.width >= 1.0f || projection.height >= 1.0f) { return projection; }

        var width = Mathf.Max(projection.width, 0.5f);
        var height = Mathf.Max(projection.height, 0.5f);

        return Rect.MinMaxRect(projection.center.x - width / 2,
                               projection.center.y - height / 2,
                               projection.center.x + width / 2,
                               projection.center.y + height / 2);
    }

    private void onAirXRMessageReceived(AXRMessage message) {
        var serverMessage = message as AirXRServerMessage;
        int srcPlayerID = serverMessage.source.ToInt32();
        if (srcPlayerID != playerID) {
            return;
        }

        if (serverMessage.IsMediaStreamEvent()) {
            if (serverMessage.Name.Equals(AirXRServerMessage.NameInitialized)) {
                onAirXRMediaStreamInitialized(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameStarted)) {
                onAirXRMediaStreamStarted(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameEncodeVideoFrame)) {
                onAirXRMediaStreamEncodeVideoFrame(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameSetCameraProjection)) {
                onAirXRMediaStreamSetCameraProjection(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameStopped)) {
                onAirXRMediaStreamStopped(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameCleanupUp)) {
                onAirXRMediaStreamCleanedUp(serverMessage);
            }
        }
    }

    private void onAirXRMediaStreamInitialized(AirXRServerMessage message) {
        Assert.IsNull(mediaStream);

        initializeCamerasForMediaStream();
        AXRServerPlugin.SendCameraClipPlanes(playerID, cameras[0].nearClipPlane, cameras[0].farClipPlane);

        mediaStream = new AirXRServerMediaStream(playerID, _config, cameras.Length);
        GL.IssuePluginEvent(AXRServerPlugin.InitStreams_RenderThread_Func, AXRServerPlugin.RenderEventArg((uint)playerID));

        inputStream.Init();
    }

    private void onAirXRMediaStreamStarted(AirXRServerMessage message) {
        startToRenderCamerasForMediaStream();
        inputStream.Start();
    }

    private void onAirXRMediaStreamEncodeVideoFrame(AirXRServerMessage message) {
        _encodeVideoFrameRequested = true;
    }

    private void onAirXRMediaStreamSetCameraProjection(AirXRServerMessage message) {
        updateCameraProjection(_config, message.CameraProjection);
    }

    private void onAirXRMediaStreamStopped(AirXRServerMessage message) {
        onStopRender();
        disableCameras();

        _mediaStreamJustStopped = true; // StopCoroutine(_CallPluginEndOfFrame) executes the routine one more in the next frame after the call. 
                                        // so use a flag to completely stop the routine.

        GL.IssuePluginEvent(AXRServerPlugin.ResetStreams_RenderThread_Func, AXRServerPlugin.RenderEventArg((uint)playerID));

        inputStream.Stop();
    }

    private void onAirXRMediaStreamCleanedUp(AirXRServerMessage message) {
        Assert.IsNotNull(mediaStream);

        inputStream.Cleanup();

        GL.IssuePluginEvent(AXRServerPlugin.CleanupStreams_RenderThread_Func, AXRServerPlugin.RenderEventArg((uint)playerID));

        mediaStream.Destroy();
        mediaStream = null;

        foreach (Camera cam in cameras) {
            cam.targetTexture = null;
        }
    }

    private IEnumerator CallPluginEndOfFrame() {
        yield return new WaitForEndOfFrame();

        Assert.IsNotNull(mediaStream);
        GL.IssuePluginEvent(AXRServerPlugin.EncodeVideoFrame_RenderThread_Func, AXRServerPlugin.RenderEventArg((uint)playerID, (uint)_viewNumber, (uint)mediaStream.currentFramebufferIndex));    // the first render event

        while (_mediaStreamJustStopped == false) {
            yield return new WaitForEndOfFrame();

            if (_mediaStreamJustStopped) {
                yield break;
            }
            else if (_encodeVideoFrameRequested) {
                Assert.IsNotNull(mediaStream);

                GL.IssuePluginEvent(AXRServerPlugin.EncodeVideoFrame_RenderThread_Func, AXRServerPlugin.RenderEventArg((uint)playerID, (uint)_viewNumber, (uint)mediaStream.currentFramebufferIndex));
                _encodeVideoFrameRequested = false;
            }
        }
    }

    protected Transform getOrCreateGameObject(string name, Transform parent, bool create) {
        Transform result = findDirectChildByName(parent, name);
        if (result == null && create) {
            result = new GameObject(name).transform;
            result.parent = parent;
            result.localPosition = Vector3.zero;
            result.localRotation = Quaternion.identity;
            result.localScale = Vector3.one;
        }
        return result;
    }

    protected abstract Transform headAnchor { get; }
    protected abstract void ensureGameObjectIntegrity(bool create);
    protected virtual void onAwake() { }
    protected virtual void onStart() { }
    protected abstract void setupCamerasOnBound(AirXRClientConfig config);
    protected virtual void onStartRender() { }
    protected virtual void onStopRender() { }
    protected abstract void updateCameraProjection(AirXRClientConfig config, float[] projection);
    protected abstract void updateCameraProjection(AirXRClientConfig config, Rect leftRenderProj, Rect rightRenderProj, Rect leftEncodingProj, Rect rightEncodingProj);
    protected abstract void updateCameraTransforms(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation);
    protected abstract void updateCameraTransforms(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose);
    protected virtual void updateControllerTransforms(AirXRClientConfig config) { }
    protected virtual void updateControllerTransforms(AirXRClientConfig config, AirXRPredictedMotionProvider motionProvider, bool bypassPrediction) { }

    internal int playerID { get; private set; }

    internal AirXRServerInputStream inputStream { get; private set; }
    public AirXRServerMediaStream mediaStream { get; private set; }

    internal bool isStreaming => AXRServerPlugin.IsStreaming(playerID);

    internal abstract bool raycastGraphic { get; }
    internal abstract Matrix4x4 clientSpaceToWorldMatrix { get; }
    internal abstract Transform headPose { get; }
    internal abstract Camera[] cameras { get; }

    internal void BindPlayer(int playerID) {
        Assert.IsFalse(isBoundToClient);
        Assert.IsNull(_config);

        this.playerID = playerID;
        _config = AirXRClientConfig.Get(playerID);

        Assert.IsNotNull(_config);

        _cameraEventEmitter.Bind(playerID);
    }

    internal void BindPlayer(int playerID, AirXRServerMediaStream mediaStream, AirXRServerInputStream inputStream) {
        BindPlayer(playerID);

        this.mediaStream = mediaStream;
        this.inputStream = inputStream;
        this.inputStream.owner = this;

        initializeCamerasForMediaStream();
        if (isStreaming) {
            startToRenderCamerasForMediaStream();
        }
    }

    internal void UnbindPlayer() {
        Assert.IsTrue(isBoundToClient);

        playerID = AXRServerPlugin.InvalidPlayerID;
        _config = null;

        _cameraEventEmitter.Unbind();
    }

    internal void PreHandOverStreams() {
        Assert.IsTrue(isBoundToClient);

        // do nothing
    }

    internal void PostHandOverStreams() {
        foreach (Camera cam in cameras) {
            cam.targetTexture = null;
        }
    }

    internal void EnableNetworkTimeWarp(bool enable) {
        if (isBoundToClient) {
            AXRServerPlugin.EnableNetworkTimeWarp(playerID, enable);
        }
    }

    public AirXRClientType type {
        get {
            return GetType() == typeof(AirVRCameraRig) ? AirXRClientType.Stereoscopic : AirXRClientType.Monoscopic;
        }
    }

    public bool isBoundToClient {
        get {
            return playerID >= 0;
        }
    }

    public bool isActive {
        get {
            return isBoundToClient && isStreaming;
        }
    }

    public AirXRClientConfig GetConfig() {
        if (isBoundToClient) {
            return _config;
        }
        return null;
    }

    public void RecenterPose() {
        if (isBoundToClient) {
            AXRServerPlugin.RecenterPose(playerID);
        }
    }

    public void SendUserData(byte[] data) {
        if (isBoundToClient) {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);

            try {
                Marshal.Copy(data, 0, ptr, data.Length);
                AXRServerPlugin.SendUserData(playerID, ptr, data.Length);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

    public void Disconnect() {
        if (isBoundToClient) {
            AXRServerPlugin.Disconnect(playerID);
        }
    }

    private class CameraEventEmitter : MonoBehaviour {
        private int _playerID = AXRServerPlugin.InvalidPlayerID;
        private int _viewNumber;

        public bool bound {
            get { return _playerID != AXRServerPlugin.InvalidPlayerID; }
        }

        public void Bind(int playerID) {
            _playerID = playerID;
        }

        public void Unbind() {
            _playerID = AXRServerPlugin.InvalidPlayerID;
        }

        public void UpdatePerFrame(int viewNumber) {
            _viewNumber = viewNumber;
        }

        public void OnPreRender() {
            if (bound) {
                GL.IssuePluginEvent(AXRServerPlugin.GameFrameRenderingStarted_RenderThread_Func, AXRServerPlugin.RenderEventArg((uint)_playerID, (uint)_viewNumber, 0));
            }
        }
    }
}
