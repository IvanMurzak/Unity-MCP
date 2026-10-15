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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ToolFormat
{
    public enum PresentationTestMode { Fast, Slow }

    public class PresentationTestItem
    {
        public string? name;
        public int count;
    }

    public class PresentationTestSettings
    {
        public string? title;
        public PresentationTestMode mode;
        public List<PresentationTestItem>? items;
    }

    public static class PresentationTestTools
    {
        public static PresentationTestSettings Echo(PresentationTestSettings settings, int times = 1, bool loud = false)
        {
            if (loud) settings.title = settings.title?.ToUpperInvariant();
            if (settings.items != null) foreach (var item in settings.items) item.count *= times;
            return settings;
        }
    }

    /// <summary>The RQL text presentation through McpPlugin's own list / call pipeline (<see cref="ToolPresentation"/>).</summary>
    public class RqlToolPresentationTests
    {
        private const string Tool = "presentation-echo";
        private McpToolManager _manager = null!;

        // The suite runs under the configured data layer (toolFormat, default Rql): restore it after each test.
        private IToolBinding? _savedBinding;
        private IToolPresentation? _savedPresentation;
        private ToolTextEscaping _savedEscaping;

        [SetUp]
        public void SetUp()
        {
            _savedBinding = ToolBinding.Current;
            _savedPresentation = ToolPresentation.Current;
            _savedEscaping = ToolPresentation.Escaping;
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var reflector = UnityMcpPluginEditor.Instance.Reflector!;
            var tools = new ToolRunnerCollection(reflector, null)
            {
                [Tool] = RunTool.CreateFromStaticMethod(reflector, null, Tool, typeof(PresentationTestTools).GetMethod(nameof(PresentationTestTools.Echo))!),
            };
            _manager = new McpToolManager(NullLogger<McpToolManager>.Instance, reflector, tools);
            ToolBinding.Current = null;
            RqlToolPresentation.Enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            ToolBinding.Current = _savedBinding;
            ToolPresentation.Current = _savedPresentation;
            ToolPresentation.Escaping = _savedEscaping;
        }

        private ResponseListTool Listed() => _manager.RunListTool(new RequestListTool()).Result.Value!.Single(t => t.Name == Tool);

        private (ResponseCallTool result, string text) Call(string json)
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            var outer = _manager.RunCallTool(new RequestCallTool(Tool, args)).Result;
            var result = outer.Value!;
            return (result, string.Join("\n", result.Content.Select(c => c.Text)));
        }

        [Test]
        public void ComplexParametersBecomeLiteralStringsWithTypeText()
        {
            var listed = Listed();
            var schema = listed.InputSchema.GetRawText();
            StringAssert.DoesNotContain("$defs", schema);
            StringAssert.DoesNotContain("$ref", schema);
            var props = listed.InputSchema.GetProperty("properties");
            Assert.AreEqual("string", props.GetProperty("settings").GetProperty("type").GetString());
            StringAssert.Contains("RQL literal: PresentationTestSettings", props.GetProperty("settings").GetProperty("description").GetString());
            Assert.AreEqual("integer", props.GetProperty("times").GetProperty("type").GetString());
            Assert.AreEqual("boolean", props.GetProperty("loud").GetProperty("type").GetString());
            // Value-type members are required in ReflectorNet's schema; the type text keeps that.
            StringAssert.Contains("PresentationTestSettings {title string, mode Fast|Slow required, items [PresentationTestItem]}", listed.Description);
            StringAssert.Contains("PresentationTestItem {name string, count int required}", listed.Description);
            Assert.IsNull(listed.OutputSchema);
        }

        [Test]
        public void LiteralArgumentsReachTheToolAndTheResultIsCompactText()
        {
            var (result, text) = Call("{\"settings\": \"{title: \\\"Hi\\\", mode: Slow, items: [{name: \\\"a\\\", count: 2}, {name: \\\"b\\\", count: 3}]}\", \"times\": 2, \"loud\": true}");
            Assert.AreEqual(ResponseStatus.Success, result.Status, text);
            Assert.IsNull(result.StructuredContent);
            StringAssert.Contains("title: HI", text);
            StringAssert.Contains("name | count\na | 4\nb | 6", text);
            StringAssert.DoesNotContain("\"title\"", text);
        }

        [Test]
        public void JsonObjectArgumentsStillWork()
        {
            var (result, text) = Call("{\"settings\": {\"title\": \"Json, a+b<c\", \"items\": [{\"name\": \"x\", \"count\": 1}]}}");
            Assert.AreEqual(ResponseStatus.Success, result.Status, text);
            StringAssert.Contains("title: \"Json, a+b<c\"", text);   // quoted for the comma, '+' and '<' not escaped
        }

        [Test]
        public void ABrokenLiteralIsRejectedWithTheExpectedType()
        {
            var (result, text) = Call("{\"settings\": \"{title: }\"}");
            Assert.AreEqual(ResponseStatus.Error, result.Status, text);
            StringAssert.Contains("argument 'settings'", text);
            StringAssert.Contains("PresentationTestSettings", text);
        }

        [TestCase(ToolTextEscaping.Minimal, "title: \"é+<, x\"")]
        [TestCase(ToolTextEscaping.Ascii, "title: \"\\u00e9+<, x\"")]
        [TestCase(ToolTextEscaping.Json, "title: \"\\u00E9\\u002B\\u003C, x\"")]
        public void ResultTextFollowsTheConfiguredEscaping(ToolTextEscaping escaping, string expected)
        {
            ToolPresentation.Escaping = escaping;
            var (result, text) = Call("{\"settings\": {\"title\": \"é+<, x\"}}");
            Assert.AreEqual(ResponseStatus.Success, result.Status, text);
            StringAssert.Contains(expected, text);
        }

        [Test]
        public void ConfigSelectsThePresentation()
        {
            RqlToolPresentation.Apply(ToolPresentationFormat.RqlLean, ToolTextEscaping.Ascii);
            Assert.AreSame(RqlToolPresentation.Lean, ToolPresentation.Current);
            Assert.AreEqual(ToolTextEscaping.Ascii, ToolPresentation.Escaping);
            RqlToolPresentation.Apply(ToolPresentationFormat.Json, ToolTextEscaping.Minimal);
            Assert.IsNull(ToolPresentation.Current);
        }

        [Test]
        public void DisabledKeepsTheStandardJsonPresentation()
        {
            RqlToolPresentation.Enabled = false;
            StringAssert.Contains("$defs", Listed().InputSchema.GetRawText());
        }
    }
}
