using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;

public class AirVRPredictedMotionProvider {
    private NetMQ.Msg _msgRecv;
    private PullSocket _zmqPredictedMotion;

    private float _predictionTime;

    public long timestamp { get; private set; }
    public Vector4 projection { get; private set; }
    public Pose leftEye { get; private set; }
    public Pose rightEye { get; private set; }
    public Pose rightHand { get; private set; }

    public void Connect(string endpoint) {
        if (_zmqPredictedMotion != null) { return; }

        _zmqPredictedMotion = new PullSocket();
        _zmqPredictedMotion.Connect(endpoint);

        _msgRecv = new NetMQ.Msg();
        _msgRecv.InitPool(8 + 4 * 4);
    }

    public void Update() {
        if (_zmqPredictedMotion == null) { return; }

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

            var headOrientation = getRotation(_msgRecv.Data, ref pos);
            projection = getProjection(_msgRecv.Data, ref pos);

            leftEye = new Pose(getPosition(_msgRecv.Data, ref pos), headOrientation);
            rightEye = new Pose(getPosition(_msgRecv.Data, ref pos), headOrientation);
            rightHand = new Pose(getPosition(_msgRecv.Data, ref pos), getRotation(_msgRecv.Data, ref pos));
        }
    }

    public void Close() {
        if (_zmqPredictedMotion == null) { return; }

        _zmqPredictedMotion.Close();
        _msgRecv.Close();

        _zmqPredictedMotion.Dispose();
        _zmqPredictedMotion = null;
    }

    private long getLong(byte[] buffer, ref int pos) {
        var result = BitConverter.ToInt64(_msgRecv.Data, pos);
        pos += 8;

        return result;
    }

    private float getFloat(byte[] buffer, ref int pos) {
        var result = BitConverter.ToSingle(_msgRecv.Data, pos);
        pos += 4;

        return result;
    }

    private Vector3 getPosition(byte[] buffer, ref int pos) {
        var result = new Vector3(BitConverter.ToSingle(_msgRecv.Data, pos),
                                 BitConverter.ToSingle(_msgRecv.Data, pos + 4),
                                 BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 2));
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
        var result = new Vector4(BitConverter.ToSingle(_msgRecv.Data, pos),
                                 BitConverter.ToSingle(_msgRecv.Data, pos + 4),
                                 BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 2),
                                 BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 3));
        pos += 16;

        return result;
    }
}
