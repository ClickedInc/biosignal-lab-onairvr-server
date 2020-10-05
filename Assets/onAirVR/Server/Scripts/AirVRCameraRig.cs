/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
public abstract class AirVRCameraRig : MonoBehaviour {
    private const int InvalidPlayerID = -1;

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_GetViewNumber(int playerID, long timeStamp,
                                                     float orientationX, float orientationY, float orientationZ, float orientationW,
                                                     float renderProjL, float renderProjT, float renderProjR, float renderProjB,
                                                     float encodingProjL, float encodingProjT, float encodingProjR, float encodingProjB,
                                                     out int viewNumber);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_InitStreams_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_EncodeVideoFrame_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_ResetStreams_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_CleanupStreams_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern bool onairvr_IsStreaming(int playerID);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_AdjustBitRate_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_RecenterPose(int playerID);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_EnableNetworkTimeWarp(int playerID, bool enable);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_SendCameraClipPlanes(int playerID, float nearClip, float farClip);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_SendUserData(int playerID, IntPtr data, int length);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_Disconnect(int playerID);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_GameFrameRenderingStarted_RenderThread_Func();

    private AirVRClientConfig _config;
    private bool _mediaStreamJustStopped;
    private int _viewNumber;
    private bool _encodeVideoFrameRequested;
    private AirVRCameraEventEmitter _cameraEventEmitter;

    public bool bypassPrediction { get; set; } = false;

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
        ensureGameObjectIntegrity();
        if (Application.isPlaying == false) {
            return;
        }

        AirVRServer.LoadOnce(FindObjectOfType<AirVRServerInitParams>());

        disableCameras();
        AirVRCameraRigManager.managerOnCurrentScene.RegisterCameraRig(this);
        AirVRCameraRigManager.managerOnCurrentScene.eventDispatcher.MessageReceived += onAirVRMessageReceived;

        playerID = InvalidPlayerID;

        inputStream = new AirVRServerInputStream() {
            owner = this
        };

