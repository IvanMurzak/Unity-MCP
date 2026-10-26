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
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Tools
{
    /// <summary>What script-execute returns to the agent: bounded, readable values and errors (RQL data layer).</summary>
    public class ScriptExecuteResultTests
    {
        private IToolBinding? _savedBinding;

        [SetUp]
        public void SetUp()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            _savedBinding = Plugin.ToolBinding;
            RqlUnity.Reset();
            RqlToolBinding.Instance.Reset();
            Plugin.ToolBinding = RqlToolBinding.Instance;
        }

        [TearDown]
        public void TearDown() => Plugin.ToolBinding = _savedBinding;

        private static IToolManager Plugin => UnityMcpPluginEditor.Instance.Tools!;

        private static (ResponseCallTool result, string text) Call(string tool, object args)
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(args))!;
            var r = Plugin.RunCallTool(new RequestCallTool(tool, parsed)).Result.Value!;
            return (r, string.Join("\n", r.Content.Select(c => c.Text)));
        }

        private static string Run(string code)
        {
            var (r, text) = Call(Tool_Script.ScriptExecuteToolId, new { csharpCode = code });
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            return text;
        }

        [Test]
        public void ATypedNullIsNullNotTheStringNull() =>
            Assert.AreEqual("System.String: null", Run("(string)null"));

        [Test]
        public void NonFiniteNumbersAreReturned()
        {
            StringAssert.Contains("NaN", Run("double.NaN"));
            StringAssert.Contains("Infinity", Run("float.PositiveInfinity"));
        }

        [Test]
        public void ObjectAndRandomMeanUnityEngines()
        {
            StringAssert.Contains("UnityEngine.Object", Run("typeof(Object).FullName"));
            StringAssert.Contains("UnityEngine.Random", Run("typeof(Random).FullName"));
            // The code's own alias or type of that name wins (a second alias would not compile).
            StringAssert.Contains("System.Random", Run("using Random = System.Random;\ntypeof(Random).FullName"));
            StringAssert.Contains("7", Run("class Random { public static int Seven() => 7; }\nRandom.Seven()"));
        }

        [Test]
        public void HostAliasesSkipNamesTheRequestDeclares()
        {
            CollectionAssert.AreEquivalent(new[] { "Object = UnityEngine.Object", "Random = UnityEngine.Random" },
                Tool_Script.WithHostAliases("1", new string[0]));
            CollectionAssert.AreEquivalent(new[] { "Random = System.Random", "Object = UnityEngine.Object" },
                Tool_Script.WithHostAliases("1", new[] { "Random = System.Random" }));
            CollectionAssert.AreEquivalent(new[] { "Random = UnityEngine.Random" },
                Tool_Script.WithHostAliases("using Object = System.Object;\n1", new string[0]));
        }

        [Test]
        public void LargeResultsAreBounded()
        {
            var text = Run("new string('x', 200000)");
            Assert.Less(text.Length, Tool_Script.ResultValue.MaxChars + 1000);
            StringAssert.Contains($"truncated: {Tool_Script.ResultValue.MaxChars} of 200000 characters", text);

            var list = Run("Enumerable.Range(0, 5000).ToList()");
            StringAssert.Contains($"{5000 - Tool_Script.ResultValue.MaxItems} more items", list);
            StringAssert.DoesNotContain("| 4999", list);

            // An endless sequence is cut, not enumerated forever.
            StringAssert.Contains("more items", Run("IEnumerable<int> F() { var i = 0; while (true) yield return i++; }\nF()"));
        }

        [Test]
        public void AGraphTooLargeToSerializeWholeReturnsItsBeginning()
        {
            var text = Run("Enumerable.Range(0, 900).Select(i => new { a = i, b = new string('y', 300) }).ToList()");
            StringAssert.Contains("too large to return whole", text);
            Assert.Less(text.Length, Tool_Script.ResultValue.MaxSerializedChars); // 270 000+ characters as JSON
        }

        [Test]
        public void DictionariesAndQueriesShowTheirItems()
        {
            var dictionary = Run("new Dictionary<string, int> { [\"alpha\"] = 11 }");
            StringAssert.Contains("alpha", dictionary);
            StringAssert.Contains("11", dictionary);
            var query = Run("from x in new[] { 3, 1, 2 } orderby x select x * 10");
            StringAssert.Contains("30", query);
            StringAssert.DoesNotContain("Current", query);
        }

        [Test]
        public void CompileErrorsAreCapped()
        {
            LogAssert.ignoreFailingMessages = true; // the failed call is logged
            var code = string.Join("\n", Enumerable.Range(0, 40).Select(i => $"var v{i} = missing{i};"));
            var (r, text) = Call(Tool_Script.ScriptExecuteToolId, new { csharpCode = code });
            Assert.AreEqual(ResponseStatus.Error, r.Status, text);
            StringAssert.Contains("more errors", text);
            StringAssert.DoesNotContain("missing39", text);
        }

        [Test]
        public void SchemaOfAnUnknownTypeIsAnError()
        {
            LogAssert.ignoreFailingMessages = true;
            var (r, text) = Call(Tool_Rql.SchemaToolId, new { type = "NoSuchTypeAtAll" });
            Assert.AreEqual(ResponseStatus.Error, r.Status, text);
            StringAssert.Contains("unknown-type", text);
            var (ok, light) = Call(Tool_Rql.SchemaToolId, new { type = "Light" });
            Assert.AreEqual(ResponseStatus.Success, ok.Status, light);
        }
    }
}
