using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HTC.UnityPlugin.FoveatedRendering 
{
    public class ViveFoveatedRenderer : MonoBehaviour {
        private const float OuterRadii = 10.0f;

        private ViveFoveatedCamera[] _cameras;

        [SerializeField] private float _innerRadii = 0.25f;
        [SerializeField] private float _middleRadii = 0.33f;
        [SerializeField] private ShadingRate _innerRate = ShadingRate.X1_PER_PIXEL;
        [SerializeField] private ShadingRate _middleRate = ShadingRate.X1_PER_2X2_PIXELS;
        [SerializeField] private ShadingRate _outerRate = ShadingRate.X1_PER_4X4_PIXELS;

        public bool initialized { get; private set; }

        public void UpdateGazeDirection(Vector3 left, Vector3 right) {
            ViveFoveatedRenderingAPI.SetNormalizedGazeDirection(left.normalized, right.normalized);
        }

        public void UpdateScale(float scale, float aspect) {
            ViveFoveatedRenderingAPI.SetRegionRadii(TargetArea.INNER, new Vector2(_innerRadii / scale, _innerRadii / scale * aspect));
            ViveFoveatedRenderingAPI.SetRegionRadii(TargetArea.MIDDLE, new Vector2(_middleRadii / scale, _middleRadii / scale * aspect));
            ViveFoveatedRenderingAPI.SetRegionRadii(TargetArea.PERIPHERAL, new Vector2(OuterRadii / scale, OuterRadii / scale * aspect));
        }

        private void Awake() {
            _cameras = GetComponentsInChildren<ViveFoveatedCamera>();
            foreach (var cam in _cameras) {
                cam.renderer = this;
            }

            ViveFoveatedRenderingAPI.InitializeNativeLogger(str => Debug.Log(str));

            initialized = ViveFoveatedRenderingAPI.InitializeFoveatedRendering(90.0f, 1.0f);

            ViveFoveatedRenderingAPI.SetFoveatedRenderingShadingRatePreset(ShadingRatePreset.SHADING_RATE_CUSTOM);
            ViveFoveatedRenderingAPI.SetShadingRate(TargetArea.INNER, _innerRate);
            ViveFoveatedRenderingAPI.SetShadingRate(TargetArea.MIDDLE, _middleRate);
            ViveFoveatedRenderingAPI.SetShadingRate(TargetArea.PERIPHERAL, _outerRate);

            ViveFoveatedRenderingAPI.SetFoveatedRenderingPatternPreset(ShadingPatternPreset.SHADING_PATTERN_CUSTOM);
            ViveFoveatedRenderingAPI.SetRegionRadii(TargetArea.INNER, Vector2.one * _innerRadii);
            ViveFoveatedRenderingAPI.SetRegionRadii(TargetArea.MIDDLE, Vector2.one * _middleRadii);
            ViveFoveatedRenderingAPI.SetRegionRadii(TargetArea.PERIPHERAL, Vector2.one * OuterRadii);

            ViveFoveatedRenderingAPI.SetNormalizedGazeDirection(new Vector3(0.0f, 0.25f, 1.0f), new Vector3(0.0f, -0.25f, 1.0f));
            GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
        }

        private void OnEnable() {
            if (initialized == false) { return; }

            foreach (var cam in _cameras) {
                cam.Enable();
            }
        }

        private void LateUpdate() {
            if (initialized == false) { return; }

            GL.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.UPDATE_GAZE);
        }

        private void OnDisable() {
            if (initialized == false) { return; }

            foreach (var cam in _cameras) {
                cam.Disable();
            }
        }

        private void OnDestroy() {
            ViveFoveatedRenderingAPI.ReleaseFoveatedRendering();
            ViveFoveatedRenderingAPI.ReleaseNativeLogger();
        }
    }
}

