using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour {

    [SerializeField] private AirVRCameraRig rig;

    [SerializeField] private UnityEvent onGetKey;
    [SerializeField] private UnityEvent onGetDownKey;
    [SerializeField] private UnityEvent onGetUpKey;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (AirVRInput.Get(rig,AirVRInput.Touchpad.Button.Touch))
        {
            onGetKey.Invoke();
        }
        if (AirVRInput.GetDown(rig, AirVRInput.Touchpad.Button.Touch))
        {
            onGetDownKey.Invoke();
        }
        if (AirVRInput.GetUp(rig, AirVRInput.Touchpad.Button.Touch))
        {
            onGetUpKey.Invoke();
        }
    }
}
