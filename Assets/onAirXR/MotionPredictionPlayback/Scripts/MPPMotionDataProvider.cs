using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using UnityEngine;

public abstract class MPPMotionDataProvider {
    public static float DefaultFoveationInnerRadius = 1.06f;
    public static float DefaultFoveationMiddleRadius = 1.42f;

    public abstract bool reachedToEnd { get; }
    public abstract double currentTimestamp { get; }
    public abstract void AdvanceToNext(bool realtime);
    public abstract bool GetCurrentMotion(bool predictive, AirXRServerSettings.OverfillMode overfillingMode, ref MPPMotionData motionFrame, ref MPPMotionData motionHead, ref (int frame, int head) cursor);

    protected void adjustFrameProjectionOverfilling(AirXRServerSettings.OverfillMode mode, ref MPPMotionData motionFrame, ref MPPMotionData motionHead) {
        switch (mode) {
            case AirXRServerSettings.OverfillMode.Optimal:
                motionFrame.leftProjection = MPPUtils.calcOptimalOverfilling(motionHead.orientation, motionFrame.orientation, motionHead.leftProjection);
                motionFrame.rightProjection = MPPUtils.calcOptimalOverfilling(motionHead.orientation, motionFrame.orientation, motionHead.rightProjection);
                break;
            case AirXRServerSettings.OverfillMode.None:
                motionFrame.leftProjection = motionHead.leftProjection;
                motionFrame.rightProjection = motionHead.rightProjection;
                break;
        }
    }
}

public class MPPMotionDataFile : MPPMotionDataProvider {
    public enum Type {
        Raw,
        PerfMetric
    }

    private const string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private const string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";

    private static readonly char[] TRIM_CHARS = { '\"' };

    private string[] _lines;
    private string[] _header;
    private int _cursor;
    private float _playbackStartRealTime;

    public string filepath { get; private set; }
    public float length { get; private set; }
    public Type type => ContainsKey("prediction_time") ? Type.Raw : Type.PerfMetric;
    public int count => (_lines?.Length ?? 1) - 1;
    public float fps => count / length;
    public (int from, int to) window { get; set; } = (0, int.MaxValue);

    public MPPMotionDataFile(string path) {
        filepath = path;

        var text = File.ReadAllText(path);
        if (string.IsNullOrEmpty(text)) { return; }

        _lines = text.Split('\r').Where((line) => string.IsNullOrWhiteSpace(line) == false).ToArray();
        if (_lines.Length < 1) { return; }

        _header = _lines[0].Split(',');

        var startts = MPPUtils.FlicksToSecond((double)Read(0)["timestamp"]);
        var lastts = MPPUtils.FlicksToSecond((double)Read(count - 1)["timestamp"]);
        length = (float)(lastts - startts);

        Debug.LogFormat("[MotionPredictionPlayback] motion data file loaded: length = {0}s ({1}), fps = {2}", length, count, fps);
    }

    public void SeekToStart() {
        _playbackStartRealTime = Time.realtimeSinceStartup;
        _cursor = window.from;
    }

    public Dictionary<string, object> Read(int index) {
        var line = index + 1;
        if (line >= _lines.Length) { return null; }

        var data = new Dictionary<string, object>();
        var cols = _lines[line].Split(',');

        for (var col = 0; col < _header.Length && col < cols.Length; col++) {
            var value = cols[col];
            value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");

            var obj = double.TryParse(value, out double f) ? f : (object)value;
            data[_header[col]] = obj;
        }
        return data;
    }

    public bool ContainsKey(string key) {
        foreach (var col in _header) {
            if (col == key) {
                return true;
            }
        }
        return false;
    }

    private bool doesCursorReachToEnd(int cursor) => cursor >= count || cursor >= window.to;

    // implements MPPMotionDataProvider
    public override bool reachedToEnd => doesCursorReachToEnd(_cursor);
    public override double currentTimestamp => MPPUtils.FlicksToSecond((double)Read(_cursor)["timestamp"]);

    public override void AdvanceToNext(bool realtime) {
        if (realtime == false) {
            _cursor++;
            return;
        }

        if (doesCursorReachToEnd(_cursor + 1)) {
            _cursor++;
            return;
        }

        var elapsedRealTime = Time.realtimeSinceStartup - _playbackStartRealTime;
        var startts = (double)Read(window.from)["timestamp"];

        while (MPPUtils.FlicksToSecond((double)Read(_cursor + 1)["timestamp"] - startts) < elapsedRealTime) {
            _cursor++;

            if (doesCursorReachToEnd(_cursor + 1)) {
                break;
            }
        }
    }

