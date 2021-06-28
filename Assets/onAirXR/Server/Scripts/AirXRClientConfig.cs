/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;
using System;

public enum AirXRClientType {
    Monoscopic,
    Stereoscopic
}

[Serializable]
public class AirXRClientConfig {
    public struct PhysicalCameraProps {
        public Vector2 sensorSize;
        public float focalLength;
        public Vector2 lensShift;

        public Vector2 leftLensShift {
            get {
                return lensShift;
            }
        }

        public Vector2 rightLensShift {
            get {
                var result = lensShift;
                result.x = -result.x;

                return result;
            }
        }

        public float aspect {
            get {
                return sensorSize.x / sensorSize.y;
            }
        }
    }

    internal static Matrix4x4 CalcCameraProjectionMatrix(float[] projection, float near, float far) {
        float left = projection[0] * near;
        float top = projection[1] * near;
        float right = projection[2] * near;
        float bottom = projection[3] * near;

        Matrix4x4 result = Matrix4x4.zero;
        result[0, 0] = 2 * near / (right - left);
        result[1, 1] = 2 * near / (top - bottom);
        result[0, 2] = (right + left) / (right - left);
        result[1, 2] = (top + bottom) / (top - bottom);
        result[2, 2] = (near + far) / (near - far);
        result[2, 3] = 2 * near * far / (near - far);
        result[3, 2] = -1.0f;

        return result;
    }

    internal static Matrix4x4 FlipCameraProjectionMatrixHorizontally(Matrix4x4 matrix) {
        matrix[0, 2] *= -1.0f;
        return matrix;
    }

    internal static PhysicalCameraProps CalcPhysicalCameraProps(float[] projection) {
        var result = new PhysicalCameraProps();
        result.sensorSize = new Vector2(projection[2] - projection[0],
                                        projection[1] - projection[3]);
        result.focalLength = 1;
        result.lensShift = new Vector2((projection[2] + projection[0]) / 2 / (projection[2] - projection[0]),
                                       (projection[1] + projection[3]) / 2 / (projection[1] - projection[3]));
        return result;
    }

    public static AirXRClientConfig Get(int playerID) {
        string json = "";
        if (AXRServerPlugin.GetConfig(playerID, ref json)) {
            return JsonUtility.FromJson<AirXRClientConfig>(json);
        }
        return null;
    }

    public static void Set(int playerID, AirXRClientConfig config) {
        AXRServerPlugin.SetConfig(playerID, JsonUtility.ToJson(config));
    }

    public AirXRClientConfig() {
        CameraProjection = new float[4];
    }

    [SerializeField] protected string UserID;
    [SerializeField] protected bool Stereoscopy;
    [SerializeField] protected int VideoWidth;
    [SerializeField] protected int VideoHeight;
    [SerializeField] protected float[] CameraProjection;
    [SerializeField] protected float FrameRate;
    [SerializeField] protected float InterpupillaryDistance;
    [SerializeField] protected Vector3 EyeCenterPosition;

    [SerializeField] protected int EyeTextureWidth;
    [SerializeField] protected int EyeTextureHeight;
    [SerializeField] protected string ProfileReportEndpoint;
    [SerializeField] protected string MotionOutputEndpoint;

    internal Matrix4x4 GetCameraProjectionMatrix(float near, float far) {
        if (isCameraProjectionValid(CameraProjection) == false) { return Matrix4x4.zero; }

        return CalcCameraProjectionMatrix(CameraProjection, near, far);
    }

    internal Matrix4x4 GetLeftEyeCameraProjection(float near, float far) {
        return GetCameraProjectionMatrix(near, far);
    }

    internal Matrix4x4 GetRightEyeCameraProjection(float near, float far) {
        Matrix4x4 result = GetCameraProjectionMatrix(near, far);
        if (result != Matrix4x4.zero) {
            result[0, 2] *= -1.0f;
        }
        return result;
    }

    internal (float width, float height) GetEncodingProjectionSize() {
        var widthScale = videoWidth / 2.0f / eyeTextureWidth;
        var heightScale = videoHeight / eyeTextureHeight;

        return ((CameraProjection[2] - CameraProjection[0]) * widthScale,
                (CameraProjection[1] - CameraProjection[3]) * heightScale);
    }

    public AirXRClientType type => Stereoscopy ? AirXRClientType.Stereoscopic : AirXRClientType.Monoscopic;
    public int videoWidth => VideoWidth;
    public int videoHeight => VideoHeight;
    public float framerate => FrameRate;
    public float[] cameraProjection => CameraProjection;

    public float fov {
        get {
            float tAngle = Mathf.Atan(Mathf.Abs(CameraProjection[1]));
            float bAngle = Mathf.Atan(Mathf.Abs(CameraProjection[3]));
            return Mathf.Rad2Deg * (tAngle * Mathf.Sign(CameraProjection[1]) - bAngle * Mathf.Sign(CameraProjection[3]));
        }
    }

    public Vector3 eyeCenterPosition => EyeCenterPosition;
    public float ipd => InterpupillaryDistance;
    public string userID => UserID;
    public PhysicalCameraProps physicalCameraProps => CalcPhysicalCameraProps(CameraProjection);

    public int eyeTextureWidth => EyeTextureWidth;
    public int eyeTextureHeight => EyeTextureHeight;
    public string profileReportEndpoint => ProfileReportEndpoint;
    public string motionOutputEndpoint => MotionOutputEndpoint;

    private bool isCameraProjectionValid(float[] projection) {
        return projection[2] - projection[0] > 0 && projection[1] - projection[3] > 0;
    }
}
