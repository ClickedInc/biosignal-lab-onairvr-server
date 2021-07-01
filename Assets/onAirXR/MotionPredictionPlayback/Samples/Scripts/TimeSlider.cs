using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSlider : MonoBehaviour {

    public bool isActive;
    private Slider timeSlider;

	void Awake () {
        isActive = true;
        timeSlider = GetComponent<Slider>();
	}
	
	void Update () {
        if (isActive)
            timeSlider.value -= Time.deltaTime;
	}

    public void SetTimer(bool active)
    {
        isActive = active;
    }
}
