/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEditor;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

class AirXRServerSettingsProvider : SettingsProvider {
    private class Styles {
        public static GUIContent licenseFilePath = new GUIContent("License File");
        public static GUIContent maxClientCount = new GUIContent("Max Client Count");
        public static GUIContent port = new GUIContent("Port");
        public static GUIContent adaptiveFrameRate = new GUIContent("Adaptive Frame Rate");
        public static GUIContent minFrameRate = new GUIContent("Minimum Frame Rate");
        public static GUIContent bypassPrediction = new GUIContent("Bypass Prediction");
        public static GUIContent visualizeRenderingInfo = new GUIContent("Visualize Rendering Info");
        public static GUIContent overfillMode = new GUIContent("Overfilling Mode");
        public static GUIContent useFoveatedRendering = new GUIContent("Enable Foveated Rendering");
        public static GUIContent labelPriority = new GUIContent("Priority");
        public static GUIContent labelShadingRate = new GUIContent("Shading Rate");
        public static GUIContent labelInner = new GUIContent("Inner");
        public static GUIContent labelMiddle = new GUIContent("Middle");
        public static GUIContent labelOuter = new GUIContent("Outer");
        public static GUIContent labelBasis = new GUIContent("Basis");
        public static GUIContent labelOverfillOnly = new GUIContent("Overfill Only");
        public static GUIContent labelFoveatedOverfill = new GUIContent("Foveated Overfill");
        public static GUIContent labelDefault = new GUIContent("Default");
        public static GUIContent labelAutoFit = new GUIContent("Auto Fit");
        public static GUIContent labelLength = new GUIContent("Length (in # of points)");
    }

    private SerializedObject _settings;
    private SerializedProperty _licenseFilePath;
    private SerializedProperty _maxClientCount;
    private SerializedProperty _port;
    private SerializedProperty _adaptiveFrameRate;
    private SerializedProperty _minFrameRate;
    private SerializedProperty _bypassPrediction;
    private SerializedProperty _overfillMode;
    private SerializedProperty _useFoveatedRendering;
    private SerializedProperty _foveatedRenderingPriority;
    private SerializedProperty _foveatedShadingInnerRate;
    private SerializedProperty _foveatedShadingMiddleRate;
    private SerializedProperty _foveatedShadingOuterRate;
    private SerializedProperty _visualizeRenderingInfo;
    private SerializedProperty _renderPerfGraphColorBasis;
    private SerializedProperty _renderPerfGraphColorOverfillOnly;
    private SerializedProperty _renderPerfGraphColorFoveatedOverfill;
    private SerializedProperty _renderPerfGraphDefaultRangeY;
    private SerializedProperty _renderPerfGraphAutoFitRangeY;
    private SerializedProperty _renderPerfGraphLength;

    public AirXRServerSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
        : base(path, scope) { }

    public override void OnActivate(string searchContext, VisualElement rootElement) {
        var settings = AssetDatabase.LoadAssetAtPath<AirXRServerSettings>(AirXRServerSettings.ProjectAssetPath);
        if (settings == null) {
            settings = ScriptableObject.CreateInstance<AirXRServerSettings>();
            AssetDatabase.CreateAsset(settings, AirXRServerSettings.ProjectAssetPath);
            AssetDatabase.SaveAssets();
        }

        _settings = new SerializedObject(settings);
        _licenseFilePath = _settings.FindProperty("license");
        _maxClientCount = _settings.FindProperty("maxClientCount");
        _port = _settings.FindProperty("stapPort");
        _adaptiveFrameRate = _settings.FindProperty("adaptiveFrameRate");
        _minFrameRate = _settings.FindProperty("minFrameRate");
        _bypassPrediction = _settings.FindProperty("bypassPrediction");
        _overfillMode = _settings.FindProperty("overfillMode");
        _useFoveatedRendering = _settings.FindProperty("useFoveatedRendering");
        _foveatedRenderingPriority = _settings.FindProperty("foveatedRenderingPriority");
        _foveatedShadingInnerRate = _settings.FindProperty("foveatedShadingInnerRate");
        _foveatedShadingMiddleRate = _settings.FindProperty("foveatedShadingMiddleRate");
        _foveatedShadingOuterRate = _settings.FindProperty("foveatedShadingOuterRate");
        _visualizeRenderingInfo = _settings.FindProperty("visualizeRenderingInfo");
        _renderPerfGraphColorBasis = _settings.FindProperty("renderPerfGraphColorBasis");
        _renderPerfGraphColorOverfillOnly = _settings.FindProperty("renderPerfGraphColorOverfillOnly");
        _renderPerfGraphColorFoveatedOverfill = _settings.FindProperty("renderPerfGraphColorFoveatedOverfill");
        _renderPerfGraphDefaultRangeY = _settings.FindProperty("renderPerfGraphDefaultRangeY");
        _renderPerfGraphAutoFitRangeY = _settings.FindProperty("renderPerfGraphAutoFitRangeY");
        _renderPerfGraphLength = _settings.FindProperty("renderPerfGraphLength");
    }

    public override void OnGUI(string searchContext) {
        EditorGUILayout.PropertyField(_licenseFilePath, Styles.licenseFilePath);
        EditorGUILayout.PropertyField(_maxClientCount, Styles.maxClientCount);
        EditorGUILayout.PropertyField(_port, Styles.port);
        EditorGUILayout.PropertyField(_adaptiveFrameRate, Styles.adaptiveFrameRate);
        EditorGUILayout.PropertyField(_minFrameRate, Styles.minFrameRate);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_visualizeRenderingInfo, Styles.visualizeRenderingInfo);
        EditorGUILayout.PropertyField(_overfillMode, Styles.overfillMode);

        EditorGUILayout.PropertyField(_useFoveatedRendering, Styles.useFoveatedRendering);
        if (_useFoveatedRendering.boolValue) {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.PropertyField(_foveatedRenderingPriority, Styles.labelPriority);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.labelShadingRate, EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_foveatedShadingInnerRate, Styles.labelInner);
            EditorGUILayout.PropertyField(_foveatedShadingMiddleRate, Styles.labelMiddle);
            EditorGUILayout.PropertyField(_foveatedShadingOuterRate, Styles.labelOuter);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Graph", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_renderPerfGraphLength, Styles.labelLength);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Range", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(_renderPerfGraphDefaultRangeY, Styles.labelDefault);
        EditorGUILayout.PropertyField(_renderPerfGraphAutoFitRangeY, Styles.labelAutoFit);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Color", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(_renderPerfGraphColorBasis, Styles.labelBasis);
        EditorGUILayout.PropertyField(_renderPerfGraphColorOverfillOnly, Styles.labelOverfillOnly);
        EditorGUILayout.PropertyField(_renderPerfGraphColorFoveatedOverfill, Styles.labelFoveatedOverfill);
        
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("For Development", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_bypassPrediction, Styles.bypassPrediction);

        _settings.ApplyModifiedProperties();
    }

    [SettingsProvider]
    public static SettingsProvider CreateAirXRServerSettingsProvider() {
        var provider = new AirXRServerSettingsProvider("Project/onAirXR");
        provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();

        return provider;
    }
}

[CustomEditor(typeof(AirXRServerSettings))]
public class AirXRServerSettingsEditor : Editor {
    public override void OnInspectorGUI() {
        if (GUILayout.Button("Open onAirXR Server Settings...")) {
            SettingsService.OpenProjectSettings("Project/onAirXR");
        }
    }
}
