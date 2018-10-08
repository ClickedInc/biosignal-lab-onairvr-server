using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private float autoFadeTime = 5.0f;
    [SerializeField] private GameObject canvas;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private VideoManager videoManager;
    [SerializeField] private Transform pivot;

    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject pauseButton;

    private AirVRCameraRig rig;
    private bool fadeOut;
    private float waitTime;
    private bool onPointer;
    private bool onPlayHead;

    private void Awake()
    {
        rig = FindObjectOfType<AirVRCameraRig>();
    }

    public void ActivePlayButton()
    {
        playButton.SetActive(true);
        pauseButton.SetActive(false);
    }

    public void ActivePauseButton()
    {
        playButton.SetActive(false);
        pauseButton.SetActive(true);
    }

    public bool IsOnPointer()
    {
        return onPointer;
    }

    public void OnPlayBodyClick()
    {
        if (videoManager.seeking)
            return;
        videoManager.Pause();
        videoManager.SetBodyClickTime();
    }

    public void OnPlayHeadDown()
    {
        if (videoManager.seeking)
            return;
        onPlayHead = true;
        videoManager.Pause();
        videoManager.StartPlayHeadMode();
    }

    public void OnPlayHeadUp()
    {
        if (!onPlayHead)
            return;
        onPlayHead = false;
        videoManager.StopPlayHeadMode();
        videoManager.Play();
    }

    public bool IsFadeOut()
    {
        return fadeOut;
    }

    public bool IsActiveCanvas()
    {
        return canvas.activeSelf;
    }

    public void OnPlayButtonClick()
    {
        if (videoManager.seeking)
            return;
        if (videoManager.playing)
            return;
        videoManager.Play();
    }

    public void OnPauseButtonClick()
    {
        if (videoManager.seeking)
            return;
        if (!videoManager.playing)
            return;
        videoManager.Pause();
    }

    public void OnFastfowardButtonClick()
    {
        if (videoManager.seeking)
            return;
        videoManager.Forward();
    }

    public void OnBackwardButtonClick()
    {
        if (videoManager.seeking)
            return;
        videoManager.Reset();
    }

    public void OnPointerEnter()
    {
        PopUp();
        onPointer = true;
    }

    public void OnPointerExit()
    {
        onPointer = false;
    }

    public void PopUp()
    {
        if(!IsActiveCanvas())
        {
            pivot.eulerAngles = new Vector3(0.0f, rig.headPose.eulerAngles.y, 0.0f);
            videoManager.ResetPlayHead();
            canvas.SetActive(true);
        }
        fadeOut = false;
        group.alpha = 1.0f;
        waitTime = 0.0f;
    }

    public void Disable()
    {
        onPointer = false;
        fadeOut = false;
        canvas.SetActive(false);
    }

    public void FadeOut()
    {
        fadeOut = true;
    }

    private void Update()
    {
        if (!canvas.activeSelf)
            return;

        if(onPointer)
            return;

        if (waitTime >= autoFadeTime && !fadeOut)
        {
            FadeOut();
            return;
        }

        if(fadeOut)
        {
            if(group.alpha > 0.0f )
            {
                group.alpha -= Time.deltaTime * fadeSpeed;
                return;
            }

            Disable();
            return;
        }

        waitTime += Time.deltaTime;
    }
}
