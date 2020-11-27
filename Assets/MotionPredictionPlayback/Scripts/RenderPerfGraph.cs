using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RenderPerfGraph : MonoBehaviour
{
    private Material _material;
    private List<float> _optimal = new List<float>();
    private List<float> _foveated = new List<float>();

    [SerializeField] private Color _colorIdeal = Color.yellow;
    [SerializeField] private Color _colorOptimal = Color.green;
    [SerializeField] private Color _colorFoveated = Color.magenta;
    [SerializeField] private int _length = 1000;
    [SerializeField] private Vector2 _range = new Vector2(0.2f, 1.1f);

    public void AddMeasurement(float ideal, float optimal, float foveated) {
        _optimal.Add(optimal / ideal);
        _foveated.Add(foveated / ideal);
    }

    private void Awake() {
        _material = new Material(Shader.Find("Unlit/PerfGraph"));
    }

    private void OnPostRender() {
        GL.PushMatrix();
        _material.SetPass(0);
        GL.LoadOrtho();

        //renderBasis(_colorOptimal);
        //renderFoveatedReduce();

        renderBasis(_colorIdeal);
        renderOptimal();
        renderFoveated();

        GL.PopMatrix();
    }

    private void renderBasis(Color color) {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);
        GL.Vertex3(0f, scaledValue(1.0f), 0f);
        GL.Vertex3(_optimal.Count / (float)_length, scaledValue(1.0f), 0f);
        GL.End();
    }

    private void renderOptimal() {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(_colorOptimal);
        for (var index = 0; index < _optimal.Count; index++) {
            GL.Vertex3(index / (float)_length, scaledValue(_optimal[index]), 0f);
        }
        GL.End();
    }

    private void renderFoveated() {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(_colorFoveated);
        for (var index = 0; index < _foveated.Count; index++) {
            GL.Vertex3(index / (float)_length, scaledValue(_foveated[index]), 0f);
        }
        GL.End();
    }

    private void renderFoveatedReduce() {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(_colorFoveated);
        for (var index = 0; index < _optimal.Count; index++) {
            GL.Vertex3(index / (float)_length, scaledValue(_foveated[index] / _optimal[index]), 0f);
        }
        GL.End();
    }

    private float scaledValue(float value) {
        return (value - _range.x) / (_range.y - _range.x);
    }
}
