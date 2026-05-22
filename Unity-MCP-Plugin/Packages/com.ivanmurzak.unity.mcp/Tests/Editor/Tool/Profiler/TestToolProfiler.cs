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
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolProfiler : BaseTest
    {
        protected Tool_Profiler _tool = null!;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_Profiler();
        }

        [TearDown]
        public void TestTearDown()
        {
            // Leave the profiler in a known-disabled state so subsequent tests do not
            // see drift from a previous test's Start() call.
            _tool.Stop();
        }
    }
}
