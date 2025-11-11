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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestLogEntry
    {
        public string Condition { get; set; }
        public string? StackTrace { get; set; }
        public LogType Type { get; set; }
        public DateTime Timestamp { get; set; }

        public int LogLevel => ToLogLevel(Type);

        public TestLogEntry(LogType type, string condition, string? stackTrace = null) : this(type, condition, stackTrace, DateTime.Now)
        {
            // none
        }
        public TestLogEntry(LogType type, string condition, string? stackTrace, DateTime timestamp)
        {
            Condition = condition;
            StackTrace = stackTrace;
            Type = type;
            Timestamp = timestamp;
        }

        public string ToStringFormat(bool includeType, bool includeStacktrace)
        {
            return includeStacktrace && !string.IsNullOrEmpty(StackTrace)
                ? includeType
                    ? $"[{Timestamp:HH:mm:ss}] [{Type}] {Condition}\n{StackTrace}"
                    : $"[{Timestamp:HH:mm:ss}] {Condition}\n{StackTrace}"
                : includeType
                    ? $"[{Timestamp:HH:mm:ss}] [{Type}] {Condition}"
                    : $"[{Timestamp:HH:mm:ss}] {Condition}";
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(StackTrace)
                ? $"[{Timestamp:HH:mm:ss}] [{Type}] {Condition}"
                : $"[{Timestamp:HH:mm:ss}] [{Type}] {Condition}\n{StackTrace}";
        }

        public static int ToLogLevel(LogType type)
        {
            return type switch
            {
                LogType.Log => 1,
                LogType.Warning => 2,
                LogType.Assert => 3,
                LogType.Error => 4,
                LogType.Exception => 5,
                _ => 6
            };
        }
    }
}
