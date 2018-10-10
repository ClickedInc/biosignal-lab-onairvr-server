using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSlider : MonoBehaviour {

    private Slider timeSlider;

	void Awake () {
        timeSlider = GetComponent<Slider>();
	}
	
	void Update () {
        timeSlider.value -= Time.deltaTime;
	}
}
