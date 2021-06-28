/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the MIT license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEditor;

[CustomEditor(typeof(AirVRCameraRig))]

public class AirVRCameraRigEditor : Editor {
    private SerializedProperty propTrackingModel;
    private SerializedProperty propExternalTrackingOrigin;
    private SerializedProperty propExternalTracker;

    private SerializedProperty propEventSystemResponsive;
    private SerializedProperty propRenderControllersOnClient;
    private SerializedProperty propRaycastGraphic;
    private SerializedProperty propRaycastPhysics;
    private SerializedProperty propPhysicsRaycastEventMask;

    private SerializedProperty propSendAudio;
    private SerializedProperty propAudioInput;
    private SerializedProperty propTargetAudioMixer;
    private SerializedProperty propExposedRendererIDParameterName;

    private void OnEnable() {
        propEventSystemResponsive = serializedObject.FindProperty("_eventSystemResponsive");
        propRenderControllersOnClient = serializedObject.FindProperty("_renderControllersOnClient");
        propRaycastGraphic = serializedObject.FindProperty("_raycastGraphic");
        propRaycastPhysics = serializedObject.FindProperty("_raycastPhysics");
        propPhysicsRaycastEventMask = serializedObject.FindProperty("_physicsRaycastEventMask");

        propSendAudio = serializedObject.FindProperty("_sendAudio");
        propAudioInput = serializedObject.FindProperty("_audioInput");
        propTargetAudioMixer = serializedObject.FindProperty("_targetAudioMixer");
        propExposedRendererIDParameterName = serializedObject.FindProperty("_exposedRendererIDParameterName");

        propTrackingModel = serializedObject.FindProperty("_trackingModel");
        propExternalTrackingOrigin = serializedObject.FindProperty("_externalTrackingOrigin");
        propExternalTracker = serializedObject.FindProperty("_externalTracker");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(propTrackingModel);
        if (propTrackingModel.enumValueIndex == (int)AirVRCameraRig.TrackingModel.ExternalTracker) {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(propExternalTrackingOrigin);
            EditorGUILayout.PropertyField(propExternalTracker);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.PropertyField(propEventSystemResponsive);
        if (propEventSystemResponsive.enumValueIndex != (int)AirVRCameraRig.HandSelect.None) {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.PropertyField(propRenderControllersOnClient);

            EditorGUILayout.PropertyField(propRaycastGraphic);
            EditorGUILayout.PropertyField(propRaycastPhysics);
            if (propRaycastPhysics.boolValue) {
                EditorGUILayout.PropertyField(propPhysicsRaycastEventMask);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.PropertyField(propSendAudio);
        if (propSendAudio.boolValue) {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.PropertyField(propAudioInput);
            if (propAudioInput.enumValueIndex == (int)AirXRServerAudioOutputRouter.Input.AudioPlugin) {
                EditorGUILayout.PropertyField(propTargetAudioMixer);
                EditorGUILayout.PropertyField(propExposedRendererIDParameterName);
            }
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
