using System;
using UnityEditor;
using UnityEngine;

namespace HTC.UnityPlugin.FoveatedRendering {
    [CustomEditor(typeof(ViveFoveatedRenderer))]
    public class ViveFoveatedRendererInspector : Editor {
        private SerializedProperty _propInnerRadii;
        private SerializedProperty _propMiddleRadii;
        private SerializedProperty _propInnerRate;
        private SerializedProperty _propMiddleRate;
        private SerializedProperty _propOuterRate;

        private void OnEnable() {
            _propInnerRadii = serializedObject.FindProperty("_innerRadii");
            _propMiddleRadii = serializedObject.FindProperty("_middleRadii");
            _propInnerRate = serializedObject.FindProperty("_innerRate");
            _propMiddleRate = serializedObject.FindProperty("_middleRate");
            _propOuterRate = serializedObject.FindProperty("_outerRate");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Shading Radii", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_propInnerRadii, new GUIContent("Inner"));
            EditorGUILayout.PropertyField(_propMiddleRadii, new GUIContent("Middle"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shading Rate", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_propInnerRate, new GUIContent("Inner"));
            EditorGUILayout.PropertyField(_propMiddleRate, new GUIContent("Middle"));
            EditorGUILayout.PropertyField(_propOuterRate, new GUIContent("Outer"));
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
