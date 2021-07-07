using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MPPRenderPerfGraph : MonoBehaviour {
    private MotionPredictionPlayback _owner;
    private Camera _camera;
    private Material _material;
    private List<Metrics> _points = new List<Metrics>();

    private Color colorBasis => _owner.settings.RenderPerfGraphColorBasis;
    private Color colorOverfillOnly => _owner.settings.RenderPerfGraphColorOverfillOnly;
    private Color colorFoveatedOverfill => _owner.settings.RenderPerfGraphColorFoveatedOverfill;
    private int length => _owner.settings.RenderPerfGraphLength;
    private bool autoFitRange => _owner.settings.RenderPerfGraphAutoFitRangeY;

    [SerializeField] private Vector2 _range = Vector2.zero;

    public void AddPoint(MPPMotionData motionHead, MPPMotionData motionFrame) {
        var ideal = motionHead.projection.width * motionHead.projection.height;
        var overfillOnly = motionFrame.projection.width * motionFrame.projection.height;

        var innerArea = calcRadiiArea(motionFrame.projection, motionFrame.foveationInnerRadius);
        var middleArea = calcRadiiArea(motionFrame.projection, motionFrame.foveationMiddleRadius);
        var foveatedOverfill = innerArea + (middleArea - innerArea) / 4 + (overfillOnly - middleArea) / 16;

        var point = new Metrics {
            ideal = ideal,
            overfillOnly = overfillOnly,
            foveatedOverfill = foveatedOverfill
        };

        _points.Add(point);

        if (_points.Count > length) {
            _points.RemoveAt(0);
        }

        if (autoFitRange) {
            var range = new Vector2(Mathf.Min(point.ratioOfOverfillOnlyToIdeal, point.ratioOfFoveatedOverfillToIdeal),
                                Mathf.Max(point.ratioOfOverfillOnlyToIdeal, point.ratioOfFoveatedOverfillToIdeal));
            _range = new Vector2(Mathf.Min(range.x, _range.x), Mathf.Max(range.y, _range.y));
        }
    }

    private void Awake() {
        _owner = GetComponentInParent<MotionPredictionPlayback>();
        _camera = GetComponent<Camera>();
        _material = new Material(Shader.Find("onAirXR/Unlit Vertex"));
    }

    private void Start() {
        var displayAspect = (float)Display.main.renderingWidth / Display.main.renderingHeight;

        var error = 0.01f;
        if (displayAspect > 2 - error) {
            gameObject.SetActive(false);
            return;
        }

        _camera.rect = Rect.MinMaxRect(0, 0, 1, 1 - displayAspect / 2);
        _range = _owner.settings.RenderPerfGraphDefaultRangeY;
    }

    private void OnPostRender() {
        if (_points.Count == 0) { return; }

        GL.PushMatrix();
        _material.SetPass(0);
        GL.LoadOrtho();

        renderBasis();
        renderOverfillOnly();
        renderFoveatedOverfill();

        GL.PopMatrix();
    }

    private float calcRadiiArea(MPPProjection projection, float radius) {
        var overflow_l = calcOverflowedSideArea(radius, -projection.left);
        var overflow_t = calcOverflowedSideArea(radius, projection.top);
        var overflow_r = calcOverflowedSideArea(radius, projection.right);
        var overflow_b = calcOverflowedSideArea(radius, -projection.bottom);

        var overflow_lt = calcOverflowedCornerArea(radius, -projection.left, projection.top);
        var overflow_rt = calcOverflowedCornerArea(radius, projection.right, projection.top);
        var overflow_rb = calcOverflowedCornerArea(radius, projection.right, -projection.bottom);
        var overflow_lb = calcOverflowedCornerArea(radius, -projection.left, -projection.bottom);

        return Mathf.PI * radius * radius - (overflow_l + overflow_t + overflow_r + overflow_b) + (overflow_lt + overflow_rt + overflow_rb + overflow_lb);
    }

    private float calcOverflowedSideArea(float radius, float side) {
        var cut = Mathf.Abs(side);
        if (cut >= radius) { return 0; }

        var halfAngle = Mathf.Acos(cut / radius);
        var sector = halfAngle * radius * radius;
        var segment = sector - cut * radius * Mathf.Sin(halfAngle);

        if (side >= 0) {
            return segment;
        }
        else {
            return Mathf.PI * radius * radius - segment;
        }
    }

    private float calcOverflowedCornerArea(float radius, float side, float top) {
        if (new Vector2(side, top).magnitude >= radius) { return 0; }

        var side_segment = calcOverflowedSideArea(radius, Mathf.Abs(side));
        var top_cross = Mathf.Sqrt(radius * radius - top * top);
        var top_cross_segment = calcOverflowedSideArea(radius, top_cross);

        var corner_area = (side_segment - top_cross_segment - 2 * Mathf.Abs(top) * (top_cross - Mathf.Abs(side))) / 2;

        if (side < 0 && top < 0) {
            return Mathf.PI * radius * radius - corner_area;
        }
        else if (side < 0) {
            return top_cross_segment - corner_area;
        }
        else if (top < 0) {
            return side_segment - corner_area;
        }
        else {
            return corner_area;
        }
    }

    private void renderBasis() {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(colorBasis);
        GL.Vertex3(1 - (float)_points.Count / length, scaledY(1), 0);
        GL.Vertex3(1, scaledY(1), 0);
        GL.End();
    }

    private void renderOverfillOnly() {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(colorOverfillOnly);
        for (var index = 0; index < _points.Count; index++) {
            GL.Vertex3(1 - ((float)_points.Count - index) / length, scaledY(_points[index].ratioOfOverfillOnlyToIdeal), 0);
        }
        GL.End();
    }

    private void renderFoveatedOverfill() {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(colorFoveatedOverfill);
        for (var index = 0; index < _points.Count; index++) {
            GL.Vertex3(1 - ((float)_points.Count - index) / length, scaledY(_points[index].ratioOfFoveatedOverfillToIdeal), 0);
        }
        GL.End();
    }

    private float scaledY(float value) {
        return (value - _range.x) / (_range.y - _range.x);
    }

    private struct Metrics {
        public float ideal;
        public float overfillOnly;
        public float foveatedOverfill;

        public float ratioOfOverfillOnlyToIdeal => overfillOnly / ideal;
        public float ratioOfFoveatedOverfillToIdeal => foveatedOverfill / ideal;
    }
}
