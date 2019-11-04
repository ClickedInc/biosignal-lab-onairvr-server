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
        dropableApplePooler = GameContentManager.Instance.ApplePooler;
        targetPooler = GameContentManager.Instance.TargetPooler;

        dropableApplePooler.Pool(dropableApple, 20);
    }

    private void LateUpdate () {
        if (isCatched)
        {
            var position = cameraRig.centerEyeAnchor.position;
            var forward = cameraRig.centerEyeAnchor.forward;

            if (AirVRInput.IsDeviceFeedbackEnabled(cameraRig, AirVRInput.Device.TrackedController)) {
                var rot = Quaternion.identity;
                AirVRInput.GetTrackedDevicePositionAndOrientation(cameraRig, AirVRInput.Device.TrackedController, out position, out rot);

                forward = rot * Vector3.forward;
            }

            currentDropedApple.transform.position = forward * 5;
            currentDropedApple.transform.LookAt(position);
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
