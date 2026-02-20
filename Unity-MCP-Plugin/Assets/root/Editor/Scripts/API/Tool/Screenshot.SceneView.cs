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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Screenshot
    {
        [McpPluginTool
        (
            "screenshot-scene-view",
            Title = "Screenshot / Scene View"
        )]
        [Description("Captures a screenshot from the Unity Editor Scene View and returns it as an image. " +
            "Returns the image directly for visual inspection by the LLM.")]
        public ResponseCallTool ScreenshotSceneView
        (
            [Description("Width of the screenshot in pixels.")]
            int width = 1920,
            [Description("Height of the screenshot in pixels.")]
            int height = 1080
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView == null && SceneView.sceneViews.Count > 0)
                    sceneView = SceneView.sceneViews[0] as SceneView;

                if (sceneView == null)
                    return ResponseCallTool.Error("No Scene View window is open.");

                var sceneCamera = sceneView.camera;
                if (sceneCamera == null)
                    return ResponseCallTool.Error("Scene View camera is not available.");

                var rt = new RenderTexture(width, height, 24);
                var prevTarget = sceneCamera.targetTexture;
                var prevActive = RenderTexture.active;

                sceneCamera.targetTexture = rt;
                sceneCamera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                sceneCamera.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                Object.DestroyImmediate(rt);

                var pngBytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                return ResponseCallTool.Image(pngBytes, McpPlugin.Common.Consts.MimeType.ImagePng,
                    $"Screenshot from Scene View ({width}x{height})");
            });
        }
    }
}
