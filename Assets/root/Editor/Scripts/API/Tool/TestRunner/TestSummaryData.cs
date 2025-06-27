#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
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
    }
}