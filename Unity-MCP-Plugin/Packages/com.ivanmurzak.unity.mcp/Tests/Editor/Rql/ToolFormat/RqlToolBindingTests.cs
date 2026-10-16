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

        private const string Items = "binding-items";
        private McpToolManager _local = null!;

        // The suite runs under the configured data layer (toolFormat, default Rql): these tests force RQL on the plugin's
        // tools and restore the layer after each test.
        private IToolBinding? _savedBinding;
        private ToolTextEscaping _savedEscaping;

        [SetUp]
        public void SetUp()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            _savedBinding = Plugin.ToolBinding;
            _savedEscaping = RqlToolBinding.Instance.Escaping;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Alpha");
            RqlUnity.Reset();
            RqlToolBinding.Instance.Reset();
            var reflector = UnityMcpPluginEditor.Instance.Reflector!;
            var tools = new ToolRunnerCollection(reflector, null)
            {
                [Echo] = RunTool.CreateFromStaticMethod(reflector, null, Echo, typeof(PresentationTestTools).GetMethod(nameof(PresentationTestTools.Echo))!),
                [Items] = RunTool.CreateFromStaticMethod(reflector, null, Items, typeof(PresentationTestTools).GetMethod(nameof(PresentationTestTools.Items))!),
            };
            _local = new McpToolManager(NullLogger<McpToolManager>.Instance, reflector, tools) { ToolBinding = RqlToolBinding.Instance };
            Plugin.ToolBinding = RqlToolBinding.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            Plugin.ToolBinding = _savedBinding;
            RqlToolBinding.Instance.Escaping = _savedEscaping;
        }

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
            RqlToolBinding.Apply(_local, ToolPresentationFormat.Json, ToolTextEscaping.Ascii);
            Assert.IsNull(_local.ToolBinding);
            Assert.AreEqual(ToolTextEscaping.Ascii, RqlToolBinding.Instance.Escaping);
            StringAssert.Contains("$defs", Listed(_local, Echo).InputSchema.GetRawText(), "Json = ReflectorNet's schema");
            RqlToolBinding.Apply(_local, ToolPresentationFormat.Rql, ToolTextEscaping.Minimal);
            Assert.AreSame(RqlToolBinding.Instance, _local.ToolBinding);
            StringAssert.DoesNotContain("$defs", Listed(_local, Echo).InputSchema.GetRawText());
        }

        [Test]
        public void ApplyPrebuildsSignaturesSoABackgroundListingNeverWaitsForTheMainThread()
        {
            RqlToolBinding.Instance.Reset();
            RqlToolBinding.Apply(_local, ToolPresentationFormat.Rql, ToolTextEscaping.Minimal);
            var echo = (RunTool)_local.GetAllTools().Single(t => t.Name == Echo);
            // The main thread blocks here: a Describe that still had to build the signature would wait for it forever.
            var background = System.Threading.Tasks.Task.Run(() => RqlToolBinding.Instance.Describe(echo.Name, echo.Method, echo.Description));
            Assert.IsTrue(background.Wait(System.TimeSpan.FromSeconds(5)), "Describe on a background thread waited for the main thread");
            StringAssert.Contains("PresentationTestSettings", background.Result!.Description);
        }

        [Test]
        public void ThePluginsToolsGetTheConfiguredLayer()
        {
            var config = UnityMcpPluginEditor.Instance.ConnectionConfigForTests;
            var saved = config.ToolFormat;
            try
            {
                config.ToolFormat = ToolPresentationFormat.Json;
                UnityMcpPluginEditor.Instance.ApplyToolFormat(UnityMcpPluginEditor.Instance.McpPluginInstance!);
                Assert.IsNull(Plugin.ToolBinding);
                config.ToolFormat = ToolPresentationFormat.Rql;
                UnityMcpPluginEditor.Instance.ApplyToolFormat(UnityMcpPluginEditor.Instance.McpPluginInstance!);
                Assert.AreSame(RqlToolBinding.Instance, Plugin.ToolBinding);
            }
            finally { config.ToolFormat = saved; }
        }

        [TestCase(ToolTextEscaping.Minimal, "\"é+<|x\"")]
        [TestCase(ToolTextEscaping.Ascii, "\"\\u00e9+<|x\"")]
        [TestCase(ToolTextEscaping.Json, "\"\\u00E9\\u002B\\u003C|x\"")]
        public void QuotedResultTextFollowsTheConfiguredEscaping(ToolTextEscaping escaping, string expected)
        {
            RqlToolBinding.Instance.Escaping = escaping;
            var (r, text) = Call(_local, Items, "{\"name\": \"é+<|x\"}");
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.StartsWith("2 results\nname|count", text);
            StringAssert.Contains(expected + "|1", text);
        }
    }
}
