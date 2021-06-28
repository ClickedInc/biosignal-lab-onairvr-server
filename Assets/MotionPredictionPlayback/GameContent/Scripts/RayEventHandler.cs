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

    public void Raycast(AirVRCameraRig cameraRig)
    {
        RaycastHit hit = new RaycastHit();

        Ray ray = new Ray(cameraRig.rightHandAnchor.position, cameraRig.rightHandAnchor.forward);
        Debug.DrawLine(ray.origin, ray.direction * 500, Color.red);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            mouseRayEvents.Invoke(hit.transform.gameObject);
        }
    }
}
