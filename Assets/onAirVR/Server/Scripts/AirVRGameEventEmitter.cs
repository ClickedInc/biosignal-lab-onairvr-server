using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class AirVRGameEventEmitter {
    public enum Type {
        Fruit,
        Input
    }

    [DllImport(AirVRServerPlugin.Name)]
    private static extern long onairvr_GetGameEventTimestamp(int playerID);

    [DllImport(AirVRServerPlugin.Name)]
    private static extern void onairvr_EmitGameEvent(int playerID, long timestamp, string type, string id, string evt);

    public AirVRGameEventEmitter(AirVRCameraRig cameraRig) {
        _cameraRig = cameraRig;
    }

    private AirVRCameraRig _cameraRig;

    public long gameEventTimestamp {
        get {
            if (_cameraRig.isBoundToClient == false) { return -1; }

            return onairvr_GetGameEventTimestamp(_cameraRig.playerID);
        }
    }

    public void EmitEvent(long timestamp, Type type, string id, string evt) {
        if (_cameraRig.isBoundToClient == false) { return; }

        onairvr_EmitGameEvent(_cameraRig.playerID, timestamp, toTypeString(type), id, evt);
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
