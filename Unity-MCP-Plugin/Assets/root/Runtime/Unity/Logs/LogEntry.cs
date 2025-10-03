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
    [Serializable]
    public class LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType logType;
        public DateTime timestamp;
        public string logTypeString;

        public LogEntry(string message, string stackTrace, LogType logType)
        {
            this.message = message;
            this.stackTrace = stackTrace;
            this.logType = logType;
            this.timestamp = DateTime.Now;
            this.logTypeString = logType.ToString();
        }

        public override string ToString() => ToString(includeStackTrace: false);

        public string ToString(bool includeStackTrace)
        {
            if (includeStackTrace && !string.IsNullOrEmpty(stackTrace))
                return $"{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{logTypeString}] {message}\nStack Trace:\n{stackTrace}";
            else
                return $"{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{logTypeString}] {message}";
        }
    }
}

