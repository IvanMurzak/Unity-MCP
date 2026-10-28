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
using com.IvanMurzak.CodeExec;
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

        /// <summary>A call that must fail; the failure is logged, which is expected here.</summary>
        private static string Fails(string tool, object args)
        {
            LogAssert.ignoreFailingMessages = true;
            var (r, text) = Call(tool, args);
            Assert.AreEqual(ResponseStatus.Error, r.Status, text);
            return text;
        }

        // Code-Exec's recursion guard: an error, not a stack overflow that ends the Editor.
        [Test]
        public void RunawayRecursionFailsWithAnError() =>
            StringAssert.Contains("Recursion too deep",
                Fails(Tool_Script.ScriptExecuteToolId, new { csharpCode = "int Down(int n) => Down(n + 1) + 1;\nDown(0)" }));

        // The skill text quotes the limits as literals (an attribute cannot format constants): keep them in step.
        [Test]
        public void TheSkillQuotesTheLimitsInForce()
        {
            var body = typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute))!
                .GetCustomAttributes(typeof(AiSkillBodyAttribute), false).Cast<AiSkillBodyAttribute>().Single().Body;
            foreach (var phrase in new[]
                     {
                         $"a string to {OutputLimits.DefaultValueChars} characters",
                         $"a collection to {Tool_Script.ResultValue.MaxItems} items",
                         $"over {Tool_Script.ResultValue.MaxSerializedChars} characters as JSON comes back as the first {OutputLimits.DefaultValueChars}",
                         $"errors to {OutputLimits.DefaultErrorChars} characters and {OutputLimits.MaxDiagnostics} compile errors",
                         $"a time budget of {Tool_Script.MaxSnippetTimeoutMs / 1000} s",
                         $"running {Tool_Script.ReloadGraceMs / 1000} s after a compile is requested",
                     })
                StringAssert.Contains(phrase, body);
        }

        // Wave 6: an agent polling with script-execute after writing a script kept the reload held forever (each call locks
        // it until 1 s after its reply): isCompiling stayed true for minutes. A compile our lock held defers new calls.
        [Test]
        public void ACompileOurLockHeldDefersCallsUntilItReloads()
        {
            var t0 = new System.DateTime(2026, 10, 28, 12, 0, 0, System.DateTimeKind.Utc);
            Tool_Script.DeferForReload(compiling: false, playing: false, reloadLocked: false, t0); // reset
            try
            {
                Assert.IsFalse(Tool_Script.DeferForReload(true, false, false, t0), "a compile we never held: calls run");
                Assert.IsTrue(Tool_Script.DeferForReload(true, false, true, t0), "pending while our lock holds the reload");
                Assert.IsTrue(Tool_Script.DeferForReload(true, false, false, t0.AddSeconds(5)), "still pending after the lock dropped");
                Assert.IsFalse(Tool_Script.DeferForReload(true, false, false, t0 + Tool_Script.DeferLimit), "not forever");
                Assert.IsFalse(Tool_Script.DeferForReload(false, false, false, t0.AddSeconds(61)), "reloaded: reset");
                Assert.IsFalse(Tool_Script.DeferForReload(true, false, false, t0.AddSeconds(62)), "a new compile starts unheld");
                Assert.IsFalse(Tool_Script.DeferForReload(true, true, true, t0.AddSeconds(63)), "Play Mode never defers");
            }
            finally { Tool_Script.DeferForReload(false, false, false, t0); }
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
            // Code-Exec accepts a usings entry written as a directive.
            CollectionAssert.AreEquivalent(new[] { "using Random = System.Random;", "Object = UnityEngine.Object" },
                Tool_Script.WithHostAliases("1", new[] { "using Random = System.Random;" }));
            var (r, text) = Call(Tool_Script.ScriptExecuteToolId, new { csharpCode = "typeof(Random).FullName", usings = new[] { "using Random = System.Random;" } });
            Assert.AreEqual(ResponseStatus.Success, r.Status, text);
            StringAssert.Contains("System.Random", text);
        }

        public sealed class Inventory : IEnumerable<int>
        {
            public int gold = 7;
            public IEnumerator<int> GetEnumerator() { yield return 1; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Test]
        public void CollectionsAndEnumerableTypesKeepTheirShape()
        {
            Assert.IsInstanceOf<HashSet<int>>(Tool_Script.ResultValue.Shape(new HashSet<int> { 1, 2 }));
            var inventory = new Inventory();
            Assert.AreSame(inventory, Tool_Script.ResultValue.Shape(inventory));
            var list = new List<int> { 1, 2, 3 };
            Assert.AreSame(list, Tool_Script.ResultValue.Shape(list));
            Assert.IsInstanceOf<List<object?>>(Tool_Script.ResultValue.Shape(list.Select(x => x * 2)));
            var big = (List<object?>)Tool_Script.ResultValue.Shape(new HashSet<int>(Enumerable.Range(0, 1500)))!;
            Assert.AreEqual(Tool_Script.ResultValue.MaxItems + 1, big.Count);
            StringAssert.Contains("500 more items", (string)big[Tool_Script.ResultValue.MaxItems]!);
        }

        [Test]
        public void LargeResultsAreBounded()
        {
            // Code-Exec clips a text value (MaxValueChars).
            var text = Run("new string('x', 200000)");
            Assert.Less(text.Length, OutputLimits.DefaultValueChars + 1000);
            StringAssert.Contains($"truncated: {OutputLimits.DefaultValueChars} of 200000 characters shown", text);

            var list = Run("Enumerable.Range(0, 5000).ToList()");
            StringAssert.Contains($"{5000 - Tool_Script.ResultValue.MaxItems} more items", list);
            StringAssert.DoesNotContain("| 4999", list);

            // An endless sequence is cut, not enumerated forever.
            StringAssert.Contains("more items", Run("IEnumerable<int> F() { var i = 0; while (true) yield return i++; }\nF()"));
        }

        [Test]
        public void AGraphTooLargeToSerializeWholeReturnsItsBeginning()
        {
            // One object (not a collection) whose JSON is 270 000+ characters: only its beginning fits.
            var text = Run("new { items = Enumerable.Range(0, 900).Select(i => new { a = i, b = new string('y', 300) }).ToList() }");
            StringAssert.Contains($"[truncated: {OutputLimits.DefaultValueChars} of ", text);
            Assert.Less(text.Length, OutputLimits.DefaultValueChars + 2000); // + the quotes the JSON string escapes
        }

        // A collection whose items are too large together keeps whole items and says how many it left out, instead of the
        // first characters of its JSON (which lost the "more items" note and every item past the cut).
        [Test]
        public void ACollectionOfLargeItemsKeepsTheItemsThatFit()
        {
            var text = Run("Enumerable.Range(0, 900).Select(i => new { a = i, b = new string('y', 300) }).ToList()");
            StringAssert.DoesNotContain("[truncated:", text);
            StringAssert.IsMatch(@"… \d+ more items \(first \d+ of 900 shown\)", text);
            var shown = int.Parse(System.Text.RegularExpressions.Regex.Match(text, @"first (\d+) of 900").Groups[1].Value);
            Assert.That(shown, Is.InRange(Tool_Script.ResultValue.MinItems, 899));
            Assert.Less(text.Length, Tool_Script.ResultValue.MaxSerializedChars);
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
            var code = string.Join("\n", Enumerable.Range(0, 40).Select(i => $"var v{i} = missing{i};"));
            var text = Fails(Tool_Script.ScriptExecuteToolId, new { csharpCode = code });
            StringAssert.Contains("CE0002", text); // Code-Exec: 20 diagnostics, then a count of the rest
            StringAssert.DoesNotContain("missing39", text);
        }

        [Test]
        public void ALongErrorIsClippedAndSaysSo()
        {
            var text = Fails(Tool_Script.ScriptExecuteToolId, new { csharpCode = "throw new System.Exception(new string('e', 30000));" });
            StringAssert.Contains("Truncated: error", text);
            Assert.Less(text.Length, OutputLimits.DefaultErrorChars + 2000);
        }

        [Test]
        public void EveryRunStartsWithFreshStatics()
        {
            const string code = "public static class Counter { public static int N; }\n++Counter.N";
            StringAssert.Contains(": 1", Run(code));
            StringAssert.Contains(": 1", Run(code));
        }

        // Mono never unloads: a snippet with a readonly lookup table (not state, Code-Exec 0.3.1) reuses its assembly, while
        // a snippet with real static state loads one per run. Measured after a warm-up run, which may load twice.
        [TestCase("public static class Table { public static readonly System.Collections.Generic.IReadOnlyDictionary<string, int> Map = new System.Collections.Generic.Dictionary<string, int> { [\"b\"] = 2 }; public static readonly string Name = \"t\"; }\nTable.Map[\"b\"]", false)]
        [TestCase("public static class Tally { public static int N; }\nTally.N = 2;\nTally.N", true)]
        public void OnlySnippetsWithStaticStateLoadAnAssemblyPerRun(string code, bool loadsPerRun)
        {
            const int runs = 3;
            StringAssert.Contains(": 2", Run(code));
            var before = SnippetAssemblies();
            for (var i = 0; i < runs; i++)
                StringAssert.Contains(": 2", Run(code));
            Assert.AreEqual(loadsPerRun ? runs : 0, SnippetAssemblies() - before);
        }

        static int SnippetAssemblies() => System.AppDomain.CurrentDomain.GetAssemblies().Count(LoadedAssemblies.IsSnippet);

        [Test]
        public void IncludesStayInsideTheSnippetsFolder()
        {
            var text = Fails(Tool_Script.ScriptExecuteToolId, new { csharpCode = "1", include = new[] { "../../ProjectSettings/ProjectSettings.asset" } });
            StringAssert.Contains("InvalidRequest", text);
            StringAssert.DoesNotContain("CS1525", text); // the asset is never compiled
        }

        [Test]
        public void SchemaOfAnUnknownTypeIsAnError()
        {
            StringAssert.Contains("unknown-type", Fails(Tool_Rql.SchemaToolId, new { type = "NoSuchTypeAtAll" }));
            var (ok, light) = Call(Tool_Rql.SchemaToolId, new { type = "Light" });
            Assert.AreEqual(ResponseStatus.Success, ok.Status, light);
        }
    }
}
