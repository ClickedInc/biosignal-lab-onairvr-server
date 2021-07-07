using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;

public class AirXRPredictedMotionProvider {
    private AirXRPredictiveCameraRig _owner;
    private NetMQ.Msg _msgRecv;
    private PullSocket _zmqPredictedMotion;
    private MPPLiveMotionDataProvider _motionDataProvider;
    private bool _prevExternalInputActualPress;
    private bool _prevExternalInputPredictivePress;

    public long timestamp { get; private set; }
    public float predictionTime { get; private set; }
    public Rect projection { get; private set; }
    public Pose leftEye { get; private set; }
    public Pose rightEye { get; private set; }
    public float foveationInnerRadius { get; private set; }
    public float foveationMiddleRadius { get; private set; }
    public Pose rightHand { get; private set; }
    public ushort externalInputId { get; private set; }
    public bool externalInputActualPress { get; private set; }
    public bool externalInputPredictivePress { get; private set; }

    public AirXRPredictedMotionProvider(AirXRPredictiveCameraRig owner, MPPLiveMotionDataProvider motionDataProvider) {
        _owner = owner;
        _motionDataProvider = motionDataProvider;
    }

    public void Connect(string endpoint) {
        if (_owner.bypassPrediction || _zmqPredictedMotion != null) { return; }

        _zmqPredictedMotion = new PullSocket();
        _zmqPredictedMotion.Connect(endpoint);

        _msgRecv = new NetMQ.Msg();
        _msgRecv.InitPool(8 + 4 * 4);
    }

    public void Update() {
        if (_owner.bypassPrediction) {
            projection = Rect.MinMaxRect(-1, -1, 1, 1);
            return;
        }

        if (_zmqPredictedMotion == null) { return; }

        _prevExternalInputActualPress = externalInputActualPress;
        _prevExternalInputPredictivePress = externalInputPredictivePress;

        while (_zmqPredictedMotion.TryReceive(ref _msgRecv, TimeSpan.Zero)) {
            if (_msgRecv.Size <= 0) {
                continue;
            }

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(_msgRecv.Data, 0, 8);
                for (int i = 0; i < 45; i++) {
                    Array.Reverse(_msgRecv.Data, 8 + i * 4, 4);
                }
            }

            int pos = 0;
            timestamp = getLong(_msgRecv.Data, ref pos);
            predictionTime = getFloat(_msgRecv.Data, ref pos);

            var inputLeftEyePosition = getPosition(_msgRecv.Data, ref pos);
            var inputRightEyePosition = getPosition(_msgRecv.Data, ref pos);
            var inputHeadOrientation = getRotation(_msgRecv.Data, ref pos);
            var inputProjection = getProjection(_msgRecv.Data, ref pos);
            var inputRightHandPose = new Pose(getPosition(_msgRecv.Data, ref pos), getRotation(_msgRecv.Data, ref pos));

            var leftEyePosition = getPosition(_msgRecv.Data, ref pos);
            var rightEyePosition = getPosition(_msgRecv.Data, ref pos);
            var headOrientation = getRotation(_msgRecv.Data, ref pos);

            leftEye = new Pose(leftEyePosition, headOrientation);
            rightEye = new Pose(rightEyePosition, headOrientation);

            projection = getProjection(_msgRecv.Data, ref pos);

            foveationInnerRadius = getFloat(_msgRecv.Data, ref pos);
            foveationMiddleRadius = getFloat(_msgRecv.Data, ref pos);

            rightHand = new Pose(getPosition(_msgRecv.Data, ref pos), getRotation(_msgRecv.Data, ref pos));

            externalInputId = getUshort(_msgRecv.Data, ref pos);

            var actualPress = getBool(_msgRecv.Data, ref pos);
            var predictedPress = getBool(_msgRecv.Data, ref pos);

            if (_prevExternalInputActualPress != actualPress) {
                externalInputActualPress = actualPress;
            }
            if (_prevExternalInputPredictivePress != predictedPress) {
                externalInputPredictivePress = predictedPress;
            }

            //var offsetX = Mathf.Sin(Time.realtimeSinceStartup) / 2;
            //var offsetY = Mathf.Cos(Time.realtimeSinceStartup) / 2;
            //projection = Rect.MinMaxRect(-1.0f + offsetX, -1.0f + offsetY, 1.0f + offsetX, 1.0f + offsetY);

            _motionDataProvider.Put(timestamp, predictionTime,
                                    new Pose(inputLeftEyePosition, inputHeadOrientation), new Pose(inputRightEyePosition, inputHeadOrientation),
                                    MPPProjection.FromRect(inputProjection), inputRightHandPose,
                                    leftEye, rightEye, MPPProjection.FromRect(projection), foveationInnerRadius, foveationMiddleRadius, rightHand);
        }
    }

    public void Close() {
        if (_zmqPredictedMotion == null) { return; }

        _zmqPredictedMotion.Close();
        _msgRecv.Close();

        _zmqPredictedMotion.Dispose();
        _zmqPredictedMotion = null;
    }

    public bool GetButton(bool predicted) {
        return predicted ? externalInputPredictivePress : externalInputActualPress;
    }

    public bool GetButtonDown(bool predicted) {
        return (predicted ? _prevExternalInputPredictivePress : _prevExternalInputActualPress) == false &&
               (predicted ? externalInputPredictivePress : externalInputActualPress);
    }

    public bool GetButtonUp(bool predicted) {
        return (predicted ? _prevExternalInputPredictivePress : _prevExternalInputActualPress) &&
               (predicted ? externalInputPredictivePress : externalInputActualPress) == false;
    }

    private bool getBool(byte[] buffer, ref int pos) {
        var result = buffer[pos] > 0;
        pos += 1;

        return result;
    }

    private ushort getUshort(byte[] buffer, ref int pos) {
        var result = BitConverter.ToUInt16(buffer, pos);
        pos += 2;

        return result;
    }

    private long getLong(byte[] buffer, ref int pos) {
        var result = BitConverter.ToInt64(buffer, pos);
        pos += 8;

        return result;
    }

    private float getFloat(byte[] buffer, ref int pos) {
        var result = BitConverter.ToSingle(buffer, pos);
        pos += 4;

        return result;
    }

    private Vector3 getPosition(byte[] buffer, ref int pos) {
        // convert coordinate from OpenGL to Unity
        var result = new Vector3(BitConverter.ToSingle(buffer, pos),
                                 BitConverter.ToSingle(buffer, pos + 4),
                                 -BitConverter.ToSingle(buffer, pos + 4 * 2));
        pos += 12;

        return result;
    }

    private Quaternion getRotation(byte[] buffer, ref int pos) {
        // convert coordinate from OpenGL to Unity
        var result = new Quaternion(-BitConverter.ToSingle(buffer, pos),
                                    -BitConverter.ToSingle(buffer, pos + 4),
                                    BitConverter.ToSingle(buffer, pos + 4 * 2),
                                    BitConverter.ToSingle(buffer, pos + 4 * 3));
        pos += 16;

        return result;
    }

    private Rect getProjection(byte[] buffer, ref int pos) {
        var left = BitConverter.ToSingle(buffer, pos);
        var top = BitConverter.ToSingle(buffer, pos + 4);
        var right = BitConverter.ToSingle(buffer, pos + 4 * 2);
        var bottom = BitConverter.ToSingle(buffer, pos + 4 * 3);

        pos += 16;

        return Rect.MinMaxRect(left, bottom, right, top);
    }
}
