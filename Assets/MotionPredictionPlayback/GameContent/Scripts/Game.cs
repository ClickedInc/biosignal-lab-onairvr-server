using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {

    [SerializeField] private StageManager stageManager;

	// Use this for initialization
	void Start () {
        stageManager.StartStage();
    }
	
	// Update is called once per frame
	void Update () {

    }
}
