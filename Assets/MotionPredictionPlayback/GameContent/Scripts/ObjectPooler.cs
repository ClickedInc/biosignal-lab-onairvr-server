using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ObjectPooler : ScriptableObject {

    public List<GameObject> pool = new List<GameObject>();

    public void OnEnable()
    {
        pool.Clear();    
    }

    public void Pool(GameObject gameObject,int num)
    {
        for (int i=0; i<num; i++)
        {
            GameObject tmp = Instantiate(gameObject);
            tmp.SetActive(false);
            pool.Add(tmp);
        }
    }

    public GameObject GetPool()
    {
        foreach(GameObject item in pool)
        {
            if (item.activeSelf == false)
            {
                item.SetActive(true);
                return item;
            }
        }

        Debug.Log("Null pool");
        return null;  
    }

    public void ReturnPool()
    {
        foreach(GameObject item in pool)
        {
            if(item.activeSelf == true)
            {
                item.SetActive(false);
                break;
            }
        }
    }

    public void ReturnPool(GameObject gameObject)
    {
        foreach (GameObject item in pool)
        {
            if (item.activeSelf == true && item == gameObject)
            {
                item.SetActive(false);
                break;
            }
        }
    }
}
