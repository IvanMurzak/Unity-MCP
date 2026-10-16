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
using System.Text.Json.Serialization;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public class LogEntry
    {
        public LogType LogType { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string? StackTrace { get; set; }

        /// <summary>
        /// Monotonic capture number (starts at 1), assigned by the log storage atomically with storing the entry.
        /// 0 means "not assigned" (never stored, or written before sequences existed).
        /// Serialized as lowercase <c>sequence</c>; agents pass the highest value they received as <c>sinceSequence</c>.
        /// </summary>
        [JsonPropertyName("sequence")]
        public long Sequence { get; set; }

        public LogEntry()
        {
            LogType = LogType.Log;
            Message = string.Empty;
            Timestamp = DateTime.Now;
            StackTrace = null;
        }
        public LogEntry(LogType logType, string message)
        {
            LogType = logType;
            Message = message;
            Timestamp = DateTime.Now;
            StackTrace = null;
        }
        public LogEntry(LogType logType, string message, string? stackTrace = null)
        {
            LogType = logType;
            Message = message;
            Timestamp = DateTime.Now;
            StackTrace = string.IsNullOrEmpty(stackTrace) ? null : stackTrace;
        }
        public LogEntry(LogType logType, string message, DateTime timestamp, string? stackTrace = null)
        {
            LogType = logType;
            Message = message;
            Timestamp = timestamp;
            StackTrace = string.IsNullOrEmpty(stackTrace) ? null : stackTrace;
        }

        /// <summary>
        /// Returns an independent copy. Storages share entry instances (the buffered storage hands out its own
        /// buffer slots), so anything that strips a field for a caller must strip it on a copy.
        /// </summary>
        public LogEntry Clone(bool includeStackTrace)
            => new LogEntry(LogType, Message, Timestamp, includeStackTrace ? StackTrace : null) { Sequence = Sequence };

        public override string ToString() => ToString(includeStackTrace: false);

        public string ToString(bool includeStackTrace)
        {
            return includeStackTrace && !string.IsNullOrEmpty(StackTrace)
                ? $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LogType}] {Message}\nStack Trace:\n{StackTrace}"
                : $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LogType}] {Message}";
        }
    }
}

