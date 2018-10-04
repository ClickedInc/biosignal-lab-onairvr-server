using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ObjectPooler : ScriptableObject {

    private List<GameObject> pool = new List<GameObject>();

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

    public void MunaulPositionPool(GameObject gameObject, int num,Transform[] transforms)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject tmp = Instantiate(gameObject);
            tmp.SetActive(false);
            pool.Add(tmp);

            if (transforms.Length < i)
                return;

            tmp.transform.position = transforms[i].position;
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

        Debug.Log("There is no useable pool");
        try
        {
            return pool[pool.Count - 1];
        }
        catch
        {
            Debug.Log("Checking for whether or not pool is empty");
            return null;
        }

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

    public void RetureAllPool()
    {
        foreach (GameObject item in pool)
        {
            item.SetActive(false);
        }
    }
}