        Debug.Assert(cameras != null && cameras.Length > 0);
        _cameraEventEmitter = cameras[0].gameObject.AddComponent<AirVRCameraEventEmitter>();
    }

    private void Start() {
        ensureGameObjectIntegrity();
        if (Application.isPlaying == false) {
            return;
        }

        init();
    }

    private void Update() {
        ensureGameObjectIntegrity();
        if (Application.isPlaying == false) {
            return;
        }
    }

    // Events called by AirVRCameraRigManager to guarantee the update execution order
    internal virtual void OnUpdate() {
        inputStream.UpdateReceivers();
    }

    internal void OnLateUpdate() {
        if (mediaStream != null && _mediaStreamJustStopped == false && _encodeVideoFrameRequested) {
            Assert.IsTrue(isBoundToClient);

            long timeStamp = 0;
            Vector3 leftEyePosition = Vector3.zero;
            Quaternion leftEyeOrientation = Quaternion.identity;
            Vector3 rightEyePosition = Vector3.zero;
            Quaternion rightEyeOrientation = Quaternion.identity;

            if (bypassPrediction) {
                inputStream.GetTransform(AirVRInputDeviceName.HeadTracker, (byte)AirVRHeadTrackerKey.Transform, ref leftEyePosition, ref leftEyeOrientation);
            }
            else {
                inputStream.GetTransform(AirVRInputDeviceName.HeadTracker, (byte)AirVRHeadTrackerInputDevice.ControlKey.TransformRightEye, ref timeStamp, ref rightEyePosition, ref rightEyeOrientation);
                inputStream.GetTransform(AirVRInputDeviceName.HeadTracker, (byte)AirVRHeadTrackerInputDevice.ControlKey.TransformLeftEye, ref timeStamp, ref leftEyePosition, ref leftEyeOrientation);
            }

            var projection = inputStream.GetAxis4D(AirVRInputDeviceName.HeadTracker, (byte)AirVRHeadTrackerInputDevice.ControlKey.Projection);
            var center = new Vector2((projection.x + projection.z) / 2, (projection.y + projection.w) / 2);

            var encodingProjSize = _config.GetEncodingProjectionSize();
            var encodingProjection = Rect.MinMaxRect(-encodingProjSize.width / 2 + center.x,
                                                     -encodingProjSize.height / 2 + center.y,
                                                     encodingProjSize.width / 2 + center.x,
                                                     encodingProjSize.height / 2 + center.y);

            var renderProjection = makeSafeRenderProjection(
                Rect.MinMaxRect(Mathf.Max(encodingProjection.xMin, projection.x),
                                Mathf.Max(encodingProjection.yMin, projection.w),
                                Mathf.Min(encodingProjection.xMax, projection.z),
                                Mathf.Min(encodingProjection.yMax, projection.y))
            );

            onairvr_GetViewNumber(playerID, timeStamp, 
                                  leftEyeOrientation.x, leftEyeOrientation.y, leftEyeOrientation.z, leftEyeOrientation.w, 
                                  renderProjection.xMin, renderProjection.yMax, renderProjection.xMax, renderProjection.yMin,
                                  encodingProjection.xMin, encodingProjection.yMax, encodingProjection.xMax, encodingProjection.yMin,
                                  out _viewNumber);

            if (bypassPrediction) {
                updateCameraTransforms(_config, leftEyePosition, leftEyeOrientation);
            }
            else {
                updateCameraTransforms(_config, leftEyePosition, leftEyeOrientation, rightEyePosition, rightEyeOrientation);
            }

            updateCameraProjection(_config, renderProjection, encodingProjection);
            updateControllerTransforms(_config);

            mediaStream.GetNextFramebufferTexturesAsRenderTargets(cameras);

            _cameraEventEmitter.UpdatePerFrame(_viewNumber);
        }
        inputStream.UpdateSenders();
    }

    private void OnDestroy() {
        if (Application.isPlaying == false) {
            return;
        }

        if (AirVRCameraRigManager.CheckIfExistManagerOnCurrentScene()) {
            AirVRCameraRigManager.managerOnCurrentScene.eventDispatcher.MessageReceived -= onAirVRMessageReceived;
            AirVRCameraRigManager.managerOnCurrentScene.UnregisterCameraRig(this);
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

    private void onAirVRMessageReceived(AirVRMessage message) {
        AirVRServerMessage serverMessage = message as AirVRServerMessage;
        int srcPlayerID = serverMessage.source.ToInt32();
        if (srcPlayerID != playerID) {
            return;
        }

        if (serverMessage.IsMediaStreamEvent()) {
            if (serverMessage.Name.Equals(AirVRServerMessage.NameInitialized)) {
                onAirVRMediaStreamInitialized(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirVRServerMessage.NameStarted)) {
                onAirVRMediaStreamStarted(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirVRServerMessage.NameEncodeVideoFrame)) {
                onAirVRMediaStreamEncodeVideoFrame(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirVRServerMessage.NameSetCameraProjection)) {
                onAirVRMediaStreamSetCameraProjection(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirVRServerMessage.NameStopped)) {
                onAirVRMediaStreamStopped(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirVRServerMessage.NameCleanupUp)) {
                onAirVRMediaStreamCleanedUp(serverMessage);
            }
        }
        else if (serverMessage.IsInputStreamEvent()) {
            if (serverMessage.Name.Equals(AirVRServerMessage.NameRemoteInputDeviceRegistered)) {
                onAirVRInputStreamRemoteInputDeviceRegistered(serverMessage);
            }
            else if (serverMessage.Name.Equals(AirVRServerMessage.NameRemoteInputDeviceUnregistered)) {
                onAirVRInputStreamRemoteInputDeviceUnregistered(serverMessage);
            }
        }
    }

    private void onAirVRMediaStreamInitialized(AirVRServerMessage message) {
        Assert.IsNull(mediaStream);

        initializeCamerasForMediaStream();
        onairvr_SendCameraClipPlanes(playerID, cameras[0].nearClipPlane, cameras[0].farClipPlane);

        mediaStream = new AirVRServerMediaStream(playerID, _config, cameras.Length);
        GL.IssuePluginEvent(onairvr_InitStreams_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)playerID));

        inputStream.Init();
    }

    private void onAirVRMediaStreamStarted(AirVRServerMessage message) {
        startToRenderCamerasForMediaStream();
        inputStream.Start();
    }

    private void onAirVRMediaStreamEncodeVideoFrame(AirVRServerMessage message) {
        _encodeVideoFrameRequested = true;
    }

    private void onAirVRMediaStreamSetCameraProjection(AirVRServerMessage message) {
        updateCameraProjection(_config, message.CameraProjection);
    }

    private void onAirVRMediaStreamStopped(AirVRServerMessage message) {
        onStopRender();
        disableCameras();

        _mediaStreamJustStopped = true; // StopCoroutine(_CallPluginEndOfFrame) executes the routine one more in the next frame after the call. 
                                        // so use a flag to completely stop the routine.

        GL.IssuePluginEvent(onairvr_ResetStreams_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)playerID));

        inputStream.Stop();
    }

    private void onAirVRMediaStreamCleanedUp(AirVRServerMessage message) {
        Assert.IsTrue(_mediaStreamJustStopped);
        Assert.IsNotNull(mediaStream);

        inputStream.Cleanup();

        GL.IssuePluginEvent(onairvr_CleanupStreams_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)playerID));

        mediaStream.Destroy();
        mediaStream = null;

        foreach (Camera cam in cameras) {
            cam.targetTexture = null;
        }
    }

    private void onAirVRInputStreamRemoteInputDeviceRegistered(AirVRServerMessage message) {
        Assert.IsTrue(string.IsNullOrEmpty(message.DeviceName) == false);

        inputStream.HandleRemoteInputDeviceRegistered(message.DeviceName, (byte)message.DeviceID, message.Options);
    }

    private void onAirVRInputStreamRemoteInputDeviceUnregistered(AirVRServerMessage message) {
        inputStream.HandleRemoteInputDeviceUnregistered((byte)message.DeviceID);
    }

    private IEnumerator CallPluginEndOfFrame() {
        yield return new WaitForEndOfFrame();

        Assert.IsNotNull(mediaStream);
        GL.IssuePluginEvent(onairvr_EncodeVideoFrame_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)playerID, (uint)_viewNumber, (uint)mediaStream.currentFramebufferIndex));    // the first render event

        while (_mediaStreamJustStopped == false) {
            yield return new WaitForEndOfFrame();

            if (_mediaStreamJustStopped) {
                yield break;
            }
            else if (_encodeVideoFrameRequested) {
                Assert.IsNotNull(mediaStream);

                GL.IssuePluginEvent(onairvr_EncodeVideoFrame_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)playerID, (uint)_viewNumber, (uint)mediaStream.currentFramebufferIndex));
                _encodeVideoFrameRequested = false;
            }
        }
    }

    protected Transform getOrCreateGameObject(string name, Transform parent) {
        Transform result = findDirectChildByName(parent, name);
        if (result == null) {
            result = new GameObject(name).transform;
            result.parent = parent;
            result.localPosition = Vector3.zero;
            result.localRotation = Quaternion.identity;
            result.localScale = Vector3.one;
        }
        return result;
    }

    protected abstract void ensureGameObjectIntegrity();
    protected virtual void init() { }
    protected abstract void setupCamerasOnBound(AirVRClientConfig config);
    protected virtual void onStartRender() { }
    protected virtual void onStopRender() { }
    protected abstract void updateCameraProjection(AirVRClientConfig config, float[] projection);
    protected abstract void updateCameraProjection(AirVRClientConfig config, Rect renderProjection, Rect encodingProjection);
    protected abstract void updateCameraTransforms(AirVRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation);
    protected virtual void updateCameraTransforms(AirVRClientConfig config, Vector3 leftEyePosition, Quaternion leftEyeOrientation, Vector3 rightEyePosition, Quaternion rightEyeOrientation) { }
    protected virtual void updateControllerTransforms(AirVRClientConfig config) { }

    internal int playerID { get; private set; }

    internal AirVRServerInputStream inputStream { get; private set; }
    public AirVRServerMediaStream mediaStream { get; private set; }

    internal bool isStreaming {
        get {
            return onairvr_IsStreaming(playerID);
        }
    }

    internal abstract Matrix4x4 clientSpaceToWorldMatrix { get; }

    internal abstract Transform headPose { get; }

    internal abstract Camera[] cameras { get; }

    internal void BindPlayer(int playerID) {
        Assert.IsFalse(isBoundToClient);
        Assert.IsNull(_config);

        this.playerID = playerID;
        _config = AirVRServerPlugin.GetConfig(playerID);
        _cameraEventEmitter.Bind(playerID);

        Assert.IsNotNull(_config);
    }

    internal void BindPlayer(int playerID, AirVRServerMediaStream mediaStream, AirVRServerInputStream inputStream) {
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

        playerID = InvalidPlayerID;
        _config = null;

        _cameraEventEmitter.Unbind();
    }

    internal void PreHandOverStreams() {
        Assert.IsTrue(isBoundToClient);

        inputStream.DisableAllDeviceFeedbacks();
    }

    internal void PostHandOverStreams() {
        foreach (Camera cam in cameras) {
            cam.targetTexture = null;
        }
    }

    internal void EnableNetworkTimeWarp(bool enable) {
        if (isBoundToClient) {
            onairvr_EnableNetworkTimeWarp(playerID, enable);
        }
    }

    public AirVRClientType type {
        get {
            return GetType() == typeof(AirVRStereoCameraRig) ? AirVRClientType.Stereoscopic : AirVRClientType.Monoscopic;
        }
    }

    public bool isBoundToClient {
        get {
            return playerID >= 0;
        }
    }

    public AirVRClientConfig GetConfig() {
        if (isBoundToClient) {
            return _config;
        }
        return null;
    }

    public void AdjustBitrate(uint bitrateInKbps) {
        if (isBoundToClient) {
            GL.IssuePluginEvent(onairvr_AdjustBitRate_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)playerID, bitrateInKbps));
        }
    }

    public void RecenterPose() {
        if (isBoundToClient) {
            onairvr_RecenterPose(playerID);
        }
    }

    public void SendUserData(byte[] data) {
        if (isBoundToClient) {
            IntPtr ptr = Marshal.AllocHGlobal(data.Length);

            try {
                Marshal.Copy(data, 0, ptr, data.Length);
                onairvr_SendUserData(playerID, ptr, data.Length);
            }
            finally {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }

    public void Disconnect() {
        if (isBoundToClient) {
            onairvr_Disconnect(playerID);
        }
    }

    private class AirVRCameraEventEmitter : MonoBehaviour {
        private int _playerID = InvalidPlayerID;
        private int _viewNumber;

        public bool bound {
            get { return _playerID != InvalidPlayerID; }
        }

        public void Bind(int playerID) {
            _playerID = playerID;
        }

        public void Unbind() {
            _playerID = InvalidPlayerID;
        }

        public void UpdatePerFrame(int viewNumber) {
            _viewNumber = viewNumber;
        }

        public void OnPreRender() {
            if (bound) {
                GL.IssuePluginEvent(onairvr_GameFrameRenderingStarted_RenderThread_Func(), AirVRServerPlugin.RenderEventArg((uint)_playerID, (uint)_viewNumber));
            }
        }
    }
}
