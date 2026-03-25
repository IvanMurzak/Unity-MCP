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
    public class ScreenshotGameViewTests : BaseTest
    {
        // -----------------------------------------------------------------------
        // Setup — ensure Game View window is open and focused before each test
        // -----------------------------------------------------------------------

        [SetUp]
        public void EnsureGameViewOpen()
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                return;

            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
            gameView.Show();
            gameView.Focus();
            gameView.Repaint();
        }

        // -----------------------------------------------------------------------
        // Helpers — mirror the exact checks the tool performs internally
        // -----------------------------------------------------------------------

        private static bool GameViewTypeAvailable =>
            System.Type.GetType("UnityEditor.GameView,UnityEditor") != null;

        private static void CloseGameView()
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            foreach (var window in windows)
            {
                if (window is EditorWindow editorWindow)
                    editorWindow.Close();
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
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should return a non-error response when the Game View is open and focused");
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
        // Happy path — Game View is open and ready
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_WhenGameViewReady_ReturnsImage()
        {
            var result = new Tool_Screenshot().ScreenshotGameView();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status,
                "Should return a non-error response when the Game View render texture is available");
        }

        // -----------------------------------------------------------------------
        // Error path — render texture unavailable (Game View closed)
        // -----------------------------------------------------------------------

        [Test]
        public void ScreenshotGameView_WhenRenderTextureNotReady_ReturnsSpecificError()
        {
            CloseGameView();

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
            // Method takes no parameters; an empty JSON object is the correct input.
            RunTool("screenshot-game-view", "{}");
        }

        [Test]
        public void ScreenshotGameView_ViaRunTool_WhenRenderTextureNotReady_ReturnsErrorJson()
        {
            CloseGameView();

            LogAssert.ignoreFailingMessages = true;
            try
            {
                var raw = RunToolRaw("screenshot-game-view", "{}");

                Assert.IsNotNull(raw, "Raw result must not be null");
                Assert.IsTrue(
                    raw.Contains("No Game View window") ||
                    raw.Contains("Game View render texture is not available"),
                    $"Expected a known error message. Actual JSON:\n{raw}");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
