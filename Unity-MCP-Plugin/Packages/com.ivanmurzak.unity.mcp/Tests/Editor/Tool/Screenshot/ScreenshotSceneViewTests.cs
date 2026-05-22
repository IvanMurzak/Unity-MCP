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
        // Setup — ensure Scene View window is open and focused before each test
        // -----------------------------------------------------------------------

        [SetUp]
        public void EnsureSceneViewOpen()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0)
                sceneView = SceneView.sceneViews[0] as SceneView;

            if (sceneView == null)
                sceneView = EditorWindow.GetWindow<SceneView>("Scene", true);

            sceneView.Show();
            sceneView.Focus();
            sceneView.Repaint();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns true when at least one SceneView is available (the tool can
        /// render), false when no SceneView window is open (CI / batch mode).
        /// </summary>
        private static bool SceneViewAvailable =>
            SceneView.lastActiveSceneView != null || SceneView.sceneViews.Count > 0;

        private static void CloseSceneView()
        {
            var windows = Resources.FindObjectsOfTypeAll<SceneView>();
            foreach (var window in windows)
                window.Close();
        }

        // -----------------------------------------------------------------------
        // No-SceneView error path — close the window to test error handling
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotSceneView_WhenNoSceneViewOpen_ReturnsSpecificError()
        {
            CloseSceneView();

            var result = new Tool_Screenshot().ScreenshotSceneView(width: 320, height: 240);

            Assert.IsNotNull(result, "Result should never be null");
            Assert.AreEqual(ResponseStatus.Error, result.Status,
                "Without a SceneView the tool must return an error response");
        }

        // -----------------------------------------------------------------------
        // Happy path — Scene View is open and ready
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotSceneView_WithSceneView_DefaultDimensions_ReturnsImage()
        {
            var result = new Tool_Screenshot().ScreenshotSceneView();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should succeed with default (1920×1080) dimensions when SceneView is available");
        }

        [Test]
        public void ScreenshotSceneView_WithSceneView_CustomDimensions_ReturnsImage()
        {
            var result = new Tool_Screenshot().ScreenshotSceneView(width: 640, height: 480);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should succeed with custom dimensions when SceneView is available");
        }

        [Test]
        public void ScreenshotSceneView_WithSceneView_SmallDimensions_ReturnsImage()
        {
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
        public void ScreenshotSceneView_ViaRunTool_WhenSceneViewReady_Succeeds()
        {
            RunTool("screenshot-scene-view", @"{""width"": 320, ""height"": 240}");
        }

        [Test]
        public void ScreenshotSceneView_ViaRunTool_WhenNoSceneView_ReturnsErrorJson()
        {
            CloseSceneView();

            LogAssert.ignoreFailingMessages = true;
            try
            {
                var raw = RunToolRaw("screenshot-scene-view", @"{""width"": 320, ""height"": 240}");

                Assert.IsNotNull(raw, "Raw result should not be null");
                Assert.IsTrue(
                    raw.Contains("No Scene View") || raw.Contains("Scene View camera"),
                    $"Expected 'No Scene View' error message. Actual JSON:\n{raw}");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
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
