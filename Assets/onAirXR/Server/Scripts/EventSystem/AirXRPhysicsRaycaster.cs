/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(AirXRPointer))]

public class AirXRPhysicsRaycaster : BaseRaycaster {
    private static List<AirXRPhysicsRaycaster> _allRaycasters = new List<AirXRPhysicsRaycaster>();

    public static List<AirXRPhysicsRaycaster> GetAllRaycasters() {
        return _allRaycasters;
    }

    protected AirXRPhysicsRaycaster() { }


    [SerializeField]
    protected LayerMask _eventMask = -1;

    public AirXRPointer pointer { get; private set; }

    public override Camera eventCamera {
        get {
            return pointer.cameraRig.cameras[0];
        }
    }

    public virtual int depth {
        get {
            return (int)eventCamera.depth;
        }
    }

    public int finalEventMask {
        get {
            return eventCamera.cullingMask & _eventMask;
        }
    }

    public LayerMask eventMask {
        get {
            return _eventMask;
        }
        set {
            _eventMask = value;
        }
    }

    protected override void OnEnable() {
        pointer = GetComponent<AirXRPointer>();

        base.OnEnable();
        _allRaycasters.Add(this);
    }

    protected override void OnDisable() {
        base.OnDisable();
        _allRaycasters.Remove(this);
    }

    public Vector2 GetScreenPosition(Vector3 worldPosition) {
        return eventCamera.WorldToScreenPoint(worldPosition);
    }

    // overrides BaseRaycaster
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
        if (eventData.IsVRPointer() == false || pointer.interactable == false) {
            return;
        }

        var ray = pointer.GetWorldRay();
        float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;
        var hits = Physics.RaycastAll(ray, dist, finalEventMask);

        if (hits.Length > 0) {
            if (hits.Length > 1) {
                System.Array.Sort(hits, (r1, r2) => r1.distance.CompareTo(r2.distance));
            }

            for (int i = 0; i < hits.Length; i++) {
                var result = new RaycastResult {
                    gameObject = hits[i].collider.gameObject,
                    module = this,
                    distance = hits[i].distance,
                    index = resultAppendList.Count,
                    worldPosition = hits[0].point,
                    worldNormal = hits[0].normal
                };
                resultAppendList.Add(result);
            }
        }
    }
}
