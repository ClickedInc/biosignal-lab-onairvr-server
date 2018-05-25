/***********************************************************

  Copyright (c) 2017-2018 Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[Serializable]
public class AirVRServerParams {
    public const float DefaultMaxFrameRate = 60.0f;
    public const float DefaultDefaultFrameRate = 30.0f;
    public const float DefaultApplicationFrameRate = 0.0f;
    public const int DefaultVideoBitrate = 24000000;
    public const int DefaultMaxClientCount = 1;
    public const int DefaultPort = 9090;
    public const string DefaultLicense = "onairvr.license";

    public AirVRServerParams() {
        MaxFrameRate = DefaultMaxFrameRate;
        DefaultFrameRate = DefaultDefaultFrameRate;
        ApplicationFrameRate = DefaultApplicationFrameRate;
        VsyncCount = QualitySettings.vSyncCount;
        VideoBitrate = DefaultVideoBitrate;
        MaxClientCount = DefaultMaxClientCount;
        StapPort = DefaultPort;
        License = DefaultLicense;
    }

    public AirVRServerParams(AirVRServerInitParams initParams) {
        MaxFrameRate = initParams.maxFrameRate;
        DefaultFrameRate = initParams.defaultFrameRate;
        ApplicationFrameRate = DefaultApplicationFrameRate;
        VsyncCount = QualitySettings.vSyncCount;
        VideoBitrate = initParams.videoBitrate;
        MaxClientCount = initParams.maxClientCount;
        StapPort = initParams.port;
        License = initParams.licenseFilePath;
    }

    public float MaxFrameRate;
    public float DefaultFrameRate;
    public float ApplicationFrameRate;
    public int VsyncCount;
    public int VideoBitrate;
    public int MaxClientCount;
    public string License;
    public int StapPort;
    public int AmpPort;
    public bool LoopbackOnly;
    public string UserData;
    public string GroupServer;

    private int parseInt(string value, int defaultValue, Func<int, bool> predicate, Action<string> failed = null) {
        int result;
        if (int.TryParse(value, out result) && predicate(result)) {
            return result;
        }

        if (failed != null) {
            failed(value);
        }
        return defaultValue;
    }

    private float parseFloat(string value, float defaultValue, Func<float, bool> predicate, Action<string> failed = null) {
        float result;
        if (float.TryParse(value, out result) && predicate(result)) {
            return result;
        }

        if (failed != null) {
            failed(value);
        }
        return defaultValue;
    }

    private Dictionary<string, string> parseCommandLine(string[] args) {
        if (args == null) {
            return null;
        }

        Dictionary<string, string> result = new Dictionary<string, string>();
        for (int i = 0; i < args.Length; i++) {
            int splitIndex = args[i].IndexOf("=");
            if (splitIndex <= 0) {
                continue;
            }

            string name = args[i].Substring(0, splitIndex);
            string value = args[i].Substring(splitIndex + 1);
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value)) {
                continue;
            }

            result.Add(name, value);
        }
        return result.Count > 0 ? result : null;
    }

    public void ParseCommandLineArgs(string[] args) {
        Dictionary<string, string> pairs = parseCommandLine(args);
        if (pairs == null) {
            return;
        }

        string keyConfigFile = "onairvr_config";
        if (pairs.ContainsKey(keyConfigFile)) {
            if (File.Exists(pairs[keyConfigFile])) {
                try {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(pairs[keyConfigFile]), this);
                }
                catch (Exception e) {
                    Debug.LogWarning("[onAirVR] WARNING: failed to parse " + pairs[keyConfigFile] + " : " + e.ToString());
                }
            }
            pairs.Remove("onairvr_config");
        }

        foreach (string key in pairs.Keys) {
            if (key.Equals("onairvr_stap_port")) {
                StapPort = parseInt(pairs[key], StapPort,
                    (parsed) => {
                        return 0 <= parsed && parsed <= 65535;
                    },
                    (val) => {
                        Debug.LogWarning("[onAirVR] WARNING: STAP Port number of the command line argument is invalid : " + val);
                    });
            }
            else if (key.Equals("onairvr_amp_port")) {
                AmpPort = parseInt(pairs[key], AmpPort,
                    (parsed) => {
                        return 0 <= parsed && parsed <= 65535;
                    },
                    (val) => {
                        Debug.LogWarning("[onAirVR] WARNING: AMP Port number of the command line argument is invalid : " + val);
                    });
            }
            else if (key.Equals("onairvr_loopback_only")) {
                LoopbackOnly = pairs[key].Equals("true");
            }
            else if (key.Equals("onairvr_license")) {
                License = pairs[key];
            }
            else if (key.Equals("onairvr_video_bitrate")) {
                VideoBitrate = parseInt(pairs[key], VideoBitrate,
                    (parsed) => {
                        return parsed > 0;
                    });
            }
            else if (key.Equals("onairvr_user_data")) {
                UserData = WWW.UnEscapeURL(pairs[key]);
            }
            else if (key.Equals("onairvr_group_server")) {
                GroupServer = pairs[key];
            }
            else if (key.Equals("onairvr_application_frame_rate")) {
                ApplicationFrameRate = parseFloat(pairs[key], ApplicationFrameRate,
                    (parsed) => {
                        return parsed >= 0.0f;
                    });
            }
            else if (key.Equals("onairvr_vsync_count")) {
                VsyncCount = parseInt(pairs[key], VsyncCount,
                    (parsed) => {
                        return parsed >= 0;
                    });
            }
        }
    }
}

public class AirVRServer : MonoBehaviour {
    private const int StartupErrorNotSupportdingGPU = -1;
    private const int StartupErrorLicenseNotYetVerified = -2;
    private const int StartupErrorLicenseFileNotFound = -3;
    private const int StartupErrorInvalidLicenseFile = -4;
    private const int StartupErrorLicenseExpired = -5;

    private const int GroupOfPictures = 60;

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_GetAirVRServerPluginPtr(ref System.IntPtr result);

    [DllImport(AirVRServerPlugin.AudioPluginName)]
    private static extern void onairvr_SetAirVRServerPluginPtr(System.IntPtr ptr);

    [DllImport(AirVRServerPlugin.Name, CharSet = CharSet.Ansi)]
    private static extern int onairvr_SetLicenseFile(string filePath);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern int onairvr_Startup(int maxConnectionCount, int portSTAP, int portAMP, bool loopbackOnlyForSTAP, int audioSampleRate);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern System.IntPtr onairvr_Startup_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_Shutdown();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern IntPtr onairvr_Shutdown_RenderThread_Func();

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_SetVideoEncoderParameters(float maxFrameRate, float defaultFrameRate,
                                                                 int maxBitRate, int defaultBitRate, int gopCount);

    [DllImport(AirVRServerPlugin.AudioPluginName)]
    private static extern void onairvr_EncodeAudioFrame(int playerID, float[] data, int sampleCount, int channels, double timestamp);

    [DllImport(AirVRServerPlugin.AudioPluginName)]
    private static extern void onairvr_EncodeAudioFrameForAllPlayers(float[] data, int sampleCount, int channels, double timestamp);

    //[DllImport(AirVRServerPlugin.Name)]
    //private static extern System.IntPtr RenderServerShutdownFunc();

    public interface EventHandler {
        void AirVRServerFailed(string reason);
        void AirVRServerClientConnected(int clientHandle);
        void AirVRServerClientDisconnected(int clientHandle);
    }

    private static AirVRServer _instance;
    private static EventHandler _Delegate;

    internal static AirVRServerParams serverParams {
        get {
            Debug.Assert(_instance != null);
            Debug.Assert(_instance._serverParams != null);

            return _instance._serverParams;
        }
    }

    internal static void LoadOnce(AirVRServerInitParams initParams = null) {
        if (_instance == null) {
            GameObject go = new GameObject("AirVRServer");
            go.AddComponent<AirVRServer>();
            Debug.Assert(_instance != null);

            _instance._serverParams = (initParams != null) ? new AirVRServerParams(initParams) : new AirVRServerParams();
            _instance._serverParams.ParseCommandLineArgs(Environment.GetCommandLineArgs());
        }
    }

    internal static int GetApplicationFrameRate(float maxCameraRigVideoFrameRate) {
        return serverParams.ApplicationFrameRate > 0.0f ?
            Mathf.RoundToInt(serverParams.ApplicationFrameRate) :
            Mathf.Max(Mathf.RoundToInt(maxCameraRigVideoFrameRate), 10);
    }

    internal static void NotifyClientConnected(int clientHandle) {
        if (_Delegate != null) {
            _Delegate.AirVRServerClientConnected(clientHandle);
        }
    }

    internal static void NotifyClientDisconnected(int clientHandle) {
        if (_Delegate != null) {
            _Delegate.AirVRServerClientDisconnected(clientHandle);
        }
    }

    public static EventHandler Delegate {
        set {
            _Delegate = value;
        }
    }

    public static void SendAudioFrame(AirVRCameraRig cameraRig, float[] data, int sampleCount, int channels, double timestamp) {
        if (cameraRig.isBoundToClient) {
            onairvr_EncodeAudioFrame(cameraRig.playerID, data, data.Length / channels, channels, AudioSettings.dspTime);
        }
    }

    public static void SendAudioFrameToAllCameraRigs(float[] data, int sampleCount, int channels, double timestamp) {
        onairvr_EncodeAudioFrameForAllPlayers(data, data.Length / channels, channels, AudioSettings.dspTime);
    }

    private bool _startedUp = false;
    private AirVRServerParams _serverParams;

    void Awake() {
        if (_instance != null) {
            new UnityException("[onAirVR] ERROR: There must exist only one AirVRServer instance.");
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        try {
            QualitySettings.vSyncCount = serverParams.VsyncCount;

            onairvr_SetLicenseFile(Application.isEditor ? System.IO.Path.Combine("Assets/onAirVR/Server/Editor/Misc", AirVRServerParams.DefaultLicense) : serverParams.License);
            onairvr_SetVideoEncoderParameters(serverParams.MaxFrameRate, serverParams.DefaultFrameRate, serverParams.VideoBitrate, serverParams.VideoBitrate, GroupOfPictures);

            int startupResult = onairvr_Startup(serverParams.MaxClientCount, serverParams.StapPort, serverParams.AmpPort, serverParams.LoopbackOnly, AudioSettings.outputSampleRate);
            if (startupResult == 0) {   // no error
                System.IntPtr pluginPtr = System.IntPtr.Zero;
                onairvr_GetAirVRServerPluginPtr(ref pluginPtr);
                onairvr_SetAirVRServerPluginPtr(pluginPtr);

                GL.IssuePluginEvent(onairvr_Startup_RenderThread_Func(), 0);
                _startedUp = true;

                Debug.Log("[onAirVR] INFO: The onAirVR Server has started on port " + serverParams.StapPort + ".");
            }
            else {
                string reason;
                switch (startupResult) {
                    case StartupErrorNotSupportdingGPU:
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

                Debug.LogError("[onAirVR] ERROR: Failed to startup : " + reason);
                if (_Delegate != null) {
                    _Delegate.AirVRServerFailed(reason);
                }
            }
        }
        catch (System.DllNotFoundException) {
            if (_Delegate != null) {
                _Delegate.AirVRServerFailed("Failed to load onAirVR server plugin");
            }
        }
    }

    void OnDestroy() {
        if (_startedUp) {
            GL.IssuePluginEvent(onairvr_Shutdown_RenderThread_Func(), 0);
            GL.Flush();

            onairvr_Shutdown();
        }

        NetMQ.NetMQConfig.Cleanup(false);
    }
}
