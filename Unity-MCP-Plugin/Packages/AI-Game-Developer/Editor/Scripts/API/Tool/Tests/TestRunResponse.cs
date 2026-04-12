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
using System.ComponentModel;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestRunResponse
    {
        [Description("Summary of the test run including total, passed, failed, and skipped counts.")]
        public TestSummaryData Summary { get; set; } = new TestSummaryData();

        [Description("List of individual test results with details about each test.")]
        public List<TestResultData> Results { get; set; } = new List<TestResultData>();

        [Description("Log entries captured during test execution.")]
        public List<TestLogEntry>? Logs { get; set; }
    }
}
