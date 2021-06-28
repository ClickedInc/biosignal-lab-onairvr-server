/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.EventSystems;

internal class AirXRPointerEventData : PointerEventData {
    public AirXRPointerEventData(EventSystem eventSystem) : base(eventSystem) { }

    public Ray worldSpaceRay;
}

internal static class PointerEventDataExtension {
    public static bool IsVRPointer(this PointerEventData pointerEventData) {
        return pointerEventData is AirXRPointerEventData;
    }

    public static Ray GetRay(this PointerEventData pointerEventData) {
        return (pointerEventData as AirXRPointerEventData).worldSpaceRay;
    }
}