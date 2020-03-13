/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public abstract class AirVRInputDevice : AirVRInputReceiver {
    public abstract class Control {
        public abstract void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id);
    }

    private class Axis4D : Control {
        public Vector4 value { get; set; }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            var quat = inputStream.GetQuaternion(device, id);
            value = new Vector4(quat.x, quat.y, quat.z, quat.w);
        }
    }

    private class Axis3D : Control {
        public Vector3 value { get; set; }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            value = inputStream.GetVector3D(device, id);
        }
    }

    private class Axis2D : Control {
        public Vector2 value { get; set; }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            value = inputStream.GetVector2D(device, id);
        }
    }

    private class Axis : Control {
        public float value { get; set; }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            value = inputStream.GetAxis(device, id);
        }
    }

    private class Button : Control {
        private float _prev;
        private float _current;

        public float value {
            get {
                return _current;
            }
            set {
                _prev = _current;
                _current = value;
            }
        }

        public bool IsDown() {
            return _prev == 0.0f && _current != 0.0f;
        }

        public bool IsUp() {
            return _prev != 0.0f && _current == 0.0f;
        }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            value = inputStream.GetButton(device, id);
        }
    }

    private class Orientation : Control {
        public Quaternion value { get; set; }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            value = inputStream.GetQuaternion(device, id);
        }
    }

    private class Xform : Control {
        private long _timeStamp;
        private Vector3 _position;
        private Quaternion _orientation;

        public long timeStamp {
            get {
                return _timeStamp;
            }
            set {
                _timeStamp = value;
            }
        }

        public Vector3 position {
            get {
                return _position;
            }
            set {
                _position = value;
            }
        }

        public Quaternion orientation {
            get {
                return _orientation;
            }
            set {
                _orientation = value;
            }
        }

        public override void PollInput(AirVRInputDevice device, AirVRInputStream inputStream, byte id) {
            inputStream.GetTransform(device, id, out _timeStamp, out _position, out _orientation);
        }
    }

    public AirVRInputDevice() {
        _controls = new Dictionary<byte, Control>();
        _extControls = new Dictionary<byte, Control>();

        MakeControlList();
    }

    private Dictionary<byte, Control> _controls;
    private Dictionary<byte, Control> _extControls;

    private Control findControl(byte controlID) {
        return _controls.ContainsKey(controlID) ? _controls[controlID] :
               _extControls.ContainsKey(controlID) ? _extControls[controlID] :
                                                     null;
    }

    protected abstract string deviceName { get; }
    protected abstract void MakeControlList();

    protected virtual void UpdateExtendedControls() { }

    protected void AddControlTouch(byte controlID) {
        _controls.Add(controlID, new AirVRInput.Touch());
    }

    protected void AddControlTransform(byte controlID) {
        _controls.Add(controlID, new Xform());
    }

    protected void AddControlOrientation(byte controlID) {
        _controls.Add(controlID, new Orientation());
    }

    protected void AddControlAxis3D(byte controlID) {
        _controls.Add(controlID, new Axis3D());
    }

    protected void AddControlAxis2D(byte controlID) {
        _controls.Add(controlID, new Axis2D());
    }

    protected void AddControlAxis(byte controlID) {
        _controls.Add(controlID, new Axis());
    }

    protected void AddControlButton(byte controlID) {
        _controls.Add(controlID, new Button());
    }

    protected void AddExtControlTransform(byte controlID) {
        _extControls.Add(controlID, new Xform());
    }

    protected void AddExtControlAxis4D(byte controlID) {
        _extControls.Add(controlID, new Axis4D());
    }

    protected void AddExtControlAxis3D(byte controlID) {
        _extControls.Add(controlID, new Axis3D());
    }
    
    protected void AddExtControlAxis2D(byte controlID) {
        _extControls.Add(controlID, new Axis2D());
    }

    protected void AddExtControlAxis(byte controlID) {
        _extControls.Add(controlID, new Axis());
    }

    protected void AddExtControlButton(byte controlID) {
        _extControls.Add(controlID, new Button());
    }

    protected void OverrideControlTransform(byte controlID, long timeStamp, Vector3 position, Quaternion orientation) {
        Assert.IsTrue(_controls.ContainsKey(controlID));

        var xform = _controls[controlID] as Xform;
        xform.timeStamp = timeStamp;
        xform.position = position;
        xform.orientation = orientation;
    }

    protected void SetExtControlTransform(byte controlID, long timestamp, Vector3 position, Quaternion orientation) {
        Assert.IsTrue(_extControls.ContainsKey(controlID));

        var xform = _extControls[controlID] as Xform;
        xform.timeStamp = timestamp;
        xform.position = position;
        xform.orientation = orientation;
    }

    protected void SetExtControlAxis4D(byte controlID, Vector4 value) {
        Assert.IsTrue(_extControls.ContainsKey(controlID));
        (_extControls[controlID] as Axis4D).value = value;
    }

    protected void SetExtControlAxis3D(byte controlID, Vector3 value) {
        Assert.IsTrue(_extControls.ContainsKey(controlID));
        (_extControls[controlID] as Axis3D).value = value;
    }

    protected void SetExtControlAxis2D(byte controlID, Vector2 value) {
        Assert.IsTrue(_extControls.ContainsKey(controlID));
        (_extControls[controlID] as Axis2D).value = value;
    }

    protected void SetExtControlAxis(byte controlID, float value) {
        Assert.IsTrue(_extControls.ContainsKey(controlID));
        (_extControls[controlID] as Axis).value = value;
    }

    protected void SetExtControlButton(byte controlID, float value) {
        Assert.IsTrue(_extControls.ContainsKey(controlID));
        (_extControls[controlID] as Button).value = value;
    }

    public bool GetTouch(byte controlID, ref Vector2 position, ref bool touch) {
        Control control = findControl(controlID);
        if (control != null) {
            position = (control as AirVRInput.Touch).position;
            touch = (control as AirVRInput.Touch).touch;
            return true;
        }
        return false;
    }

    public bool GetTransform(byte controlID, ref Vector3 position, ref Quaternion orientation) {
        Control control = findControl(controlID);
        if (control != null) {
            position = (control as Xform).position;
            orientation = (control as Xform).orientation;
            return true;
        }
        return false;
    }

    public bool GetTransform(byte controlID, ref long timeStamp, ref Vector3 position, ref Quaternion orientation) {
        Control control = findControl(controlID);
        if (control != null) {
            position = (control as Xform).position;
            orientation = (control as Xform).orientation;
            timeStamp = (control as Xform).timeStamp;
            return true;
        }
        return false;
    }

    public Quaternion GetOrientation(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Orientation).value : Quaternion.identity;
    }

    public Vector4 GetAxis4D(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Axis4D).value : Vector4.zero;
    }

    public Vector3 GetAxis3D(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Axis3D).value : Vector3.zero;
    }

    public Vector2 GetAxis2D(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Axis2D).value : Vector2.zero;
    }

    public float GetAxis(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Axis).value : 0.0f;
    }

    public float GetButtonRaw(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Button).value : 0.0f;
    }

    public bool GetButton(byte controlID) {
        return GetButtonRaw(controlID) != 0.0f;
    }

    public bool GetButtonDown(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Button).IsDown() : false;
    }

    public bool GetButtonUp(byte controlID) {
        Control control = findControl(controlID);
        return control != null ? (control as Button).IsUp() : false;
    }

    // touches
    public int GetTouchCount() {
        int count = 0;
        foreach (var control in _controls.Values) {
            var touch = control as AirVRInput.Touch;
            if (touch == null) { continue; }

            if (touch.active) {
                count++;
            }
        }
        return count;
    }

    public AirVRInput.Touch GetTouch(int index) {
        var i = 0;
        foreach (var control in _controls.Values) {
            var touch = control as AirVRInput.Touch;
            if (touch == null || touch.active == false) { continue; }

            if (i == index) {
                return touch;
            }
            else {
                i++;
            }
        }
        return null;
    }

    // implements IAirVRInputReceiver
    public override string name {
        get {
            return deviceName;
        }
    }
    
    public override void PollInputsPerFrame(AirVRInputStream inputStream) {
        foreach (var controlID in _controls.Keys) {
            _controls[controlID].PollInput(this, inputStream, controlID);
        }
        UpdateExtendedControls();
    }
}
