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

        if (AirVRInput.GetDown(rig, AirVRInput.Button.Y)) {
            rig.Disconnect();
            return;
        }

        if (AirVRInput.GetDown(rig, AirVRInput.Button.RIndexTrigger) ||
            AirVRInput.GetDown(rig, AirVRInput.Button.A)) {
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
