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

namespace com.IvanMurzak.Unity.MCP
{
    public class LogEntry
    {
        public LogType LogType { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }
        public string? StackTrace { get; }

        public LogEntry(LogType logType, string message)
        {
            LogType = logType;
            Message = message;
            Timestamp = DateTime.Now;
        }
        public LogEntry(LogType logType, string message, string? stackTrace = null)
        {
            LogType = logType;
            Message = message;
            Timestamp = DateTime.Now;
            StackTrace = stackTrace;
        }
        public LogEntry(LogType logType, string message, DateTime timestamp, string? stackTrace = null)
        {
            LogType = logType;
            Message = message;
            Timestamp = timestamp;
            StackTrace = stackTrace;
        }

        public override string ToString() => ToString(includeStackTrace: false);

        public string ToString(bool includeStackTrace)
        {
            return includeStackTrace && !string.IsNullOrEmpty(StackTrace)
                ? $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LogType}] {Message}\nStack Trace:\n{StackTrace}"
                : $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LogType}] {Message}";
        }
    }
}

