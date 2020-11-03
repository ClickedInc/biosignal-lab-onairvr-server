using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class score : MonoBehaviour
{
    public Text bestscore;
    // Start is called before the first frame update
    private void Update()
    {
        bestscore.text = "최고점수 :"+PlayerPrefs.GetInt("Best Score", 0).ToString();
    }

    // Update is called once per frame

}
