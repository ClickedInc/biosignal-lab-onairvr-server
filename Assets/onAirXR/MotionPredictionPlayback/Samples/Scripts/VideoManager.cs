using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
public class VideoManager : MonoBehaviour
{
    [SerializeField] private bool autoStart;
    [SerializeField] private float progressLerpSpeed;

    [SerializeField] private GameObject contents;
    [SerializeField] private UIManager ui;

    [SerializeField] private Transform progressStart;
    [SerializeField] private Transform progressEnd;
    [SerializeField] private Transform progressHead;

    public bool playing { private set; get;}
    public bool prepared
    {
        get
        {
            return player.isPrepared;
        }
    }

    private bool playHeadMode;
    public Vector3 dir { set; get; }
    public bool seeking { private set; get; }

    private AirXRPointer pointer;
    private float magnitude;
    private VideoPlayer player;
    private float progressTotal;
    private AirVRCameraRig rig;
    private double saveTime;
    private Vector3 target;

    private Vector3 playHeadStartPoint;

    public void SetBodyClickTime()
    {
        seeking = true;
        RaycastHit hit = new RaycastHit();
        if (!Physics.Raycast(pointer.GetWorldRay(), out hit))
        {
            Debug.LogError("Not Found Panel");
            return;
        }

        magnitude = (hit.point - progressStart.position).magnitude;

        target = progressStart.position + dir.normalized * magnitude;

        PlayHeadBoundCheck();

        float current = magnitude / progressTotal;
        saveTime = current * player.clip.length;
        Play();
    }

    public void StartPlayHeadMode()
    {
        playHeadMode = true;
        seeking = true;
        playHeadStartPoint = progressHead.position;
    }

    public void StopPlayHeadMode()
    {
        playHeadMode = false;
    }

    public void Play()
    {
        player.time= saveTime;
        if (!seeking)
        {
            ui.ActivePauseButton();
            player.Play();
        }

        playing = true;
    }

    public void DisableContents()
    {
        contents.SetActive(false);
    }

    public void Pause()
    {
        playing = false;
        saveTime = player.time;
        ui.ActivePlayButton();
        player.Pause();
    }

    public void PlayHeadBoundCheck()
    {
        if (Vector3.Cross(progressStart.position, target).y < 0)
            target = progressStart.position;
        else if (Vector3.Cross(progressEnd.position, target).y > 0)
            target = progressEnd.position;
    }

    public void ResetPlayHead()
    {
        float progressCurrent = (float)(player.time / player.clip.length);
        dir = progressEnd.position - progressStart.position;
        magnitude = progressCurrent * progressTotal;
        target = progressStart.position + dir.normalized * magnitude;
    }

    public void Reset()
    {
        Pause();
        saveTime -= 10.0f;
        Play();
    }

    public void Forward()
    {
        Pause();
        saveTime += 10.0f;
        Play();
    }

    private void Awake()
    {
        player = FindObjectOfType<VideoPlayer>();
        pointer = FindObjectOfType<AirXRPointer>();

        if (!player)
        {
            Debug.LogError("Not Found Video Player");
            return;
        }

        rig = FindObjectOfType<AirVRCameraRig>();

        player.prepareCompleted += OnPrepareCompleted;
        player.seekCompleted += Player_seekCompleted;
    }

    private void Player_seekCompleted(VideoPlayer source)
    {
        seeking = false;
        ui.ActivePauseButton();
        player.Play();
    }

    private void Start ()
    {
        progressTotal = (progressEnd.position - progressStart.position).magnitude;
        //progressTotal = Mathf.Abs(progressStart.position.x) + Mathf.Abs(progressEnd.position.x);

        if (autoStart)
            Play();
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        source.time = saveTime;
        contents.SetActive(true);
    }

    private void Update ()
    {
        if (!player.isPrepared)
            return;

        if(playHeadMode)
        {
            RaycastHit hit = new RaycastHit();
            if(!Physics.Raycast( pointer.GetWorldRay() , out hit))
            {
                ui.OnPlayHeadUp();
                return;
            }

            Vector3 cross = Vector3.Cross(playHeadStartPoint, hit.point);
            dir = progressEnd.position - progressStart.position;

            if (cross.y < 0)
            {
                dir = -dir;
            }

            magnitude = (hit.point - playHeadStartPoint).magnitude;
            target = playHeadStartPoint + dir.normalized * magnitude;
            PlayHeadBoundCheck();
            float current = (target - progressStart.position).magnitude / progressTotal;

            if (cross.y < 0 && current < 0.13f)
            {
                target = progressStart.position;
                current = 0.0f;
            }
            if (cross.y > 0 &&  current > 0.9f)
            {
                target = progressEnd.position;
                current = 1.0f;
            }

            saveTime = current * player.clip.length;
        }
        else if (!seeking)
        {
            ResetPlayHead();
        }

        progressHead.transform.position = Vector3.Lerp(progressHead.transform.position, target, Time.deltaTime * progressLerpSpeed);
    }
}

