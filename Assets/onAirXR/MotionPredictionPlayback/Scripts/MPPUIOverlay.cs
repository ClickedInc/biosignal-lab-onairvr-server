using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class MPPUIOverlay : MonoBehaviour {
    private MotionPredictionPlayback _owner;
    private Image _panelPlayback;
    private Image _panelLive;
    private Text _textInputMotionData;
    private Text _textCaptureOutputPath;
    private Button _buttonPlay;
    private Text _labelButtonPlay;
    private Button _buttonCapture;
    private Text _labelButtonCapture;
    private Dropdown _playbackMode;
    private Dropdown _liveMode;

    public void NotifyMotionDataProviderLoaded(MPPMotionDataProvider motionData) {
        if (motionData is MPPMotionDataFile motionDataFile) {
            _panelPlayback.gameObject.SetActive(true);
            _panelLive.gameObject.SetActive(false);

            _textInputMotionData.text = Path.GetFileName(motionDataFile.filepath);

            var options = new List<string> {
                "Predict_NoTimeWarp",
                "Predict_TimeWarp"
            };

            if (motionDataFile.type == MPPMotionDataFile.Type.Raw) {
                options.Add("NotPredict_NoTimeWarp");
                options.Add("NotPredict_TimeWarp");
            }

            _playbackMode.ClearOptions();
            _playbackMode.AddOptions(options);

            _playbackMode.SetValueWithoutNotify((int)_owner.playbackMode);
        }
        else {
            _panelLive.gameObject.SetActive(true);
            _panelPlayback.gameObject.SetActive(false);

            _playbackMode.SetValueWithoutNotify((int)_owner.playbackMode);
        }
    }

    public void NotifyCaptureOutputPathSet(string path) {
        _textCaptureOutputPath.text = Path.GetFileName(path);
    }

    private void Awake() {
        _owner = GetComponentInParent<MotionPredictionPlayback>();

        _panelPlayback = transform.Find("Playback").GetComponent<Image>();
        _textInputMotionData = _panelPlayback.transform.Find("InputMotionData/Value").GetComponent<Text>();
        _textCaptureOutputPath = _panelPlayback.transform.Find("CaptureOutputPath/Value").GetComponent<Text>();
        _buttonPlay = _panelPlayback.transform.Find("Play").GetComponent<Button>();
        _labelButtonPlay = _buttonPlay.transform.Find("Text").GetComponent<Text>();
        _buttonCapture = _panelPlayback.transform.Find("Capture").GetComponent<Button>();
        _labelButtonCapture = _buttonCapture.transform.Find("Text").GetComponent<Text>();
        _playbackMode = _panelPlayback.transform.Find("Mode/Dropdown").GetComponent<Dropdown>();

        _panelLive = transform.Find("Live").GetComponent<Image>();
        _liveMode = _panelLive.transform.Find("Mode/Dropdown").GetComponent<Dropdown>();

        _liveMode.ClearOptions();
        _liveMode.AddOptions(new List<string> {
            "Predict_NoTimeWarp",
            "Predict_TimeWarp",
            "NotPredict_NoTimeWarp",
            "NotPredict_TimeWarp"
        });

        _liveMode.SetValueWithoutNotify((int)_owner.playbackMode);
    }

    private void Update() {
        updateElements();
    }

    private void updateElements() {
        _buttonPlay.interactable = _owner.playbackState != MotionPredictionPlayback.PlaybackState.Capturing;
        _labelButtonPlay.text = _owner.playbackState == MotionPredictionPlayback.PlaybackState.Playing ? "Stop" : "Play";

        _buttonCapture.interactable = _owner.playbackState != MotionPredictionPlayback.PlaybackState.Playing;
        _labelButtonCapture.text = _owner.playbackState == MotionPredictionPlayback.PlaybackState.Capturing ? "Stop" : "Capture";
    }

    // handle ui events
    public void OnBrowseInputMotionDataFile() {
        var lastOpenedFile = PlayerPrefs.GetString(MotionPredictionPlayback.PrefKeyInputMotionDataFile, "Assets/onAirXR/MotionPredictionPlayback/sample.csv");
        var path = EditorUtility.OpenFilePanel("Select a motion data file...", Path.GetDirectoryName(lastOpenedFile), "csv");
        if (string.IsNullOrEmpty(path)) { return; }

        _owner.OnLoadInputMotionDataFile(path);
    }

    public void OnBrowseCaptureOutputPath() {
        var lastOpenedPath = PlayerPrefs.GetString(MotionPredictionPlayback.PrefKeyCaptureOutputPath, ".");
        var path = EditorUtility.OpenFolderPanel("Select a folder to save capture output...", Path.GetDirectoryName(lastOpenedPath), "");
        if (string.IsNullOrEmpty(path)) { return; }

        _owner.OnSetCaptureOutputPath(path);
    }

    public void OnChangePlaybackMode(int mode) {
        _owner.OnSetPlaybackMode((MotionPredictionPlayback.PlaybackMode)mode);
    }

    public void OnChangePlaybackRangeFrom(string value) {
        _owner.OnSetPlaybackRangeFrom(int.TryParse(value, out int val) && val > 0 ? val : 0);
    }

    public void OnChangePlaybackRangeTo(string value) {
        _owner.OnSetPlaybackRangeTo(int.TryParse(value, out int val) && val > 0 ? val : int.MaxValue);
    }

    public void OnPlayButtonClicked() {
        _owner.OnTogglePlay();
    }

    public void OnCaptureButtonClicked() {
        _owner.OnToggleCapture();
    }
}
