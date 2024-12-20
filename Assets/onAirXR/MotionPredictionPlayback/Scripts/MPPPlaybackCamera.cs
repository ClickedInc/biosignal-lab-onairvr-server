﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPPPlaybackCamera : MonoBehaviour {
    private MotionPredictionPlayback _owner;
    private PlaybackCamera _leftCamera;
    private PlaybackCamera _rightCamera;
    private float _displayAspect = 2.0f;

    public Camera leftCaptureCamera => _leftCamera.capture;

    public void Apply(MPPMotionData motionFrame, MPPMotionData motionHead, bool useTimewarp, Vector2 encodingProjSize) {
        _leftCamera.previewAnchor.localRotation = _rightCamera.previewAnchor.localRotation = useTimewarp ? motionHead.orientation : motionFrame.orientation;
        _leftCamera.sceneTextureAnchor.localRotation = _rightCamera.sceneTextureAnchor.localRotation = motionFrame.orientation;

        var leftProjectionMatrix = motionHead.leftProjection.GetMatrix(_leftCamera.preview.nearClipPlane, _leftCamera.preview.farClipPlane);
        var rightProjectionMatrix = motionHead.rightProjection.GetMatrix(_rightCamera.preview.nearClipPlane, _rightCamera.preview.farClipPlane);

        var leftViewportWidth = 0.5f * motionHead.leftProjection.aspect;
        _leftCamera.preview.projectionMatrix = motionHead.leftProjection.GetMatrix(_leftCamera.preview.nearClipPlane, _leftCamera.preview.farClipPlane, _owner.settings.PreviewRenderScale);
        _leftCamera.preview.rect = new Rect(0.5f - leftViewportWidth, 1 - _displayAspect / 2, leftViewportWidth, 1);

        var rightViewportWidth = 0.5f * motionHead.rightProjection.aspect;
        _rightCamera.preview.projectionMatrix = motionHead.rightProjection.GetMatrix(_rightCamera.preview.nearClipPlane, _rightCamera.preview.farClipPlane, _owner.settings.PreviewRenderScale);
        _rightCamera.preview.rect = new Rect(0.5f, 1 - _displayAspect / 2, rightViewportWidth, 1);

        _leftCamera.capture.projectionMatrix = leftProjectionMatrix;
        _rightCamera.capture.projectionMatrix = rightProjectionMatrix;

        _leftCamera.SetProjection(motionHead.leftProjection, motionFrame.leftProjection);

        _leftCamera.sceneTexture.localPosition = new Vector3(motionFrame.leftProjection.center.x,
                                                             motionFrame.leftProjection.center.y,
                                                             1.0f);
        _leftCamera.sceneTexture.localScale = new Vector3(encodingProjSize.x, encodingProjSize.y, 1);

        _rightCamera.sceneTexture.localPosition = new Vector3(motionFrame.rightProjection.center.x,
                                                              motionFrame.rightProjection.center.y,
                                                              1.0f);
        _rightCamera.sceneTexture.localScale = new Vector3(encodingProjSize.x, encodingProjSize.y, 1);

        var mat = _leftCamera.sceneTextureRenderer.material;
        if (mat.HasProperty("_Bound")) {
            mat.SetFloat("_Opacity", _owner.settings.VisualizeRenderingInfo && _owner.settings.UseFoveatedRendering ? 1.0f : 0.0f);

            mat.SetFloat("_InnerRadii", motionFrame.foveationInnerRadius);
            mat.SetFloat("_MidRadii", motionFrame.foveationMiddleRadius);
            mat.SetFloat("_GazeX", -motionFrame.leftProjection.center.x);
            mat.SetFloat("_GazeY", -motionFrame.leftProjection.center.y);
            mat.SetVector("_Bound", new Vector4(-motionFrame.leftProjection.width / 2, 
                                                motionFrame.leftProjection.height / 2, 
                                                motionFrame.leftProjection.width / 2, 
                                                -motionFrame.leftProjection.height / 2));
        }
    }

    public void RenderToCapture() {
        _leftCamera.capture.Render();
        _rightCamera.capture.Render();
    }

    private void Awake() {
        _owner = GetComponentInParent<MotionPredictionPlayback>();
        _leftCamera = new PlaybackCamera(transform.Find("LeftSide"));
        _rightCamera = new PlaybackCamera(transform.Find("RightSide"));

        _displayAspect = (float)Display.main.renderingWidth / Display.main.renderingHeight;

        var error = 0.01f;
        if (_displayAspect > 2 + error) {
            throw new UnityException("[ERROR] the display aspect must be less or equal than 2.");
        }
    }

    private void Start() {
        transform.localPosition = Vector3.down * 1000.0f;

        _leftCamera.Init(_owner.settings.VisualizeRenderingInfo);
        _rightCamera.Init(_owner.settings.VisualizeRenderingInfo);

        var pos = Vector3.forward;
        _leftCamera.sceneTexture.localPosition = pos;
        _rightCamera.sceneTexture.localPosition = pos;
        _leftCamera.eyeViewport.transform.localPosition = pos;
        _leftCamera.frameViewport.transform.localPosition = pos;

        if (_leftCamera.frameBorder != null) {
            _leftCamera.frameBorder.transform.localPosition = pos;
        }
    }

    private struct PlaybackCamera {
        public Transform previewAnchor;
        public Camera preview;
        public Transform captureAnchor;
        public Camera capture;
        public Transform sceneTextureAnchor;
        public Transform sceneTexture;
        public MeshRenderer sceneTextureRenderer;
        public MeshRenderer eyeViewport;
        public MeshRenderer frameViewport;
        public MeshRenderer frameBorder;

        public PlaybackCamera(Transform root) {
            previewAnchor = root.Find("Eye");
            preview = previewAnchor.GetComponent<Camera>();

            captureAnchor = previewAnchor.Find("CaptureCamera");
            capture = captureAnchor.GetComponent<Camera>();

            sceneTextureAnchor = root.Find("Frame");
            sceneTexture = sceneTextureAnchor.Find("TargetTexture");
            sceneTextureRenderer = sceneTexture.GetComponent<MeshRenderer>();

            eyeViewport = previewAnchor.Find("Viewport")?.GetComponent<MeshRenderer>();
            frameViewport = sceneTextureAnchor.Find("Viewport")?.GetComponent<MeshRenderer>();
            frameBorder = sceneTextureAnchor.Find("Border")?.GetComponent<MeshRenderer>();
        }

        public void Init(bool showRenderingInfo) {
            preview.aspect = capture.aspect = 1.0f;

            eyeViewport?.gameObject.SetActive(showRenderingInfo);
            frameViewport?.gameObject.SetActive(showRenderingInfo);
            frameBorder?.gameObject.SetActive(showRenderingInfo);
        }

        public void SetProjection(MPPProjection eyeProjection, MPPProjection frameProjection) {
            setViewportProjection(eyeViewport, eyeProjection);
            setViewportProjection(frameViewport, eyeProjection);
            setViewportProjection(frameBorder, frameProjection);
        }

        private void setViewportProjection(MeshRenderer viewport, MPPProjection projection) {
            if (viewport == null) { return; }

            viewport.material.SetVector("_Projection", new Vector4(projection.left, projection.top, projection.right, projection.bottom));
        }
    }
}
