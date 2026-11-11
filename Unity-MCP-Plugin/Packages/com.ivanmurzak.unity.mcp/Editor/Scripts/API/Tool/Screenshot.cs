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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_Screenshot
    {
        private const int MaxDimension = 16384;

        // Encoded screenshots travel back to the AI as base64 PNG inside a JSON envelope over
        // the MCP transport, which caps a single message (MaximumReceiveMessageSize, 128 MB).
        // A capture whose pixel dimensions are too large produces a PNG that exceeds that cap
        // and is silently dropped in transit — the image never reaches the chat. Every
        // screenshot tool keeps its longest output edge at or below this value so the payload
        // stays transportable.
        //
        // Worst case (incompressible content) the message is ~ D*D*4 bytes for a D-px square, so
        // the 128 MB cap allows up to ~5790 px. 3840 (true 4K) leaves comfortable headroom
        // (~59 MB worst case) while staying well clear of that ceiling. Going higher has little
        // value: Claude's vision downsamples images to ~1568 px on the longest edge before the
        // model sees them, so extra resolution past ~4K only inflates the payload.
        private const int MaxScreenshotDimension = 3840;

        // `-nographics` (no graphics device) renders nothing yet raises no error: every capture would come back as a
        // blank gray image that looks like a valid screenshot. Fail loudly instead.
        // The device never changes during a session; read once on the main thread (tools may run off it).
        // volatile: written on the main thread, read on tool threads. internal: tests force it to cover the guard
        // in editors that do have a device.
        internal static volatile bool NoGraphicsDevice;

        [InitializeOnLoadMethod]
        private static void ReadGraphicsDevice() => NoGraphicsDevice = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private static ResponseCallTool? NoGraphicsError() =>
            NoGraphicsDevice
                ? ResponseCallTool.Error("Screenshots need a graphics device, but the editor has none " +
                    "(GraphicsDeviceType.Null, e.g. started with -nographics). " +
                    "Start it with a graphics device: drop -nographics (headless -batchmode keeps rendering).")
                : null;

        /// <summary>
        /// Every pixel is black: nothing drew into the view (a Game View that no camera renders to, e.g. in an editor
        /// started headless with no scene open), which Unity reports with no error. Main thread (reads the texture).
        /// </summary>
        internal static bool IsAllBlack(Texture2D texture)
        {
            foreach (var pixel in texture.GetPixels32())
                if (pixel.r != 0 || pixel.g != 0 || pixel.b != 0)
                    return false;
            return true;
        }

        private static ResponseCallTool BlackFrameError(string toolId, string window) =>
            ResponseCallTool.Error($"{toolId} read back an all-black image: nothing drew into the {window} (no camera " +
                "renders to it, e.g. no scene is open). Open a scene with a camera (`editor | call OpenScene(...)`), or " +
                "render a camera with screenshot-camera.");

        // On a cold shader cache (a new project, a fresh editor) the first capture of a URP scene came back as background only:
        // a render that compiles shader variants draws nothing, and so do further renders until the GPU work is read back.
        // Measured on Unity 6000.3 URP: render, render, readback was empty; render, readback, render was right (every time).
        // So a warm-up render, then a 1-pixel readback, compiles what the view needs (synchronously: async compilation would
        // draw placeholders) before the capture render.
        internal static void RenderSync(Camera camera)
        {
            var wasAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            var prevActive = RenderTexture.active;
            var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                camera.Render();
                RenderTexture.active = camera.targetTexture;
                pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, recalculateMipMaps: false);
                camera.Render();
            }
            finally
            {
                RenderTexture.active = prevActive;
                ShaderUtil.allowAsyncCompilation = wasAsync;
                Object.DestroyImmediate(pixel);
            }
        }

        // Scale (width, height) so the longest edge is at most MaxScreenshotDimension, preserving
        // aspect ratio. Returns the input unchanged when it is already within the limit.
        private static (int width, int height) ClampToTransportLimit(int width, int height)
        {
            var longest = Mathf.Max(width, height);
            if (longest <= MaxScreenshotDimension)
                return (width, height);

            var scale = (float)MaxScreenshotDimension / longest;
            return (Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                    Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }
    }
}