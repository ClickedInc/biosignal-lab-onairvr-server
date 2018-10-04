using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    [SerializeField] private ObjectPooler dropableApplePooler;
    [SerializeField] private GameObject dropableApple;
    [SerializeField] private GameObject head;

    private bool isCatched;
    private GameObject currentDropedApple;
    public ObjectPooler targetPooler;

    // Use this for initialization
    void Awake () {
        dropableApplePooler.Pool(dropableApple, 50);
    }

    // Update is called once per frame
    void Update () {
        if (isCatched)
        {
            currentDropedApple.transform.position = head.transform.forward * 5;
            currentDropedApple.transform.LookAt(head.transform.position);
        }
    }

    public void CatchApple(GameObject catchedApple)
    {
        targetPooler.ReturnPool(gameObject);

        if (!currentDropedApple)
        {
            currentDropedApple = dropableApplePooler.GetPool();
            isCatched = true;
        }
    }

    public void DropApple()
    {
        currentDropedApple = null;
        isCatched = false;
    }
}
