using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
   
    [SerializeField] private GameObject dropableApple;
    [SerializeField] private GameObject head;

    private bool isCatched;
    private GameObject currentDropedApple;
    private ObjectPooler dropableApplePooler;
    private ObjectPooler targetPooler;

    private void Start()
    {
        dropableApplePooler = GameContentManager.Instance.ApplePooler;
        targetPooler = GameContentManager.Instance.TargetPooler;

        dropableApplePooler.Pool(dropableApple, 20);
    }

    void Update () {
        if (isCatched)
        {
            currentDropedApple.transform.position = head.transform.forward * 5;
            currentDropedApple.transform.LookAt(head.transform.position);
        }
    }

    public void CatchApple(GameObject catchedApple)
    {
        targetPooler.ReturnPool(catchedApple);
        dropableApplePooler.ReturnPool(catchedApple);

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
