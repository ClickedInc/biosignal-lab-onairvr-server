using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class CollisionEventHandler : MonoBehaviour {

    [Serializable]
    public class CollisionEvent : UnityEvent<GameObject> 
    {

    }

    public CollisionEvent OnEnter;
    public CollisionEvent OnStay;
    public CollisionEvent OnExit;

    private void OnTriggerEnter(Collider other)
    {
        OnEnter.Invoke(other.gameObject);
    }
}
