/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public class AirXRServerInputStream : AXRInputStream {
    public AirXRServerInputStream(AirXRCameraRig owner) {
        this.owner = owner;
    }

    public AirXRCameraRig owner { get; set; }

    public long inputRecvTimestamp => AXRServerPlugin.GetInputRecvTimestamp(owner.playerID);

    // implements AirXRInputStream
    protected override float maxSendingRatePerSec { get { return 90.0f; } }

    protected override void BeginPendInputImpl(ref long timestamp) {
        AXRServerPlugin.BeginPendInput(owner.playerID, ref timestamp);
    }

    protected override void PendStateImpl(byte device, byte control, byte state) {
        if (owner.isBoundToClient == false) { return; }

        AXRServerPlugin.PendInputState(owner.playerID, device, control, state);
    }

    protected override void PendByteAxisImpl(byte device, byte control, byte axis) { }
    protected override void PendAxisImpl(byte device, byte control, float axis) { }
    protected override void PendAxis2DImpl(byte device, byte control, Vector2 axis2D) { }
    protected override void PendPoseImpl(byte device, byte control, Vector3 position, Quaternion rotation) { }

    protected override void PendRaycastHitImpl(byte device, byte control, Vector3 origin, Vector3 hitPosition, Vector3 hitNormal) {
        if (owner.isBoundToClient == false) { return; }

        AXRServerPlugin.PendInputRaycastHit(owner.playerID, device, control, origin, hitPosition, hitNormal);
    }

    protected override void PendVibrationImpl(byte device, byte control, float frequency, float amplitude) {
        if (owner.isBoundToClient == false) { return; }

        AXRServerPlugin.PendInputVibration(owner.playerID, device, control, frequency, amplitude);
    }

    protected override void PendTouch2DImpl(byte device, byte control, Vector2 position, byte state, bool active) {}

    protected override void SendPendingInputEventsImpl(long timestamp) {
        AXRServerPlugin.SendPendingInputs(owner.playerID, timestamp);
    }

    protected override bool GetStateImpl(byte device, byte control, ref byte state) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputState(owner.playerID, device, control, ref state);
    }

    protected override bool GetByteAxisImpl(byte device, byte control, ref byte axis) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputByteAxis(owner.playerID, device, control, ref axis);   
    }

    protected override bool GetAxisImpl(byte device, byte control, ref float axis) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputAxis(owner.playerID, device, control, ref axis);
    }

    protected override bool GetAxis2DImpl(byte device, byte control, ref Vector2 axis2D) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputAxis2D(owner.playerID, device, control, ref axis2D);
    }

    protected override bool GetPoseImpl(byte device, byte control, ref Vector3 position, ref Quaternion rotation) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputPose(owner.playerID, device, control, ref position, ref rotation);
    }

    protected override bool GetRaycastHitImpl(byte device, byte control, ref Vector3 origin, ref Vector3 hitPosition, ref Vector3 hitNormal) { return false; }
    protected override bool GetVibrationImpl(byte device, byte control, ref float frequency, ref float amplitude) { return false; }

    protected override bool GetTouch2DImpl(byte device, byte control, ref Vector2 position, ref byte state) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputTouch2D(owner.playerID, device, control, ref position, ref state);
    }

    protected override bool IsActiveImpl(byte device, byte control) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.IsInputActive(owner.playerID, device, control);
    }

    protected override bool IsActiveImpl(byte device, byte control, AXRInputDirection direction) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.IsInputDirectionActive(owner.playerID, device, control, (byte)direction);
    }

    protected override bool GetActivatedImpl(byte device, byte control) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputActivated(owner.playerID, device, control);
    }

    protected override bool GetActivatedImpl(byte device, byte control, AXRInputDirection direction) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputDirectionActivated(owner.playerID, device, control, (byte)direction);
    }

    protected override bool GetDeactivatedImpl(byte device, byte control) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputDeactivated(owner.playerID, device, control);
    }

    protected override bool GetDeactivatedImpl(byte device, byte control, AXRInputDirection direction) {
        if (owner.isBoundToClient == false) { return false; }

        return AXRServerPlugin.GetInputDirectionDeactivated(owner.playerID, device, control, (byte)direction);
    }

    protected override void UpdateInputFrameImpl() {
        if (owner.isBoundToClient == false) { return; }

        AXRServerPlugin.UpdateInputFrame(owner.playerID);
    }

    protected override void ClearInputImpl() {
        if (owner.isBoundToClient == false) { return; }

        AXRServerPlugin.ClearInput(owner.playerID);
    }
}
