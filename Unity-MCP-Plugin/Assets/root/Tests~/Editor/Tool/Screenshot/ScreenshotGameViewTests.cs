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
using System.Reflection;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ScreenshotGameViewTests : BaseTest
    {
        // -----------------------------------------------------------------------
        // Helpers — mirror the exact checks the tool performs internally
        // -----------------------------------------------------------------------

        private static bool GameViewTypeAvailable =>
            System.Type.GetType("UnityEditor.GameView,UnityEditor") != null;

        private static bool GameViewRenderTextureReady
        {
            get
            {
                var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null) return false;

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView == null) return false;

                var rtField = gameViewType.GetField("m_RenderTexture",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var rt = rtField?.GetValue(gameView) as RenderTexture;
                return rt != null && rt.IsCreated();
            }
        }

        // -----------------------------------------------------------------------
        // Safety — the tool must never throw
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var result = new Tool_Screenshot().ScreenshotGameView();
                Assert.IsNotNull(result);
            });
        }

        // -----------------------------------------------------------------------
        // Always-runs: result is either a success or one of the known error messages
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_ReturnsKnownResponseType()
        {
            var result = new Tool_Screenshot().ScreenshotGameView();

            Assert.IsNotNull(result, "Result must never be null");

            if (result.Status == ResponseStatus.Error)
            {
                // One of the three documented error messages must appear in the JSON
                LogAssert.Expect(LogType.Error, new Regex("GameView type not found|No Game View window|Game View render texture is not available"));
                var raw = RunToolRaw("screenshot-game-view", "{}");
                Assert.IsTrue(
                    raw.Contains("GameView type not found") ||
                    raw.Contains("No Game View window") ||
                    raw.Contains("Game View render texture is not available"),
                    $"Error response must contain a known message. Actual JSON:\n{raw}");
            }
        }

        // -----------------------------------------------------------------------
        // GameView type must always be resolvable inside the Unity Editor
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_GameViewTypeFound_InEditorContext()
        {
            // UnityEditor.GameView is always present when running inside the Editor.
            Assert.IsTrue(GameViewTypeAvailable,
                "UnityEditor.GameView type must be resolvable in an Editor test context");
        }

        // -----------------------------------------------------------------------
        // Happy path — only exercised when the Game View RT is ready
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_WhenGameViewReady_ReturnsImage()
        {
            if (!GameViewRenderTextureReady)
            {
                Assert.Ignore("Game View render texture is not ready; skipping happy-path test.");
                return;
            }

            var result = new Tool_Screenshot().ScreenshotGameView();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should return a non-error response when the Game View render texture is available");
        }

        // -----------------------------------------------------------------------
        // Error path — render texture unavailable
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_WhenRenderTextureNotReady_ReturnsSpecificError()
        {
            if (GameViewRenderTextureReady)
            {
                Assert.Ignore("Game View render texture is ready; skipping render-texture-unavailable test.");
                return;
            }

            var result = new Tool_Screenshot().ScreenshotGameView();

            Assert.IsNotNull(result);
            Assert.AreEqual(ResponseStatus.Error, result.Status,
                "Should return an error when the render texture is not available");
        }

        // -----------------------------------------------------------------------
        // Full MCP framework path
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_ViaRunTool_WhenGameViewReady_Succeeds()
        {
            if (!GameViewRenderTextureReady)
            {
                Assert.Ignore("Game View render texture is not ready; skipping framework happy-path test.");
                return;
            }

            // Method takes no parameters; an empty JSON object is the correct input.
            RunTool("screenshot-game-view", "{}");
        }

        [Test]
        public void ScreenshotGameView_ViaRunTool_WhenRenderTextureNotReady_ReturnsErrorJson()
        {
            if (GameViewRenderTextureReady)
            {
                Assert.Ignore("Game View render texture is ready; skipping framework error-path test.");
                return;
            }

            LogAssert.Expect(LogType.Error, new Regex("No Game View window|Game View render texture is not available"));
            var raw = RunToolRaw("screenshot-game-view", "{}");

            Assert.IsNotNull(raw, "Raw result must not be null");
            Assert.IsTrue(
                raw.Contains("No Game View window") ||
                raw.Contains("Game View render texture is not available"),
                $"Expected a known error message. Actual JSON:\n{raw}");
        }
    }
}
