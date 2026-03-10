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
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ScreenshotSceneViewTests : BaseTest
    {
        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns true when at least one SceneView is available (the tool can
        /// render), false when no SceneView window is open (CI / batch mode).
        /// </summary>
        private static bool SceneViewAvailable =>
            SceneView.lastActiveSceneView != null || SceneView.sceneViews.Count > 0;

        // -----------------------------------------------------------------------
        // No-SceneView error path — always testable
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotSceneView_WhenNoSceneViewOpen_ReturnsSpecificError()
        {
            // This test is only meaningful when no SceneView window is present.
            // In environments where a SceneView IS open the tool succeeds instead,
            // so we skip the assertion and just verify the tool doesn't crash.
            var result = new Tool_Screenshot().ScreenshotSceneView(width: 320, height: 240);

            Assert.IsNotNull(result, "Result should never be null");

            if (!SceneViewAvailable)
            {
                Assert.AreEqual(ResponseStatus.Error, result.Status,
                    "Without a SceneView the tool must return an error response");
            }
            else
            {
                // SceneView is present — tool should succeed
                Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                    "With a SceneView open the tool should return a non-error response");
            }
        }

        // -----------------------------------------------------------------------
        // Happy path — only run when a SceneView is actually available
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotSceneView_WithSceneView_DefaultDimensions_ReturnsImage()
        {
            if (!SceneViewAvailable)
            {
                Assert.Ignore("No SceneView window open; skipping happy-path test.");
                return;
            }

            var result = new Tool_Screenshot().ScreenshotSceneView();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should succeed with default (1920×1080) dimensions when SceneView is available");
        }

        [Test]
        public void ScreenshotSceneView_WithSceneView_CustomDimensions_ReturnsImage()
        {
            if (!SceneViewAvailable)
            {
                Assert.Ignore("No SceneView window open; skipping happy-path test.");
                return;
            }

            var result = new Tool_Screenshot().ScreenshotSceneView(width: 640, height: 480);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should succeed with custom dimensions when SceneView is available");
        }

        [Test]
        public void ScreenshotSceneView_WithSceneView_SmallDimensions_ReturnsImage()
        {
            if (!SceneViewAvailable)
            {
                Assert.Ignore("No SceneView window open; skipping happy-path test.");
                return;
            }

            // Small texture keeps the test fast
            var result = new Tool_Screenshot().ScreenshotSceneView(width: 16, height: 16);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should succeed with small dimensions when SceneView is available");
        }

        // -----------------------------------------------------------------------
        // Full MCP framework path
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotSceneView_ViaRunTool_HandledCorrectly()
        {
            if (!SceneViewAvailable)
            {
                // When no SceneView is open the tool returns ResponseCallTool.Error,
                // which causes RunTool to fail. Use RunToolRaw to verify the error
                // message is the expected one rather than an unexpected crash.
                LogAssert.Expect(LogType.Error, new Regex("No Scene View|Scene View camera"));
                var raw = RunToolRaw("screenshot-scene-view", @"{""width"": 320, ""height"": 240}");

                Assert.IsNotNull(raw, "Raw result should not be null");
                Assert.IsTrue(
                    raw.Contains("No Scene View") || raw.Contains("Scene View camera"),
                    $"Expected 'No Scene View' error message. Actual JSON:\n{raw}");
            }
            else
            {
                // SceneView present — full success path
                RunTool("screenshot-scene-view", @"{""width"": 320, ""height"": 240}");
            }
        }

        [Test]
        public void ScreenshotSceneView_DoesNotThrowException()
        {
            // Regardless of whether a SceneView is open, the tool must never
            // throw — it should return a ResponseCallTool (success or error).
            Assert.DoesNotThrow(() =>
            {
                var result = new Tool_Screenshot().ScreenshotSceneView(width: 16, height: 16);
                Assert.IsNotNull(result);
            });
        }
    }
}
