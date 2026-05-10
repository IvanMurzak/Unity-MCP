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
#if UNITY_6000_5_OR_NEWER
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using AIGD;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ScreenshotIsolatedTests : BaseTest
    {
        static GameObject CreateTargetCube(string name = "IsolatedTarget")
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            return go;
        }

        [Test]
        public void ScreenshotIsolated_NullGameObjectRef_ReturnsError()
        {
            var result = new Tool_Screenshot().ScreenshotIsolated(gameObjectRef: null);
            Assert.IsNotNull(result);
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Should error on null gameObjectRef");
        }

        [Test]
        public void ScreenshotIsolated_UnresolvableRef_ReturnsError()
        {
            var orphanRef = new GameObjectRef { Name = "__never_exists_in_scene__" };
            var result = new Tool_Screenshot().ScreenshotIsolated(gameObjectRef: orphanRef);
            Assert.IsNotNull(result);
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Should error when target cannot be resolved");
        }

        [Test]
        public void ScreenshotIsolated_NoRenderers_ReturnsError()
        {
            var go = new GameObject("EmptyTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };
            var result = new Tool_Screenshot().ScreenshotIsolated(gameObjectRef: goRef, includeChildren: false);
            Assert.IsNotNull(result);
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Should error when target has no renderers");
        }

        [Test]
        public void ScreenshotIsolated_DefaultIsolated_RestoresLayer()
        {
            var go = CreateTargetCube("LayerRestoreTarget");
            const int OriginalLayer = 5;
            go.layer = OriginalLayer;
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                resolution: 64);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Should succeed with a primitive target");
            Assert.AreEqual(OriginalLayer, go.layer, "Original layer must be restored after isolated screenshot");
        }

        [Test]
        public void ScreenshotIsolated_RestoresInactiveSelf()
        {
            var go = CreateTargetCube("InactiveTarget");
            go.SetActive(false);
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                resolution: 64);

            Assert.IsNotNull(result);
            Assert.IsFalse(go.activeSelf, "activeSelf must be restored to false after the capture");
        }

        [Test]
        public void ScreenshotIsolated_NotIsolated_DoesNotChangeLayer()
        {
            var go = CreateTargetCube("NotIsolatedTarget");
            const int OriginalLayer = 7;
            go.layer = OriginalLayer;
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                isolated: false,
                resolution: 64);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status);
            Assert.AreEqual(OriginalLayer, go.layer, "Layer must remain untouched when isolated=false");
        }

        [Test]
        public void ScreenshotIsolated_AllSingleViews_Succeed()
        {
            var go = CreateTargetCube("ViewSweepTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var views = new[]
            {
                Tool_Screenshot.CameraView.Front,
                Tool_Screenshot.CameraView.Back,
                Tool_Screenshot.CameraView.Left,
                Tool_Screenshot.CameraView.Right,
                Tool_Screenshot.CameraView.Top,
                Tool_Screenshot.CameraView.Bottom
            };

            foreach (var view in views)
            {
                var result = new Tool_Screenshot().ScreenshotIsolated(
                    gameObjectRef: goRef,
                    cameraView: view,
                    resolution: 32);
                Assert.IsNotNull(result, $"View {view} returned null");
                Assert.AreNotEqual(ResponseStatus.Error, result.Status, $"View {view} should succeed");
            }
        }

        [Test]
        public void ScreenshotIsolated_CompositeView_Succeeds()
        {
            var go = CreateTargetCube("CompositeTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                cameraView: Tool_Screenshot.CameraView.Composite,
                resolution: 32);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Composite view should succeed");
        }

        [Test]
        public void ScreenshotIsolated_AllBackgroundModes_Succeed()
        {
            var go = CreateTargetCube("BackgroundSweepTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var modes = new[]
            {
                Tool_Screenshot.BackgroundMode.SolidColor,
                Tool_Screenshot.BackgroundMode.Skybox,
                Tool_Screenshot.BackgroundMode.Transparent
            };

            foreach (var mode in modes)
            {
                var result = new Tool_Screenshot().ScreenshotIsolated(
                    gameObjectRef: goRef,
                    backgroundMode: mode,
                    resolution: 32);
                Assert.IsNotNull(result, $"Background {mode} returned null");
                Assert.AreNotEqual(ResponseStatus.Error, result.Status, $"Background {mode} should succeed");
            }
        }

        [Test]
        public void ScreenshotIsolated_CustomLightsJson_Parsed()
        {
            var go = CreateTargetCube("LightedTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            const string lightsJson = @"[
                {""type"":""Directional"",""color"":""#FFF4E5"",""intensity"":1.2,""rotation"":[45,-45,0]},
                {""type"":""Point"",""color"":""#4466FF"",""intensity"":0.5,""position"":[-3,2,2],""range"":15}
            ]";

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                lights: lightsJson,
                resolution: 32);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(ResponseStatus.Error, result.Status, "Custom multi-light JSON should be accepted");
        }

        [Test]
        public void ScreenshotIsolated_InvalidLightsJson_ReturnsError()
        {
            var go = CreateTargetCube("BadLightsTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                lights: "{ this is not json",
                resolution: 32);

            Assert.IsNotNull(result);
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Invalid JSON should produce an error response");
        }

        [Test]
        public void ScreenshotIsolated_InvalidBackgroundColor_ReturnsError()
        {
            var go = CreateTargetCube("BadColorTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                backgroundColor: "not-a-color",
                resolution: 32);

            Assert.IsNotNull(result);
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Invalid background color hex should error");
        }

        [Test]
        public void ScreenshotIsolated_OutOfRangeResolution_ReturnsError()
        {
            var go = CreateTargetCube("BadResolutionTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var result = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                resolution: 0);
            Assert.AreEqual(ResponseStatus.Error, result.Status);

            var result2 = new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                resolution: 100000);
            Assert.AreEqual(ResponseStatus.Error, result2.Status);
        }

        [Test]
        public void ScreenshotIsolated_NoTemporaryGameObjectsLeftBehind()
        {
            var go = CreateTargetCube("CleanupTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var beforeCount = Object.FindObjectsByType<Camera>().Length;

            new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                resolution: 32);

            var afterCount = Object.FindObjectsByType<Camera>().Length;
            Assert.AreEqual(beforeCount, afterCount, "All temporary cameras must be destroyed in finally");

            var leftoverLights = new List<Light>(Object.FindObjectsByType<Light>()).FindAll(l =>
                l != null && l.name != null && l.name.StartsWith("__TempIsolation"));
            Assert.AreEqual(0, leftoverLights.Count, "All temporary lights must be destroyed in finally");
        }

        [Test]
        public void ScreenshotIsolated_Composite_NoTemporaryGameObjectsLeftBehind()
        {
            var go = CreateTargetCube("CompositeCleanupTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var beforeCameras = Object.FindObjectsByType<Camera>().Length;
            var beforeLights = Object.FindObjectsByType<Light>().Length;

            new Tool_Screenshot().ScreenshotIsolated(
                gameObjectRef: goRef,
                cameraView: Tool_Screenshot.CameraView.Composite,
                resolution: 32);

            var afterCameras = Object.FindObjectsByType<Camera>().Length;
            Assert.AreEqual(beforeCameras, afterCameras, "All composite-quadrant temporary cameras must be destroyed in finally");

            var leftoverIsolation = new List<GameObject>();
            foreach (var camera in Object.FindObjectsByType<Camera>())
            {
                if (camera != null && camera.name != null && camera.name.StartsWith("__TempIsolation"))
                    leftoverIsolation.Add(camera.gameObject);
            }
            Assert.AreEqual(0, leftoverIsolation.Count, "No __TempIsolationCamera GameObjects must survive a composite render");

            var afterLights = Object.FindObjectsByType<Light>().Length;
            Assert.AreEqual(beforeLights, afterLights, "All composite-quadrant temporary lights must be destroyed in finally");
        }

        [Test]
        public void ScreenshotIsolated_Composite_NoRenderTextureReleaseWarning()
        {
            // Regression for: per-quadrant RenderTexture was being destroyed in the inner
            // finally while the temp camera still had it as `targetTexture`, producing
            // "Releasing render texture that is set as Camera.targetTexture!" warnings.
            var go = CreateTargetCube("CompositeNoWarningTarget");
            var goRef = new GameObjectRef { InstanceID = go.GetEntityId() };

            var capturedWarnings = new List<string>();
            Application.LogCallback handler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Warning && condition.Contains("Releasing render texture"))
                    capturedWarnings.Add(condition);
            };

            Application.logMessageReceived += handler;
            try
            {
                new Tool_Screenshot().ScreenshotIsolated(
                    gameObjectRef: goRef,
                    cameraView: Tool_Screenshot.CameraView.Composite,
                    resolution: 32);
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }

            Assert.AreEqual(0, capturedWarnings.Count,
                "Composite render must not emit 'Releasing render texture that is set as Camera.targetTexture!' warnings. "
                + "Captured: " + string.Join("; ", capturedWarnings));
        }
    }
}
#endif
