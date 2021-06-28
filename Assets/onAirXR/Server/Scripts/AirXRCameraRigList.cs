/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;

public class AirXRCameraRigList {
    private Dictionary<AirXRClientType, List<AirXRCameraRig>> _cameraRigsAvailable;
    private Dictionary<AirXRClientType, List<AirXRCameraRig>> _cameraRigsRetained;

    public AirXRCameraRigList() {
        _cameraRigsAvailable = new Dictionary<AirXRClientType, List<AirXRCameraRig>>();
        _cameraRigsRetained = new Dictionary<AirXRClientType, List<AirXRCameraRig>>();
    }

    private AirXRCameraRig getBoundCameraRig(AirXRClientType type, int playerID) {
        if (_cameraRigsRetained.ContainsKey(type)) {
            foreach (var cameraRig in _cameraRigsRetained[type]) {
                if (cameraRig.playerID == playerID) {
                    return cameraRig;
                }
            }
        }
        return null;
    }

    public void GetAllCameraRigs(List<AirXRCameraRig> result) {
        foreach (var key in _cameraRigsRetained.Keys) {
            result.AddRange(_cameraRigsRetained[key]);
        }
        foreach (var key in _cameraRigsAvailable.Keys) {
            result.AddRange(_cameraRigsAvailable[key]);
        }
    }

    public void GetAvailableCameraRigs(AirXRClientType type, List<AirXRCameraRig> result) {
        if (_cameraRigsAvailable.ContainsKey(type)) {
            result.AddRange(_cameraRigsAvailable[type]);
        }
    }

    public void GetAllRetainedCameraRigs(List<AirXRCameraRig> result) {
        foreach (var key in _cameraRigsRetained.Keys) {
            result.AddRange(_cameraRigsRetained[key]);
        }
    }

    public AirXRCameraRig GetBoundCameraRig(int playerID) {
        if (playerID >= 0) {
            return getBoundCameraRig(AirXRClientType.Stereoscopic, playerID) ?? 
                   getBoundCameraRig(AirXRClientType.Monoscopic, playerID);
        }
        return null;
    }

    public void AddUnboundCameraRig(AirXRCameraRig cameraRig) {
        if (_cameraRigsAvailable.ContainsKey(cameraRig.type) == false) {
            _cameraRigsAvailable.Add(cameraRig.type, new List<AirXRCameraRig>());
        }
        if (_cameraRigsRetained.ContainsKey(cameraRig.type) == false) {
            _cameraRigsRetained.Add(cameraRig.type, new List<AirXRCameraRig>());
        }
        if (_cameraRigsAvailable[cameraRig.type].Contains(cameraRig) == false &&
            _cameraRigsRetained[cameraRig.type].Contains(cameraRig) == false) {
            _cameraRigsAvailable[cameraRig.type].Add(cameraRig);
        }
    }

    public void RemoveCameraRig(AirXRCameraRig cameraRig) {
        if (_cameraRigsAvailable.ContainsKey(cameraRig.type) == false ||
            _cameraRigsRetained.ContainsKey(cameraRig.type) == false) {
            return;
        }

        if (_cameraRigsAvailable[cameraRig.type].Contains(cameraRig)) {
            _cameraRigsAvailable[cameraRig.type].Remove(cameraRig);
        }
        else if (_cameraRigsRetained[cameraRig.type].Contains(cameraRig)) {
            _cameraRigsRetained[cameraRig.type].Remove(cameraRig);
        }
    }

    public AirXRCameraRig RetainCameraRig(AirXRCameraRig cameraRig) {
        if (_cameraRigsAvailable.ContainsKey(cameraRig.type) && _cameraRigsRetained.ContainsKey(cameraRig.type)) {
            if (_cameraRigsAvailable[cameraRig.type].Contains(cameraRig)) {
                _cameraRigsAvailable[cameraRig.type].Remove(cameraRig);
                _cameraRigsRetained[cameraRig.type].Add(cameraRig);
                return cameraRig;
            }
        }
        return null;
    }

    public void ReleaseCameraRig(AirXRCameraRig cameraRig) {
        if (_cameraRigsAvailable.ContainsKey(cameraRig.type) && _cameraRigsRetained.ContainsKey(cameraRig.type)) {
            if (_cameraRigsRetained[cameraRig.type].Contains(cameraRig)) {
                _cameraRigsRetained[cameraRig.type].Remove(cameraRig);
                _cameraRigsAvailable[cameraRig.type].Add(cameraRig);
            }
        }
    }
}
