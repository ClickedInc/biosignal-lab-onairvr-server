/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public interface IAirXRTrackingModelContext {
    void RecenterCameraRigPose();
}

public abstract class AirXRTrackingModel {
    public AirXRTrackingModel(IAirXRTrackingModelContext context, Transform leftEyeAnchor, Transform centerEyeAnchor, Transform rightEyeAnchor) {
        this.context = context;
        this.leftEyeAnchor = leftEyeAnchor;
        this.centerEyeAnchor = centerEyeAnchor;
        this.rightEyeAnchor = rightEyeAnchor;

        HMDSpaceToWorldMatrix = centerEyeAnchor.parent.localToWorldMatrix;
    }

    protected IAirXRTrackingModelContext context    { get; private set; }
    protected Transform leftEyeAnchor               { get; private set; }
    protected Transform centerEyeAnchor             { get; private set; }
    protected Transform rightEyeAnchor              { get; private set; }

    protected virtual Quaternion HMDTrackingRootRotation {
        get {
            return centerEyeAnchor.parent.rotation;
        }
    }

    protected abstract void OnUpdateEyePose(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation);
    protected abstract void OnUpdateEyePose(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose);

    public Matrix4x4 HMDSpaceToWorldMatrix  { get; private set; }

    public virtual void StartTracking() {}

    public virtual void StopTracking() {
        HMDSpaceToWorldMatrix = centerEyeAnchor.parent.localToWorldMatrix;
    }

    public void UpdateEyePose(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation) {
        OnUpdateEyePose(config, centerEyePosition, centerEyeOrientation);

        Transform trackingRoot = centerEyeAnchor.parent;
        HMDSpaceToWorldMatrix = Matrix4x4.TRS(trackingRoot.localToWorldMatrix.MultiplyPoint(centerEyeAnchor.localPosition - centerEyePosition),
                                              HMDTrackingRootRotation,
                                              trackingRoot.lossyScale);
    }

    public void UpdateEyePose(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose) {
        OnUpdateEyePose(config, leftEyePose, rightEyePose);

        var centerEyePosition = (leftEyePose.position + rightEyePose.position) / 2;
        var trackingRoot = centerEyeAnchor.parent;
        HMDSpaceToWorldMatrix = Matrix4x4.TRS(trackingRoot.localToWorldMatrix.MultiplyPoint(centerEyeAnchor.localPosition - centerEyePosition),
                                              HMDTrackingRootRotation,
                                              trackingRoot.lossyScale);
    }
}

public class AirXRHeadTrackingModel : AirXRTrackingModel {
    public AirXRHeadTrackingModel(IAirXRTrackingModelContext context, Transform leftEyeAnchor, Transform centerEyeAnchor, Transform rightEyeAnchor)
        : base(context, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) {}

    // implements AirXRTrackingModel
    protected override void OnUpdateEyePose(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation) {
        centerEyeAnchor.localRotation = leftEyeAnchor.localRotation = rightEyeAnchor.localRotation = centerEyeOrientation;

        leftEyeAnchor.localPosition = centerEyePosition + centerEyeOrientation * (Vector3.left * 0.5f * config.ipd);
        centerEyeAnchor.localPosition = centerEyePosition;
        rightEyeAnchor.localPosition = centerEyePosition + centerEyeOrientation * (Vector3.right * 0.5f * config.ipd);
    }

    protected override void OnUpdateEyePose(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose) {
        centerEyeAnchor.localPosition = (leftEyePose.position + rightEyePose.position) / 2;
        centerEyeAnchor.localRotation = leftEyePose.rotation;
        leftEyeAnchor.localPosition = leftEyePose.position;
        leftEyeAnchor.localRotation = leftEyePose.rotation;
        rightEyeAnchor.localPosition = rightEyePose.position;
        rightEyeAnchor.localRotation = rightEyePose.rotation;
    }
}

public class AirXRIPDOnlyTrackingModel : AirXRTrackingModel {
    public AirXRIPDOnlyTrackingModel(IAirXRTrackingModelContext context, Transform leftEyeAnchor, Transform centerEyeAnchor, Transform rightEyeAnchor)
        : base(context, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) {}

    // implements AirXRTrackingModel
    protected override void OnUpdateEyePose(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation) {
        centerEyeAnchor.localRotation = leftEyeAnchor.localRotation = rightEyeAnchor.localRotation = centerEyeOrientation;

        leftEyeAnchor.localPosition = centerEyeOrientation * (Vector3.left * 0.5f * config.ipd);
        centerEyeAnchor.localPosition = Vector3.zero;
        rightEyeAnchor.localPosition = centerEyeOrientation * (Vector3.right * 0.5f * config.ipd);
    }

