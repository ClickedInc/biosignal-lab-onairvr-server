/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public sealed class AirVRCameraRig : AirXRCameraRig, IAirXRTrackingModelContext {
    private readonly string TrackingSpaceName = "TrackingSpace";
    private readonly string LeftEyeAnchorName = "LeftEyeAnchor";
    private readonly string RightEyeAnchorName = "RightEyeAnchor";
    private readonly string CenterEyeAnchorName = "CenterEyeAnchor";
    private readonly string LeftHandAnchorName = "LeftHandAnchor";
    private readonly string RightHandAnchorName = "RightHandAnchor";
    private readonly int CameraLeftIndex = 0;
    private readonly int CameraRightIndex = 1;

    public enum HandSelect {
        None,
        Left,
        Right,
        Both
    }

    public enum TrackingModel {
        Head,
        InterpupillaryDistanceOnly,
        ExternalTracker,
        NoPositionTracking
    }

    private Matrix4x4 _worldToHMDSpaceMatrix;

    private Camera[] _cameras;

    private AirXRTrackingModel _trackingModelObject;

    [SerializeField] private HandSelect _eventSystemResponsive = HandSelect.None;
    [SerializeField] private bool _renderControllersOnClient = false;
    [SerializeField] private bool _raycastGraphic = true;
    [SerializeField] private bool _raycastPhysics = true;
    [SerializeField] private LayerMask _physicsRaycastEventMask = -1;

    [SerializeField] private TrackingModel _trackingModel = TrackingModel.Head;
    [SerializeField] private Transform _externalTrackingOrigin = null;
    [SerializeField] private Transform _externalTracker = null;

    internal Transform trackingSpace { get; private set; }
    internal bool renderControllersOnClient { get { return _renderControllersOnClient; } }

    public Camera leftEyeCamera {
        get {
            return _cameras[CameraLeftIndex];
        }
    }

    public Camera rightEyeCamera {
        get {
            return _cameras[CameraRightIndex];
        }
    }

    public Transform leftEyeAnchor { get; private set; }
    public Transform centerEyeAnchor { get; private set; }
    public Transform rightEyeAnchor { get; private set; }
    public Transform leftHandAnchor { get; private set; }
    public Transform rightHandAnchor { get; private set; }

    private TrackingModel trackingModelOf(AirXRTrackingModel trackingModelObject) {
        return trackingModelObject.GetType() == typeof(AirXRHeadTrackingModel)             ? TrackingModel.Head :
               trackingModelObject.GetType() == typeof(AirXRIPDOnlyTrackingModel)          ? TrackingModel.InterpupillaryDistanceOnly :
               trackingModelObject.GetType() == typeof(AirXRExternalTrackerTrackingModel)  ? TrackingModel.ExternalTracker :
               trackingModelObject.GetType() == typeof(AirXRNoPotisionTrackingModel)       ? TrackingModel.NoPositionTracking : TrackingModel.Head;
    }

    private AirXRTrackingModel createTrackingModelObject(TrackingModel model) {
        return model == TrackingModel.InterpupillaryDistanceOnly ?  new AirXRIPDOnlyTrackingModel(this, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) :
               model == TrackingModel.ExternalTracker            ?  new AirXRExternalTrackerTrackingModel(this, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor, _externalTrackingOrigin, _externalTracker) :
               model == TrackingModel.NoPositionTracking         ?  new AirXRNoPotisionTrackingModel(this, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) :
                                                                    new AirXRHeadTrackingModel(this, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) as AirXRTrackingModel;
    }

    private void updateTrackingModel() {
        if (_trackingModelObject == null || trackingModelOf(_trackingModelObject) != _trackingModel) {
            _trackingModelObject = createTrackingModelObject(_trackingModel);
        }
        if (trackingModelOf(_trackingModelObject) == TrackingModel.ExternalTracker) {
            var model = _trackingModelObject as AirXRExternalTrackerTrackingModel;
            model.trackingOrigin = _externalTrackingOrigin;
            model.tracker = _externalTracker;
        }
    }

    // implements AirXRCameraRig
    protected override Transform headAnchor => centerEyeAnchor;

    private bool ensureCameraObjectIntegrity(Transform xform) {
        if (xform.gameObject.GetComponent<Camera>() == null) {
            xform.gameObject.AddComponent<Camera>();
            return false;
        }
        return true;
    }

    protected override void ensureGameObjectIntegrity(bool create) {
        if (trackingSpace == null) {
            trackingSpace = getOrCreateGameObject(TrackingSpaceName, transform, create);
        }
        if (leftEyeAnchor == null) {
            leftEyeAnchor = getOrCreateGameObject(LeftEyeAnchorName, trackingSpace, create);
        }
        if (centerEyeAnchor == null) {
            centerEyeAnchor = getOrCreateGameObject(CenterEyeAnchorName, trackingSpace, create);
        }
        if (rightEyeAnchor == null) {
            rightEyeAnchor = getOrCreateGameObject(RightEyeAnchorName, trackingSpace, create);
        }
        if (leftHandAnchor == null) {
            leftHandAnchor = getOrCreateGameObject(LeftHandAnchorName, trackingSpace, create);
        }
        if (rightHandAnchor == null) {
            rightHandAnchor = getOrCreateGameObject(RightHandAnchorName, trackingSpace, create);
        }

        if ((leftEyeAnchor == null || rightEyeAnchor == null) && create == false) { return; }

        bool updateCamera = false;
        if (_cameras == null) {
            _cameras = new Camera[2];
            updateCamera = true;
        }

        if (ensureCameraObjectIntegrity(leftEyeAnchor) == false || updateCamera) {
            _cameras[CameraLeftIndex] = leftEyeAnchor.GetComponent<Camera>();
        }
        if (ensureCameraObjectIntegrity(rightEyeAnchor) == false || updateCamera) {
            _cameras[CameraRightIndex] = rightEyeAnchor.GetComponent<Camera>();
        }
    }

    protected override void onAwake() {
        switch (_eventSystemResponsive) {
            case HandSelect.Left:
                prepareForEventSystem(leftHandAnchor.gameObject.AddComponent<AirXRPointer>(), AXRInputDeviceID.LeftHandTracker);
                break;
            case HandSelect.Right:
                prepareForEventSystem(rightHandAnchor.gameObject.AddComponent<AirXRPointer>(), AXRInputDeviceID.RightHandTracker);
                break;
            case HandSelect.Both:
                prepareForEventSystem(leftHandAnchor.gameObject.AddComponent<AirXRPointer>(), AXRInputDeviceID.LeftHandTracker);
                prepareForEventSystem(rightHandAnchor.gameObject.AddComponent<AirXRPointer>(), AXRInputDeviceID.RightHandTracker);
                break;
        }
    }

    private void prepareForEventSystem(AirXRPointer pointer, AXRInputDeviceID srcDevice) {
        pointer.Configure(this, srcDevice);

        if (_raycastPhysics) {
            var raycaster = pointer.gameObject.AddComponent<AirXRPhysicsRaycaster>();
            raycaster.eventMask = _physicsRaycastEventMask;
        }
    }

    protected override void onStart() {
        if (_trackingModelObject == null) {
            _trackingModelObject = createTrackingModelObject(_trackingModel);
        }
    }

    protected override void setupCamerasOnBound(AirXRClientConfig config) {
#if UNITY_2018_2_OR_NEWER
        var props = config.physicalCameraProps;

        leftEyeCamera.usePhysicalProperties = true;
        leftEyeCamera.focalLength = props.focalLength;
        leftEyeCamera.sensorSize = props.sensorSize;
        leftEyeCamera.lensShift = props.leftLensShift;
        leftEyeCamera.aspect = props.aspect;
        leftEyeCamera.gateFit = Camera.GateFitMode.None;

        rightEyeCamera.usePhysicalProperties = true;
        rightEyeCamera.focalLength = props.focalLength;
        rightEyeCamera.sensorSize = props.sensorSize;
        rightEyeCamera.lensShift = props.rightLensShift;
        rightEyeCamera.aspect = props.aspect;
        rightEyeCamera.gateFit = Camera.GateFitMode.None;
#else
        leftEyeCamera.projectionMatrix = config.GetLeftEyeCameraProjection(leftEyeCamera.nearClipPlane, leftEyeCamera.farClipPlane);
        rightEyeCamera.projectionMatrix = config.GetRightEyeCameraProjection(rightEyeCamera.nearClipPlane, rightEyeCamera.farClipPlane);
#endif
    }

    protected override void onStartRender() {
        updateTrackingModel();
        _trackingModelObject.StartTracking();
    }

    protected override void onStopRender() {
        updateTrackingModel();
        _trackingModelObject.StopTracking();
    }

    protected override void updateCameraProjection(AirXRClientConfig config, float[] projection) {
        // do nothing; a stereoscopic camera must keep its inherent projection
    }

    protected override void updateCameraProjection(AirXRClientConfig config, Rect renderProjection, Rect encodingProjection) {
        float[] leftRenderProj = { renderProjection.xMin, renderProjection.yMax, renderProjection.xMax, renderProjection.yMin };

        float centerBiasX = config.cameraProjection[0] + config.cameraProjection[2];
        float[] rightRenderProj = {
            renderProjection.xMin - centerBiasX,
            renderProjection.yMax,
            renderProjection.xMax - centerBiasX,
            renderProjection.yMin
        };

        leftEyeCamera.projectionMatrix = AirXRClientConfig.CalcCameraProjectionMatrix(leftRenderProj, leftEyeCamera.nearClipPlane, leftEyeCamera.farClipPlane);
        rightEyeCamera.projectionMatrix = AirXRClientConfig.CalcCameraProjectionMatrix(rightRenderProj, rightEyeCamera.nearClipPlane, rightEyeCamera.farClipPlane);

        var viewport = new Rect(0.5f - renderProjection.width / encodingProjection.width / 2,
                                0.5f - renderProjection.height / encodingProjection.height / 2, 
                                renderProjection.width / encodingProjection.width,
                                renderProjection.height / encodingProjection.height);

        leftEyeCamera.rect = viewport;
        rightEyeCamera.rect = viewport;
    }

    protected override void updateCameraTransforms(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation) {
        updateTrackingModel();
        _trackingModelObject.UpdateEyePose(config, centerEyePosition, centerEyeOrientation);
    }

    protected override void updateCameraTransforms(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose) {
        updateTrackingModel();
        _trackingModelObject.UpdateEyePose(config, leftEyePose, rightEyePose);
    }

    protected override void updateControllerTransforms(AirXRClientConfig config) {
        var pose = inputStream.GetPose((byte)AXRInputDeviceID.LeftHandTracker, (byte)AXRHandTrackerControl.Pose);
        leftHandAnchor.localPosition = pose.position;
        leftHandAnchor.localRotation = pose.rotation;

        pose = inputStream.GetPose((byte)AXRInputDeviceID.RightHandTracker, (byte)AXRHandTrackerControl.Pose);
        rightHandAnchor.localPosition = pose.position;
        rightHandAnchor.localRotation = pose.rotation;
    }

    protected override void updateControllerTransforms(AirXRClientConfig config, AirXRPredictedMotionProvider motionProvider, bool bypassPrediction) {
        var pose = inputStream.GetPose((byte)AXRInputDeviceID.LeftHandTracker, (byte)AXRHandTrackerControl.Pose);
        leftHandAnchor.localPosition = pose.position;
        leftHandAnchor.localRotation = pose.rotation;

        pose = motionProvider == null || bypassPrediction ? inputStream.GetPose((byte)AXRInputDeviceID.RightHandTracker, (byte)AXRHandTrackerControl.Pose) :
                                                            motionProvider.rightHand;
        rightHandAnchor.localPosition = pose.position;
        rightHandAnchor.localRotation = pose.rotation;
    }

    internal override bool raycastGraphic => _raycastGraphic;
    internal override Matrix4x4 clientSpaceToWorldMatrix => _trackingModelObject.HMDSpaceToWorldMatrix;
    internal override Transform headPose => centerEyeAnchor;
    internal override Camera[] cameras => _cameras;

    // implements IAirXRTrackingModelContext
    void IAirXRTrackingModelContext.RecenterCameraRigPose() {
        RecenterPose();
    }
}
