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
using System.Collections.Concurrent;
using com.IvanMurzak.ReflectorNet.Utils;
using R3;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public static class LogUtils
    {
        public const int MaxLogEntries = 5000; // Default max entries to keep in memory
        static ConcurrentQueue<LogEntry> _logEntries = new();
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
                        var subscription = Observable.Timer(
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(1)
                        )
                        .Subscribe(x =>
                        {
                            LogCache.HandleLogCache();
                        });
                        _isSubscribed = true;
                        _logEntries = LogCache.GetCachedLogEntries();
                    }
                }
            });
        }

        static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            try
            {
<<<<<<< HEAD
                var logEntry = new LogEntry(message, stackTrace, type);
                lock (_lockObject)
                {
                    _logEntries.Enqueue(logEntry);
=======
                // LogCache.CacheLogEntry(logEntry);
                _logEntries.Enqueue(logEntry);
>>>>>>> a0d3f74a (update log cache with R3 timer to remove unityeditor dependency and update editor-logs.txt filepath)

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

