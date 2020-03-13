using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirVRScreenTouchInputDevice : AirVRInputDevice {
    protected override string deviceName {
        get {
            return AirVRInputDeviceName.ScreenTouch;
        }
    }

    protected override void MakeControlList() {
        // supports up to 10 touches
        foreach (byte controlID in Enumerable.Range(0, 10)) {
            AddControlTouch(controlID);
        }
    }
}
