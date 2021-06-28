using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;

[RequireComponent(typeof(AirVRCameraRig))]
[RequireComponent(typeof(OCSVRWorksCameraRig))]
public class AirXRPredictiveCameraRig : MonoBehaviour, AirXRCameraRigManager.PredictionEventHandler {
    private NetMQ.Msg _msg;
    private PushSocket _zmqReportEndpoint;

    private AirVRCameraRig _cameraRig;
    private OCSVRWorksCameraRig _foveatedRenderer;

    public AirXRPredictedMotionProvider predictedMotionProvider { get; private set; }
    public AirXRGameEventEmitter gameEventEmitter { get; private set; }

    public bool bypassPrediction => AirXRServer.settings.BypassPrediction;

    private void Awake() {
        _msg = new NetMQ.Msg();

        _cameraRig = GetComponent<AirVRCameraRig>();
        _foveatedRenderer = GetComponent<OCSVRWorksCameraRig>();

        predictedMotionProvider = new AirXRPredictedMotionProvider(this);
        gameEventEmitter = new AirXRGameEventEmitter(_cameraRig);
    }

    private void Start() {
        AirXRCameraRigManager.managerOnCurrentScene.predictionEventHandler = this;

        _foveatedRenderer.OnUpdateFoveationPattern += updateFoveationPattern;
        _foveatedRenderer.OnUpdateGazeLocation += updateGazeLocation;
    }

    private void Update() {
        predictedMotionProvider.Update();
    }

    private void OnDestroy() {
        _foveatedRenderer.OnUpdateFoveationPattern -= updateFoveationPattern;
        _foveatedRenderer.OnUpdateGazeLocation -= updateGazeLocation;

        if (_zmqReportEndpoint != null) {
            try {
                _zmqReportEndpoint.Close();
                _zmqReportEndpoint.Dispose();
                _zmqReportEndpoint = null;
            }
            catch (Exception) { }
        }

        predictedMotionProvider.Close();
    }

    // handle OCSVRWorksCameraRig events
    private void updateFoveationPattern(OCSVRWorksCameraRig cameraRig) {
        var config = _cameraRig.GetConfig();
        if (config == null) { return; }

        var originalProjHeight = config.cameraProjection[1] - config.cameraProjection[3];
        var overfillProj = predictedMotionProvider.projection;
        var overfillWidth = overfillProj.xMax - overfillProj.xMin;
        var overfillHeight = overfillProj.yMax - overfillProj.yMin;
        var overfillAspect = overfillWidth / overfillHeight;

        var encodingSize = config.GetEncodingProjectionSize();
        var videoAspect = config.videoWidth / 2.0f / config.videoHeight;

        cameraRig.UpdateFoveationPattern(originalProjHeight / (overfillAspect >= 1.0f ? overfillHeight : overfillWidth), encodingSize.height / encodingSize.width * videoAspect);
    }

    private void updateGazeLocation(OCSVRWorksCameraRig cameraRig) {
        var config = _cameraRig.GetConfig();
        if (config == null) { return; }

        var leftProj = predictedMotionProvider.projection;
        var rightProj = Rect.MinMaxRect(
            leftProj.xMin - (config.cameraProjection[0] + config.cameraProjection[2]),
            leftProj.yMin,
            leftProj.xMax - (config.cameraProjection[0] + config.cameraProjection[2]),
            leftProj.yMax
        );
        var scale = leftProj.width / leftProj.height >= 1.0f ? leftProj.height : leftProj.width;

        var leftGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -leftProj.center.x / scale,
            y = -leftProj.center.y / scale
        };
        var rightGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -rightProj.center.x / scale,
            y = -rightProj.center.y / scale
        };

        cameraRig.UpdateGazeLocation(leftGaze, rightGaze);
    }

    // implements AirXRCameraRigManager.PredictionEventHandler
    void AirXRCameraRigManager.PredictionEventHandler.OnStartPrediction(AirXRCameraRig cameraRig, string profileReportEndpoint, string motionOutputEndpoint) {
        if (bypassPrediction || _zmqReportEndpoint != null) { return; }

        string endpoint = convertZmqEndpoint(profileReportEndpoint);
        if (string.IsNullOrEmpty(endpoint) == false) {
            _zmqReportEndpoint = new PushSocket();
            _zmqReportEndpoint.Connect(endpoint);
        }

        predictedMotionProvider.Connect(convertZmqEndpoint(motionOutputEndpoint));
    }

    void AirXRCameraRigManager.PredictionEventHandler.OnProfileDataReceived(AirXRCameraRig cameraRig, byte[] cbor) {
        if (bypassPrediction || _zmqReportEndpoint == null) { return; }

        _msg.InitPool(cbor.Length);
        Array.Copy(cbor, _msg.Data, _msg.Data.Length);

        _zmqReportEndpoint.TrySend(ref _msg, TimeSpan.Zero, false);
    }
    
    void AirXRCameraRigManager.PredictionEventHandler.OnStopPrediction(AirXRCameraRig cameraRig) {
        if (_zmqReportEndpoint == null) { return; }

        _zmqReportEndpoint.Close();
        _zmqReportEndpoint.Dispose();
        _zmqReportEndpoint = null;

        predictedMotionProvider.Close();
    }

    private string convertZmqEndpoint(string endpoint) {
        string[] tokens = endpoint.Split(':');
        if (tokens.Length == 3 && tokens[0].Equals("amqp")) {
            tokens[0] = "tcp";
        }
        else {
            return endpoint;
        }

        return tokens[0] + ":" + tokens[1] + ":" + tokens[2];
    }
}
