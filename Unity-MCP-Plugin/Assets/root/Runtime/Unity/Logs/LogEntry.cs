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
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public LogType LogType { get; set; }
        public DateTime Timestamp { get; set; }

        public LogEntry(string message, string stackTrace, LogType logType)
        {
            Message = message;
            StackTrace = stackTrace;
            LogType = logType;
            Timestamp = DateTime.Now;
        }

        public override string ToString() => ToString(includeStackTrace: false);

        public string ToString(bool includeStackTrace)
        {
            if (includeStackTrace && !string.IsNullOrEmpty(StackTrace))
                return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LogType}] {Message}\nStack Trace:\n{StackTrace}";
            else
                return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LogType}] {Message}";
        }
    }
}

