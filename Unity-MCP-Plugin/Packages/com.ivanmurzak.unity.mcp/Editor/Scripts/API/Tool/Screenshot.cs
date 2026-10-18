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