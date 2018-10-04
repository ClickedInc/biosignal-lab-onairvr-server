using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class RayEventHandler : MonoBehaviour {

    [Serializable]
    public class MouseEvent : UnityEvent<GameObject>
    {

    }

    [SerializeField] private ObjectPooler pooler;
    [SerializeField] private string targetTag;
    [SerializeField] private MouseEvent mouseRayEvents;

    private Ray ray;

    public void Raycast(GameObject rayOrigin)
    {
        RaycastHit hit = new RaycastHit();

        Ray ray = new Ray(rayOrigin.transform.position, rayOrigin.transform.forward);
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
