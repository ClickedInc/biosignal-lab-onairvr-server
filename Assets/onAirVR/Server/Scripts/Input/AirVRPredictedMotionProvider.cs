using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;

public class AirVRPredictedMotionProvider {
    private NetMQ.Msg _msgRecv;
    private PullSocket _zmqPredictedMotion;

    private float _predictionTime;
    private bool _prevExternalInputActualPress;
    private bool _prevExternalInputPredictivePress;

    public bool bypassPrediction { get; private set; }
    public long timestamp { get; private set; }
    public Vector4 projection { get; private set; }
    public Pose leftEye { get; private set; }
    public Pose rightEye { get; private set; }
    public Pose rightHand { get; private set; }
    public ushort externalInputId { get; private set; }
    public bool externalInputActualPress { get; private set; }
    public bool externalInputPredictivePress { get; private set; }

    public AirVRPredictedMotionProvider(bool bypassPrediction) {
        this.bypassPrediction = bypassPrediction;
    }

    public void Connect(string endpoint) {
        if (bypassPrediction || _zmqPredictedMotion != null) { return; }

        _zmqPredictedMotion = new PullSocket();
        _zmqPredictedMotion.Connect(endpoint);

        _msgRecv = new NetMQ.Msg();
        _msgRecv.InitPool(8 + 4 * 4);
    }

    public void Update() {
        if (bypassPrediction) {
            projection = new Vector4(-1f, 1f, 1f, -1f);
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
                for (int i = 0; i < 22; i++) {
                    Array.Reverse(_msgRecv.Data, 8 + i * 4, 4);
                }
            }

            int pos = 0;
            timestamp = getLong(_msgRecv.Data, ref pos);
            _predictionTime = getFloat(_msgRecv.Data, ref pos);

            var leftEyePosition = getPosition(_msgRecv.Data, ref pos);
            var rightEyePosition = getPosition(_msgRecv.Data, ref pos);
            var headOrientation = getRotation(_msgRecv.Data, ref pos);

            leftEye = new Pose(leftEyePosition, headOrientation);
            rightEye = new Pose(rightEyePosition, headOrientation);

            projection = getProjection(_msgRecv.Data, ref pos);

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
        }
    }

    public void Close() {
        if (bypassPrediction || _zmqPredictedMotion == null) { return; }

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

    private Vector4 getProjection(byte[] buffer, ref int pos) {
        var result = new Vector4(BitConverter.ToSingle(buffer, pos),
                                 BitConverter.ToSingle(buffer, pos + 4),
                                 BitConverter.ToSingle(buffer, pos + 4 * 2),
                                 BitConverter.ToSingle(buffer, pos + 4 * 3));
        pos += 16;

        return result;
    }
}
