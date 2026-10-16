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
        [AiTool
        (
            ScreenshotGameViewToolId,
            Title = "Screenshot / Game View",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Capture a screenshot of the Unity Editor's Game View by reading its internal " +
            "render texture directly. Image size matches the current Game View resolution; the tool corrects Y-flip on " +
            "DirectX / Metal so the output is always upright. Requires an open Game View window.")]
        [AiSkillBody("Captures a screenshot from the Unity Editor Game View and returns it as an image. " +
            "Reads the Game View's own render texture directly via the Unity Editor API. " +
            "The image size matches the current Game View resolution. " +
            "Returns the image directly for visual inspection by the LLM.\n\n" +
            "## Behavior\n\n" +
            "Locates `UnityEditor.GameView`, repaints it, then reflects the `m_RenderTexture` field and reads back via " +
            "`Texture2D.ReadPixels`. On graphics APIs whose UV origin is top-left (`SystemInfo.graphicsUVStartsAtTop`), " +
            "the read-back pixels are vertically flipped before encoding so the orientation matches what the user sees. " +
            "Returns a PNG image with the resolution baked into the caption.")]
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
                //
                // The Game View renders at the live resolution, which on high-DPI / 4K
                // displays can be large enough that the encoded PNG exceeds the MCP transport's
                // message-size limit — the result is then dropped in transit and never reaches
                // the chat. Clamp the longest edge to `MaxScreenshotDimension`, downscaling on the
                // GPU (an RT→RT blit preserves pixel orientation, so the flip compensation below
                // stays correct), so the payload stays comparable to the other screenshot tools.
                var srcWidth = sourceRt.width;
                var srcHeight = sourceRt.height;

                var scale = Mathf.Min(1f, (float)MaxScreenshotDimension / Mathf.Max(srcWidth, srcHeight));
                var width = Mathf.Max(1, Mathf.RoundToInt(srcWidth * scale));
                var height = Mathf.Max(1, Mathf.RoundToInt(srcHeight * scale));

                var prevActive = RenderTexture.active;
                RenderTexture? scaledRt = null;
                Texture2D? tex = null;
                byte[]? pngBytes = null;
                try
                {
                    // When the Game View is within the limit (scale == 1) the read path is
                    // byte-identical to a direct read of the source RT; otherwise we read from a
                    // downscaled copy.
                    var readSource = sourceRt;
                    if (scale < 1f)
                    {
                        scaledRt = RenderTexture.GetTemporary(width, height, 0, sourceRt.format);
                        Graphics.Blit(sourceRt, scaledRt);
                        readSource = scaledRt;
                    }

                    RenderTexture.active = readSource;
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
                    if (scaledRt != null)
                        RenderTexture.ReleaseTemporary(scaledRt);
                    if (tex != null)
                        UnityEngine.Object.DestroyImmediate(tex);
                }

                return ResponseCallTool.Image(pngBytes, McpPlugin.Common.Consts.MimeType.ImagePng,
                    $"Screenshot from Game View ({width}x{height})");
            });
        }
    }
}
