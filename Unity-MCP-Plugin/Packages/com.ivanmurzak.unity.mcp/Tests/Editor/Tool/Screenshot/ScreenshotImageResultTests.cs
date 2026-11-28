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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
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
            AssertPngImage(Call(layer, Tool_Screenshot.ScreenshotCameraToolId, new Dictionary<string, object> { ["width"] = 64, ["height"] = 48 }));

        [TestCase("Json")]
        [TestCase("Rql")]
        public void ScreenshotIsolated_ReturnsAPngImageBlock(string layer)
        {
            // A GameObjectRef in each layer's own argument format: JSON in the Json layer, RQL text in the RQL one.
            object target = layer == "Rql" ? $"unity.find(\"{Target}\")" : new Dictionary<string, object> { ["name"] = Target };
            AssertPngImage(Call(layer, Tool_Screenshot.ScreenshotIsolatedToolId, new Dictionary<string, object> { ["gameObjectRef"] = target, ["resolution"] = 64 }));
        }

        ResponseCallTool Call(string layer, string tool, Dictionary<string, object> args)
        {
            Tools.ToolBinding = layer == "Rql" ? RqlToolBinding.Instance : null;
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(args))!;
            var response = Tools.RunCallTool(new RequestCallTool(tool, parsed)).Result;
            Assert.AreNotEqual(ResponseStatus.Error, response.Status, response.Message);
            var result = response.Value!;
            Assert.AreEqual(ResponseStatus.Success, result.Status, string.Join("\n", result.Content.Select(c => c.Text)));
            return result;
        }

        static void AssertPngImage(ResponseCallTool result)
        {
            var image = result.Content.SingleOrDefault(c => c.Type == Consts.ContentType.Image);
            Assert.IsNotNull(image, "an image content block reaches the agent; got: " + string.Join(", ", result.Content.Select(c => c.Type)));
            Assert.AreEqual(Consts.MimeType.ImagePng, image!.MimeType);
            Assert.IsFalse(string.IsNullOrEmpty(image.Data), "the image block carries the PNG");
            var png = Convert.FromBase64String(image.Data!);
            CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4).ToArray(), "the data is a PNG");
        }
    }
}
