using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirVRRightHandTrackerInputDevice : AirVRInputDevice {
    private AirVRPredictedMotionProvider _motionProvider;

    public AirVRRightHandTrackerInputDevice(AirVRPredictedMotionProvider motionProvider) {
        _motionProvider = motionProvider;
    }

    // implements AirVRInputDevice
    protected override string deviceName {
        get {
            return AirVRInputDeviceName.RightHandTracker;
        }
    }

    protected override void MakeControlList() {
        AddControlTransform((byte)AirVRRightHandTrackerKey.Transform);
    }

    protected override void UpdateExtendedControls() {
        if (isRegistered == false) { return; }

        if (_motionProvider.bypassPrediction == false) {
            OverrideControlTransform((byte)AirVRRightHandTrackerKey.Transform, _motionProvider.timestamp, _motionProvider.rightHand.position, _motionProvider.rightHand.rotation);
        }
    }
}
