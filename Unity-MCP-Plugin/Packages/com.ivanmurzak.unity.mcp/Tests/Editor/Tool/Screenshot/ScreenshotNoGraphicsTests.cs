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
using System.Linq;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Under <c>-nographics</c> nothing renders and Unity raises no error, so a capture would be a blank gray image.
    /// Every screenshot tool must report that instead. Only meaningful in an editor started with <c>-nographics</c>.
    /// </summary>
    public class ScreenshotNoGraphicsTests : BaseTest
    {
        static readonly (string name, Func<ResponseCallTool> capture)[] Tools =
        {
            ("screenshot-camera", () => new Tool_Screenshot().ScreenshotCamera(width: 64, height: 64)),
            ("screenshot-game-view", () => new Tool_Screenshot().ScreenshotGameView()),
            ("screenshot-scene-view", () => new Tool_Screenshot().ScreenshotSceneView(width: 64, height: 64)),
            ("screenshot-isolated", () => new Tool_Screenshot().ScreenshotIsolated(gameObjectRef: null)),
        };

        [Test]
        public void ScreenshotTools_WithoutGraphicsDevice_ReportIt()
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                Assert.Ignore("The editor has a graphics device; run with -nographics to exercise this.");

            foreach (var (name, capture) in Tools)
            {
                var result = capture();
                Assert.AreEqual(ResponseStatus.Error, result.Status, name);
                StringAssert.Contains("-nographics", string.Join("\n", result.Content.Select(c => c.Text)), name);
            }
        }
    }
}
