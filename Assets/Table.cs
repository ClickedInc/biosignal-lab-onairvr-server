using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.AddComponent<DestroySelf>();
        collision.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

    }

}
