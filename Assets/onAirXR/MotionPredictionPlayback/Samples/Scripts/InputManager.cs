using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private UIManager ui;

    private AirVRCameraRig rig;

    private void Awake()
    {
        rig = FindObjectOfType<AirVRCameraRig>();
        ui = FindObjectOfType<UIManager>();
    }

    private void Update ()
    {
        if (rig == null || rig.gameObject.activeSelf == false) {
            return;
        }

        if (AirXRInput.GetDown(rig, AirXRInput.Button.Y)) {
            rig.Disconnect();
            return;
        }

        if (AirXRInput.GetDown(rig, AirXRInput.Button.RIndexTrigger) ||
            AirXRInput.GetDown(rig, AirXRInput.Button.A)) {
            if (ui.IsOnPointer())
                return;

            if (ui.IsFadeOut() || !ui.IsActiveCanvas()) {
                ui.PopUp();
                return;
            }

            ui.FadeOut();
        }
    }
}
