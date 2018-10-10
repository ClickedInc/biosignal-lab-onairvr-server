using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameContentInputManager : MonoBehaviour {

    [SerializeField] private AirVRCameraRig rig;

    [SerializeField] private UnityEvent onGetKey;
    [SerializeField] private UnityEvent onGetKeyDown;
    [SerializeField] private UnityEvent onGetKeyUp;

	void Update () {
        if (rig.gameObject.activeSelf == false) {
            return;
        }


		if (AirVRInput.Get(rig,AirVRInput.Touchpad.Button.Touch))
        {
            onGetKey.Invoke();
        }
        if (AirVRInput.GetDown(rig, AirVRInput.Touchpad.Button.Touch))
        {
            onGetKeyDown.Invoke();
        }
        if (AirVRInput.GetUp(rig, AirVRInput.Touchpad.Button.Touch))
        {
            onGetKeyUp.Invoke();
        }
    }
}
