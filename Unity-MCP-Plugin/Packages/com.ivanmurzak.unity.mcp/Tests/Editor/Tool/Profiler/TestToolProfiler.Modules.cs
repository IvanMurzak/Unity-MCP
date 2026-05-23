/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler
    {
        [Test]
        public void ListModules_IncludesAllKnownModules()
        {
            var data = _tool.ListModules();
            Assert.IsNotNull(data, "ListModules() should never return null.");
            Assert.IsNotNull(data.Modules, "Modules should be populated.");
            Assert.AreEqual(Tool_Profiler.AvailableModules.Count, data.Modules!.Count,
                "ListModules() should return one entry per AvailableModules entry.");

            foreach (var name in Tool_Profiler.AvailableModules)
                Assert.IsTrue(data.Modules.Any(m => m.Name == name), $"Module list should contain '{name}'.");
        }

        [Test]
        public void EnableModule_TogglesBookkeeping()
        {
            // CPU is in the default-enabled set per Tool_Profiler.cs.
            var disableResult = _tool.EnableModule("CPU", enabled: false);
            Assert.IsTrue(disableResult.Contains("[Success]"), $"Disabling CPU should succeed.\n{disableResult}");

            var afterDisable = _tool.ListModules().Modules!.First(m => m.Name == "CPU");
            Assert.IsFalse(afterDisable.Enabled, "CPU should be marked disabled after EnableModule(CPU, false).");

            var enableResult = _tool.EnableModule("CPU", enabled: true);
            Assert.IsTrue(enableResult.Contains("[Success]"), $"Enabling CPU should succeed.\n{enableResult}");

            var afterEnable = _tool.ListModules().Modules!.First(m => m.Name == "CPU");
            Assert.IsTrue(afterEnable.Enabled, "CPU should be marked enabled after EnableModule(CPU, true).");
        }

        [Test]
        public void EnableModule_RejectsEmptyModuleName()
        {
            var result = _tool.EnableModule("", enabled: true);
            Assert.IsTrue(result.Contains("[Error]"), $"Empty module name should return error.\n{result}");
            Assert.IsTrue(result.Contains("required"), "Error should mention required.");
        }

        [Test]
        public void EnableModule_RejectsUnknownModuleName()
        {
            var result = _tool.EnableModule("NotARealModule");
            Assert.IsTrue(result.Contains("[Error]"), $"Unknown module name should return error.\n{result}");
            Assert.IsTrue(result.Contains("Unknown profiler module"), "Error should mention 'Unknown profiler module'.");
        }
    }
}
