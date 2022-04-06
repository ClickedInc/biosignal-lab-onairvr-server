using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPPSceneCamera : MonoBehaviour {
    private MotionPredictionPlayback _owner;
    private Transform _leftEyeAnchor;
    private Camera _leftEyeCamera;
    private Transform _rightEyeAnchor;
    private Camera _rightEyeCamera;
    private OCSVRWorksCameraRig.GazeLocation _leftGazeLocation = new OCSVRWorksCameraRig.GazeLocation { x = 0, y = 0 };
    private OCSVRWorksCameraRig.GazeLocation _rightGazeLocation = new OCSVRWorksCameraRig.GazeLocation { x = 0, y = 0 };
    private float _foveationPatternInnerRadius = MPPMotionDataProvider.DefaultFoveationInnerRadius;
    private float _foveationPatternMiddleRadius = MPPMotionDataProvider.DefaultFoveationMiddleRadius;
    private float _foveationPatternScale = 1.0f;

    public OCSVRWorksCameraRig foveatedRenderer { get; private set; }

    public void Apply(MPPMotionData motionFrame, MPPMotionData motionHead, Vector2 encodingProjSize) {
        _leftEyeAnchor.localPosition = motionFrame.leftEyePos;
        _rightEyeAnchor.localPosition = motionFrame.rightEyePos;
        _leftEyeAnchor.localRotation = _rightEyeAnchor.localRotation = motionFrame.orientation;

        _leftEyeCamera.projectionMatrix = motionFrame.leftProjection.GetMatrix(_leftEyeCamera.nearClipPlane, _leftEyeCamera.farClipPlane);
        _rightEyeCamera.projectionMatrix = motionFrame.rightProjection.GetMatrix(_rightEyeCamera.nearClipPlane, _rightEyeCamera.farClipPlane);

        _leftEyeCamera.rect = new Rect(0.5f - motionFrame.leftProjection.width / encodingProjSize.x / 2,
                                       0.5f - motionFrame.leftProjection.height / encodingProjSize.y / 2,
                                       motionFrame.leftProjection.width / encodingProjSize.x,
                                       motionFrame.leftProjection.height / encodingProjSize.y); ;
        _rightEyeCamera.rect = new Rect(0.5f - motionFrame.rightProjection.width / encodingProjSize.x / 2,
                                        0.5f - motionFrame.rightProjection.height / encodingProjSize.y / 2,
                                        motionFrame.rightProjection.width / encodingProjSize.x,
                                        motionFrame.rightProjection.height / encodingProjSize.y); ;

        _leftEyeCamera.targetTexture.Release();
        _rightEyeCamera.targetTexture.Release();

        _foveationPatternInnerRadius = motionFrame.foveationInnerRadius;
        _foveationPatternMiddleRadius = motionFrame.foveationMiddleRadius;
        _foveationPatternScale = 1 / (motionFrame.leftProjection.aspect >= 1.0f ? motionFrame.leftProjection.height : motionFrame.leftProjection.width);

        _leftGazeLocation.x = -motionFrame.leftProjection.center.x / motionFrame.leftProjection.width;
        _leftGazeLocation.y = -motionFrame.leftProjection.center.y / motionFrame.leftProjection.height;
        _rightGazeLocation.x = -motionFrame.rightProjection.center.x / motionFrame.rightProjection.width;
        _rightGazeLocation.y = -motionFrame.rightProjection.center.y / motionFrame.rightProjection.height;
    }

    public void Render() {
        _leftEyeCamera.Render();
        _rightEyeCamera.Render();
    }

    private void Awake() {
        _owner = GetComponentInParent<MotionPredictionPlayback>();

        _leftEyeAnchor = transform.Find("LeftEye");
        _leftEyeCamera = _leftEyeAnchor.GetComponent<Camera>();

        _rightEyeAnchor = transform.Find("RightEye");
        _rightEyeCamera = _rightEyeAnchor.GetComponent<Camera>();

        foveatedRenderer = GetComponent<OCSVRWorksCameraRig>();

        foveatedRenderer.OnUpdateFoveationPattern += onUpdateFoveationPattern;
        foveatedRenderer.OnUpdateGazeLocation += onUpdateGazeLocation;
    }

    private void Start() {
        foveatedRenderer.enabled = _owner.playbackModeStartedByEditor ||
                                   _owner.settings.FoveatedRenderPriority == AirXRServerSettings.FoveatedRenderingPriority.PlaybackFirst;
    }

    private void OnDestroy() {
        foveatedRenderer.OnUpdateFoveationPattern -= onUpdateFoveationPattern;
        foveatedRenderer.OnUpdateGazeLocation -= onUpdateGazeLocation;
    }

    private void onUpdateFoveationPattern(OCSVRWorksCameraRig cameraRig) {
        foveatedRenderer.UpdateFoveationPatternProps(_foveationPatternInnerRadius, _foveationPatternMiddleRadius, _foveationPatternScale);
    }

    private void onUpdateGazeLocation(OCSVRWorksCameraRig cameraRig) {
        foveatedRenderer.UpdateGazeLocation(_leftGazeLocation, _rightGazeLocation);
    }
}
