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
using System;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestSummaryData
    {
        public TestRunStatus Status { get; set; } = TestRunStatus.Unknown;
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public TimeSpan Duration { get; set; }

        public void Clear()
        {
            Status = TestRunStatus.Unknown;
            TotalTests = 0;
            PassedTests = 0;
            FailedTests = 0;
            SkippedTests = 0;
            Duration = TimeSpan.Zero;
        }
    }
}
