/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEditor;

[CustomEditor(typeof(AirViewCameraRig))]

public class AirViewCameraRigEditor : Editor {
    private SerializedProperty _propSendAudio;
    private SerializedProperty _propAudioInput;
    private SerializedProperty _propTargetAudioMixer;
    private SerializedProperty _propExposedRendererIDParameterName;

    private void OnEnable() {
        _propSendAudio = serializedObject.FindProperty("_sendAudio");
        _propAudioInput = serializedObject.FindProperty("_audioInput");
        _propTargetAudioMixer = serializedObject.FindProperty("_targetAudioMixer");
        _propExposedRendererIDParameterName = serializedObject.FindProperty("_exposedRendererIDParameterName");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_propSendAudio);
        if (_propSendAudio.boolValue) {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.PropertyField(_propAudioInput);
            if (_propAudioInput.enumValueIndex == (int)AirXRServerAudioOutputRouter.Input.AudioPlugin) {
                EditorGUILayout.PropertyField(_propTargetAudioMixer);
                EditorGUILayout.PropertyField(_propExposedRendererIDParameterName);
            }
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
