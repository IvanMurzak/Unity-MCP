/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public static class LogUtils
    {
        public const int MaxLogEntries = 5000; // Default max entries to keep in memory
        private const int MemoryCacheSize = 100; // Short-term cache size before flushing to file

        static readonly ConcurrentQueue<LogEntry> _logEntries = new(); // Short-term cache
        static readonly object _lockObject = new();
        static bool _isSubscribed = false;

        public static int LogEntries
        {
            get
            {
                lock (_lockObject)
                {
                    // Return combined count from memory cache and file
                    return _logEntries.Count + LogFileManager.GetLogEntryCount();
                }
            }
        }

        public static void ClearLogs()
        {
            lock (_lockObject)
            {
                _logEntries.Clear();
                LogFileManager.ClearLogFile();
            }
        }

        public static LogEntry[] GetAllLogs()
        {
            lock (_lockObject)
            {
                // Combine file logs with memory cache logs
                var fileEntries = LogFileManager.ReadLogEntries();
                var memoryEntries = _logEntries.ToArray();
                
                // Merge and sort by timestamp
                var allEntries = new List<LogEntry>(fileEntries);
                allEntries.AddRange(memoryEntries);
                
                return allEntries.OrderBy(entry => entry.timestamp).ToArray();
            }
        }

        /// <summary>
        /// Gets the last N log entries efficiently, with optional filtering
        /// </summary>
        public static LogEntry[] GetLastLogs(int maxEntries, LogType? filterType = null, DateTime? sinceTime = null)
        {
            lock (_lockObject)
            {
                var result = new List<LogEntry>();
                
                // First, get from memory cache (most recent)
                var memoryEntries = _logEntries.ToArray()
                    .Where(entry => filterType == null || entry.logType == filterType)
                    .Where(entry => sinceTime == null || entry.timestamp >= sinceTime)
                    .OrderBy(entry => entry.timestamp)
                    .ToArray();
                
                result.AddRange(memoryEntries);
                
                // If we need more entries, get from file
                if (result.Count < maxEntries)
                {
                    var remainingCount = maxEntries - result.Count;
                    var fileEntries = LogFileManager.GetLastLogEntries(remainingCount * 2, filterType); // Get extra to account for time filtering
                    
                    if (sinceTime.HasValue)
                    {
                        fileEntries = fileEntries.Where(entry => entry.timestamp >= sinceTime.Value).ToArray();
                    }
                    
                    // Remove duplicates and merge
                    var memoryTimestamps = new HashSet<DateTime>(memoryEntries.Select(e => e.timestamp));
                    var uniqueFileEntries = fileEntries.Where(entry => !memoryTimestamps.Contains(entry.timestamp));
                    
                    result.InsertRange(0, uniqueFileEntries);
                }
                
                // Sort and take the last N entries
                return result.OrderBy(entry => entry.timestamp)
                    .TakeLast(maxEntries)
                    .ToArray();
            }
        }

        static LogUtils()
        {
            EnsureSubscribed();
        }

        static void EnsureSubscribed()
        {
            MainThread.Instance.RunAsync(() =>
            {
                lock (_lockObject)
                {
                    if (!_isSubscribed)
                    {
                        Application.logMessageReceived += OnLogMessageReceived;
                        _isSubscribed = true;
                    }
                }
            });
        }

        static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            var logEntry = new LogEntry(message, stackTrace, type);
            
            lock (_lockObject)
            {
                // Add to short-term memory cache
                _logEntries.Enqueue(logEntry);

                // Save to file immediately for persistence
                LogFileManager.AppendLogEntry(logEntry);

                // Clean memory cache after saving to file (keep only recent entries)
                while (_logEntries.Count > MemoryCacheSize)
                {
                    _logEntries.TryDequeue(out _);
                }
            }
        }
    }
}

