using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene :  MonoBehaviour
{
    [SerializeField] private GameObject play;
    [SerializeField] private GameObject standby;

    [SerializeField] private VideoManager videoManager;

    private UIManager ui;
    private AirVRCameraRig rig;
    private Vector3 playPos;

    private void Awake()
    {
        rig = FindObjectOfType<AirVRCameraRig>();
        ui = FindObjectOfType<UIManager>();
    }

    private void Update()
    {
        if (!rig)
            return;

        if (rig.isBoundToClient)
        {
            if (!play.activeSelf )
            {
                ActivePlay();
            }

            if (!videoManager.prepared)
                return;

            DisableStandby();

            return;
        }

        if (!standby.activeSelf)
        {
            DisablePlay();
            ActiveStandby();
            return;
        }
    }

    public void ActivePlay()
    {
        rig.RecenterPose();
        play.SetActive(true);
        videoManager.Play();
        ui.PopUp();
    }

    public void DisablePlay()
    {
        videoManager.Pause();
        videoManager.DisableContents();
        ui.Disable();
        play.SetActive(false);
    }

    public void ActiveStandby()
    {
        rig.RecenterPose();
        rig.headPose.rotation = Quaternion.identity;
        standby.SetActive(true);
    }

    public void DisableStandby()
    {
        standby.SetActive(false);
    }
}
