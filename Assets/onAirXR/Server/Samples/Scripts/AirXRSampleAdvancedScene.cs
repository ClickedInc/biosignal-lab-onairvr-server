/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using UnityEngine;

public class AirXRSampleAdvancedScene : MonoBehaviour, AirXRCameraRigManager.EventHandler {
    [SerializeField] private AirXRCameraRig _primaryCameraRig = null;

    void Awake() {
        AirXRCameraRigManager.managerOnCurrentScene.Delegate = this;
    }

    // implements AirXRCameraRigMananger.EventHandler
    public void AirXRCameraRigWillBeBound(int clientHandle, AirXRClientConfig config, List<AirXRCameraRig> availables, out AirXRCameraRig selected) {
        if (availables.Contains(_primaryCameraRig)) {
            selected = _primaryCameraRig;
        }
        else if (availables.Count > 0) {
            selected = availables[0];
        }
        else {
            selected = null;
        }
    }

    public void AirXRCameraRigActivated(AirXRCameraRig cameraRig) {}
    public void AirXRCameraRigDeactivated(AirXRCameraRig cameraRig) {}
    public void AirXRCameraRigHasBeenUnbound(AirXRCameraRig cameraRig) {}
    public void AirXRCameraRigUserDataReceived(AirXRCameraRig cameraRig, byte[] data) {}
}
