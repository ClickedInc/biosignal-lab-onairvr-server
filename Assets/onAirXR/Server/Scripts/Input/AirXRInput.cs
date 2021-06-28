/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Assertions;

public static class AirXRInput {
    public enum Device : byte {
        HeadTracker = AXRInputDeviceID.HeadTracker,
        LeftHandTracker = AXRInputDeviceID.LeftHandTracker,
        RightHandTracker = AXRInputDeviceID.RightHandTracker,
        Controller = AXRInputDeviceID.Controller
    }

    public enum Axis2D {
        LThumbstick,
        RThumbstick
    }

    public enum Axis {
        LIndexTrigger,
        RIndexTrigger,
        LHandTrigger,
        RHandTrigger
    }

    public enum Button {
        LIndexTrigger,
        RIndexTrigger,
        LHandTrigger,
        RHandTrigger,
        A,
        B,
        X,
        Y,
        Start,
        Back,
        LThumbstick,
        RThumbstick,
        LThumbstickUp,
        LThumbstickDown,
        LThumbstickLeft,
        LThumbstickRight,
        RThumbstickUp,
        RThumbstickDown,
        RThumbstickLeft,
        RThumbstickRight
    }

    public enum Property {
        Battery
    }

    public static bool IsDeviceAvailable(AirXRCameraRig cameraRig, Device device) {
        switch (device) {
            case Device.HeadTracker:
                return true;
            case Device.LeftHandTracker:
            case Device.RightHandTracker:
                return cameraRig.inputStream.GetState((byte)device, (byte)AXRHandTrackerControl.Status) != 0;
            default:
                return false;
        }
    }

