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
        private const string Optionals = "binding-optionals";
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
                [Optionals] = RunTool.CreateFromStaticMethod(reflector, null, Optionals, typeof(PresentationTestTools).GetMethod(nameof(PresentationTestTools.Optionals))!),
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

        private static string?[] Required(IToolManager manager, string name) =>
            Listed(manager, name).InputSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();

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
        public void CSharpExecTakesNamedRqlParametersIncludingLiveObjects()
        {
            StringAssert.Contains("RQL: SerializedMemberList", Listed(Plugin, Tool_Script.CSharpExecToolId).InputSchema.GetRawText());
            var code = "public class Script { public static string Main(UnityEngine.GameObject go, int n) { return go.name + \":\" + (n * 7); } }";
            var (r, text) = Call(Plugin, Tool_Script.CSharpExecToolId,
                JsonSerializer.Serialize(new { csharpCode = code, parameters = "{go: unity.find(\"Alpha\"), n: 6}" }));
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.Contains("System.String", text);
            StringAssert.Contains("Alpha:42", text);
        }

        // A null parameter is passed as null (its JSON is "null"), not a NullReferenceException.
        [Test]
        public void CSharpExecTakesANullParameter()
        {
            var code = "public class Script { public static string Main(object label) { return label == null ? \"no label\" : \"label\"; } }";
            var (r, text) = Call(Plugin, Tool_Script.CSharpExecToolId,
                JsonSerializer.Serialize(new { csharpCode = code, parameters = "{label: null}" }));
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.Contains("no label", text);
        }

        // A parameter expression that names nothing (e.g. the objects source used as a value) and a list each get a reason the
        // agent can act on, not "Missing values are not supported".
        [Test]
        public void CSharpExecExplainsParametersItCannotPass()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true; // the tool logs its error response
            var (missing, missingText) = Call(Plugin, Tool_Script.CSharpExecToolId,
                JsonSerializer.Serialize(new { csharpCode = "x", parameters = "{x: objects.first(name == \"Alpha\")}" }));
            Assert.AreEqual(ResponseStatus.Error, missing.Status, missingText);
            StringAssert.Contains("parameter 'x' has no value", missingText);
            StringAssert.Contains("unity.find", missingText);
            var (list, listText) = Call(Plugin, Tool_Script.CSharpExecToolId,
                JsonSerializer.Serialize(new { csharpCode = "x", parameters = "{x: [1, 2]}" }));
            Assert.AreEqual(ResponseStatus.Error, list.Status, listText);
            StringAssert.Contains("lists are not supported", listText);
        }

        // Required = no default, not [Optional] and not nullable (NRT string? / GameObjectRef? or Nullable<int>), in both
        // data layers; nullability never puts "null" into a schema type (some clients turn a type array into ""/0/false).
        [TestCase(ToolPresentationFormat.Rql)]
        [TestCase(ToolPresentationFormat.Json)]
        public void OnlyNonNullableParametersWithoutDefaultsAreRequired(ToolPresentationFormat layer)
        {
            RqlToolBinding.Apply(_local, layer, ToolTextEscaping.Minimal);
            CollectionAssert.AreEqual(new[] { "name" }, Required(_local, Optionals));
            StringAssert.DoesNotContain("\"null\"", Listed(_local, Optionals).InputSchema.GetRawText());
        }

        [Test]
        public void NullableParametersKeepTheirPlainRqlType()
        {
            RqlToolBinding.Apply(_local, ToolPresentationFormat.Rql, ToolTextEscaping.Minimal);
            var properties = Listed(_local, Optionals).InputSchema.GetProperty("properties");
            Assert.AreEqual("string", properties.GetProperty("note").GetProperty("type").GetString());
            Assert.AreEqual("string", properties.GetProperty("gameObjectRef").GetProperty("type").GetString());
            Assert.AreEqual("integer", properties.GetProperty("count").GetProperty("type").GetString());
        }

        [TestCase("{\"name\": \"a\"}", "a|<none>|<none>|<none>|1")]
        [TestCase("{\"name\": \"a\", \"note\": null, \"gameObjectRef\": null, \"count\": null}", "a|<none>|<none>|<none>|1")]
        [TestCase("{\"name\": \"a\", \"note\": \"n\", \"gameObjectRef\": \"unity.find(\\\"Alpha\\\")\", \"count\": 2, \"times\": 3}", "a|n|ref|2|3")]
        public void OmittedOrNullNullableArgumentsReachTheMethodAsNull(string json, string expected)
        {
            var (r, text) = Call(_local, Optionals, json);
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.Contains(expected, text);
        }

        [TestCase(ToolPresentationFormat.Rql)]
        [TestCase(ToolPresentationFormat.Json)]
        public void ScreenshotIsolatedRequiresItsTarget(ToolPresentationFormat layer)
        {
            RqlToolBinding.Apply(Plugin, layer, ToolTextEscaping.Minimal);
            CollectionAssert.AreEqual(new[] { "gameObjectRef" }, Required(Plugin, Tool_Screenshot.ScreenshotIsolatedToolId));
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
