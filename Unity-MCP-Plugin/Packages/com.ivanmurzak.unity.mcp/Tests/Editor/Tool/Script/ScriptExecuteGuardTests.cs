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
using com.IvanMurzak.CodeExec;
using com.IvanMurzak.CodeExec.Roslyn;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Tools
{
    /// <summary>
    /// Code-Exec's time guard on Unity's Mono, on the main thread: an endless loop stops at its budget; script-execute always
    /// sends a budget below the server's tool timeout. (Runaway recursion: <see cref="ScriptExecuteResultTests"/>.)
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

        [TestCase(300_000, Tool_Script.MaxSnippetTimeoutMs)]
        [TestCase(60_000, 50_000)]
        [TestCase(5_000, 1_000)]
        [TestCase(0, 1_000)]
        public void TheBudgetEndsBeforeTheToolTimeout(int toolTimeoutMs, int expected) =>
            Assert.AreEqual(expected, Tool_Script.SnippetTimeoutMs(toolTimeoutMs));
    }
}
