using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageClearUI : MonoBehaviour {

    [SerializeField] private GameObject head;

	// Update is called once per frame
	void LateUpdate () {
        transform.position = head.transform.forward * 4;
        
        transform.LookAt(head.transform.position);

        transform.Rotate(new Vector3(0, 180, 0));
    }
}
