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
        [McpPluginTool
        (
            "screenshot-game-view",
            Title = "Screenshot / Game View"
        )]
        [Description("Captures a screenshot from the Unity Editor Game View and returns it as an image. " +
            "Reads the Game View's own render texture directly via the Unity Editor API. " +
            "The image size matches the current Game View resolution. " +
            "Returns the image directly for visual inspection by the LLM.")]
        public ResponseCallTool ScreenshotGameView()
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
                var rt = rtField?.GetValue(gameView) as RenderTexture;

                if (rt == null || !rt.IsCreated())
                    return ResponseCallTool.Error("Game View render texture is not available. " +
                        "Ensure the Game View window is open and visible.");

                var prevActive = RenderTexture.active;
                Texture2D? tex = null;
                byte[]? pngBytes = null;
                try
                {
                    RenderTexture.active = rt;
                    tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
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
                    $"Screenshot from Game View ({rt.width}x{rt.height})");
            });
        }
    }
}
