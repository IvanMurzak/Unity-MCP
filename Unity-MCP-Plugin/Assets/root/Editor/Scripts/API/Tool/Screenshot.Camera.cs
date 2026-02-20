/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Screenshot
    {
        [McpPluginTool
        (
            "screenshot-camera",
            Title = "Screenshot / Camera"
        )]
        [Description("Captures a screenshot from a camera and returns it as an image. " +
            "If no camera is specified, uses the Main Camera. " +
            "Returns the image directly for visual inspection by the LLM.")]
        public ResponseCallTool ScreenshotCamera
        (
            [Description("Reference to the camera GameObject. If not specified, uses the Main Camera.")]
            GameObjectRef? cameraRef = null,
            [Description("Width of the screenshot in pixels.")]
            int width = 1920,
            [Description("Height of the screenshot in pixels.")]
            int height = 1080
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var camera = cameraRef?.FindGameObject()?.GetComponent<Camera>()
                    ?? Camera.main
                    ?? Camera.allCameras.FirstOrDefault();

                if (camera == null)
                    throw new Exception("No camera found in the scene to capture the screenshot.");

                if (camera == null)
                {
                    var availableCameras = Camera.allCameras.Select(c => c.name).ToArray();
                    var msg = availableCameras.Length > 0
                        ? $"Camera not found. Available cameras: {string.Join(", ", availableCameras)}"
                        : "No cameras found in the scene.";
                    return ResponseCallTool.Error(msg);
                }

                var rt = new RenderTexture(width, height, 24);
                var prevTarget = camera.targetTexture;

                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                camera.targetTexture = prevTarget;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                var pngBytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                return ResponseCallTool.Image(pngBytes, McpPlugin.Common.Consts.MimeType.ImagePng,
                    $"Screenshot from camera '{camera.name}' ({width}x{height})");
            });
        }
    }
}