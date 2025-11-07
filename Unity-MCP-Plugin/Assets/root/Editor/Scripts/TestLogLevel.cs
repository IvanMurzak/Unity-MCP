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
using UnityEditor;
using UnityEngine;
using com.IvanMurzak.Unity.MCP;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static class TestLogLevel
    {
        [MenuItem("Window/AI Game Developer/Test LogDebug")]
        public static void TestLogDebugMethod()
        {
            var currentLogLevel = UnityMcpPlugin.LogLevel;
            Debug.Log($"Current LogLevel configuration: {currentLogLevel} ({(int)currentLogLevel})");

            Debug.Log($"IsLogEnabled(Trace): {UnityMcpPlugin.IsLogEnabled(LogLevel.Trace)}");
            Debug.Log($"IsLogEnabled(Debug): {UnityMcpPlugin.IsLogEnabled(LogLevel.Debug)}");
            Debug.Log($"IsLogEnabled(Info): {UnityMcpPlugin.IsLogEnabled(LogLevel.Info)}");
            Debug.Log($"IsLogEnabled(Warning): {UnityMcpPlugin.IsLogEnabled(LogLevel.Warning)}");
            Debug.Log($"IsLogEnabled(Error): {UnityMcpPlugin.IsLogEnabled(LogLevel.Error)}");

            Debug.Log("=== Testing all log levels ===");
            UnityMcpPlugin.Instance.LogTrace("This is a TRACE message", typeof(TestLogLevel));
            UnityMcpPlugin.Instance.LogDebug("This is a DEBUG message", typeof(TestLogLevel));
            UnityMcpPlugin.Instance.LogInfo("This is an INFO message", typeof(TestLogLevel));
            UnityMcpPlugin.Instance.LogWarn("This is a WARNING message", typeof(TestLogLevel));
            UnityMcpPlugin.Instance.LogError("This is an ERROR message", typeof(TestLogLevel));
        }
    }
}
