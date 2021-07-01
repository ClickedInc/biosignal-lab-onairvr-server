using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public class OCSVRWorksFoveatedRenderer {
    public enum RenderMode : int {
        Mono = 1,   // NV_VRS_RENDER_MODE_MONO
        Left = 2,   // NV_VRS_RENDER_MODE_LEFT_EYE
        Right = 3,  // NV_VRS_RENDER_MODE_RIGHT_EYE
        Stereo = 4  // NV_VRS_RENDER_MODE_STEREO
    }

    public enum ShadingRate : byte {
        X1 = 5,             // NV_PIXEL_X1_PER_RASTER_PIXEL
        X1_PER_2x2 = 8,     // NV_PIXEL_X1_PER_2X2_RASTER_PIXELS
        X1_PER_4x4 = 11,    // NV_PIXEL_X1_PER_4X4_RASTER_PIXELS
        Cull = 0            // NV_PIXEL_X0_CULL_RASTER_PIXELS
    }

    [DllImport(OCSVRWorks.LibName)]
    private extern static IntPtr ocs_VRWorks_EnableFoveatedRendering_RenderEvent();

    [DllImport(OCSVRWorks.LibName)]
    private extern static IntPtr ocs_VRWorks_DisableFoveatedRendering_RenderEvent();

    private Camera _camera;
    private Dictionary<CameraEvent, CommandBuffer> _commands;

    public OCSVRWorksFoveatedRenderer(Camera camera, RenderMode mode, float depth) {
        _camera = camera;
        camera.depth = mode == RenderMode.Right ? depth + 1 : depth;

        createRenderCommands(mode);
    }

    public void Enable() {
        foreach (var evt in _commands.Keys) {
            _camera.AddCommandBuffer(evt, _commands[evt]);
        }
    }

    public void Disable() {
        foreach (var evt in _commands.Keys) {
            _camera.RemoveCommandBuffer(evt, _commands[evt]);
        }
    }

    private void createRenderCommands(RenderMode mode) {
        _commands = new Dictionary<CameraEvent, CommandBuffer>();

        var command = new CommandBuffer();
        command.IssuePluginEvent(ocs_VRWorks_EnableFoveatedRendering_RenderEvent(), (int)mode);
        _commands.Add(CameraEvent.BeforeForwardOpaque, command);

        command = new CommandBuffer();
        command.IssuePluginEvent(ocs_VRWorks_DisableFoveatedRendering_RenderEvent(), 0);
        _commands.Add(CameraEvent.AfterForwardAlpha, command);
    }
}