    public override bool GetCurrentMotion(bool predictive, AirXRServerSettings.OverfillMode overfillingMode, ref MPPMotionData motionFrame, ref MPPMotionData motionHead, ref (int frame, int head) cursor) {
        cursor.frame = cursor.head = 0;

        var cursorForHead = _cursor;
        if (type == Type.Raw) {
            if (cursorForHead + 1 >= count) { return false; }

            var dataForFrame = Read(_cursor);
            var predictionTime = (double)dataForFrame["prediction_time"] / 1000.0;
            var framets = MPPUtils.FlicksToSecond((double)dataForFrame["timestamp"]);

            var headts = MPPUtils.FlicksToSecond((double)Read(cursorForHead + 1)["timestamp"]);
            while (headts - framets < predictionTime) {
                if (cursorForHead + 2 >= count) { return false; }

                cursorForHead++;
                headts = MPPUtils.FlicksToSecond((double)Read(cursorForHead + 1)["timestamp"]);
            }
        }

        if (readMotionData(predictive, _cursor, ref motionFrame) == false ||
            readMotionData(false, cursorForHead, ref motionHead) == false) { return false; }

        adjustFrameProjectionOverfilling(overfillingMode, ref motionFrame, ref motionHead);

        cursor.frame = _cursor;
        cursor.head = cursorForHead;
        return true;
    }

    private bool readMotionData(bool predictive, int cursor, ref MPPMotionData result) {
        if (cursor >= count) { return false; }

        var leftEyePosPrefix = predictive ? "predicted_left_eye_position_" : "input_left_eye_position_";
        var rightEyePosPrefix = predictive ? "predicted_right_eye_position_" : "input_right_eye_position_";
        var orientationPrefix = predictive ? "predicted_head_orientation_" : "input_head_orientation_";
        var inputProjectionPrefix = "input_camera_projection_";
        var predictedLeftProjectionPrefix = "predicted_left_camera_projection_";
        var predictedRightProjectionPrefix = "predicted_right_camera_projection_";

        var data = Read(cursor);

        result = new MPPMotionData {
            leftEyePos = new Vector3((float)(double)data[leftEyePosPrefix + "x"],
                                     (float)(double)data[leftEyePosPrefix + "y"],
                                     (float)(double)data[leftEyePosPrefix + "z"]),
            rightEyePos = new Vector3((float)(double)data[rightEyePosPrefix + "x"],
                                      (float)(double)data[rightEyePosPrefix + "y"],
                                      (float)(double)data[rightEyePosPrefix + "z"]),
            orientation = new Quaternion(-(float)(double)data[orientationPrefix + "x"],
                                         -(float)(double)data[orientationPrefix + "y"],
                                         (float)(double)data[orientationPrefix + "z"],
                                         (float)(double)data[orientationPrefix + "w"]),
            leftProjection = predictive? 
                new MPPProjection {
                    left = (float)(double)data[predictedLeftProjectionPrefix + "left"],
                    top = (float)(double)data[predictedLeftProjectionPrefix + "top"],
                    right = (float)(double)data[predictedLeftProjectionPrefix + "right"],
                    bottom = (float)(double)data[predictedLeftProjectionPrefix + "bottom"]
                } : 
                new MPPProjection {
                    left = (float)(double)data[inputProjectionPrefix + "left"],
                    top = (float)(double)data[inputProjectionPrefix + "top"],
                    right = (float)(double)data[inputProjectionPrefix + "right"],
                    bottom = (float)(double)data[inputProjectionPrefix + "bottom"]
                },
            rightProjection = predictive ?
                new MPPProjection {
                    left = (float)(double)data[predictedRightProjectionPrefix + "left"],
                    top = (float)(double)data[predictedRightProjectionPrefix + "top"],
                    right = (float)(double)data[predictedRightProjectionPrefix + "right"],
                    bottom = (float)(double)data[predictedRightProjectionPrefix + "bottom"]
                } :
                new MPPProjection {
                    left = -(float)(double)data[inputProjectionPrefix + "right"],
                    top = (float)(double)data[inputProjectionPrefix + "top"],
                    right = -(float)(double)data[inputProjectionPrefix + "left"],
                    bottom = (float)(double)data[inputProjectionPrefix + "bottom"]
                },
            foveationInnerRadius = predictive ? (float)(double)data["predicted_foveation_inner_radius"] : DefaultFoveationInnerRadius,
            foveationMiddleRadius = predictive ? (float)(double)data["predicted_foveation_middle_radius"] : DefaultFoveationMiddleRadius
        };
        return true;
    }
}

