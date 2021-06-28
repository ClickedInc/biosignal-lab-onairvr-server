/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using System.Collections.Generic;

public class AirXRSampleSimpleScene : MonoBehaviour, AirXRCameraRigManager.EventHandler {
    public AudioSource music;

    private void Awake() {
        if (FindObjectOfType<MotionPredictionPlayback>()?.playbackModeStartedByEditor ?? true) { return; }

        AirXRCameraRigManager.managerOnCurrentScene.Delegate = this;
    }

    // implements AirXRCameraRigManager.EventHandler
    public void AirXRCameraRigWillBeBound(int clientHandle, AirXRClientConfig config, List<AirXRCameraRig> availables, out AirXRCameraRig selected) {
        selected = availables.Count > 0 ? availables[0] : null;

        if (selected) {
            music.Play();
        }
    }

    public void AirXRCameraRigActivated(AirXRCameraRig cameraRig) {
        // The sample onairvr client app just sends back what it receives.
        // (https://github.com/onairvr/onairvr-client-for-oculus-mobile)

        string pingMessage = "ping from " + System.Environment.MachineName;
        cameraRig.SendUserData(System.Text.Encoding.UTF8.GetBytes(pingMessage));
    }

    public void AirXRCameraRigDeactivated(AirXRCameraRig cameraRig) {}

    public void AirXRCameraRigHasBeenUnbound(AirXRCameraRig cameraRig) {
        // NOTE : This event occurs in OnDestroy() of AirXRCameraRig during unloading scene.
        //        You should be careful because some objects in the scene might be destroyed already on this event.
        if (music != null) {
            music.Stop();
        }
    }

    public void AirXRCameraRigUserDataReceived(AirXRCameraRig cameraRig, byte[] userData) {
        Debug.Log("User data received: " + System.Text.Encoding.UTF8.GetString(userData));
    }
}
