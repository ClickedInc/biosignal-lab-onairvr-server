using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private GameObject dropableApple;
    [SerializeField] private GameObject head;

    private bool isCatched;
    private GameObject currentDropedApple;
    private ObjectPooler dropableApplePooler;
    private ObjectPooler targetPooler;
    private int pickIndex;

    private void Start()
    {
        dropableApplePooler = GameContentManager.Instance.ApplePooler;
        targetPooler = GameContentManager.Instance.TargetPooler;

        dropableApplePooler.Pool(dropableApple, 20);
    }

    private void LateUpdate () {
        if (isCatched)
        {
            currentDropedApple.transform.position = head.transform.forward * 5;
            currentDropedApple.transform.LookAt(head.transform.position);
        }
    }

    public void PickApple(GameObject pickedApple)
    {
        pickIndex += 1;
        audioPlayer.PlayPickSound();

        if (pickIndex >= Random.Range(3, 6))
        {
            CatchApple(pickedApple);
            pickIndex = 0;
        }
    }

    public void CatchApple(GameObject catchedApple)
    {
        audioPlayer.PlayCatchSound();
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
