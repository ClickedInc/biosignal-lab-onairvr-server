/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine.EventSystems;

public class AirXREventSystem : EventSystem {
    protected override void OnApplicationFocus(bool hasFocus) {
        // do nothing to prevents from being paused when lose focus
    }
}
