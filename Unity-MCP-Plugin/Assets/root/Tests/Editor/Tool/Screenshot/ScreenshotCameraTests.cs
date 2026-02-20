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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ScreenshotCameraTests : BaseTest
    {
        [Test]
        public void ScreenshotCamera_WithCameraRef_ReturnsImage()
        {
            // Arrange
            var go = new GameObject("TestCamera");
            go.AddComponent<Camera>();
            var cameraRef = new GameObjectRef { InstanceID = go.GetInstanceID() };

            // Act
            var result = new Tool_Screenshot().ScreenshotCamera(cameraRef: cameraRef, width: 320, height: 240);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Should succeed when a valid camera is referenced");
        }

        [Test]
        public void ScreenshotCamera_DefaultDimensions_ReturnsImage()
        {
            // Arrange
            var go = new GameObject("DefaultDimsCamera");
            go.AddComponent<Camera>();
            var cameraRef = new GameObjectRef { InstanceID = go.GetInstanceID() };

            // Act — uses default 1920×1080
            var result = new Tool_Screenshot().ScreenshotCamera(cameraRef: cameraRef);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Should succeed with default (1920×1080) dimensions");
        }

        [Test]
        public void ScreenshotCamera_CustomDimensions_ReturnsImage()
        {
            // Arrange
            var go = new GameObject("CustomDimsCamera");
            go.AddComponent<Camera>();
            var cameraRef = new GameObjectRef { InstanceID = go.GetInstanceID() };

            // Act
            var result = new Tool_Screenshot().ScreenshotCamera(cameraRef: cameraRef, width: 640, height: 480);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Should succeed with custom dimensions");
        }

        [Test]
        public void ScreenshotCamera_NullCameraRef_UsesFirstAvailableCamera()
        {
            // Arrange: any camera in the scene — no MainCamera tag needed
            var go = new GameObject("AnyCamera");
            go.AddComponent<Camera>();

            // Act — null ref falls back to Camera.main ?? Camera.allCameras.FirstOrDefault()
            var result = new Tool_Screenshot().ScreenshotCamera(cameraRef: null, width: 320, height: 240);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Should fall back to the first available camera");
        }

        [Test]
        public void ScreenshotCamera_MainCameraTag_UsedWhenNoCameraRefProvided()
        {
            // Arrange: camera tagged "MainCamera" — picked up by Camera.main
            var go = new GameObject("MainCameraObject");
            go.tag = "MainCamera";
            go.AddComponent<Camera>();

            // Act
            var result = new Tool_Screenshot().ScreenshotCamera(cameraRef: null, width: 320, height: 240);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Should use the MainCamera-tagged camera");
        }

        [Test]
        public void ScreenshotCamera_NoCameraInScene_ThrowsException()
        {
            // Arrange: scene is empty (TearDown destroys all GameObjects before each test)

            // Act & Assert
            var ex = Assert.Throws<Exception>(() =>
                new Tool_Screenshot().ScreenshotCamera(width: 320, height: 240));

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex!.Message.Contains("No camera found"),
                $"Exception should mention 'No camera found'. Actual: {ex.Message}");
        }

        [Test]
        public void ScreenshotCamera_ViaRunTool_WithCameraRef_Succeeds()
        {
            // Arrange
            var go = new GameObject("RunToolCamera");
            go.AddComponent<Camera>();

            // Act — exercises the full MCP framework path
            RunTool("screenshot-camera", $@"{{
                ""cameraRef"": {{
                    ""instanceID"": {go.GetInstanceID()}
                }},
                ""width"": 320,
                ""height"": 240
            }}");
        }

        [Test]
        public void ScreenshotCamera_ViaRunTool_SmallestDimensions_Succeeds()
        {
            // Arrange
            var go = new GameObject("SmallCamera");
            go.AddComponent<Camera>();

            // Act — small render texture to keep the test fast
            RunTool("screenshot-camera", $@"{{
                ""cameraRef"": {{
                    ""instanceID"": {go.GetInstanceID()}
                }},
                ""width"": 16,
                ""height"": 16
            }}");
        }
    }
}
