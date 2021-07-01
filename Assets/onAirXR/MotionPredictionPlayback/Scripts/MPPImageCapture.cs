using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MPPImageCapture {
    private MotionPredictionPlayback _owner;
    private RenderTexture _source;
    private int _seqnum;

    public string outputPath { private get; set; }

    public MPPImageCapture(MotionPredictionPlayback owner) {
        _owner = owner;
    }
    
    public void Prepare(RenderTexture source) {
        _source = source;
        _seqnum = 0;

        if (Directory.Exists(outputPath) == false) {
            Directory.CreateDirectory(outputPath);
        }
    }

    public void Capture(double time, MotionPredictionPlayback.PlaybackMode playbackMode) {
        if (string.IsNullOrEmpty(outputPath) || _source == null) { return; }

        var desc = toPlaybackModeString(playbackMode);
        var path = Path.Combine(outputPath, desc);
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }

        var oldrt = RenderTexture.active;
        RenderTexture.active = _source;

        var image = new Texture2D(_source.width, _source.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();

        File.WriteAllBytes(screenshotName(path, _seqnum, time, desc), image.EncodeToPNG());

        RenderTexture.active = oldrt;
        Object.Destroy(image);

        _owner.OnImageCaptured(this, _seqnum);

        _seqnum++;
    }

    private string toPlaybackModeString(MotionPredictionPlayback.PlaybackMode mode) {
        switch (mode) {
            case MotionPredictionPlayback.PlaybackMode.NotPredict_NoTimeWarp:
                return "NotPredict_NoTimeWarp";
            case MotionPredictionPlayback.PlaybackMode.NotPredict_TimeWarp:
                return "NotPredict_TimeWarp";
            case MotionPredictionPlayback.PlaybackMode.Predict_NoTimeWarp:
                return "Predict_NoTimeWarp";
            case MotionPredictionPlayback.PlaybackMode.Predict_TimeWarp:
                return "Predict_TimeWarp";
            default:
                return "Unknown";
        }
    }

    private string screenshotName(string folder, int seqnum, double time, string desc) {
        return Path.Combine(folder, string.Format("{0}_{1:#.000}_{2}.png", seqnum, time, desc));
    }
}
