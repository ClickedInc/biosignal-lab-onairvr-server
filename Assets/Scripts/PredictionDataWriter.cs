using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private List<Data> datas = new List<Data>();

    private void Awake()
    {
        AirVRPredictedHeadTrackerInputDevice.eventHandler = this;
    }

    public void OnReceivedPredictionServerData(double timeStamp, Quaternion predictedOrientation, float predictionTime, Quaternion originalOrientation)
    {
        datas.Add(new Data(timeStamp, predictedOrientation, predictionTime, originalOrientation));
    }

    void OnDestroy () {
        using (var writer = new CsvFileWriter(path))
        {
            List<string> columns = new List<string>() { "time_stamp", "predicted_ori", "prediction_time", "original_ori" };// making Index Row
            writer.WriteRow(columns);
            columns.Clear();

            foreach (var data in datas)
            {
                columns.Add(data.timeStamp.ToString()); // Name
                columns.Add(data.predicted.ToString()); // Level
                columns.Add(data.predictionTime.ToString()); // Hp
                columns.Add(data.original.ToString()); // Int
                writer.WriteRow(columns);
                columns.Clear();
            }
        }
	}
}
