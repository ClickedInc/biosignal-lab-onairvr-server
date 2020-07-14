using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;
using HTC.UnityPlugin.FoveatedRendering;

[RequireComponent(typeof(AirVRStereoCameraRig))]
public class AirVRPredictiveCameraRig : MonoBehaviour, AirVRServer.ProfilerEventHandler {
    private NetMQ.Msg _msg;
    private PushSocket _zmqReportEndpoint;

    private AirVRStereoCameraRig _cameraRig;
    private ViveFoveatedRenderer _foveatedRenderer;

    private void Awake() {
        _msg = new NetMQ.Msg();

        AirVRServer.profilerEventHandler = this;

        _cameraRig = GetComponent<AirVRStereoCameraRig>();
        _foveatedRenderer = GetComponent<ViveFoveatedRenderer>();
    }

    private void LateUpdate() {
        updateFoveatedRendering();
    }

    private void updateFoveatedRendering() {
        if (_foveatedRenderer.initialized == false) { return; }

        var config = _cameraRig.GetConfig();
        if (config == null) { return; }

        Vector4 leftProj = _cameraRig.predictedMotionProvider.projection;
        Vector4 rightProj = new Vector4 {
            x = leftProj.x - (config.cameraProjection[0] + config.cameraProjection[2]),
            y = leftProj.y,
            z = leftProj.z - (config.cameraProjection[0] + config.cameraProjection[2]),
            w = leftProj.w
        };

        //var leftGaze = new Vector3((leftProj.x + leftProj.z) / (leftProj.z - leftProj.x), 
        //                           -(leftProj.y + leftProj.w) / (leftProj.y - leftProj.w), 
        //                           1.0f);
        //var rightGaze = new Vector3((rightProj.x + rightProj.z) / (rightProj.z - rightProj.x),
        //                            -(rightProj.y + rightProj.w) / (rightProj.y - rightProj.w),
        //                            1.0f);

        var leftGaze = new Vector3((leftProj.x + leftProj.z - (config.cameraProjection[0] + config.cameraProjection[2])) / (leftProj.z - leftProj.x),
                                   -(leftProj.y + leftProj.w) / (leftProj.y - leftProj.w) * 1.35f,
                                   1.0f);

        _foveatedRenderer.UpdateGazeDirection(leftGaze, leftGaze);
        _foveatedRenderer.UpdateScale((leftProj.z - leftProj.x) / 2, 1.35f);
    }

    // implements AirVRServer.ProfilerEventHandler
    public void AirVRProfilerEnabled(int clientHandle, string reportEndpoint) {
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
        Debug.Assert(_zmqReportEndpoint != null);

        _msg.InitPool(cbor.Length);
        Array.Copy(cbor, _msg.Data, _msg.Data.Length);

        _zmqReportEndpoint.TrySend(ref _msg, TimeSpan.Zero, false);
    }

    public void AirVRProfilerDisabled(int clientHandle) {
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
