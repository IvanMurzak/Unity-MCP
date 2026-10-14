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
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.ToolFormat;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ToolFormat
{
    /// <summary>Phase 2: RQL instead of ReflectorNet as the tools' data layer (McpPlugin IToolBinding).</summary>
    public class RqlToolBindingTests
    {
        private const string Echo = "binding-echo";
        private McpToolManager _local = null!;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Alpha");
            RqlUnity.Reset();
            RqlToolBinding.Instance.Reset();
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var reflector = UnityMcpPluginEditor.Instance.Reflector!;
            var tools = new ToolRunnerCollection(reflector, null)
            {
                [Echo] = RunTool.CreateFromStaticMethod(reflector, null, Echo, typeof(PresentationTestTools).GetMethod(nameof(PresentationTestTools.Echo))!),
            };
            _local = new McpToolManager(NullLogger<McpToolManager>.Instance, reflector, tools);
            ToolBinding.Current = RqlToolBinding.Instance;
        }

        [TearDown]
        public void TearDown() => ToolBinding.Current = null;

        private static ResponseListTool Listed(IToolManager manager, string name) =>
            manager.RunListTool(new RequestListTool()).Result.Value!.Single(t => t.Name == name);

        private static (ResponseCallTool result, string text) Call(IToolManager manager, string name, string json)
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            var r = manager.RunCallTool(new RequestCallTool(name, args)).Result.Value!;
            return (r, string.Join("\n", r.Content.Select(c => c.Text)));
        }

        private static IToolManager Plugin => UnityMcpPluginEditor.Instance.Tools!;

        [Test]
        public void ToolTypesComeFromRqlNotReflectorNet()
        {
            var listed = Listed(Plugin, "screenshot-isolated");
            var schema = listed.InputSchema.GetRawText();
            StringAssert.DoesNotContain("$defs", schema);
            StringAssert.DoesNotContain("$ref", schema);
            var gameObjectRef = listed.InputSchema.GetProperty("properties").GetProperty("gameObjectRef");
            Assert.AreEqual("string", gameObjectRef.GetProperty("type").GetString());
            StringAssert.Contains("RQL: GameObjectRef", gameObjectRef.GetProperty("description").GetString());
            StringAssert.Contains("GameObjectRef — a GameObject: ref(\"GameObject#12\")", listed.Description);
            Assert.AreEqual("number", listed.InputSchema.GetProperty("properties").GetProperty("fieldOfView").GetProperty("type").GetString());
            Assert.IsNull(listed.OutputSchema);
        }

        [Test]
        public void RecordArgumentsAreRqlLiteralsAndResultsAreRqlOutput()
        {
            var listed = Listed(_local, Echo);
            StringAssert.Contains("PresentationTestSettings {title string, mode Fast|Slow, items [PresentationTestItem]", listed.Description);
            var (r, text) = Call(_local, Echo, "{\"settings\": \"{title: \\\"Hi\\\", mode: Slow, items: [{name: a, count: 2}]}\", \"times\": 3}");
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.Contains("Hi", text);
            StringAssert.Contains("Slow", text);
            StringAssert.Contains("6", text);   // count 2 × times 3
            StringAssert.DoesNotContain("\"title\"", text);
        }

        [Test]
        public void ScriptExecuteTakesNamedRqlParametersIncludingLiveObjects()
        {
            StringAssert.Contains("RQL: SerializedMemberList", Listed(Plugin, Tool_Script.ScriptExecuteToolId).InputSchema.GetRawText());
            var code = "public class Script { public static string Main(UnityEngine.GameObject go, int n) { return go.name + \":\" + (n * 7); } }";
            var (r, text) = Call(Plugin, Tool_Script.ScriptExecuteToolId,
                JsonSerializer.Serialize(new { csharpCode = code, parameters = "{go: unity.find(\"Alpha\"), n: 6}" }));
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.Contains("System.String", text);
            StringAssert.Contains("Alpha:42", text);
        }

        [Test]
        public void ABadArgumentNamesTheParameterAndTheExpectedType()
        {
            var (r, text) = Call(_local, Echo, "{\"settings\": \"{title: }\"}");
            Assert.AreEqual(ResponseStatus.Error, r.Status, text);
            StringAssert.Contains("'settings'", text);
            StringAssert.Contains("PresentationTestSettings", text);
        }

        [Test]
        public void ConfigSelectsTheBinding()
        {
            RqlToolPresentation.Apply(ToolPresentationFormat.Rql, ToolTextEscaping.Minimal);
            Assert.AreSame(RqlToolBinding.Instance, ToolBinding.Current);
            Assert.IsNull(ToolPresentation.Current);
            RqlToolPresentation.Apply(ToolPresentationFormat.Json, ToolTextEscaping.Minimal);
            Assert.IsNull(ToolBinding.Current);
        }
    }
}
