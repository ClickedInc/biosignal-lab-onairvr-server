/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class AirXRServerSettings : ScriptableObject {
    public const string AssetName = "AirXRServerSettings.asset";
    public const string ProjectAssetPath = "Assets/onAirXR/Server/Resources/" + AssetName;

    public enum OverfillMode {
        Predictive,
        Optimal,
        None
    }

    public enum FoveatedRenderingPriority {
        StreamingFirst,
        PlaybackFirst
    }

    [SerializeField] private string license = "noncommercial.license";
    [SerializeField] private int maxClientCount = 1;
    [SerializeField] private int stapPort = 9090;
    [SerializeField] private bool adaptiveFrameRate = false;
    [SerializeField] [Range(10, 120)] private int minFrameRate = 10;

    [SerializeField] private bool visualizeRenderingInfo = false;
    [SerializeField] private OverfillMode overfillMode = OverfillMode.Predictive;
    [SerializeField] private bool useFoveatedRendering = true;
    [SerializeField] private FoveatedRenderingPriority foveatedRenderingPriority = FoveatedRenderingPriority.StreamingFirst;
    [SerializeField] private OCSVRWorksFoveatedRenderer.ShadingRate foveatedShadingInnerRate = OCSVRWorksFoveatedRenderer.ShadingRate.X1;
    [SerializeField] private OCSVRWorksFoveatedRenderer.ShadingRate foveatedShadingMiddleRate = OCSVRWorksFoveatedRenderer.ShadingRate.X1_PER_2x2;
    [SerializeField] private OCSVRWorksFoveatedRenderer.ShadingRate foveatedShadingOuterRate = OCSVRWorksFoveatedRenderer.ShadingRate.X1_PER_4x4;
    [SerializeField] private Color renderPerfGraphColorBasis = Color.yellow;
    [SerializeField] private Color renderPerfGraphColorOverfillOnly = Color.green;
    [SerializeField] private Color renderPerfGraphColorFoveatedOverfill = Color.magenta;
    [SerializeField] private Vector2 renderPerfGraphDefaultRangeY = new Vector2(0, 3);
    [SerializeField] private bool renderPerfGraphAutoFitRangeY = true;
    [SerializeField] private int renderPerfGraphLength = 1000;
    [SerializeField] private bool bypassPrediction = false;

    // overridable by command line args only
    [SerializeField] private int ampPort;
    [SerializeField] private bool loopbackOnly;
    [SerializeField] private string profiler;

    public bool AdaptiveFrameRate { get { return adaptiveFrameRate; } }
    public int MinFrameRate { get { return minFrameRate; } }
    public int MaxClientCount { get { return maxClientCount; } }
    public string License { get { return license; } }
    public int StapPort { get { return stapPort; } }
    public int AmpPort { get { return ampPort; } }
    public bool LoopbackOnly { get { return loopbackOnly; } }
    public string Profiler { get { return profiler; } }

    public bool VisualizeRenderingInfo => visualizeRenderingInfo;
    public float PreviewRenderScale => visualizeRenderingInfo ? 2.0f : 1.0f;
    public OverfillMode OverfillingMode => overfillMode;
    public bool UseFoveatedRendering => useFoveatedRendering;
    public FoveatedRenderingPriority FoveatedRenderPriority => foveatedRenderingPriority;
    public OCSVRWorksFoveatedRenderer.ShadingRate FoveatedShadingInnerRate => foveatedShadingInnerRate;
    public OCSVRWorksFoveatedRenderer.ShadingRate FoveatedShadingMiddleRate => foveatedShadingMiddleRate;
    public OCSVRWorksFoveatedRenderer.ShadingRate FoveatedShadingOuterRate => foveatedShadingOuterRate;
    public Color RenderPerfGraphColorBasis => renderPerfGraphColorBasis;
    public Color RenderPerfGraphColorOverfillOnly => renderPerfGraphColorOverfillOnly;
    public Color RenderPerfGraphColorFoveatedOverfill => renderPerfGraphColorFoveatedOverfill;
    public Vector2 RenderPerfGraphDefaultRangeY => renderPerfGraphDefaultRangeY;
    public bool RenderPerfGraphAutoFitRangeY => renderPerfGraphAutoFitRangeY;
    public int RenderPerfGraphLength => renderPerfGraphLength;
    public bool BypassPrediction => bypassPrediction;

    public void ParseCommandLineArgs(string[] args) {
        Dictionary<string, string> pairs = AXRUtils.ParseCommandLine(args);
        if (pairs == null) {
            return;
        }

        string keyConfigFile = "config";
        if (pairs.ContainsKey(keyConfigFile)) {
            if (File.Exists(pairs[keyConfigFile])) {
                try {
                    var reader = new AirXRServerSettingsReader();
                    reader.ReadSettings(pairs[keyConfigFile], this);
                }
                catch (Exception e) {
                    Debug.LogWarning("[onAirXR] WARNING: failed to parse " + pairs[keyConfigFile] + " : " + e.ToString());
                }
            }
            pairs.Remove("config");
        }

        foreach (string key in pairs.Keys) {
            if (key.Equals("onairxr_stap_port") || 
                key.Equals("onairvr_stap_port")) {
                stapPort = parseInt(pairs[key], StapPort,
                    (parsed) => {
                        return 0 <= parsed && parsed <= 65535;
                    },
                    (val) => {
                        Debug.LogWarning("[onAirXR] WARNING: STAP Port number of the command line argument is invalid : " + val);
                    });
            }
            else if (key.Equals("onairxr_amp_port") || 
                     key.Equals("onairvr_amp_port")) {
                ampPort = parseInt(pairs[key], AmpPort,
                    (parsed) => {
                        return 0 <= parsed && parsed <= 65535;
                    },
                    (val) => {
                        Debug.LogWarning("[onAirXR] WARNING: AMP Port number of the command line argument is invalid : " + val);
                    });
            }
            else if (key.Equals("onairxr_loopback_only") || 
                     key.Equals("onairvr_loopback_only")) {
                loopbackOnly = pairs[key].Equals("true");
            }
            else if (key.Equals("onairxr_license") ||
                     key.Equals("onairvr_license")) {
                license = pairs[key];
            }
            else if (key.Equals("onairxr_adaptive_frame_rate") ||
                     key.Equals("onairvr_adaptive_frame_rate")) {
                adaptiveFrameRate = pairs[key].Equals("true");
            }
            else if (key.Equals("onairxr_min_frame_rate") ||
                     key.Equals("onairvr_min_frame_rate")) {
                minFrameRate = parseInt(pairs[key], MinFrameRate,
                    (parsed) => {
                        return parsed >= 0;
                    });
            }
            else if (key.Equals("onairxr_profiler") ||
                     key.Equals("onairvr_profiler")) {
                profiler = pairs[key];
            }
        }
    }

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
}
