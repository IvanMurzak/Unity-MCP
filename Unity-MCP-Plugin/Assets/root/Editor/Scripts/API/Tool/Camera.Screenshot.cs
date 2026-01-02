/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Camera
    {
        [McpPluginTool
        (
            "Camera_Screenshot",
            Title = "Capture camera view as image"
        )]
        [Description(@"Captures a screenshot from a camera and returns it as an image.
If no camera is specified, uses the Main Camera.
Returns the image directly for visual inspection by the LLM.")]
        public ResponseCallTool Screenshot
        (
            [Description("Name of the camera GameObject. If not specified, uses the Main Camera.")]
            string? cameraName = null,
            [Description("Width of the screenshot in pixels.")]
            int width = 1920,
            [Description("Height of the screenshot in pixels.")]
            int height = 1080
        )
        => MainThread.Instance.Run(() =>
        {
            Camera? camera = null;

            if (string.IsNullOrEmpty(cameraName))
            {
                camera = Camera.main;
                if (camera == null)
                {
                    var allCameras = Camera.allCameras;
                    if (allCameras.Length > 0)
                        camera = allCameras[0];
                }
            }
            else
            {
                var go = GameObject.Find(cameraName);
                if (go != null)
                    camera = go.GetComponent<Camera>();

                if (camera == null)
                {
                    camera = Camera.allCameras.FirstOrDefault(c => c.name == cameraName);
                }
            }

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
            Object.DestroyImmediate(rt);

            var pngBytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            return ResponseCallTool.Image(pngBytes, com.IvanMurzak.McpPlugin.Common.Consts.MimeType.ImagePng,
                $"Screenshot from camera '{camera.name}' ({width}x{height})");
        });
    }
}
