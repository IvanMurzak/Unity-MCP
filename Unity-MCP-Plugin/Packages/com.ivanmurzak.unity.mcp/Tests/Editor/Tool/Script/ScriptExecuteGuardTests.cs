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
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.CodeExec;
using com.IvanMurzak.CodeExec.Roslyn;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Tools
{
    /// <summary>
    /// Code-Exec's execution guards on Unity's Mono, on the main thread: an endless loop stops at its time budget and runaway
    /// recursion fails instead of killing the Editor; script-execute always sends a budget below the server's tool timeout.
    /// </summary>
    public class ScriptExecuteGuardTests
    {
        [Test]
        public void AnEndlessLoopStopsAtItsBudget()
        {
            var runner = new CodeRunner(new RoslynCSharpCompiler(), new CodeRunnerOptions { LogCaptures = new List<ILogCapture>() });
            var clock = Stopwatch.StartNew();
            var result = runner.Execute(new ExecuteRequest { Code = "var n = 0;\nwhile (true) { n++; }", TimeoutMs = 500 });
            Assert.AreEqual(ErrorKind.Timeout, result.ErrorKind, result.Error);
            StringAssert.Contains("time budget of 500 ms", result.Error);
            Assert.Less(clock.ElapsedMilliseconds, 15_000, "stopped near its budget (compile time included)");
        }

        [Test]
        public void RunawayRecursionFailsWithAnError()
        {
            LogAssert.ignoreFailingMessages = true;
            var text = Call("int Down(int n) => Down(n + 1) + 1;\nDown(0)");
            Assert.AreEqual(ResponseStatus.Error, text.status, text.text);
            StringAssert.Contains("Recursion too deep", text.text);
        }

        [TestCase(300_000, Tool_Script.MaxSnippetTimeoutMs)]
        [TestCase(60_000, 50_000)]
        [TestCase(5_000, 1_000)]
        [TestCase(0, 1_000)]
        public void TheBudgetEndsBeforeTheToolTimeout(int toolTimeoutMs, int expected) =>
            Assert.AreEqual(expected, Tool_Script.SnippetTimeoutMs(toolTimeoutMs));

        private static (ResponseStatus status, string text) Call(string code)
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(new { csharpCode = code }))!;
            var r = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool(Tool_Script.ScriptExecuteToolId, args)).Result.Value!;
            return (r.Status, string.Join("\n", r.Content.Select(c => c.Text)));
        }
    }
}
