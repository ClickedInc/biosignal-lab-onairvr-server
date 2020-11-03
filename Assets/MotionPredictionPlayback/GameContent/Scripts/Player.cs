using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private GameObject dropableApple;

    private AirVRStereoCameraRig cameraRig;
    private bool isCatched;
    private GameObject currentDropedApple;
    private ObjectPooler dropableApplePooler;
    private ObjectPooler targetPooler;
    private int pickIndex;

    private void Awake() 
    {
        cameraRig = transform.Find("AirVRCameraRig").GetComponent<AirVRStereoCameraRig>();    
    }

    private void Start()
    {
        //dropableApplePooler = GameContentManager.Instance.ApplePooler;
        targetPooler = GameContentManager.Instance.TargetPooler;

        dropableApplePooler.Pool(dropableApple, 20);
    }

    private void LateUpdate () {
        if (isCatched)
        {
            currentDropedApple.transform.position = cameraRig.rightHandAnchor.position + cameraRig.rightHandAnchor.forward * 1.0f;
            currentDropedApple.transform.LookAt(cameraRig.rightHandAnchor.position);
        }
    }

    public void PickApple(GameObject pickedApple)
    {
        pickIndex += 1;

        if (pickIndex >= Random.Range(3, 6))
        {
            CatchApple(pickedApple);
            pickIndex = 0;
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
