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

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestRunResponse
    {
        public TestSummaryData Summary { get; set; } = new TestSummaryData();
        public List<TestResultData> Results { get; set; } = new List<TestResultData>();
        public List<TestLogEntry>? Logs { get; set; }
    }
}
