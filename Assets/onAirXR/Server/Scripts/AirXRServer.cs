/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.IO;

[Serializable]
public class AirXRServerSettingsReader {
#pragma warning disable 414
    [SerializeField] private AirXRServerSettings onairvr;
#pragma warning restore 414

    public void ReadSettings(string fileFrom, AirXRServerSettings to) {
        onairvr = to;
        JsonUtility.FromJsonOverwrite(File.ReadAllText(fileFrom), this);
    }
}

public class AirXRServer : MonoBehaviour {
    private const int StartupErrorNotSupportingGPU = -1;
    private const int StartupErrorLicenseNotYetVerified = -2;
    private const int StartupErrorLicenseFileNotFound = -3;
    private const int StartupErrorInvalidLicenseFile = -4;
    private const int StartupErrorLicenseExpired = -5;

    private const int GroupOfPictures = 0; // use infinite gop by default

    private const int ProfilerFrame = 0x01;
    private const int ProfilerReport = 0x02;

    public interface EventHandler {
        void AirXRServerFailed(string reason);
        void AirXRServerClientConnected(int clientHandle);
        void AirXRServerClientDisconnected(int clientHandle);
    }

    private static AirXRServer _instance;
    private static EventHandler _Delegate;

    internal static AirXRServerSettings settings {
        get {
            Assert.IsNotNull(_instance);
            Assert.IsNotNull(_instance._settings);

            return _instance._settings;
        }
    }

    internal static void LoadOnce() {
        if (_instance == null) {
            GameObject go = new GameObject("AirXRServer");
            go.AddComponent<AirXRServer>();
            Assert.IsNotNull(_instance);

            var settings = Resources.Load<AirXRServerSettings>("AirXRServerSettings");
            if (settings == null) {
                settings = ScriptableObject.CreateInstance<AirXRServerSettings>();
            }
            _instance._settings = settings;
            _instance._settings.ParseCommandLineArgs(Environment.GetCommandLineArgs());
        }
    }

    internal static void NotifyClientConnected(int clientHandle) {
        if (_Delegate != null) {
            _Delegate.AirXRServerClientConnected(clientHandle);
        }
    }

    internal static void NotifyClientDisconnected(int clientHandle) {
        if (_Delegate != null) {
            _Delegate.AirXRServerClientDisconnected(clientHandle);
        }
    }

    public static EventHandler Delegate {
        set {
            _Delegate = value;
        }
    }

    public static bool isInstantiated => _instance != null;

    public static void SendAudioFrame(AirXRCameraRig cameraRig, float[] data, int sampleCount, int channels, double timestamp) {
        if (cameraRig.isBoundToClient) {
            AXRServerPlugin.EncodeAudioFrame(cameraRig.playerID, data, data.Length / channels, channels, AudioSettings.dspTime);
        }
    }

    public static void SendAudioFrameToAllCameraRigs(float[] data, int sampleCount, int channels, double timestamp) {
        AXRServerPlugin.EncodeAudioFrameForAllPlayers(data, data.Length / channels, channels, AudioSettings.dspTime);
    }

    private bool _startedUp = false;
    private AirXRServerSettings _settings;
    private float _lastTimeEvalFps = 0.0f;
    private int _frameCountSinceLastEvalFps = 0;

    private void Awake() {
        if (_instance != null) {
            new UnityException("[onAirXR] ERROR: There must exist only one AirXRServer instance.");
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        _lastTimeEvalFps = Time.realtimeSinceStartup;

        try {
            Assert.IsNotNull(_settings);

            if (_settings.AdaptiveFrameRate) {
                QualitySettings.vSyncCount = 0;
            }

            AXRServerPlugin.SetLicenseFile(Application.isEditor ? "Assets/onAirXR/Server/Editor/Misc/noncommercial.license" : _settings.License);
            AXRServerPlugin.SetVideoEncoderParameters(120.0f, GroupOfPictures);

            int startupResult = AXRServerPlugin.Startup(_settings.MaxClientCount, _settings.StapPort, _settings.AmpPort, _settings.LoopbackOnly, AudioSettings.outputSampleRate);
            if (startupResult == 0) {   // no error
                var pluginPtr = IntPtr.Zero;
                AXRServerPlugin.GetPluginPtr(ref pluginPtr);
                AXRServerPlugin.SetPluginPtr(pluginPtr);

                GL.IssuePluginEvent(AXRServerPlugin.Startup_RenderThread_Func, 0);
                _startedUp = true;

                switch (_settings.Profiler) {
                    case "full":
                        AXRServerPlugin.EnableProfiler(ProfilerFrame | ProfilerReport);
                        break;
                    case "frame":
                        AXRServerPlugin.EnableProfiler(ProfilerFrame);
                        break;
                    case "report":
                        AXRServerPlugin.EnableProfiler(ProfilerReport);
                        break;
                    default:
                        break;
                }

                Debug.Log("[onAirXR] INFO: The onAirXR Server has started on port " + _settings.StapPort + ".");
            }
            else {
                string reason;
                switch (startupResult) {
                    case StartupErrorNotSupportingGPU:
                        reason = "Graphic device is not supported";
                        break;
                    case StartupErrorLicenseNotYetVerified:
                        reason = "License is not yet verified";
                        break;
                    case StartupErrorLicenseFileNotFound:
                        reason = "License file not found";
                        break;
                    case StartupErrorInvalidLicenseFile:
                        reason = "Invalid license file";
                        break;
                    case StartupErrorLicenseExpired:
                        reason = "License expired";
                        break;
                    default:
                        reason = "Unknown error occurred";
                        break;
                }

                Debug.LogError("[onAirXR] ERROR: Failed to startup : " + reason);
                if (_Delegate != null) {
                    _Delegate.AirXRServerFailed(reason);
                }
            }
        }
        catch (System.DllNotFoundException) {
            if (_Delegate != null) {
                _Delegate.AirXRServerFailed("Failed to load onAirXR server plugin");
            }
        }
    }

    private void Update() {
        const float evalFpsPeriod = 10.0f;

        if (string.IsNullOrEmpty(_settings.Profiler)) {
            return;
        }

        _frameCountSinceLastEvalFps++;

        var now = Time.realtimeSinceStartup;
        if (_lastTimeEvalFps + evalFpsPeriod < now) {
            var fps = _frameCountSinceLastEvalFps / (now - _lastTimeEvalFps);
            Debug.Log(string.Format("[onAirXR Server] FPS: {0:0.0}", fps));

            _lastTimeEvalFps = now;
            _frameCountSinceLastEvalFps = 0;
        }
    }

    private void OnDestroy() {
        if (_startedUp) {
            GL.IssuePluginEvent(AXRServerPlugin.Shutdown_RenderThread_Func, 0);
            GL.Flush();

            AXRServerPlugin.Shutdown();
        }

        NetMQ.NetMQConfig.Cleanup(false);
    }
}
