using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    // Start is called before the first frame update

    float time = 0f;
    private void Update()
    {
        time += Time.deltaTime;
        if(time >= 0.7f) {Destroy(gameObject);}
    }
}
