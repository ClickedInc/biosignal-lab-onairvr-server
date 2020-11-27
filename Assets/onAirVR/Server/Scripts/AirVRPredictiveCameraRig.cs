using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;

[RequireComponent(typeof(AirVRStereoCameraRig))]
[RequireComponent(typeof(OCSVRWorksCameraRig))]
public class AirVRPredictiveCameraRig : MonoBehaviour, AirVRServer.ProfilerEventHandler {
    private NetMQ.Msg _msg;
    private PushSocket _zmqReportEndpoint;

    private AirVRStereoCameraRig _cameraRig;
    private OCSVRWorksCameraRig _foveatedRenderer;

    [SerializeField] private bool _bypassPrediction = false;

    private void Awake() {
        _msg = new NetMQ.Msg();

        AirVRServer.profilerEventHandler = this;

        _cameraRig = GetComponent<AirVRStereoCameraRig>();
        _foveatedRenderer = GetComponent<OCSVRWorksCameraRig>();

        _cameraRig.bypassPrediction = _bypassPrediction;
    }

    private void Start() {
        //_foveatedRenderer.OnUpdateFoveationPattern += updateFoveationPattern;
        //_foveatedRenderer.OnUpdateGazeLocation += updateGazeLocation;
    }

    private void OnDestroy() {
        //_foveatedRenderer.OnUpdateFoveationPattern -= updateFoveationPattern;
        //_foveatedRenderer.OnUpdateGazeLocation -= updateGazeLocation;
    }

    // handle OCSVRWorksCameraRig events
    private void updateFoveationPattern(OCSVRWorksCameraRig cameraRig) {
        var config = _cameraRig.GetConfig();
        if (config == null) { return; }

        var originalProjHeight = config.cameraProjection[1] - config.cameraProjection[3];
        var overfillProj = _cameraRig.predictedMotionProvider.projection;
        var overfillWidth = overfillProj.z - overfillProj.x;
        var overfillHeight = overfillProj.y - overfillProj.w;
        var overfillAspect = (overfillProj.z - overfillProj.x) / (overfillProj.y - overfillProj.w);

        var encodingSize = config.GetEncodingProjectionSize();
        var videoAspect = config.videoWidth / 2.0f / config.videoHeight;

        cameraRig.UpdateFoveationPattern(originalProjHeight / (overfillAspect >= 1.0f ? overfillHeight : overfillWidth), encodingSize.height / encodingSize.width * videoAspect);
    }

    private void updateGazeLocation(OCSVRWorksCameraRig cameraRig) {
        var config = _cameraRig.GetConfig();
        if (config == null) { return; }

        var leftProj = _cameraRig.predictedMotionProvider.projection;
        var rightProj = new Vector4 {
            x = leftProj.x - (config.cameraProjection[0] + config.cameraProjection[2]),
            y = leftProj.y,
            z = leftProj.z - (config.cameraProjection[0] + config.cameraProjection[2]),
            w = leftProj.w
        };
        var width = leftProj.z - leftProj.x;
        var height = leftProj.y - leftProj.w;
        var scale = width / height >= 1.0f ? height : width;

        var leftGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -(leftProj.x + leftProj.z) / 2 / scale,
            y = -(leftProj.y + leftProj.w) / 2 / scale
        };
        var rightGaze = new OCSVRWorksCameraRig.GazeLocation {
            x = -(rightProj.x + rightProj.z) / 2 / scale,
            y = -(rightProj.y + rightProj.w) / 2 / scale
        };

        cameraRig.UpdateGazeLocation(leftGaze, rightGaze);
    }

    // implements AirVRServer.ProfilerEventHandler
    public void AirVRProfilerEnabled(int clientHandle, string reportEndpoint) {
        if (_bypassPrediction) { return; }
        
        Debug.Assert(_zmqReportEndpoint == null);
        if (string.IsNullOrEmpty(reportEndpoint)) {
            return;
        }

        string endpoint = convertZmqEndpoint(reportEndpoint);
        if (string.IsNullOrEmpty(endpoint)) {
            return;
        }

        _zmqReportEndpoint = new PushSocket();
        _zmqReportEndpoint.Connect(endpoint);
    }

    public void AirVRProfileDataReceived(int clientHandle, string json) {
        // do nothing
    }

    public void AirVRProfileDataReceived(int clientHandle, byte[] cbor) {
        if (_bypassPrediction) { return; }

        Debug.Assert(_zmqReportEndpoint != null);

        _msg.InitPool(cbor.Length);
        Array.Copy(cbor, _msg.Data, _msg.Data.Length);

        _zmqReportEndpoint.TrySend(ref _msg, TimeSpan.Zero, false);
    }

    public void AirVRProfilerDisabled(int clientHandle) {
        if (_bypassPrediction) { return; }

        _zmqReportEndpoint.Close();
        _zmqReportEndpoint.Dispose();
        _zmqReportEndpoint = null;
    }

    private string convertZmqEndpoint(string endpoint) {
        string[] tokens = endpoint.Split(':');
        if (tokens.Length == 3 && tokens[0].Equals("amqp")) {
            tokens[0] = "tcp";
        }
        else {
            return null;
        }

        return tokens[0] + ":" + tokens[1] + ":" + tokens[2];
    }
}