    public static Vector2 Get(AirXRCameraRig cameraRig, Axis2D axis) {
        switch (axis) {
            case Axis2D.LThumbstick:
                return cameraRig.inputStream.GetAxis2D((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick);
            case Axis2D.RThumbstick:
                return cameraRig.inputStream.GetAxis2D((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick);
            default:
                return Vector2.zero;
        }
    }

    public static float Get(AirXRCameraRig cameraRig, Axis axis) {
        switch (axis) {
            case Axis.LIndexTrigger:
                return cameraRig.inputStream.GetAxis((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLIndexTrigger);
            case Axis.RIndexTrigger:
                return cameraRig.inputStream.GetAxis((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRIndexTrigger);
            case Axis.LHandTrigger:
                return cameraRig.inputStream.GetAxis((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLHandTrigger);
            case Axis.RHandTrigger:
                return cameraRig.inputStream.GetAxis((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRHandTrigger);
            default:
                return 0;
        }
    }

    public static bool Get(AirXRCameraRig cameraRig, Button button) {
        switch (button) {
            case Button.LIndexTrigger:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLIndexTrigger);
            case Button.RIndexTrigger:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRIndexTrigger);
            case Button.LHandTrigger:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLHandTrigger);
            case Button.RHandTrigger:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRHandTrigger);
            case Button.A:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonA);
            case Button.B:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonB);
            case Button.X:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonX);
            case Button.Y:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonY);
            case Button.Start:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonStart);
            case Button.Back:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonBack);
            case Button.LThumbstick:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonLThumbstick);
            case Button.RThumbstick:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonRThumbstick);
            case Button.LThumbstickUp:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Up);
            case Button.LThumbstickDown:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Down);
            case Button.LThumbstickLeft:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Left);
            case Button.LThumbstickRight:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Right);
            case Button.RThumbstickUp:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Up);
            case Button.RThumbstickDown:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Down);
            case Button.RThumbstickLeft:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Left);
            case Button.RThumbstickRight:
                return cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Right);
            default:
                return false;
        }
    }

    public static byte GetDeviceProperty(AirXRCameraRig cameraRig, Device device, Property prop) {
        switch (prop) {
            case Property.Battery:
                var deviceID = parseDevice(device);
                switch (deviceID) {
                    case AXRInputDeviceID.HeadTracker:
                        return cameraRig.inputStream.GetByteAxis((byte)deviceID, (byte)AXRHeadTrackerControl.Battery);
                    case AXRInputDeviceID.LeftHandTracker:
                    case AXRInputDeviceID.RightHandTracker:
                        return cameraRig.inputStream.GetByteAxis((byte)deviceID, (byte)AXRHandTrackerControl.Battery);
                    default:
                        return 255;
                }
            default:
                break;
        }
        return 0;
    }

    public static bool GetDown(AirXRCameraRig cameraRig, Button button) {
        switch (button) {
            case Button.LIndexTrigger:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLIndexTrigger);
            case Button.RIndexTrigger:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRIndexTrigger);
            case Button.LHandTrigger:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLHandTrigger);
            case Button.RHandTrigger:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRHandTrigger);
            case Button.A:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonA);
            case Button.B:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonB);
            case Button.X:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonX);
            case Button.Y:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonY);
            case Button.Start:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonStart);
            case Button.Back:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonBack);
            case Button.LThumbstick:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonLThumbstick);
            case Button.RThumbstick:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonRThumbstick);
            case Button.LThumbstickUp:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Up);
            case Button.LThumbstickDown:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Down);
            case Button.LThumbstickLeft:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Left);
            case Button.LThumbstickRight:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Right);
            case Button.RThumbstickUp:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Up);
            case Button.RThumbstickDown:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Down);
            case Button.RThumbstickLeft:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Left);
            case Button.RThumbstickRight:
                return cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Right);
            default:
                return false;
        }
    }

    public static bool GetUp(AirXRCameraRig cameraRig, Button button) {
        switch (button) {
            case Button.LIndexTrigger:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLIndexTrigger);
            case Button.RIndexTrigger:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRIndexTrigger);
            case Button.LHandTrigger:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLHandTrigger);
            case Button.RHandTrigger:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRHandTrigger);
            case Button.A:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonA);
            case Button.B:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonB);
            case Button.X:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonX);
            case Button.Y:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonY);
            case Button.Start:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonStart);
            case Button.Back:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonBack);
            case Button.LThumbstick:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonLThumbstick);
            case Button.RThumbstick:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonRThumbstick);
            case Button.LThumbstickUp:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Up);
            case Button.LThumbstickDown:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Down);
            case Button.LThumbstickLeft:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Left);
            case Button.LThumbstickRight:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DLThumbstick, AXRInputDirection.Right);
            case Button.RThumbstickUp:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Up);
            case Button.RThumbstickDown:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Down);
            case Button.RThumbstickLeft:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Left);
            case Button.RThumbstickRight:
                return cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.Axis2DRThumbstick, AXRInputDirection.Right);
            default:
                return false;
        }
    }

    public static void SetVibration(AirXRCameraRig cameraRig, Device device, float frequency, float amplitude) {
        switch (device) {
            case Device.LeftHandTracker:
                cameraRig.inputStream.PendVibration((byte)AXRInputDeviceID.LeftHandTracker, (byte)AXRHandTrackerFeedbackControl.Vibration, frequency, amplitude);
                break;
            case Device.RightHandTracker:
                cameraRig.inputStream.PendVibration((byte)AXRInputDeviceID.RightHandTracker, (byte)AXRHandTrackerFeedbackControl.Vibration, frequency, amplitude);
                break;
        }
    }

    public static void SetVibration(AirXRCameraRig cameraRig, Device device, AnimationCurve frequency, AnimationCurve amplitude) {
        switch (device) {
            case Device.LeftHandTracker:
                renderVibration(cameraRig, (byte)AXRInputDeviceID.LeftHandTracker, (byte)AXRHandTrackerFeedbackControl.Vibration, frequency, amplitude);
                break;
            case Device.RightHandTracker:
                renderVibration(cameraRig, (byte)AXRInputDeviceID.RightHandTracker, (byte)AXRHandTrackerFeedbackControl.Vibration, frequency, amplitude);
                break;
        }
    }

    public static int GetScreenTouchCount(AirXRCameraRig cameraRig) {
        var count = 0;
        for (byte control = (byte)AXRTouchScreenControl.TouchIndexStart; control <= (byte)AXRTouchScreenControl.TouchIndexEnd; control++) {
            if (cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.TouchScreen, control) ||
                cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.TouchScreen, control)) {
                count++;
            }
        }
        return count;
    }

    public static Touch GetScreenTouch(AirXRCameraRig cameraRig, int index) {
        var i = 0;
        for (byte control = (byte)AXRTouchScreenControl.TouchIndexStart; control <= (byte)AXRTouchScreenControl.TouchIndexEnd; control++) {
            if (cameraRig.inputStream.IsActive((byte)AXRInputDeviceID.TouchScreen, control) == false &&
                cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.TouchScreen, control) == false) {
                continue;
            }

            if (i == index) {
                Touch touch = new Touch();
                touch.fingerID = control;

                byte state = 0;
                cameraRig.inputStream.GetTouch2D((byte)AXRInputDeviceID.TouchScreen, control, ref touch.position, ref state);

                if (cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.TouchScreen, control)) {
                    touch.phase = TouchPhase.Began;
                }
                else {
                    switch ((AXRTouchPhase)state) {
                        case AXRTouchPhase.Ended:
                            touch.phase = TouchPhase.Ended;
                            break;
                        case AXRTouchPhase.Canceled:
                            touch.phase = TouchPhase.Canceled;
                            break;
                        case AXRTouchPhase.Stationary:
                            touch.phase = TouchPhase.Stationary;
                            break;
                        case AXRTouchPhase.Moved:
                            touch.phase = TouchPhase.Moved;
                            break;
                        default:
                            Assert.IsTrue(false);
                            break;
                    }
                }

                return touch;
            }
            else {
                i++;
            }
        }
        return new Touch { fingerID = Touch.InvalidFingerID };
    }

    private static void renderVibration(AirXRCameraRig cameraRig, byte device, byte control, AnimationCurve frequency, AnimationCurve amplitude) {
        var fps = cameraRig.GetConfig().framerate;
        var duration = Mathf.Max(frequency.keys[frequency.keys.Length - 1].time, amplitude.keys[amplitude.keys.Length - 1].time);

        for (float t = 0.0f; t < duration; t += 1.0f / fps) {
            cameraRig.inputStream.PendVibration(device, control, frequency.Evaluate(t), amplitude.Evaluate(t));
        }

        // make sure to end with no vibration
        cameraRig.inputStream.PendVibration(device, control, 0, 0);
    }

    private static AXRInputDeviceID parseDevice(Device device) {
        switch (device) {
            case Device.HeadTracker:
                return AXRInputDeviceID.HeadTracker;
            case Device.LeftHandTracker:
                return AXRInputDeviceID.LeftHandTracker;
            case Device.RightHandTracker:
                return AXRInputDeviceID.RightHandTracker;
            case Device.Controller:
                return AXRInputDeviceID.Controller;
            default:
                return AXRInputDeviceID.HeadTracker;
        }
    }

    public struct Touch {
        public const int InvalidFingerID = -1;

        public int fingerID;
        public Vector2 position;
        public TouchPhase phase;

        public bool IsValid() {
            return fingerID != InvalidFingerID;
        }
    }
}
