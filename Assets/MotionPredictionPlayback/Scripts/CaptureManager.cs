using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CaptureManager : MonoBehaviour {
    public static int captureNum;

    public static string ScreenShotName(double time,string message,string path,int num)
    {
        string filename = string.Format("{0}_{1:#.000}_{2}.png",
                                        num,
                                        time / 705600000.0,     // flick to second
                                        message
                                        );
        return Path.Combine(path, filename);
    }

    private MotionPredictionPlaybackCamera _playbackCamera;
    private string _path;
    private RenderTexture _captureTargetTexture;

    public void Init(MotionPredictionPlaybackCamera playbackCamera) {
        _playbackCamera = playbackCamera;
        _playbackCamera.PlaybackStateChanged += playbackStateChanged;

        string inputMotionData = PlayerPrefs.GetString(PrefKeyInputMotionDataFile, "Assets/MotionPredictionPlayback/sample.csv");
        string captureOutputPath = PlayerPrefs.GetString(PrefKeyCaptureOutputPath, Path.Combine(Path.GetDirectoryName(Application.dataPath), "CaptureOutput"));

        _playbackCamera.SetInputMotionDataFile(inputMotionData);
        _playbackCamera.SetCaptureOutputPath(captureOutputPath);

        _textInputMotionData.text = Path.GetFileName(inputMotionData);
        _textCaptureOutputPath.text = Path.GetFileName(captureOutputPath);
    }

    public void Configure(string path, RenderTexture captureTargetTexture) {
        if (string.IsNullOrEmpty(path)) {
            return;
        }
        _path = path;
        _captureTargetTexture = captureTargetTexture;

        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }
    }

    public void CaptureScreenshot(double time, string message, int num) {
        if (string.IsNullOrEmpty(_path) || string.IsNullOrEmpty(message)) {
            return;
        }

        string path = Path.Combine(_path, message);
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }

        RenderTexture old = RenderTexture.active;

        RenderTexture.active = _captureTargetTexture;
        Texture2D image = new Texture2D(_captureTargetTexture.width, _captureTargetTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0);
        image.Apply();

        byte[] imageBytes = image.EncodeToPNG();
        System.IO.File.WriteAllBytes(ScreenShotName(time, message, path, num), imageBytes);

        Destroy(image);
        RenderTexture.active = old;
    }

    // handle UI interactions
    private const string PrefKeyInputMotionDataFile = "kr.co.clicked.biosignal.motiondatafile";
    private const string PrefKeyCaptureOutputPath = "kr.co.clicked.biosignal.captureoutputpath";

    private Text _textInputMotionData;
    private Text _textCaptureOutputPath;
    private Button _buttonPlay;
    private Text _textButtonPlay;
    private Button _buttonCapture;
    private Text _textButtonCapture;
    private Dropdown _playbackMode;

    private void Awake() {
        _textInputMotionData = transform.Find("Canvas/Panel/InputMotionData/Value").GetComponent<Text>();
        _textCaptureOutputPath = transform.Find("Canvas/Panel/CaptureOutputPath/Value").GetComponent<Text>();
        _buttonPlay = transform.Find("Canvas/Panel/Play").GetComponent<Button>();
        _textButtonPlay = transform.Find("Canvas/Panel/Play/Text").GetComponent<Text>();
        _buttonCapture = transform.Find("Canvas/Panel/Capture").GetComponent<Button>();
        _textButtonCapture = transform.Find("Canvas/Panel/Capture/Text").GetComponent<Text>();
        _playbackMode = transform.Find("Canvas/Panel/PlaybackMode/Dropdown").GetComponent<Dropdown>();
    }

    public void SetPlaybackMode(bool isMotionData)
    {
        List<string> option = new List<string>();
        if (isMotionData)
        {
            option = new List<string>() {
                "Predict_NoTimeWarp",
                "Predict_TimeWarp",
                "NotPredict_NoTimeWarp",
                "NotPredict_TimeWarp"
            };
        }
        else
        {
            option = new List<string>() {
                "Predict_NoTimeWarp",
                "Predict_TimeWarp"
            };
        }

        _playbackMode.ClearOptions();
        _playbackMode.AddOptions(option);
    }

    public void BrowseInputMotionDataFile() {
        string lastOpenedFile = PlayerPrefs.GetString(PrefKeyInputMotionDataFile, "Assets/MotionPredictionPlayback/sample.csv");
        string inputMotionDataFile = EditorUtility.OpenFilePanel("Select a motion data file...", Path.GetDirectoryName(lastOpenedFile), "csv");
        if (string.IsNullOrEmpty(inputMotionDataFile)) {
            return;
        }

        _playbackCamera.SetInputMotionDataFile(inputMotionDataFile);

        _textInputMotionData.text = Path.GetFileName(inputMotionDataFile);
        PlayerPrefs.SetString(PrefKeyInputMotionDataFile, inputMotionDataFile);
    }

    public void BrowseCaptureOutputPath() {
        string lastOpenedPath = PlayerPrefs.GetString(PrefKeyCaptureOutputPath, ".");
        string captureOutputPath = EditorUtility.OpenFolderPanel("Select a folder to save capture output...", Path.GetDirectoryName(lastOpenedPath), "");
        if (string.IsNullOrEmpty(captureOutputPath)) {
            return;
        }

        _playbackCamera.SetCaptureOutputPath(captureOutputPath);

        _textCaptureOutputPath.text = Path.GetFileName(captureOutputPath);
        PlayerPrefs.SetString(PrefKeyCaptureOutputPath, captureOutputPath);
    }

    public void SetPlaybackMode(int mode) {
        _playbackCamera.SetPlaybackMode((MotionPredictionPlaybackCamera.PlaybackMode)mode);
    }

    public void SetPlaybackRangeFrom(string value) {
        int parsed = 0;
        if (int.TryParse(value, out parsed)) {
            _playbackCamera.SetPlaybackRangeFrom(parsed > 0 ? parsed : 0);
        }
        else {
            _playbackCamera.SetPlaybackRangeFrom(0);
        }
    }

    public void SetPlaybackRangeTo(string value) {
        int parsed = int.MaxValue;
        if (int.TryParse(value, out parsed)) {
            _playbackCamera.SetPlaybackRangeTo(parsed > 0 ? parsed : int.MaxValue);
        }
        else {
            _playbackCamera.SetPlaybackRangeTo(int.MaxValue);
        }
    }

    public void PlayButtonClicked() {
        _playbackCamera.TogglePlay();
    }

    public void CaptureButtonClicked() {
        _playbackCamera.ToggleCapture();
    }

    private void playbackStateChanged(MotionPredictionPlaybackCamera sender, MotionPredictionPlaybackCamera.PlaybackState state) {
        _textButtonPlay.text = state == MotionPredictionPlaybackCamera.PlaybackState.Playing ? "Stop" : "Play";
        _textButtonCapture.text = state == MotionPredictionPlaybackCamera.PlaybackState.Capturing ? "Stop" : "Capture";
        _buttonCapture.interactable = state != MotionPredictionPlaybackCamera.PlaybackState.Playing;
        _buttonPlay.interactable = state != MotionPredictionPlaybackCamera.PlaybackState.Capturing;
    }
}
