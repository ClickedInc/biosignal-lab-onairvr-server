using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI : MonoBehaviour
{
    private RectTransform rt;
    public GameObject main_camera;
    private void Start()
    {
        rt = GetComponent<RectTransform>(); 

    }
    // Update is called once per frame
    void Update()
    {
        rt.anchoredPosition = main_camera.transform.position-new Vector3(0f,0.5f,0f);

    }
}
