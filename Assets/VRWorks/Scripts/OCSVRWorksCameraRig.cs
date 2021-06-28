using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;

public class OCSVRWorksCameraRig : MonoBehaviour {
    private const float DefaultPatternOuterRadii = 10.0f;

    [StructLayout(LayoutKind.Sequential)]
    public struct GazeLocation {
        public float x;
        public float y;
    }

    [DllImport(OCSVRWorks.LibName)]
    private extern static int ocs_VRWorks_InitFoveatedRendering();

    [DllImport(OCSVRWorks.LibName)]
    private extern static void ocs_VRWorks_UpdateFoveationShadingRates(ShadingRates shadingRates);

    [DllImport(OCSVRWorks.LibName)]
    private extern static void ocs_VRWorks_UpdateFoveationPattern(FoveationPattern pattern);

    [DllImport(OCSVRWorks.LibName)]
    private extern static void ocs_VRWorks_BeginUpdateGazeLocation();

    [DllImport(OCSVRWorks.LibName)]
    private extern static void ocs_VRWorks_UpdateMonoGazeLocation(GazeLocation location);

    [DllImport(OCSVRWorks.LibName)]
    private extern static void ocs_VRWorks_UpdateStereoGazeLocation(GazeLocation left, GazeLocation right);

    [DllImport(OCSVRWorks.LibName)]
    private extern static void ocs_VRWorks_EndUpdateGazeLocation();

    private List<OCSVRWorksFoveatedRenderer> _foveatedRenderer;
    private float _foveationPatternScale = 1.0f;
    private float _foveationPatternAspect = 1.0f;

    [SerializeField] private float _patternInnerRadii = 1.06f;
    [SerializeField] private float _patternMiddleRadii = 1.42f;
    [SerializeField] private OCSVRWorksFoveatedRenderer.ShadingRate _shadingInnerRate = OCSVRWorksFoveatedRenderer.ShadingRate.X1;
    [SerializeField] private OCSVRWorksFoveatedRenderer.ShadingRate _shadingMiddleRate = OCSVRWorksFoveatedRenderer.ShadingRate.X1_PER_2x2;
    [SerializeField] private OCSVRWorksFoveatedRenderer.ShadingRate _shadingOuterRate = OCSVRWorksFoveatedRenderer.ShadingRate.X1_PER_4x4;

    public delegate void UpdateFoveationPatternHandler(OCSVRWorksCameraRig cameraRig);
    public delegate void UpdateGazeLocationHandler(OCSVRWorksCameraRig cameraRig);

    public event UpdateFoveationPatternHandler OnUpdateFoveationPattern;
    public event UpdateGazeLocationHandler OnUpdateGazeLocation;

    public void UpdateFoveationPattern(float scale, float aspect) {
        _foveationPatternScale = scale;
        _foveationPatternAspect = aspect;
    }

    public void UpdateGazeLocation(GazeLocation mono) {
        ocs_VRWorks_UpdateMonoGazeLocation(mono);
    }

    public void UpdateGazeLocation(GazeLocation left, GazeLocation right) {
        ocs_VRWorks_UpdateStereoGazeLocation(left, right);
    }

    private void Awake() {
        OCSVRWorks.LoadOnce();
    }

    private void OnEnable() {
        var ret = ocs_VRWorks_InitFoveatedRendering();
        if (ret != 0) {
            Debug.LogWarning("[WARNING] failed to init foveated rendering: " + ret);
            return;
        }

        ocs_VRWorks_UpdateFoveationShadingRates(new ShadingRates {
            inner = _shadingInnerRate,
            middle = _shadingMiddleRate,
            outer = _shadingOuterRate
        });

        if (_foveatedRenderer == null) {
            _foveatedRenderer = new List<OCSVRWorksFoveatedRenderer>();

            var leftEyeCamera = transform.Find("TrackingSpace/LeftEyeAnchor").GetComponent<Camera>();
            var rightEyeCamera = transform.Find("TrackingSpace/RightEyeAnchor").GetComponent<Camera>();

            Assert.IsNotNull(leftEyeCamera);
            Assert.IsNotNull(rightEyeCamera);

            _foveatedRenderer.Add(new OCSVRWorksFoveatedRenderer(leftEyeCamera, OCSVRWorksFoveatedRenderer.RenderMode.Left, leftEyeCamera.depth));
            _foveatedRenderer.Add(new OCSVRWorksFoveatedRenderer(rightEyeCamera, OCSVRWorksFoveatedRenderer.RenderMode.Right, leftEyeCamera.depth));
        }

        foreach (var renderer in _foveatedRenderer) {
            renderer.Enable();
        }
    }

    private void LateUpdate() {
        if (_foveatedRenderer == null) { return; }

        OnUpdateFoveationPattern?.Invoke(this);

        ocs_VRWorks_UpdateFoveationPattern(new FoveationPattern {
            innerRadiiH = _patternInnerRadii * _foveationPatternScale * _foveationPatternAspect,
            innerRadiiV = _patternInnerRadii * _foveationPatternScale,
            middleRadiiH = _patternMiddleRadii * _foveationPatternScale * _foveationPatternAspect,
            middleRadiiV = _patternMiddleRadii * _foveationPatternScale,
            outerRadiiH = DefaultPatternOuterRadii * _foveationPatternScale * _foveationPatternAspect,
            outerRadiiV = DefaultPatternOuterRadii * _foveationPatternScale
        });

        if (OnUpdateGazeLocation != null) {
            ocs_VRWorks_BeginUpdateGazeLocation();
            OnUpdateGazeLocation.Invoke(this);
            ocs_VRWorks_EndUpdateGazeLocation();
        }
    }

    private void OnDisable() {
        if (_foveatedRenderer == null) { return; }

        foreach (var renderer in _foveatedRenderer) {
            renderer.Disable();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShadingRates {
        public OCSVRWorksFoveatedRenderer.ShadingRate inner;
        public OCSVRWorksFoveatedRenderer.ShadingRate middle;
        public OCSVRWorksFoveatedRenderer.ShadingRate outer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FoveationPattern {
        public float innerRadiiH;
        public float innerRadiiV;
        public float middleRadiiH;
        public float middleRadiiV;
        public float outerRadiiH;
        public float outerRadiiV;
    }
}
