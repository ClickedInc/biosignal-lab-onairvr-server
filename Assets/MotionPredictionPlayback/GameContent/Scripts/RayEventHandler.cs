using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class RayEventHandler : MonoBehaviour {

    [Serializable]
    public class MouseEvent : UnityEvent<GameObject> { }

    [SerializeField] private MouseEvent mouseRayEvents;

    private Ray ray;

    public void Raycast(AirVRStereoCameraRig cameraRig)
    {
        RaycastHit hit = new RaycastHit();

        var position = cameraRig.centerEyeAnchor.position;
        var forward = cameraRig.centerEyeAnchor.forward;

        if (AirVRInput.IsDeviceFeedbackEnabled(cameraRig, AirVRInput.Device.TrackedController)) {
            var rot = Quaternion.identity;
            AirVRInput.GetTrackedDevicePositionAndOrientation(cameraRig, AirVRInput.Device.TrackedController, out position, out rot);

            forward = rot * Vector3.forward;
        }

        Ray ray = new Ray(position, forward);
        Debug.DrawLine(ray.origin, ray.direction * 500, Color.red);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            mouseRayEvents.Invoke(hit.transform.gameObject);
        }
    }
}
