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
        if(AirVRInput.GetDown(rig , AirVRInput.TrackedController.Button.Back) ||
            AirVRInput.GetDown(rig, AirVRInput.Touchpad.Button.Back))
        {
            rig.Disconnect();
            return;
        }
        if (AirVRInput.GetDown(rig, AirVRInput.Touchpad.Button.Touch)
            || AirVRInput.GetDown(rig, AirVRInput.TrackedController.Button.TouchpadClick))
        {
            if (ui.IsOnPointer())
                return;

            if (ui.IsFadeOut() || !ui.IsActiveCanvas())
            {
                ui.PopUp();
                return;
            }

            ui.FadeOut();
        }
    }
}
