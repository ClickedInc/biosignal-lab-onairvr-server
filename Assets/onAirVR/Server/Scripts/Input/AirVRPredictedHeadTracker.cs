/***********************************************************

  Copyright (c) 2017-2018 Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using NetMQ.Sockets;
using System;

public class AirVRPredictedHeadTrackerInputDevice : AirVRInputDevice {
    [Serializable]
    private class Arguments {
        public string PredictedMotionOutputEndpoint;
    }

    public enum ControlKey {
        Transform = 0,
        FOV,
        Overfilling
    }

    private NetMQ.Msg _msgRecv;
    private PullSocket _zmqPredictedMotion;
    private Vector3 _centerEyePosition = new Vector3(0.0f, 0.097f, 0.0805f);

    private long _lastTimeStamp;
    private float _predictionTime;
    private Quaternion _lastOrientation = Quaternion.identity;
    private float _fov;
    private Vector4 _overfilling = Vector4.zero;

    // implements AirVRInputDevice
    protected override string deviceName {
        get {
            return AirVRInputDeviceName.HeadTracker;
        }
    }

    protected override void MakeControlList() {
        AddControlTransform((byte)ControlKey.Transform);

        AddExtControlAxis((byte)ControlKey.FOV);
        AddExtControlAxis4D((byte)ControlKey.Overfilling);
    }

    protected override void UpdateExtendedControls() {
        if (isRegistered == false) {
            return;
        }

        Debug.Assert(_zmqPredictedMotion != null);
        while (_zmqPredictedMotion.TryReceive(ref _msgRecv, System.TimeSpan.Zero)) {
            if (_msgRecv.Size <= 0) {
                continue;
            }

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(_msgRecv.Data, 0, 8);
                for (int i = 0; i < 10; i++) {
                    Array.Reverse(_msgRecv.Data, 8 + i * 4, 4);
                }
            }

            int pos = 0;
            _lastTimeStamp = BitConverter.ToInt64(_msgRecv.Data, pos); pos += 8;
            _predictionTime = BitConverter.ToSingle(_msgRecv.Data, pos); pos += 4;

            // convert coordinate from OpenGL to Unity
            _lastOrientation = new Quaternion(-BitConverter.ToSingle(_msgRecv.Data, pos),
                                              -BitConverter.ToSingle(_msgRecv.Data, pos + 4),
                                              BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 2),
                                              BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 3)); pos += 16;

            _fov = BitConverter.ToSingle(_msgRecv.Data, pos); pos += 4;
            _overfilling = new Vector4(BitConverter.ToSingle(_msgRecv.Data, pos),
                                       BitConverter.ToSingle(_msgRecv.Data, pos + 4),
                                       BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 2),
                                       BitConverter.ToSingle(_msgRecv.Data, pos + 4 * 3));
        }

        OverrideControlTransform((byte)ControlKey.Transform, _lastTimeStamp, _lastOrientation * _centerEyePosition, _lastOrientation);
        SetExtControlAxis((byte)ControlKey.FOV, _fov);
        SetExtControlAxis4D((byte)ControlKey.Overfilling, _overfilling);
    }

    public override void OnRegistered(byte inDeviceID, string arguments) {
        base.OnRegistered(inDeviceID, arguments);

        Arguments args = JsonUtility.FromJson<Arguments>(arguments);
        Debug.Assert(string.IsNullOrEmpty(args.PredictedMotionOutputEndpoint) == false);
        Debug.Assert(_zmqPredictedMotion == null);

        _zmqPredictedMotion = new PullSocket();
        _zmqPredictedMotion.Connect(args.PredictedMotionOutputEndpoint);

        _msgRecv = new NetMQ.Msg();
        _msgRecv.InitPool(8 + 4 * 4);
    }

    public override void OnUnregistered() {
        base.OnUnregistered();

        Debug.Assert(_zmqPredictedMotion != null);
        _zmqPredictedMotion.Close();
        _msgRecv.Close();

        _zmqPredictedMotion.Dispose();
        _zmqPredictedMotion = null;
    }
}
