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
using System.Collections.Concurrent;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public static class LogUtils
    {
        public const int MaxLogEntries = 5000; // Default max entries to keep in memory

        static readonly ConcurrentQueue<LogEntry> _logEntries = new();
        static readonly object _lockObject = new();
        static bool _isSubscribed = false;

        public static int LogEntries
        {
            get
            {
                lock (_lockObject)
                {
                    return _logEntries.Count;
                }
            }
        }

        public static void ClearLogs()
        {
            lock (_lockObject)
            {
                _logEntries.Clear();
            }
        }
        public static LogEntry[] GetAllLogs()
        {
            lock (_lockObject)
            {
                return _logEntries.ToArray();
            }
        }

        static LogUtils()
        {
            EnsureSubscribed();
        }

        public static void EnsureSubscribed()
        {
            MainThread.Instance.RunAsync(() =>
            {
                lock (_lockObject)
                {
                    if (!_isSubscribed)
                    {
                        Application.logMessageReceived += OnLogMessageReceived;
                        Application.logMessageReceivedThreaded += OnLogMessageReceived;
                        _isSubscribed = true;
                    }
                }
            });
        }

        static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            try
            {
                var logEntry = new LogEntry(message, stackTrace, type);
                lock (_lockObject)
                {
                    _logEntries.Enqueue(logEntry);

                    // Keep only the latest entries to prevent memory overflow
                    while (_logEntries.Count > MaxLogEntries)
                    {
                        var success = _logEntries.TryDequeue(out _);
                        if (!success)
                            break; // Should not happen, but just in case
                    }
                }
            }
            catch
            {
                // Ignore logging errors to prevent recursive issues
            }
        }
    }
}

