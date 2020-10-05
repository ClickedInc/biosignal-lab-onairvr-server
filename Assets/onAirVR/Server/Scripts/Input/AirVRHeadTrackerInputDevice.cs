/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Assertions;

public class AirVRHeadTrackerInputDevice : AirVRInputDevice {
    public enum ControlKey {
        TransformLeftEye = 0,
        TransformRightEye = 4,
        Projection
    }

    private AirVRPredictedMotionProvider _motionProvider;

    public AirVRHeadTrackerInputDevice(AirVRPredictedMotionProvider motionProvider) {
        _motionProvider = motionProvider;
    }

    // implements AirVRInputDevice
    protected override string deviceName {
        get {
            return AirVRInputDeviceName.HeadTracker;
        }
    }

    protected override void MakeControlList() {
        AddControlTransform((byte)ControlKey.TransformLeftEye);
        AddExtControlTransform((byte)ControlKey.TransformRightEye);
        AddExtControlAxis4D((byte)ControlKey.Projection);
    }

    protected override void UpdateExtendedControls() {
        if (isRegistered == false) { return; }

        _motionProvider.Update();

        if (_motionProvider.bypassPrediction) {
            SetExtControlAxis4D((byte)ControlKey.Projection, new Vector4(-1f, 1f, 1f, -1f));
        }
        else {
            OverrideControlTransform((byte)ControlKey.TransformLeftEye, _motionProvider.timestamp, _motionProvider.leftEye.position, _motionProvider.leftEye.rotation);
            SetExtControlTransform((byte)ControlKey.TransformRightEye, _motionProvider.timestamp, _motionProvider.rightEye.position, _motionProvider.rightEye.rotation);
            SetExtControlAxis4D((byte)ControlKey.Projection, _motionProvider.projection);
        }
    }

    public override void OnRegistered(byte inDeviceID, string options) {
        base.OnRegistered(inDeviceID, options);

        var opts = JsonUtility.FromJson<Options>(options);
        Debug.Assert(string.IsNullOrEmpty(opts.PredictedMotionOutputEndpoint) == false);

        _motionProvider.Connect(opts.PredictedMotionOutputEndpoint);
    }

    public override void OnUnregistered() {
        base.OnUnregistered();

        _motionProvider.Close();
    }

    [System.Serializable]
    private class Options {
        public string PredictedMotionOutputEndpoint;
    }
}