    protected override void OnUpdateEyePose(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose) {
        OnUpdateEyePose(config, leftEyePose.position, leftEyePose.rotation);
    }
}

public class AirXRExternalTrackerTrackingModel : AirXRTrackingModel {
    public AirXRExternalTrackerTrackingModel(IAirXRTrackingModelContext context, Transform leftEyeAnchor, Transform centerEyeAnchor, Transform rightEyeAnchor, Transform trackingOrigin, Transform tracker)
        : base(context, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) {
        _trackingSpaceChanged = false;
        _trackingOrigin = trackingOrigin;
        _tracker = tracker;

        _localTrackerRotationOnIdentityHeadOrientation = Quaternion.identity;
    }

    private bool _trackingSpaceChanged;
    private Quaternion _localTrackerRotationOnIdentityHeadOrientation;
    private Transform _trackingOrigin;
    private Transform _tracker;

    private Quaternion trackingOriginRotation {
        get {
            return _trackingOrigin != null ? _trackingOrigin.rotation : Quaternion.identity;
        }
    }

    private bool needToUpdateTrackingSpace() {
        return _trackingSpaceChanged;
    }

    private void updateTrackingSpace() {
        if (_tracker != null) {
            context.RecenterCameraRigPose();
            _localTrackerRotationOnIdentityHeadOrientation = Quaternion.Euler(0.0f, (_tracker.rotation * (_trackingOrigin != null ? Quaternion.Inverse(_trackingOrigin.rotation) : Quaternion.identity)).eulerAngles.y, 0.0f);
        }
        _trackingSpaceChanged = false;
    }

    public Transform trackingOrigin {
        set {
            if (_trackingOrigin != value) {
                _trackingOrigin = value;
                _trackingSpaceChanged = true;
            }
        }
    }

    public Transform tracker {
        set {
            if (_tracker != value) {
                _tracker = value;
                _trackingSpaceChanged = true;
            }
        }
    }

    // implements AirXRTrackingModel
    protected override Quaternion HMDTrackingRootRotation {
        get {
            return trackingOriginRotation * _localTrackerRotationOnIdentityHeadOrientation;
        }
    }

    protected override void OnUpdateEyePose(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation) {
        if (needToUpdateTrackingSpace()) {
            updateTrackingSpace();
        }

        if (_tracker != null) {
            Quaternion worldHeadOrientation = HMDTrackingRootRotation * centerEyeOrientation;
            centerEyeAnchor.rotation = leftEyeAnchor.rotation = rightEyeAnchor.rotation = worldHeadOrientation;

            Vector3 cameraRigScale = centerEyeAnchor.parent.lossyScale;
            leftEyeAnchor.position = _tracker.position + worldHeadOrientation * (Vector3.Scale(Vector3.left, cameraRigScale) * 0.5f * config.ipd);
            centerEyeAnchor.position = _tracker.position;
            rightEyeAnchor.position = _tracker.position + worldHeadOrientation * (Vector3.Scale(Vector3.right, cameraRigScale) * 0.5f * config.ipd);
        }
        else {
            centerEyeAnchor.localRotation = leftEyeAnchor.localRotation = rightEyeAnchor.localRotation = centerEyeOrientation;

            leftEyeAnchor.localPosition = centerEyeOrientation * (Vector3.left * 0.5f * config.ipd);
            centerEyeAnchor.localPosition = Vector3.zero;
            rightEyeAnchor.localPosition = centerEyeOrientation * (Vector3.right * 0.5f * config.ipd);
        }
    }

    protected override void OnUpdateEyePose(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose) {
        OnUpdateEyePose(config, leftEyePose.position, leftEyePose.rotation);
    }

    public override void StartTracking() {
        updateTrackingSpace();
    }
}

public class AirXRNoPotisionTrackingModel : AirXRTrackingModel {
    public AirXRNoPotisionTrackingModel(IAirXRTrackingModelContext context, Transform leftEyeAnchor, Transform centerEyeAnchor, Transform rightEyeAnchor)
        : base(context, leftEyeAnchor, centerEyeAnchor, rightEyeAnchor) {}

    // implements AirXRTrackingModel
    protected override void OnUpdateEyePose(AirXRClientConfig config, Vector3 centerEyePosition, Quaternion centerEyeOrientation) {
        centerEyeAnchor.localRotation = leftEyeAnchor.localRotation = rightEyeAnchor.localRotation = centerEyeOrientation;
    }

    protected override void OnUpdateEyePose(AirXRClientConfig config, Pose leftEyePose, Pose rightEyePose) {
        OnUpdateEyePose(config, leftEyePose.position, leftEyePose.rotation);
    }
}
