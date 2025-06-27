#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestResultData
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
    }
}