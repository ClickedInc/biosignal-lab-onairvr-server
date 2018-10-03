using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class MousePointRayEventHandler : MonoBehaviour {

    [Serializable]
    public class MouseEvent : UnityEvent<GameObject>
    {

    }

    [SerializeField] private ObjectPooler pooler;
    [SerializeField] private string targetTag;
    [SerializeField] private AirVRCameraRig rig;
    [SerializeField] private GameObject head;
    [SerializeField] private MouseEvent mouseRayEvents;

    private Ray ray;

	void Update () {
        if (AirVRInput.GetDown(rig,AirVRInput.Touchpad.Button.Touch))
        {
            RaycastHit hit = new RaycastHit();

            Ray ray = new Ray(head.transform.position,head.transform.forward);
            Debug.DrawLine(ray.origin, ray.direction * 500, Color.red);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.transform.tag == targetTag)
                {
                    mouseRayEvents.Invoke(hit.transform.gameObject);
                }
            }
        }
	}
}
