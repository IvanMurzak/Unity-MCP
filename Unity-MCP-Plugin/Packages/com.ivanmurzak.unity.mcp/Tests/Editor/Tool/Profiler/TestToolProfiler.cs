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
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler : BaseTest
    {
        protected Tool_Profiler _tool = null!;
        HashSet<string> _enabledModulesSnapshot = null!;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_Profiler();
            // Snapshot the shared static EnabledModules set so a test that toggles it
            // (or fails partway through toggling it) cannot pollute later tests.
            _enabledModulesSnapshot = new HashSet<string>(Tool_Profiler.EnabledModules);
        }

        [TearDown]
        public void TestTearDown()
        {
            // Leave the profiler in a known-disabled state so subsequent tests do not
            // see drift from a previous test's Start() call.
            _tool.Stop();

            // Restore EnabledModules to whatever it was at SetUp time. This is the
            // safety net for asymmetric Enable/Disable sequences (or assertion failures
            // mid-test) that would otherwise leave the static set in a drifted state.
            Tool_Profiler.EnabledModules.Clear();
            foreach (var name in _enabledModulesSnapshot)
                Tool_Profiler.EnabledModules.Add(name);
        }
    }
}
