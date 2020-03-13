using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameContentInputManager : MonoBehaviour {

    [SerializeField] private AirVRCameraRig rig;

    [SerializeField] private UnityEvent onGetKeyDown;
    [SerializeField] private UnityEvent onGetKeyUp;

	void Update () {
        if (rig.gameObject.activeSelf == false) {
            return;
        }

        if (AirVRInput.GetDown(rig, AirVRInput.Button.RIndexTrigger))
        {
            onGetKeyDown.Invoke();
        }
        if (AirVRInput.GetUp(rig, AirVRInput.Button.RIndexTrigger))
        {
            onGetKeyUp.Invoke();
        }
    }
}
