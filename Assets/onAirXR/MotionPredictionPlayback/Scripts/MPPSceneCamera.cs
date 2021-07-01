using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPPSceneCamera : MonoBehaviour {
    private Transform _leftEyeAnchor;
    private Camera _leftEyeCamera;
    private Transform _rightEyeAnchor;
    private Camera _rightEyeCamera;

    public void Apply(MotionPredictionPlayback.MotionData motionFrame, MotionPredictionPlayback.MotionData motionHead, float encodingProjSize) {
        _leftEyeAnchor.localPosition = motionFrame.leftEyePos;
        _rightEyeAnchor.localPosition = motionFrame.rightEyePos;
        _leftEyeAnchor.localRotation = _rightEyeAnchor.localRotation = motionFrame.orientation;

        var leftProjection = motionFrame.projection;
        var rightProjection = leftProjection.GetOtherEyeProjection(motionHead.projection);

        _leftEyeCamera.projectionMatrix = leftProjection.GetMatrix(_leftEyeCamera.nearClipPlane, _leftEyeCamera.farClipPlane);
        _rightEyeCamera.projectionMatrix = rightProjection.GetMatrix(_rightEyeCamera.nearClipPlane, _rightEyeCamera.farClipPlane);

        var renderProjWidth = motionFrame.projection.right - motionFrame.projection.left;
        var renderProjHeight = motionFrame.projection.top - motionFrame.projection.bottom;

        var viewport = new Rect(0.5f - renderProjWidth / encodingProjSize / 2,
                                0.5f - renderProjHeight / encodingProjSize / 2,
                                renderProjWidth / encodingProjSize,
                                renderProjHeight / encodingProjSize);
        _leftEyeCamera.rect = viewport;
        _rightEyeCamera.rect = viewport;

        _leftEyeCamera.targetTexture.Release();
        _rightEyeCamera.targetTexture.Release();
    }

    public void Render() {
        _leftEyeCamera.Render();
        _rightEyeCamera.Render();
    }

    private void Awake() {
        _leftEyeAnchor = transform.Find("LeftEye");
        _leftEyeCamera = _leftEyeAnchor.GetComponent<Camera>();

        _rightEyeAnchor = transform.Find("RightEye");
        _rightEyeCamera = _rightEyeAnchor.GetComponent<Camera>();
    }
}
