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
    /// Every screenshot tool must report that instead.
    /// </summary>
    public class ScreenshotNoGraphicsTests : BaseTest
    {
        static readonly Tool_Screenshot Tool = new Tool_Screenshot();

        static readonly TestCaseData[] Captures =
        {
            new TestCaseData((Func<ResponseCallTool>)(() => Tool.ScreenshotCamera(width: 64, height: 64))).SetName("ScreenshotCamera_WithoutGraphicsDevice_ReportsIt"),
            new TestCaseData((Func<ResponseCallTool>)(() => Tool.ScreenshotGameView())).SetName("ScreenshotGameView_WithoutGraphicsDevice_ReportsIt"),
            new TestCaseData((Func<ResponseCallTool>)(() => Tool.ScreenshotSceneView(width: 64, height: 64))).SetName("ScreenshotSceneView_WithoutGraphicsDevice_ReportsIt"),
            new TestCaseData((Func<ResponseCallTool>)(() => Tool.ScreenshotIsolated(gameObjectRef: null))).SetName("ScreenshotIsolated_WithoutGraphicsDevice_ReportsIt"),
        };

        [TestCaseSource(nameof(Captures))]
        public void Capture_WithoutGraphicsDevice_ReportsIt(Func<ResponseCallTool> capture)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                Assert.Ignore("The editor has a graphics device; run with -nographics to exercise this.");

            var result = capture();
            Assert.AreEqual(ResponseStatus.Error, result.Status);
            StringAssert.Contains("-nographics", string.Join("\n", result.Content.Select(c => c.Text)));
        }
    }
}
