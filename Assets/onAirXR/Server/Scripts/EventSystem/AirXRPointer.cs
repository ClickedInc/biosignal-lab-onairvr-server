/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AirXRPointer : MonoBehaviour {
    public static List<AirXRPointer> pointers = new List<AirXRPointer>();

    private AirVRCameraRig _cameraRig = null;
    private Feedback _feedback = null;

    private Vector3 _lastRaycastHitOrigin = Vector3.zero;
    private Vector3 _lastRaycastHitPosition = Vector3.zero;
    private Vector3 _lastRaycastHitNormal = Vector3.zero;
    
    public void Configure(AirVRCameraRig cameraRig, AXRInputDeviceID srcDevice) {
        _cameraRig = cameraRig;
        _feedback = new Feedback(this, srcDevice);
    }

    private void Start() {
        pointers.Add(this);

        _cameraRig.inputStream.RegisterInputSender(_feedback);
    }

    private void OnDestroy() {
        _cameraRig.inputStream.UnregisterInputSender(_feedback);

        pointers.Remove(this);
    }

    public AirXRCameraRig cameraRig {
        get {
            return _cameraRig;
        }
    }

    public bool interactable {
        get {
            if (_cameraRig == null) { return false; }
            if (_cameraRig.renderControllersOnClient == false) { return true; }

            return _cameraRig.inputStream.GetState(_feedback.id, (byte)AXRHandTrackerControl.Status) != 0;
        }
    }

    public bool primaryButtonPressed {
        get {
            if (_cameraRig == null) { return false; }

            switch ((AXRInputDeviceID)_feedback.id) {
                case AXRInputDeviceID.LeftHandTracker:
                    return _cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLIndexTrigger) ||
                           _cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonX);
                case AXRInputDeviceID.RightHandTracker:
                    return _cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRIndexTrigger) ||
                           _cameraRig.inputStream.GetActivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonA);
                default:
                    return false;
            }
        }
    }

    public bool primaryButtonReleased {
        get {
            if (_cameraRig == null) { return false; }

            switch ((AXRInputDeviceID)_feedback.id) {
                case AXRInputDeviceID.LeftHandTracker:
                    return _cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisLIndexTrigger) ||
                           _cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonX);
                case AXRInputDeviceID.RightHandTracker:
                    return _cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.AxisRIndexTrigger) ||
                           _cameraRig.inputStream.GetDeactivated((byte)AXRInputDeviceID.Controller, (byte)AXRControllerControl.ButtonA);
                default:
                    return false;
            }
        }
    }

    public Ray GetWorldRay() {
        if (_cameraRig) {
            switch ((AXRInputDeviceID)_feedback.id) {
                case AXRInputDeviceID.LeftHandTracker:
                    return new Ray(_cameraRig.leftHandAnchor.position, _cameraRig.leftHandAnchor.forward);
                case AXRInputDeviceID.RightHandTracker:
                    return new Ray(_cameraRig.rightHandAnchor.position, _cameraRig.rightHandAnchor.forward);
                default:
                    break;
            }
        }
        return new Ray();
    }

    public void UpdateRaycastResult(Ray ray, RaycastResult raycastResult) {
        if (_cameraRig == null || _cameraRig.renderControllersOnClient == false) { return; }

        if (raycastResult.isValid) {
            _lastRaycastHitOrigin = _cameraRig.clientSpaceToWorldMatrix.inverse.MultiplyPoint(ray.origin);
            _lastRaycastHitPosition = _cameraRig.clientSpaceToWorldMatrix.inverse.MultiplyPoint(raycastResult.worldPosition);
            _lastRaycastHitNormal = _cameraRig.clientSpaceToWorldMatrix.inverse.MultiplyVector(raycastResult.worldNormal);
        }
        else {
            _lastRaycastHitOrigin = _lastRaycastHitPosition = _lastRaycastHitNormal = Vector3.zero;
        }
    }

    private class Feedback : AXRInputSender {
        private AirXRPointer _owner;
        private AXRInputDeviceID _device;

        public Feedback(AirXRPointer owner, AXRInputDeviceID device) {
            _owner = owner;
            _device = device;
        }

        // implements AirXRInputSender
        public override byte id => (byte)_device;

        public override void PendInputsPerFrame(AXRInputStream inputStream) {
            if (_owner._cameraRig == null) { return; }

            inputStream.PendState(this, (byte)AXRHandTrackerFeedbackControl.RenderOnClient, _owner._cameraRig.renderControllersOnClient ? (byte)1 : (byte)0);
            inputStream.PendRaycastHit(this, (byte)AXRHandTrackerFeedbackControl.RaycastHit, _owner._lastRaycastHitOrigin, _owner._lastRaycastHitPosition, _owner._lastRaycastHitNormal);
        }
    }
}
