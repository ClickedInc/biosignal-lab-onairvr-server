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

    private OCSVRWorksCameraRig _foveatedRenderer;

    public AirVRCameraRig cameraRig { get; private set; }
    public AirXRPredictedMotionProvider predictedMotionProvider { get; private set; }
    public AirXRGameEventEmitter gameEventEmitter { get; private set; }
    public MPPLiveMotionDataProvider liveMotionProvider { get; private set; }

    public bool bypassPrediction => AirXRServer.settings.BypassPrediction;

    private void Awake() {
        _msg = new NetMQ.Msg();

        cameraRig = GetComponent<AirVRCameraRig>();
        _foveatedRenderer = GetComponent<OCSVRWorksCameraRig>();

        liveMotionProvider = new MPPLiveMotionDataProvider();
        predictedMotionProvider = new AirXRPredictedMotionProvider(this, liveMotionProvider);
        gameEventEmitter = new AirXRGameEventEmitter(cameraRig);
    }

    private void Start() {
        AirXRCameraRigManager.managerOnCurrentScene.predictionEventHandler = this;

        _foveatedRenderer.OnUpdateFoveationPattern += updateFoveationPattern;
        _foveatedRenderer.OnUpdateGazeLocation += updateGazeLocation;

        _foveatedRenderer.enabled = AirXRServer.settings.FoveatedRenderPriority == AirXRServerSettings.FoveatedRenderingPriority.StreamingFirst;
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
        var overfillProj = predictedMotionProvider.leftProjection;
        var overfillAspect = overfillProj.width / overfillProj.height;

        cameraRig.UpdateFoveationPatternProps(predictedMotionProvider.foveationInnerRadius,
                                              predictedMotionProvider.foveationMiddleRadius,
                                              1 / (overfillAspect >= 1.0f ? overfillProj.height : overfillProj.width));
    }

    private void updateGazeLocation(OCSVRWorksCameraRig cameraRig) {
        var config = this.cameraRig.GetConfig();
        if (config == null) { return; }

        var leftProj = predictedMotionProvider.leftProjection;
        var rightProj = predictedMotionProvider.rightProjection;

        var leftGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -leftProj.center.x / leftProj.width,
            y = -leftProj.center.y / leftProj.height
        };
        var rightGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -rightProj.center.x / rightProj.width,
            y = -rightProj.center.y / rightProj.height
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
