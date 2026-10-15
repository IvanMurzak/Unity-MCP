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
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.EditorApi
{
    /// <summary>The <c>profiler</c> source that replaced the profiler-* tools.</summary>
    public class RqlProfilerTests : AdapterTestBase
    {
        private bool _wasEnabled;
        private string _file = "";

        [SetUp]
        public void Remember()
        {
            _wasEnabled = UnityProfiler.enabled;
            RqlProfiler.ResetModules();
            _file = Path.Combine(Path.GetTempPath(), "rql-profiler-" + System.Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void Restore()
        {
            UnityProfiler.enabled = _wasEnabled;
            RqlProfiler.ResetModules();
            if (File.Exists(_file)) File.Delete(_file);
        }

        private static long Count(string rql)
        {
            var r = Q(rql);
            AssertOk(r);
            return r.Value!.GetValue<long>();
        }

        private static double Number(string cell) => double.Parse(cell, CultureInfo.InvariantCulture);

        [Test]
        public void StatusIsReadable()
        {
            Assert.AreEqual(UnityProfiler.supported ? "true" : "false", Cell(Q("profiler | select supported")));
            Assert.AreEqual(UnityProfiler.enabled ? "true" : "false", Cell(Q("profiler | select enabled")));
        }

        [Test]
        public void StartAndStopSwitchTheRuntimeProfiler()
        {
            AssertOk(M("profiler | call Start(false)"));   // no window: the Profiler window needs a graphics device
            Assert.IsTrue(UnityProfiler.enabled);
            Assert.AreEqual("true", Cell(Q("profiler | select enabled")));
            AssertOk(M("profiler | call Stop()"));
            Assert.IsFalse(UnityProfiler.enabled);
        }

        [Test]
        public void EnabledCanBeAssigned()
        {
            AssertOk(M("profiler | set enabled = true"));
            Assert.IsTrue(UnityProfiler.enabled);
            AssertOk(M("profiler | set enabled = false"));
            Assert.IsFalse(UnityProfiler.enabled);
        }

        [Test]
        public void MemorySnapshotIsInMegabytes()
        {
            var r = Q("profiler | select memory.totalAllocatedMB, memory.totalReservedMB, memory.monoUsedSizeMB");
            var allocated = Number(Cell(r, 0, 0));
            var reserved = Number(Cell(r, 0, 1));
            Assert.Greater(allocated, 0, Table(r));
            Assert.GreaterOrEqual(reserved, allocated, Table(r));
            Assert.Less(allocated, 1024 * 1024, "megabytes, not bytes: " + Table(r));
            Assert.AreEqual(UnityProfiler.GetTotalAllocatedMemoryLong() / 1048576.0, allocated, 64, Table(r));
        }

        [Test]
        public void RenderingScriptAndFrameSnapshotsAreReadable()
        {
            Assert.AreEqual(SystemInfo.graphicsDeviceType.ToString(), Cell(Q("profiler | select rendering.graphicsDeviceType")));
            Assert.AreEqual(QualitySettings.vSyncCount.ToString(CultureInfo.InvariantCulture), Cell(Q("profiler | select rendering.vSyncCount")));
            Assert.AreEqual(Time.timeScale, (float)Number(Cell(Q("profiler | select script.timeScale"))), 1e-6f);
            Assert.GreaterOrEqual(Number(Cell(Q("profiler | select frame.frameCount"))), 0);
            Assert.Greater(Number(Cell(Q("profiler | select script.realtimeSinceStartup"))), 0);
        }

        [Test]
        public void ModulesAreFilterable()
        {
            Assert.AreEqual(14L, Count("profiler | get modules | count"));
            Assert.AreEqual(9L, Count("profiler | get modules | where enabled == true | count"));
            Assert.AreEqual("false", Cell(Q("profiler | get modules | where name == \"UIDetails\" | select enabled")));
        }

        [Test]
        public void EnableModuleTogglesTheBookkeeping()
        {
            AssertOk(M("profiler | call EnableModule(\"UIDetails\")"));
            Assert.AreEqual("true", Cell(Q("profiler | get modules | where name == \"UIDetails\" | select enabled")));
            AssertOk(M("profiler | call EnableModule(\"cpu\", false)"));   // names are case-insensitive
            Assert.AreEqual("false", Cell(Q("profiler | get modules | where name == \"CPU\" | select enabled")));
        }

        [Test]
        public void AnUnknownModuleIsRejectedWithTheKnownNames()
        {
            var r = M("profiler | call EnableModule(\"Telepathy\")");
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains("Telepathy", Table(r));
            StringAssert.Contains("Rendering", Table(r));
        }

        [Test]
        public void SaveWritesASnapshotThatLoadReadsBack()
        {
            var path = _file.Replace("\\", "/");
            AssertOk(M($"profiler | call Save(\"{path}\")"));
            Assert.IsTrue(File.Exists(_file));
            using (var doc = JsonDocument.Parse(File.ReadAllText(_file)))
            {
                Assert.IsTrue(doc.RootElement.TryGetProperty("memory", out var memory));
                Assert.IsTrue(memory.TryGetProperty("totalAllocatedMB", out _));
                Assert.IsTrue(doc.RootElement.TryGetProperty("frame", out _));
                Assert.AreEqual(9, doc.RootElement.GetProperty("modules").GetArrayLength());
            }
            var loaded = M($"profiler | call Load(\"{path}\")");
            AssertOk(loaded);
            StringAssert.Contains("totalAllocatedMB", Table(loaded));
        }

        [Test]
        public void LoadRejectsAMissingFile()
        {
            var r = M($"profiler | call Load(\"{_file.Replace("\\", "/")}\")");
            Assert.IsFalse(r.Ok, Table(r));
            StringAssert.Contains("no profiler snapshot", Table(r));
        }

        [Test]
        public void ClearFramesSucceeds() => AssertOk(M("profiler | call ClearFrames()"));

        [Test]
        public void SchemaListsTheProfilerOperations()
        {
            var schema = Schema("profiler");
            StringAssert.Contains("Start(", schema);
            StringAssert.Contains("EnableModule(", schema);
            StringAssert.Contains("memory", schema);
        }

        private static ResponseCallTool CallThroughMcpPlugin(string tool, string json)
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
            return UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new RequestCallTool(tool, args)).Result.Value!;
        }

        [Test]
        public void TheProfilerToolsAreGoneAndTheAgentReachesTheProfilerThroughRql()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            Assert.IsEmpty(UnityMcpPluginEditor.Instance.Tools!.GetAllTools().Select(t => t.Name).Where(n => n.StartsWith("profiler-")));

            var start = CallThroughMcpPlugin(Tool_Rql.MutateToolId, "{\"query\": \"profiler | call Start(false)\"}");
            Assert.AreEqual(ResponseStatus.Success, start.Status, string.Join("\n", start.Content.Select(c => c.Text)));
            Assert.IsTrue(UnityProfiler.enabled);

            var read = CallThroughMcpPlugin(Tool_Rql.QueryToolId, "{\"query\": \"profiler | select enabled memory.totalAllocatedMB\"}");
            var text = string.Join("\n", read.Content.Select(c => c.Text));
            Assert.AreEqual(ResponseStatus.Success, read.Status, text);
            StringAssert.Contains("memory.totalAllocatedMB", text);
            StringAssert.Contains("true", text);
        }
    }
}
