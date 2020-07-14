using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.FoveatedRendering 
{
    [RequireComponent(typeof(Camera))]
    public class ViveFoveatedCamera : MonoBehaviour {
        private Camera _thisCamera;
        private CommandBufferManager _commandBuffer;

        [SerializeField] private RenderMode _renderMode;

        internal new ViveFoveatedRenderer renderer { private get; set; }

        internal void Enable() {
            if (renderer == null || renderer.initialized == false) { return; }

            if (_thisCamera == null) {
                _thisCamera = GetComponent<Camera>();
            }
            if (_commandBuffer == null) {
                _commandBuffer = new CommandBufferManager();
            }

            _commandBuffer.AppendCommands("Enable Foveated Rendering", CameraEvent.BeforeForwardOpaque,
                                          buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.ENABLE_FOVEATED_RENDERING),
                                          buf => buf.ClearRenderTarget(false, true, Color.black));
            _commandBuffer.AppendCommands("Disable Foveated Rendering", CameraEvent.AfterForwardAlpha,
                                          buf => buf.IssuePluginEvent(ViveFoveatedRenderingAPI.GetRenderEventFunc(), (int)EventID.DISABLE_FOVEATED_RENDERING));

            _commandBuffer.EnableCommands(_thisCamera);
        }

        internal void Disable() {
            if (renderer == null || renderer.initialized == false) { return; }

            _commandBuffer.DisableCommands(_thisCamera);
            _commandBuffer.ClearCommands();
        }

        private void OnPreRender() {
            if (renderer == null || renderer.initialized == false) { return; }

            ViveFoveatedRenderingAPI.SetRenderMode(_renderMode);
        }
    }
}
