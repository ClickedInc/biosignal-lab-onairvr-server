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

    private AirXRServerSettings _settings;
    private List<OCSVRWorksFoveatedRenderer> _foveatedRenderer;
    private float _patternInnerRadius = 1.06f;
    private float _patternMiddleRadius = 1.42f;
    private float _patternScale = 1.0f;

    public delegate void UpdateFoveationPatternHandler(OCSVRWorksCameraRig cameraRig);
    public delegate void UpdateGazeLocationHandler(OCSVRWorksCameraRig cameraRig);

    public event UpdateFoveationPatternHandler OnUpdateFoveationPattern;
    public event UpdateGazeLocationHandler OnUpdateGazeLocation;

    public void UpdateFoveationPatternProps(float innerRadius, float middleRadius, float scale) {
        _patternInnerRadius = innerRadius;
        _patternMiddleRadius = middleRadius;
        _patternScale = scale;
    }

    public void UpdateGazeLocation(GazeLocation mono) {
        ocs_VRWorks_UpdateMonoGazeLocation(mono);
    }

    public void UpdateGazeLocation(GazeLocation left, GazeLocation right) {
        ocs_VRWorks_UpdateStereoGazeLocation(left, right);
    }

    private void Awake() {
        _settings = Resources.Load<AirXRServerSettings>("AirXRServerSettings");
        if (_settings == null) {
            _settings = ScriptableObject.CreateInstance<AirXRServerSettings>();
        }

        if (_settings.UseFoveatedRendering == false) { return; }

        OCSVRWorks.LoadOnce();
    }

    private void OnEnable() {
        if (_settings.UseFoveatedRendering == false) { return; }

        var ret = ocs_VRWorks_InitFoveatedRendering();
        if (ret != 0) {
            Debug.LogWarning("[WARNING] failed to init foveated rendering: " + ret);
            return;
        }

        ocs_VRWorks_UpdateFoveationShadingRates(new ShadingRates {
            inner = _settings.FoveatedShadingInnerRate,
            middle = _settings.FoveatedShadingMiddleRate,
            outer = _settings.FoveatedShadingOuterRate
        });

        if (_foveatedRenderer == null) {
            _foveatedRenderer = new List<OCSVRWorksFoveatedRenderer>();

            var leftEyeCamera = (transform.Find("TrackingSpace/LeftEyeAnchor") ?? transform.Find("LeftEye")).GetComponent<Camera>();
            var rightEyeCamera = (transform.Find("TrackingSpace/RightEyeAnchor") ?? transform.Find("RightEye")).GetComponent<Camera>();

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
            innerRadiiH = _patternInnerRadius * 2 * _patternScale,
            innerRadiiV = _patternInnerRadius * 2 * _patternScale,
            middleRadiiH = _patternMiddleRadius * 2 * _patternScale,
            middleRadiiV = _patternMiddleRadius * 2 * _patternScale,
            outerRadiiH = DefaultPatternOuterRadii * 2 * _patternScale,
            outerRadiiV = DefaultPatternOuterRadii * 2 * _patternScale
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
