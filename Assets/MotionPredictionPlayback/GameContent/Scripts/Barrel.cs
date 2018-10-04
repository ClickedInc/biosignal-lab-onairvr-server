using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : MonoBehaviour {

    [SerializeField] private StageManager stageManager;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Put(GameObject gameObject)
    {
        if (gameObject.tag == "Apple")
        {
            Debug.Log("!!");
            stageManager.StageTargetRemove(gameObject);
        }
    }
}