public class MPPLiveMotionDataProvider : MPPMotionDataProvider {
    private Queue<MotionData> _queue = new Queue<MotionData>();

    public void Put(long timestamp, float predictionTime,
                    Pose inputLeftEye, Pose inputRightEye, MPPProjection inputProjection, Pose inputRightHand,
                    Pose predictedLeftEye, Pose predictedRightEye, 
                    MPPProjection predictedLeftProjection, MPPProjection predictedRightProjection,
                    float foveationInnerRadius, float foveationMiddleRadius, Pose predictedRightHand) {
        if (_queue.Count > 0 && _queue.Last().timestamp >= timestamp) { return; }

        _queue.Enqueue(new MotionData {
            timestamp = timestamp,
            predictionTime = predictionTime / 1000.0f,
            inputLeftEye = inputLeftEye,
            inputRightEye = inputRightEye,
            inputProjection = inputProjection,
            inputRightHand = inputRightHand,
            predictedLeftEye = predictedLeftEye,
            predictedRightEye = predictedRightEye,
            predictedLeftProjection = predictedLeftProjection,
            predictedRightProjection = predictedRightProjection,
            predictedFoveationInnerRadius = foveationInnerRadius,
            predictedFoveationMiddleRadius = foveationMiddleRadius,
            predictedRightHand = predictedRightHand
        });
    }

    public void Clear() {
        _queue.Clear();
    }

    // implements MPPMotionDataProvider
    public override bool reachedToEnd => false;
    public override double currentTimestamp => 0;

    public override void AdvanceToNext(bool realtime) {
        if (_queue.Count <= 1) { return; }

        var frame = _queue.Peek();
        while (_queue.Count > 1 && 
               MPPUtils.FlicksToSecond(frame.timestamp) + frame.predictionTime < MPPUtils.FlicksToSecond(_queue.Last().timestamp)) {
            _queue.Dequeue();

            frame = _queue.Peek();
        }
    }

    public override bool GetCurrentMotion(bool predictive, AirXRServerSettings.OverfillMode overfillingMode, ref MPPMotionData motionFrame, ref MPPMotionData motionHead, ref (int frame, int head) cursor) {
        if (_queue.Count == 0) { return false; }
        
        var frame = _queue.Peek();
        var head = _queue.Last();

        motionFrame = new MPPMotionData {
            leftEyePos = predictive ? frame.predictedLeftEye.position : frame.inputLeftEye.position,
            rightEyePos = predictive ? frame.predictedRightEye.position : frame.inputRightEye.position,
            orientation = predictive ? frame.predictedLeftEye.rotation : frame.inputLeftEye.rotation,
            leftProjection = predictive ? frame.predictedLeftProjection : frame.inputProjection,
            rightProjection = predictive ? frame.predictedRightProjection : frame.inputProjection.GetOtherEyeProjection(),
            foveationInnerRadius = predictive ? frame.predictedFoveationInnerRadius : DefaultFoveationInnerRadius,
            foveationMiddleRadius = predictive ? frame.predictedFoveationMiddleRadius : DefaultFoveationMiddleRadius
        };

        motionHead = new MPPMotionData {
            leftEyePos = head.inputLeftEye.position,
            rightEyePos = head.inputRightEye.position,
            orientation = head.inputLeftEye.rotation,
            leftProjection = head.inputProjection,
            rightProjection = head.inputProjection.GetOtherEyeProjection(),
            foveationInnerRadius = DefaultFoveationInnerRadius,
            foveationMiddleRadius = DefaultFoveationMiddleRadius
        };

        adjustFrameProjectionOverfilling(overfillingMode, ref motionFrame, ref motionHead);

        return true;
    }

    private struct MotionData {
        public long timestamp;
        public float predictionTime;

        public Pose inputLeftEye;
        public Pose inputRightEye;
        public MPPProjection inputProjection;
        public Pose inputRightHand;

        public Pose predictedLeftEye;
        public Pose predictedRightEye;
        public MPPProjection predictedLeftProjection;
        public MPPProjection predictedRightProjection;
        public float predictedFoveationInnerRadius;
        public float predictedFoveationMiddleRadius;
        public Pose predictedRightHand;

        public bool isValid => timestamp > 0;
    }
}
