using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class AirXRGameEventEmitter {
    public enum Type {
        Fruit,
        Input
    }

    [DllImport(AXRServerPlugin.Name)]
    private static extern long ocs_GetGameEventTimestamp(int playerID);

    [DllImport(AXRServerPlugin.Name)]
    private static extern void ocs_EmitGameEvent(int playerID, long timestamp, string type, string id, string evt);

    public AirXRGameEventEmitter(AirXRCameraRig cameraRig) {
        _cameraRig = cameraRig;
    }

    private AirXRCameraRig _cameraRig;

    public long gameEventTimestamp {
        get {
            if (_cameraRig.isBoundToClient == false) { return -1; }

            return ocs_GetGameEventTimestamp(_cameraRig.playerID);
        }
    }

    public void EmitEvent(long timestamp, Type type, string id, string evt) {
        if (_cameraRig.isBoundToClient == false) { return; }

        ocs_EmitGameEvent(_cameraRig.playerID, timestamp, toTypeString(type), id, evt);
    }

    private string toTypeString(Type type) {
        switch (type) {
            case Type.Fruit:
                return "fruit";
            case Type.Input:
                return "input";
            default:
                return "unknown";
        }
    }
}
