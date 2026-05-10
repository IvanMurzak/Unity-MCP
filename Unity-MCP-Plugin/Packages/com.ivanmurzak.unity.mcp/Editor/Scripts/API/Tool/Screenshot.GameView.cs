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
using System.Reflection;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Screenshot
    {
        public const string ScreenshotGameViewToolId = "screenshot-game-view";
        [McpPluginTool
        (
            ScreenshotGameViewToolId,
            Title = "Screenshot / Game View",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [Description("Captures a screenshot from the Unity Editor Game View and returns it as an image. " +
            "Reads the Game View's own render texture directly via the Unity Editor API. " +
            "The image size matches the current Game View resolution. " +
            "Returns the image directly for visual inspection by the LLM.")]
        public ResponseCallTool ScreenshotGameView(string? nothing = null)
        {
            return MainThread.Instance.Run(() =>
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                    return ResponseCallTool.Error("GameView type not found in UnityEditor assembly.");

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null)
                    return ResponseCallTool.Error("No Game View window is open.");

                gameView.Repaint();

                var rtField = gameViewType.GetField("m_RenderTexture",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var sourceRt = rtField?.GetValue(gameView) as RenderTexture;

                if (sourceRt == null || !sourceRt.IsCreated())
                    return ResponseCallTool.Error("Game View render texture is not available. " +
                        "Ensure the Game View window is open and visible.");

                // Read pixels from the GameView's internal render texture, then correct the
                // orientation for graphics APIs that render with the origin at the top-left.
                // Reading directly from the GameView RT produces a vertically flipped image on
                // DirectX/Metal because Unity performs a Y-flip at display-blit time — a step
                // bypassed when ReadPixels runs against the GameView RT directly.
                // `SystemInfo.graphicsUVStartsAtTop` identifies the APIs that need the flip.
                var width = sourceRt.width;
                var height = sourceRt.height;

                var prevActive = RenderTexture.active;
                Texture2D? tex = null;
                byte[]? pngBytes = null;
                try
                {
                    RenderTexture.active = sourceRt;
                    tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);

                    if (SystemInfo.graphicsUVStartsAtTop)
                    {
                        var pixels = tex.GetPixels32();
                        var flipped = new Color32[pixels.Length];
                        for (var y = 0; y < height; y++)
                        {
                            var srcRow = y * width;
                            var dstRow = (height - 1 - y) * width;
                            Array.Copy(pixels, srcRow, flipped, dstRow, width);
                        }
                        tex.SetPixels32(flipped);
                    }

                    tex.Apply();
                    pngBytes = tex.EncodeToPNG();
                }
                finally
                {
                    RenderTexture.active = prevActive;
                    if (tex != null)
                        UnityEngine.Object.DestroyImmediate(tex);
                }

                return ResponseCallTool.Image(pngBytes, McpPlugin.Common.Consts.MimeType.ImagePng,
                    $"Screenshot from Game View ({width}x{height})");
            });
        }
    }
}
