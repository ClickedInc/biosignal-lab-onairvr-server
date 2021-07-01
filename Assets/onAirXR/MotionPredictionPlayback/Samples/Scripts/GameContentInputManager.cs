using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameContentInputManager : MonoBehaviour {

    [SerializeField] private AirXRCameraRig rig;

    [SerializeField] private UnityEvent onGetKeyDown;
    [SerializeField] private UnityEvent onGetKeyUp;

	void Update () {
        if (rig.gameObject.activeSelf == false) {
            return;
        }

        if (AirXRInput.GetDown(rig, AirXRInput.Button.RIndexTrigger))
        {
            onGetKeyDown.Invoke();
        }
        if (AirXRInput.GetUp(rig, AirXRInput.Button.RIndexTrigger))
        {
            onGetKeyUp.Invoke();
        }
    }
}
