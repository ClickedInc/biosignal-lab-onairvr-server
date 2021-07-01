using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class OCSVRWorks : MonoBehaviour {
    public const string LibName = "ocs";

    [DllImport(LibName)]
    private extern static void ocs_VRWorks_Init();

    [DllImport(LibName)]
    private extern static void ocs_VRWorks_Release();
    
    private static OCSVRWorks _instance;

    public static void LoadOnce() {
        if (_instance) { return; }

        _instance = new GameObject("OCSVRWorks").AddComponent<OCSVRWorks>();
    }

    private void Awake() {
        ocs_VRWorks_Init();
    }

    private void OnDestroy() {
        ocs_VRWorks_Release();
    }
}
