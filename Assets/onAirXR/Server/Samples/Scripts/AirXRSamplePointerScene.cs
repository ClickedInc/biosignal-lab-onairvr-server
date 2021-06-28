/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.UI;

public class AirXRSamplePointerScene : MonoBehaviour {   
    private AirVRCameraRig _vrCameraRig;
    private Button _button;
    private Image _indicator;
    private float _remainingToStopIndicating = -1.0f;

    [SerializeField] private AnimationCurve _vibration = null;

    private void Awake() {
        _vrCameraRig = FindObjectOfType<AirVRCameraRig>();

        _button = transform.Find("Canvas/Panel/Button").GetComponent<Button>();
        _indicator = transform.Find("Canvas/Panel/Indicator").GetComponent<Image>();

        _indicator.gameObject.SetActive(false);
    }

    private void Start() {
        _button.onClick.AddListener(() => {
            _remainingToStopIndicating = _vibration.keys[_vibration.keys.Length - 1].time;
            _indicator.gameObject.SetActive(true);

            AirXRInput.SetVibration(_vrCameraRig, AirXRInput.Device.RightHandTracker, _vibration, _vibration);
        });
    }

    private void Update() {
        updateIndicator();
    }

    private void updateIndicator() {
        if (_remainingToStopIndicating <= 0.0f) { return; }

        _remainingToStopIndicating -= Time.deltaTime;
        if (_remainingToStopIndicating <= 0.0f) {
            _indicator.gameObject.SetActive(false);
        }
    }
}
