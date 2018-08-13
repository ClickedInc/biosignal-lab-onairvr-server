using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ.Sockets;

public class AirVRServerProfiler : MonoBehaviour, AirVRServer.ProfilerEventHandler {
    private NetMQ.Msg _msg;
    private PushSocket _zmqReportEndpoint;

    private void Awake() {
        _msg = new NetMQ.Msg();

        AirVRServer.profilerEventHandler = this;
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
