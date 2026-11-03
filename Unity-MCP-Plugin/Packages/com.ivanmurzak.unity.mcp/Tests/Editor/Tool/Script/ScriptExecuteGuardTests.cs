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
using System.Threading;
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

        // The snippet's own budget: `timeout` in seconds, 30 s by default, at most 290 s and 10 s below the tool timeout.
        [TestCase(300_000, null, Tool_Script.DefaultSnippetTimeoutMs)]
        [TestCase(300_000, 5.0, 5_000)]
        [TestCase(300_000, 2.5, 2_500)]
        [TestCase(300_000, 1000.0, Tool_Script.MaxSnippetTimeoutMs)]
        [TestCase(600_000, 1000.0, Tool_Script.MaxSnippetTimeoutMs)]
        [TestCase(300_000, 0.0, Tool_Script.DefaultSnippetTimeoutMs)]
        [TestCase(300_000, -4.0, Tool_Script.DefaultSnippetTimeoutMs)]
        [TestCase(300_000, 0.1, 1_000)]
        [TestCase(60_000, 120.0, 50_000)]
        [TestCase(60_000, null, Tool_Script.DefaultSnippetTimeoutMs)]
        [TestCase(5_000, null, 1_000)]
        [TestCase(0, null, 1_000)]
        public void TheBudgetIsTheTimeoutArgumentCappedBelowTheToolTimeout(int toolTimeoutMs, double? timeoutSeconds, int expected) =>
            Assert.AreEqual(expected, Tool_Script.SnippetTimeoutMs(toolTimeoutMs, timeoutSeconds));

        // Code-Exec 54d527b: under a budget, .Result on a task queued to the main thread's UnitySynchronizationContext runs it
        // inline again (affe6ef sliced the wait, so the queue never ran and the snippet timed out). Needs Mono's
        // Task.WrappedTryRunInline, which Code-Exec finds by reflection.
        [Test]
        public void AResultOnATaskQueuedToTheMainThreadRunsItInline()
        {
            Assert.IsNotNull(SynchronizationContext.Current, "EditMode tests run on the main thread with Unity's context");
            var runner = new CodeRunner(new RoslynCSharpCompiler(), new CodeRunnerOptions { LogCaptures = new List<ILogCapture>() });
            var result = runner.Execute(new ExecuteRequest
            {
                Code = "System.Threading.Tasks.Task.Factory.StartNew(() => 5, System.Threading.CancellationToken.None, " +
                       "System.Threading.Tasks.TaskCreationOptions.None, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext()).Result",
                TimeoutMs = 5_000,
            });
            Assert.IsTrue(result.Success, result.Error);
            Assert.AreEqual(5, System.Convert.ToInt32(result.Value));
        }

        // Code-Exec 2556a2f: a thread the snippet starts belongs to the run and stops once the run ends (before, it kept
        // spinning until the domain reloaded), with a warning that counts it.
        [Test]
        public void AThreadTheSnippetStartedStopsAfterTheRun()
        {
            const string key = "ScriptExecuteGuardTests.thread";
            var runner = new CodeRunner(new RoslynCSharpCompiler(), new CodeRunnerOptions { LogCaptures = new List<ILogCapture>() });
            var result = runner.Execute(new ExecuteRequest
            {
                Code = "var t = new System.Threading.Thread(() => { var n = 0L; while (true) { n++; } }) { IsBackground = true };\n" +
                       "t.Start();\n" +
                       "System.AppDomain.CurrentDomain.SetData(\"" + key + "\", t);\n" +
                       "1",
                TimeoutMs = 10_000,
            });
            try
            {
                Assert.IsTrue(result.Success, result.Error);
                var thread = (Thread)System.AppDomain.CurrentDomain.GetData(key);
                Assert.IsTrue(thread.Join(5_000), "the snippet's thread stopped after the run");
                Assert.IsTrue(result.Logs.Any(l => l.Level == "warning" && l.Message.Contains("had not finished when it ended")),
                    string.Join("\n", result.Logs));
            }
            finally
            {
                System.AppDomain.CurrentDomain.SetData(key, null);
            }
        }
    }
}
