using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MPPImageCapture {
    private const string FramesFilename = "frames.csv";

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

    public void Capture(double time, (int frame, int head) cursor, MPPMotionData motionFrame, MPPMotionData motionHead, MotionPredictionPlayback.PlaybackMode playbackMode) {
        if (string.IsNullOrEmpty(outputPath) || _source == null) { return; }

        var desc = toPlaybackModeString(playbackMode);
        var path = Path.Combine(outputPath, desc);
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }

        var framesPath = Path.Combine(outputPath, desc, FramesFilename);
        if (File.Exists(framesPath) == false) {
            writeFramesHeader(framesPath);
        }

        var oldrt = RenderTexture.active;
        RenderTexture.active = _source;

        var image = new Texture2D(_source.width, _source.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();

        File.WriteAllBytes(screenshotName(path, cursor, time, desc), image.EncodeToPNG());

        RenderTexture.active = oldrt;
        Object.Destroy(image);

        _owner.OnImageCaptured(this, _seqnum);

        _seqnum++;

        writeFramesLine(framesPath, time, cursor, motionFrame, motionHead);
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

    private string screenshotName(string folder, (int frame, int head) cursor, double time, string desc) {
        return Path.Combine(folder, $"{string.Format("{0:#.000}", time)}_f{cursor.frame}-h{cursor.head}_{desc}.png");
    }

    private void writeFramesHeader(string path) { 
        File.WriteAllText(path, string.Join(",", new string[] { 
            "time",
            "frameIndex",
            "framePitch",
            "frameYaw",
            "frameRoll",
            "leftFrameProjectionL",
            "leftFrameProjectionT",
            "leftFrameProjectionR",
            "leftFrameProjectionB",
            "rightFrameProjectionL",
            "rightFrameProjectionT",
            "rightFrameProjectionR",
            "rightFrameProjectionB",
            "headIndex",
            "headPitch",
            "headYaw",
            "headRoll",
            "leftEyeProjectionL",
            "leftEyeProjectionT",
            "leftEyeProjectionR",
            "leftEyeProjectionB",
            "rightEyeProjectionL",
            "rightEyeProjectionT",
            "rightEyeProjectionR",
            "rightEyeProjectionB",
            "blackEdgeRatio"
        }) + "\n");
    }

    private void writeFramesLine(string path, double time, (int frame, int head) cursor, MPPMotionData motionFrame, MPPMotionData motionHead) {
        using (var writer = File.AppendText(path)) {
            writer.WriteLine(string.Join(",", new string[] { 
                time.ToString(),
                cursor.frame.ToString(),
                motionFrame.orientation.eulerAngles.x.ToString(),
                motionFrame.orientation.eulerAngles.y.ToString(),
                motionFrame.orientation.eulerAngles.z.ToString(),
                motionFrame.leftProjection.left.ToString(),
                motionFrame.leftProjection.top.ToString(),
                motionFrame.leftProjection.right.ToString(),
                motionFrame.leftProjection.bottom.ToString(),
                motionFrame.rightProjection.left.ToString(),
                motionFrame.rightProjection.top.ToString(),
                motionFrame.rightProjection.right.ToString(),
                motionFrame.rightProjection.bottom.ToString(),
                cursor.head.ToString(),
                motionHead.orientation.eulerAngles.x.ToString(),
                motionHead.orientation.eulerAngles.y.ToString(),
                motionHead.orientation.eulerAngles.z.ToString(),
                motionHead.leftProjection.left.ToString(),
                motionHead.leftProjection.top.ToString(),
                motionHead.leftProjection.right.ToString(),
                motionHead.leftProjection.bottom.ToString(),
                motionHead.rightProjection.left.ToString(),
                motionHead.rightProjection.top.ToString(),
                motionHead.rightProjection.right.ToString(),
                motionHead.rightProjection.bottom.ToString(),
                roundByScale(1 - motionHead.CalcProjectionCoverage(motionFrame), 100000).ToString()
            }));
        }
    }

    private float roundByScale(double value, float scale) => (int)(value * scale) / scale;
}
