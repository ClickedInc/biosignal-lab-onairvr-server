using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PredictionDataWriter : MonoBehaviour, AirVRPredictedHeadTrackerInputDevice.EventHandler
{
    public struct Data
    {
        public double timeStamp;
        public Quaternion predicted;
        public float predictionTime;
        public Quaternion original;

        public Data(double timeStamp , Quaternion predicted , float predictionTime , Quaternion original)
        {
            this.timeStamp = timeStamp;
            this.predicted = predicted;
            this.predictionTime = predictionTime;
            this.original = original;
        }
    }

    public string path;

    private bool isRecording;

    private List<Data> datas = new List<Data>();

    private void Awake()
    {
        AirVRPredictedHeadTrackerInputDevice.eventHandler = this;
    }

    private void OnGUI()
    {
        if(!isRecording)
        {
            if (GUI.Button(new Rect(new Vector2(Screen.width  - 300 , Screen.height - 300), new Vector2(200, 200)) ,"Start Recording"))
            {
                datas.Clear();

                isRecording = true;
                Debug.Log("Start Recording");
            }
        }
        else
        {
            if (GUI.Button(new Rect(new Vector2(Screen.width - 300, Screen.height - 300), new Vector2(200, 200)), "Save"))
            {
                using (var writer = new CsvFileWriter(path))
                {
                    List<string> columns = new List<string>() { "time_stamp", "predicted_ori", "prediction_time", "original_ori" };// making Index Row
                    writer.WriteRow(columns);
                    columns.Clear();

                    foreach (var data in datas)
                    {
                        columns.Add(data.timeStamp.ToString());
                        columns.Add(data.predicted.ToString("F7"));
                        columns.Add(data.predictionTime.ToString());
                        columns.Add(data.original.ToString("F7"));
                        writer.WriteRow(columns);
                        columns.Clear();
                    }
                }

                isRecording = false;
                Debug.Log("Saved Data");
            }
        }
    }

    public void OnReceivedPredictionServerData(double timeStamp, Quaternion predictedOrientation, float predictionTime, Quaternion originalOrientation)
    {
        if (!isRecording)
            return;

        datas.Add(new Data(timeStamp, predictedOrientation, predictionTime, originalOrientation));
    }
}
