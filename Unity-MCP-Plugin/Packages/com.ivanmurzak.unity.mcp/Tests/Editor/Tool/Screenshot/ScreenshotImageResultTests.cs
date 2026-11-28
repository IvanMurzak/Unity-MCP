/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// What the agent receives from a screenshot tool called through the tool manager, in each data layer: an MCP
    /// <c>image</c> content block with a PNG, not text. Both layers run in every suite run (the binding is switched here),
    /// so the RQL layer cannot drop the image without the Json layer noticing, or the reverse.
    /// </summary>
    [RequiresGraphicsDevice]
    public class ScreenshotImageResultTests : BaseTest
    {
        const string Target = "ImageResultCube";

        IToolBinding? _savedBinding;

        static IToolManager Tools => UnityMcpPluginEditor.Instance.Tools!;

        [SetUp]
        public void CreateScene()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            _savedBinding = Tools.ToolBinding;

            var camera = new GameObject("ImageResultCamera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -5);
            var light = new GameObject("ImageResultLight").AddComponent<Light>();
            light.type = LightType.Directional;
            GameObject.CreatePrimitive(PrimitiveType.Cube).name = Target;
        }

        [TearDown]
        public void RestoreBinding() => Tools.ToolBinding = _savedBinding;

        [TestCase("Json")]
        [TestCase("Rql")]
        public void ScreenshotCamera_ReturnsAPngImageBlock(string layer) =>
            AssertPngImage(Call(layer, Tool_Screenshot.ScreenshotCameraToolId, @"{ ""width"": 64, ""height"": 48 }"));

        [TestCase("Json")]
        [TestCase("Rql")]
        public void ScreenshotIsolated_ReturnsAPngImageBlock(string layer)
        {
            // A GameObjectRef in each layer's own argument format: JSON in the Json layer, RQL text in the RQL one.
            var target = layer == "Rql"
                ? $@"""unity.find(\""{Target}\"")"""
                : $@"{{ ""name"": ""{Target}"" }}";
            AssertPngImage(Call(layer, Tool_Screenshot.ScreenshotIsolatedToolId, $@"{{ ""gameObjectRef"": {target}, ""resolution"": 64 }}"));
        }

        ResponseCallTool Call(string layer, string tool, string json)
        {
            // Apply prebuilds the RQL signatures on this (main) thread, as the plugin does, before the binding is installed.
            RqlToolBinding.Apply(Tools, layer == "Rql" ? ToolPresentationFormat.Rql : ToolPresentationFormat.Json, RqlToolBinding.Instance.Escaping);
            return RunTool(tool, json).Value!;
        }

        static void AssertPngImage(ResponseCallTool result) =>
            CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, ToolResults.PngImage(result).Take(4).ToArray(), "the image data is a PNG");
    }
}
