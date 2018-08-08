using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class CSVSimulator : MonoBehaviour {

    private Dictionary<string, int> gameviewSizeTypes = new Dictionary<string, int>()
    {
        {"StandAlone",6 },
        {"FreeAspect",0 }
    };
   
    private int captureLength;
    private int captureFrame;
    private int captureWidth;
    private int captureHeight;
    private int estimatedDelayTime;
    private int qf;
    private int qh;
    private int captureNum;
    private string csvPath;
    private string timeWarp_predict_path;
    private string timeWarp_nonPredict_path;
    private string nonTimeWarp_predict_path;
    private string nonTimeWarp_nonPredict_path;
    private CapturedataSetter captureDataSetter;
    private List<Dictionary<string, object>> data;
    private Camera[] playbackCameras = new Camera[2];
    private Camera timeWarpingCamera;
    private CaptureManager captureManger;
    private GameObject head;
    private GameObject anchor;
    private GameObject leftTexture;
    private GameObject rightTexture;
  
    private enum CaptureState
    {
        TimeWarp_Predict,
        TimeWarp_NonPredict,
        NonTimeWarp_Predict,
        NonTimeWarp_NonPredict
    }
    private CaptureState captureState;

    private void Awake()
    {
        Init();
    }

    private void Update () 
    {
        StartCoroutine(SimulationControll());
        SetGameviewScale(0.0001f);
        Debug.Log("HeadFrame : " + qf);
        Debug.Log("TextureFrame : " + qh);
    }

    private void OnDisable()
    {
        SetGameviewSize(gameviewSizeTypes["FreeAspect"]);
        SetGameviewScale(1);
    }

    private int LatencyFrameCirculate(int latencyTime)
    {
        int latencyFrame = (latencyTime * 120) / 1000;
        return latencyFrame;
    }

    private void Simulate(bool useTimeWarp, bool usePredict, string path, string message, int num)
    {
        string[] rotateDataQF = rotateDataSetting(usePredict, qf);
        string[] rotateDataQH = rotateDataSetting(usePredict, qh);

        if (rotateDataQF == null || rotateDataQH == null)
        {
            return;
        }

        transform.rotation = new Quaternion(
          float.Parse(rotateDataQF[1]),
          float.Parse(rotateDataQF[2]),
          float.Parse(rotateDataQF[3]),
          float.Parse(rotateDataQF[4])
          );

        head.transform.rotation = transform.rotation;

        if (useTimeWarp)
            anchor.transform.rotation = new Quaternion(
             float.Parse(rotateDataQH[1]),
             float.Parse(rotateDataQH[2]),
             float.Parse(rotateDataQH[3]),
             float.Parse(rotateDataQH[4])
             );
        else
            anchor.transform.rotation = transform.rotation;

        for(int i=0; i<2; i++)
        {
            playbackCameras[i].Render();
        }

        timeWarpingCamera.Render();

        if (qf < captureLength)
        {
            captureManger.CaptureScreenshot(float.Parse(data[qf]["time_stamp"].ToString()), path, message, num);
        }
        else
        {
            if (captureLength != 0)
                EditorApplication.isPlaying = false;
        }          
    }

    private IEnumerator SimulationControll()
    {
        yield return new WaitForEndOfFrame();

        captureNum++;

        for (int i = 0; i < 4; i++)
        {
            if (i == 0)
                Simulate(false, false, nonTimeWarp_nonPredict_path, "NonTimeWarp & NonPredict", captureNum);
            else if (i == 1)
                Simulate(false, true, nonTimeWarp_predict_path, "NonTimeWarp & Predict", captureNum);
            else if (i == 2)
                Simulate(true, true, timeWarp_predict_path, "TimeWarp & Predict", captureNum);
            else
                Simulate(true, false, timeWarp_nonPredict_path, "TimeWarp & NonPredict", captureNum);
        }

        if (qf++ >= LatencyFrameCirculate(estimatedDelayTime))
        {
            qh++;
        }
    }

    private string[] rotateDataSetting(bool isPredict, int dataNum)
    {
        if (dataNum >= data.Count)
        {
            return null;
        }

        string[] rotateData;

        if (isPredict)
        {
            rotateData = data[dataNum]["predicted_ori"].ToString().Split(',', '(', ')');
        }
        else
        {
            rotateData = data[dataNum]["original_ori"].ToString().Split(',', '(', ')');
        }
        return rotateData;
    }

    private void Init()
    {
        captureDataSetter = GameObject.Find("CapturedataSetter").GetComponent<CapturedataSetter>();

        this.csvPath = captureDataSetter.csvPath;
        this.captureLength = captureDataSetter.captureLength;
        this.captureFrame = captureDataSetter.captureFrame;
        this.captureWidth = captureDataSetter.captureWidth;
        this.captureHeight = captureDataSetter.captureHeight;
        this.timeWarp_predict_path = captureDataSetter.TimeWarp_predict_path;
        this.timeWarp_nonPredict_path = captureDataSetter.TimeWarp_nonPredict_path;
        this.nonTimeWarp_predict_path = captureDataSetter.NonTimeWarp_predict_path;
        this.nonTimeWarp_nonPredict_path = captureDataSetter.NonTimeWarp_nonPredict_path;
        this.estimatedDelayTime = captureDataSetter.estimatedDelayTime;

        if (timeWarp_predict_path == string.Empty || nonTimeWarp_predict_path == string.Empty 
            || timeWarp_nonPredict_path == string.Empty || nonTimeWarp_nonPredict_path == string.Empty)
        {
            DefaltPathSetting();
        }

        for (int i = 0; i < 2; i++)
        {
            playbackCameras[i] = transform.GetChild(i).GetComponent<Camera>();
            playbackCameras[i] = transform.GetChild(i).GetComponent<Camera>();
        }

        Time.captureFramerate = captureFrame;

        data = CSVReader.Read(csvPath);

        GameObject captureModule = Instantiate(Resources.Load("CaptureModule") as GameObject);
        head = GameObject.Find("HeadModel");
        captureManger = GameObject.Find("TimeWarpingCamera").GetComponent<CaptureManager>();
        timeWarpingCamera = GameObject.Find("TimeWarpingCamera").GetComponent<Camera>();
        anchor = GameObject.Find("Anchor");
        leftTexture = GameObject.Find("LeftTargetTexture");
        rightTexture = GameObject.Find("RightTargetTexture");

        PlayerSettings.defaultScreenWidth = captureWidth;
        PlayerSettings.defaultScreenHeight = captureHeight;

        SetGameviewSize(gameviewSizeTypes["StandAlone"]);

        captureModule.transform.position = new Vector3(100, 0, 0);
        leftTexture.transform.position = new Vector3(99.5f, 0, 0.866f);
        rightTexture.transform.position = new Vector3(100.5f, 0, 0.866f);
    }

    public void SetGameviewSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var gvWnd = EditorWindow.GetWindow(gvWndType);
        selectedSizeIndexProp.SetValue(gvWnd, index, null);
    }

    private void SetGameviewScale(float scale)
    {
        System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
        System.Type type = assembly.GetType("UnityEditor.GameView");
        UnityEditor.EditorWindow v = UnityEditor.EditorWindow.GetWindow(type);

        var areaField = type.GetField("m_ZoomArea", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var areaObj = areaField.GetValue(v);

        var scaleField = areaObj.GetType().GetField("m_Scale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        scaleField.SetValue(areaObj, new Vector2(scale, scale));
    }

    private void DefaltPathSetting()
    {
        timeWarp_predict_path = "CaptureFolder/UseTimeWarp & UsePredictedData";
        timeWarp_nonPredict_path = "CaptureFolder/UseTimeWarp & UseNotPredictedData";
        nonTimeWarp_predict_path = "CaptureFolder/UseNotTimeWarp & UsePredictedData";
        nonTimeWarp_nonPredict_path = "CaptureFolder/UseNotTimeWarp & UseNotPredictedData";
    }
}
