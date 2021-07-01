using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPPPlaybackCamera : MonoBehaviour {
    private PlaybackCamera _leftCamera;
    private PlaybackCamera _rightCamera;

    public Camera leftCaptureCamera => _leftCamera.capture;

    public void Apply(MotionPredictionPlayback.MotionData motionFrame, MotionPredictionPlayback.MotionData motionHead, bool useTimewarp, float encodingProjSize) {
        _leftCamera.previewAnchor.localRotation = _rightCamera.previewAnchor.localRotation = useTimewarp ? motionHead.orientation : motionFrame.orientation;
        _leftCamera.sceneTextureAnchor.localRotation = _rightCamera.sceneTextureAnchor.localRotation = motionFrame.orientation;

        var leftProjectionMatrix = motionHead.projection.GetMatrix(_leftCamera.preview.nearClipPlane, _leftCamera.preview.farClipPlane);
        var rightProjectionMatrix = motionHead.projection.GetOtherEyeProjection(motionHead.projection).GetMatrix(_rightCamera.preview.nearClipPlane, _rightCamera.preview.farClipPlane);

        _leftCamera.preview.projectionMatrix = leftProjectionMatrix;
        _leftCamera.capture.projectionMatrix = leftProjectionMatrix;
        _rightCamera.preview.projectionMatrix = rightProjectionMatrix;
        _rightCamera.capture.projectionMatrix = rightProjectionMatrix;

        var leftFrameProjection = motionFrame.projection;
        _leftCamera.sceneTexture.localPosition = new Vector3((leftFrameProjection.right + leftFrameProjection.left) / 2,
                                                             (leftFrameProjection.top + leftFrameProjection.bottom) / 2,
                                                             1);
        _leftCamera.sceneTexture.localScale = new Vector3(encodingProjSize, encodingProjSize, 1);

        var rightFrameProjection = leftFrameProjection.GetOtherEyeProjection(motionHead.projection);
        _rightCamera.sceneTexture.localPosition = new Vector3((rightFrameProjection.right + rightFrameProjection.left) / 2,
                                                              (rightFrameProjection.top + rightFrameProjection.bottom) / 2,
                                                              1);
        _rightCamera.sceneTexture.localScale = new Vector3(encodingProjSize, encodingProjSize, 1);
    }

    public void RenderToCapture() {
        _leftCamera.capture.Render();
        _rightCamera.capture.Render();
    }

    private void Awake() {
        _leftCamera = new PlaybackCamera(transform.Find("LeftSide"));
        _rightCamera = new PlaybackCamera(transform.Find("RightSide"));
    }

    private void Start() {
        transform.localPosition = Vector3.down * 1000.0f;

        _leftCamera.Init();
        _rightCamera.Init();
    }

    private struct PlaybackCamera {
        public Transform previewAnchor;
        public Camera preview;
        public Transform captureAnchor;
        public Camera capture;
        public Transform sceneTextureAnchor;
        public Transform sceneTexture;

        public PlaybackCamera(Transform root) {
            previewAnchor = root.Find("Camera");
            preview = previewAnchor.GetComponent<Camera>();

            captureAnchor = previewAnchor.Find("CaptureCamera");
            capture = captureAnchor.GetComponent<Camera>();

            sceneTextureAnchor = root.Find("Anchor");
            sceneTexture = sceneTextureAnchor.Find("TargetTexture");
        }

        public void Init() {
            preview.aspect = capture.aspect = 1.0f;
        }
    }
}
