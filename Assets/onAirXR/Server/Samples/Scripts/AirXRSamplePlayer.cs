/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public class AirXRSamplePlayer : MonoBehaviour {
    private AirVRCameraRig _cameraRig;
    private MeshRenderer _head;
    private MeshRenderer _leftController;
    private MeshRenderer _rightController;
    private Color _leftControllerColor;
    private Color _rightControllerColor;

    [SerializeField] private Color _triggeredControllerColor = new Color(224 / 255.0f, 58 / 255.0f, 69 / 255.0f);

    private void Awake() {
        _cameraRig = GetComponentInChildren<AirVRCameraRig>();
        if (_cameraRig == null) { return; }

        var head = _cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor/Head");
        if (head != null) {
            _head = head.GetComponent<MeshRenderer>();
        }
        _leftController = _cameraRig.transform.Find("TrackingSpace/LeftHandAnchor/Controller").GetComponent<MeshRenderer>();
        _rightController = _cameraRig.transform.Find("TrackingSpace/RightHandAnchor/Controller").GetComponent<MeshRenderer>();

        _leftControllerColor = _leftController.material.color;
        _rightControllerColor = _rightController.material.color;
    }

    private void Update() {
        if (_cameraRig == null) { return; }

        if (_head != null) {
            _head.enabled = _cameraRig.isActive;
        }
        _leftController.enabled = _cameraRig.isActive && AirXRInput.IsDeviceAvailable(_cameraRig, AirXRInput.Device.LeftHandTracker);
        _rightController.enabled = _cameraRig.isActive && AirXRInput.IsDeviceAvailable(_cameraRig, AirXRInput.Device.RightHandTracker);

        if (AirXRInput.Get(_cameraRig, AirXRInput.Button.X) ||
            AirXRInput.Get(_cameraRig, AirXRInput.Button.Y) ||
            AirXRInput.Get(_cameraRig, AirXRInput.Button.LIndexTrigger)) {
            _leftController.material.color = _triggeredControllerColor;
        }
        else {
            _leftController.material.color = _leftControllerColor;
        }

        if (AirXRInput.Get(_cameraRig, AirXRInput.Button.A) ||
            AirXRInput.Get(_cameraRig, AirXRInput.Button.B) ||
            AirXRInput.Get(_cameraRig, AirXRInput.Button.RIndexTrigger)) {
            _rightController.material.color = _triggeredControllerColor;
        }
        else {
            _rightController.material.color = _leftControllerColor;
        }
    }
}
