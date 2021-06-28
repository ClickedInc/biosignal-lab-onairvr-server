/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class AirXRCameraRigManager : MonoBehaviour {
    public interface EventHandler {
        void AirXRCameraRigWillBeBound(int clientHandle, AirXRClientConfig config, List<AirXRCameraRig> availables, out AirXRCameraRig selected);
        void AirXRCameraRigActivated(AirXRCameraRig cameraRig);
        void AirXRCameraRigDeactivated(AirXRCameraRig cameraRig);
        void AirXRCameraRigHasBeenUnbound(AirXRCameraRig cameraRig);
        void AirXRCameraRigUserDataReceived(AirXRCameraRig cameraRig, byte[] userData);
    }

    public interface PredictionEventHandler {
        void OnStartPrediction(AirXRCameraRig cameraRig, string profileReportEndpoint, string motionOutputEndpoint);
        void OnProfileDataReceived(AirXRCameraRig cameraRig, byte[] cbor);
        void OnStopPrediction(AirXRCameraRig cameraRig);
    }

    private static AirXRCameraRigManager _instanceOnCurrentScene;

    internal static void LoadOncePerScene() {
        if (_instanceOnCurrentScene == null) {
            _instanceOnCurrentScene = FindObjectOfType<AirXRCameraRigManager>();
            if (_instanceOnCurrentScene == null) {
                var go = new GameObject("AirXRCameraRigManager");
                go.AddComponent<AirXRCameraRigManager>();
                Assert.IsTrue(_instanceOnCurrentScene != null);
            }
        }
    }

    internal static void UnloadOncePerScene() {
        if (_instanceOnCurrentScene != null) {
            _instanceOnCurrentScene = null;
        }
    }

    internal static bool CheckIfExistManagerOnCurrentScene() {
        return _instanceOnCurrentScene != null;
    }

    public static AirXRCameraRigManager managerOnCurrentScene {
        get {
            LoadOncePerScene();
            return _instanceOnCurrentScene;
        }
    }

    private AirXRCameraRigList _cameraRigList;

    private AirXRCameraRig notifyCameraRigWillBeBound(int playerID) {
        var config = AirXRClientConfig.Get(playerID);

        var cameraRigs = new List<AirXRCameraRig>();
        _cameraRigList.GetAvailableCameraRigs(config.type, cameraRigs);

        AirXRCameraRig selected = null;
        if (Delegate != null) {
            Delegate.AirXRCameraRigWillBeBound(playerID, config, cameraRigs, out selected);
            AirXRClientConfig.Set(playerID, config);
        }
        else if (cameraRigs.Count > 0) {
            selected = cameraRigs[0];
        }
        return selected;
    }

    private void unregisterAllCameraRigs(bool applicationQuit) {
        var cameraRigs = new List<AirXRCameraRig>();
        _cameraRigList.GetAllRetainedCameraRigs(cameraRigs);

        foreach (var cameraRig in cameraRigs) {
            UnregisterCameraRig(cameraRig, applicationQuit);
        }
    }

    private void updateApplicationTargetFrameRate() {
        if (AirXRServer.settings.AdaptiveFrameRate == false) { return; }

        var cameraRigs = new List<AirXRCameraRig>();
        _cameraRigList.GetAllRetainedCameraRigs(cameraRigs);

        float maxCameraRigVideoFrameRate = 0.0f;
        foreach (var cameraRig in cameraRigs) {
            if (cameraRig.isStreaming) {
                maxCameraRigVideoFrameRate = Mathf.Max(maxCameraRigVideoFrameRate, cameraRig.GetConfig().framerate);
            } 
        }

        Application.targetFrameRate = Mathf.Max(Mathf.RoundToInt(maxCameraRigVideoFrameRate), AirXRServer.settings.MinFrameRate);
    }

    void Awake() {
        if (_instanceOnCurrentScene != null) {
            new UnityException("[onAirXR] ERROR: There must exist only one AirXRCameraRigManager at a time.");
        }
        _instanceOnCurrentScene = this;

        _cameraRigList = new AirXRCameraRigList();
        eventDispatcher = new AirXRServerEventDispatcher();
    }

    void Start() {
        var streams = new List<AirXRServerStreamHandover.Streams>();
        AirXRServerStreamHandover.TakeAllStreamsHandedOverInPrevScene(streams);
        foreach (var item in streams) {
            var selected = notifyCameraRigWillBeBound(item.playerID);
            if (selected != null) {
                _cameraRigList.RetainCameraRig(selected);
                selected.BindPlayer(item.playerID, item.mediaStream, item.inputStream);

                if (selected.isStreaming && Delegate != null) {
                    Delegate.AirXRCameraRigActivated(selected);
                }
            }
            else {
                AXRServerPlugin.Disconnect(item.playerID);
            }
        }

        updateApplicationTargetFrameRate();

        eventDispatcher.MessageReceived += onAirXRMessageReceived;
    }

    void Update() {
        AXRServerPlugin.Update();
        eventDispatcher.DispatchEvent();

        var cameraRigs = new List<AirXRCameraRig>();
        _cameraRigList.GetAllCameraRigs(cameraRigs);
        foreach (var cameraRig in cameraRigs) {
            cameraRig.OnUpdate();
        }
    }

    void LateUpdate() {
        var cameraRigs = new List<AirXRCameraRig>();
        _cameraRigList.GetAllCameraRigs(cameraRigs);
        foreach (var cameraRig in cameraRigs) {
            cameraRig.OnLateUpdate();
        }
    }

    void OnApplicationQuit() {
        unregisterAllCameraRigs(true);
    }

    void OnDestroy() {
        unregisterAllCameraRigs(false);

        eventDispatcher.MessageReceived -= onAirXRMessageReceived;
        UnloadOncePerScene();
    }

    internal AirXRServerEventDispatcher eventDispatcher { get; private set; }

    internal void RegisterCameraRig(AirXRCameraRig cameraRig) {
        _cameraRigList.AddUnboundCameraRig(cameraRig);
    }

    internal void UnregisterCameraRig(AirXRCameraRig cameraRig, bool applicationQuit = false) {
        _cameraRigList.RemoveCameraRig(cameraRig);

        if (applicationQuit == false && cameraRig.isBoundToClient) {
            cameraRig.PreHandOverStreams();
            AirXRServerStreamHandover.HandOverStreamsForNextScene(new AirXRServerStreamHandover.Streams(cameraRig.playerID, cameraRig.mediaStream, cameraRig.inputStream));

            if (Delegate != null) {
                if (cameraRig.isStreaming) {
                    Delegate.AirXRCameraRigDeactivated(cameraRig);
                }
                Delegate.AirXRCameraRigHasBeenUnbound(cameraRig);
            }
            cameraRig.PostHandOverStreams();
        }
    }

    public EventHandler Delegate { private get; set; }
    public PredictionEventHandler predictionEventHandler { private get; set; }

    // handle AirXRMessages
    private void onAirXRMessageReceived(AXRMessage message) {
        var serverMessage = message as AirXRServerMessage;
        int playerID = serverMessage.source.ToInt32();

        if (serverMessage.IsSessionEvent()) {
            if (serverMessage.Name.Equals(AirXRServerMessage.NameConnected)) {
                onAirXRSessionConnected(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameDisconnected)) {
                onAirXRSessionDisconnected(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameProfilerFrame)) {
                onAirXRProfilerFrameReceived(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameProfilerReport)) {
                onAirXRProfilerReportReceived(playerID, serverMessage);
            }
        }
        else if (serverMessage.IsPlayerEvent()) {
            if (serverMessage.Name.Equals(AirXRServerMessage.NameCreated)) {
                onAirXRPlayerCreated(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameActivated)) {
                onAirXRPlayerActivated(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameDeactivated)) {
                onAirXRPlayerDeactivated(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameDestroyed)) {
                onAirXRPlayerDestroyed(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameShowCopyright)) {
                onAirXRPlayerShowCopyright(playerID, serverMessage);
            }
            else if (serverMessage.Name.Equals(AirXRServerMessage.NameProfileCBOR)) {
                onAirXRPlayerProfileCBOR(playerID, serverMessage);
            }
        }
        else if (message.Type.Equals(AXRMessage.TypeUserData)) {
            onAirXRPlayerUserDataReceived(playerID, serverMessage);
        }
    }

    private void onAirXRSessionConnected(int playerID, AirXRServerMessage message) {
        AirXRServer.NotifyClientConnected(playerID);
    }

    private void onAirXRPlayerCreated(int playerID, AirXRServerMessage message) {
        var selected = notifyCameraRigWillBeBound(playerID);
        if (selected != null) {
            _cameraRigList.RetainCameraRig(selected);
            selected.BindPlayer(playerID);

            AXRServerPlugin.AcceptPlayer(playerID);

            var config = selected.GetConfig();
            if (string.IsNullOrEmpty(config.profileReportEndpoint) == false &&
                string.IsNullOrEmpty(config.motionOutputEndpoint) == false) {
                predictionEventHandler?.OnStartPrediction(selected, config.profileReportEndpoint, config.motionOutputEndpoint);
            }
        }
        else {
            AXRServerPlugin.Disconnect(playerID);
        }
    }

    private void onAirXRPlayerActivated(int playerID, AirXRServerMessage message) {
        var cameraRig = _cameraRigList.GetBoundCameraRig(playerID);
        if (cameraRig != null && Delegate != null) {
            Delegate.AirXRCameraRigActivated(cameraRig);
        }

        updateApplicationTargetFrameRate();
    }

    private void onAirXRPlayerDeactivated(int playerID, AirXRServerMessage message) {
        var cameraRig = _cameraRigList.GetBoundCameraRig(playerID);
        if (cameraRig != null && Delegate != null) {
            Delegate.AirXRCameraRigDeactivated(cameraRig);
        }

        updateApplicationTargetFrameRate();
    }

    private void onAirXRPlayerDestroyed(int playerID, AirXRServerMessage message) {
        var unboundCameraRig = _cameraRigList.GetBoundCameraRig(playerID);
        if (unboundCameraRig != null) {
            if (unboundCameraRig.isStreaming && Delegate != null) {
                Delegate.AirXRCameraRigDeactivated(unboundCameraRig);
            }

            if (string.IsNullOrEmpty(unboundCameraRig.GetConfig().profileReportEndpoint) == false) {
                predictionEventHandler?.OnStopPrediction(unboundCameraRig);
            }

            unboundCameraRig.UnbindPlayer();
            _cameraRigList.ReleaseCameraRig(unboundCameraRig);

            if (Delegate != null) {
                Delegate.AirXRCameraRigHasBeenUnbound(unboundCameraRig);
            }
        }
    }

    private void onAirXRPlayerShowCopyright(int playerID, AirXRServerMessage message) {
        Debug.Log("(C) 2016-present onAirXR. All right reserved.");
    }

    private void onAirXRPlayerProfileCBOR(int playerID, AirXRServerMessage message) {
        var cameraRig = _cameraRigList.GetBoundCameraRig(playerID);
        if (cameraRig == null) { return; }

        predictionEventHandler?.OnProfileDataReceived(cameraRig, message.Data_Decoded);
    }

    private void onAirXRPlayerUserDataReceived(int playerID, AirXRServerMessage message) {
        var cameraRig = _cameraRigList.GetBoundCameraRig(playerID);
        if (cameraRig != null && Delegate != null) {
            Delegate.AirXRCameraRigUserDataReceived(cameraRig, message.Data_Decoded);
        }
    }

    private void onAirXRSessionDisconnected(int playerID, AirXRServerMessage message) {
        AirXRServer.NotifyClientDisconnected(playerID);
    }

    private void onAirXRProfilerFrameReceived(int playerID, AirXRServerMessage message) {
        //Debug.Log(string.Format("profiler frame: latency: overall {0:0.000} = network {1:0.000} + decode {2:0.000}",
        //                        message.OverallLatency, message.NetworkLatency, message.DecodeLatency));
    }

    private void onAirXRProfilerReportReceived(int playerID, AirXRServerMessage message) {
        Debug.Log(string.Format("profiler report: fps {0:0.0} ({1}/{2:0.000}), avg latency: overall {3:0.000} = network {4:0.000} + decode {5:0.000}",
                                message.FrameCount / message.Duration, message.FrameCount, message.Duration,
                                message.AvgOverallLatency, message.AvgNetworkLatency, message.AvgDecodeLatency));
    }
}
