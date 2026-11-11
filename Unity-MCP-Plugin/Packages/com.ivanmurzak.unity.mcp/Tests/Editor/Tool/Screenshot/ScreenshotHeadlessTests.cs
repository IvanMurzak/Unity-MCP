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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// A headless (-batchmode) editor never draws its editor windows and Unity raises no error: a Game View capture is
    /// black and a Scene View's camera is never placed at its view. screenshot-game-view and screenshot-scene-view must
    /// say so instead of returning such an image. Batch mode is simulated so this runs in every editor.
    /// </summary>
    public class ScreenshotHeadlessTests : BaseTest
    {
        bool _savedBatchMode;
        bool _savedNoGraphicsDevice;

        // A graphics device is simulated too: CI runs this under -nographics, whose error would answer first.
        [SetUp]
        public void SimulateBatchMode()
        {
            _savedBatchMode = Tool_Screenshot.BatchMode;
            _savedNoGraphicsDevice = Tool_Screenshot.NoGraphicsDevice;
            Tool_Screenshot.BatchMode = true;
            Tool_Screenshot.NoGraphicsDevice = false;
        }

        [TearDown]
        public void RestoreBatchMode()
        {
            Tool_Screenshot.BatchMode = _savedBatchMode;
            Tool_Screenshot.NoGraphicsDevice = _savedNoGraphicsDevice;
        }

        static readonly TestCaseData[] Captures =
        {
            new TestCaseData((Func<ResponseCallTool>)(() => new Tool_Screenshot().ScreenshotGameView())).SetName("ScreenshotGameView_InBatchMode_AsksForAWindowedEditor"),
            new TestCaseData((Func<ResponseCallTool>)(() => new Tool_Screenshot().ScreenshotSceneView(width: 64, height: 64))).SetName("ScreenshotSceneView_InBatchMode_AsksForAWindowedEditor"),
        };

        [TestCaseSource(nameof(Captures))]
        public void Capture_InBatchMode_AsksForAWindowedEditor(Func<ResponseCallTool> capture)
        {
            var result = capture();

            Assert.AreEqual(ResponseStatus.Error, result.Status);
            var text = string.Join("\n", result.Content.Select(c => c.Text));
            StringAssert.Contains("needs a windowed Unity Editor", text);
            StringAssert.Contains("screenshot-camera", text);
        }
    }
}
