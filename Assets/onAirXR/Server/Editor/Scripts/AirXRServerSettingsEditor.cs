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
    }

    private SerializedObject _settings;
    private SerializedProperty _licenseFilePath;
    private SerializedProperty _maxClientCount;
    private SerializedProperty _port;
    private SerializedProperty _adaptiveFrameRate;
    private SerializedProperty _minFrameRate;
    private SerializedProperty _bypassPrediction;

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
    }

    public override void OnGUI(string searchContext) {
        EditorGUILayout.PropertyField(_licenseFilePath, Styles.licenseFilePath);
        EditorGUILayout.PropertyField(_maxClientCount, Styles.maxClientCount);
        EditorGUILayout.PropertyField(_port, Styles.port);
        EditorGUILayout.PropertyField(_adaptiveFrameRate, Styles.adaptiveFrameRate);
        EditorGUILayout.PropertyField(_minFrameRate, Styles.minFrameRate);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Motion Prediction", EditorStyles.boldLabel);
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
