using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class MotionPredictionPlaybackMenu {
    static MotionPredictionPlaybackMenu() {
        EditorApplication.playModeStateChanged += onPlayModeChanged;
    }

    private static void onPlayModeChanged(PlayModeStateChange stateChange) {
        if (stateChange == PlayModeStateChange.EnteredEditMode) {
            var playback = GameObject.FindObjectOfType<MotionPredictionPlayback>();
            if (playback != null) {
                playback.playbackModeStartedByEditor = false;
            }
        }
    }

    [MenuItem("onAirXR/Motion Prediction Playback/Enter Playback Mode", false, 200)]
    public static void EnterPlaybackMode() {
        var playback = GameObject.FindObjectOfType<MotionPredictionPlayback>();
        if (playback == null) {
            EditorUtility.DisplayDialog("Motion Prediction Playback Cancelled", "There must exist an instance of MotionPredictionPlayback in the scene.", "Dismiss");
            return;
        }

        playback.playbackModeStartedByEditor = true;

        EditorApplication.isPlaying = true;
    }

    [MenuItem("onAirXR/Motion Prediction Playback/Export Motion Prediction Playback...", false, 200)]
    public static void ExportPlugin()
    {
        string prefabfilename = "Assets/MotionPredictionPlayback";

        string exportPath = EditorUtility.SaveFilePanel("Export MotionPredictionPlayback plugin", "", "MotionPredictionPlayback", "unitypackage");
        if (string.IsNullOrEmpty(exportPath)) {
            Debug.Log("[Motion Prediction Playback] export cancelled.");
            return;
        }

        AssetDatabase.ExportPackage(prefabfilename, exportPath,
        ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);

        EditorUtility.DisplayDialog("Congratulation!", "The package is exported successfully.", "OK");
    }
}
