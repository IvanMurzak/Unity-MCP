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
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public class LogUtils : IDisposable
    {
        ConcurrentQueue<LogEntry> _logEntries = new();
        readonly LogCache _logCache;
        readonly object _lockObject = new();
        volatile bool _isSubscribed = false;
        bool _disposed = false;

        public LogUtils(string? cacheFileName = null)
        {
            if (!MainThread.Instance.IsMainThread)
                throw new System.Exception($"{nameof(LogUtils)} must be initialized on the main thread.");

            _logCache = new LogCache(this, cacheFileName: cacheFileName);
            Subscribe();
        }

        public int LogEntries
        {
            get
            {
                lock (_lockObject)
                {
                    return _logEntries.Count;
                }
            }
        }

        public void ClearLogs(bool clearFile = true)
        {
            lock (_lockObject)
            {
                _logEntries = new ConcurrentQueue<LogEntry>();
            }
            if (clearFile)
                _logCache.ClearCacheFile();
        }

        public void ClearCacheFile()
        {
            _logCache.ClearCacheFile();
        }

        /// <summary>
        /// Synchronously saves all current log entries to the cache file.
        /// </summary>
        /// <returns>A task that completes when the save operation is finished.</returns>
        public void SaveToFileImmediate()
        {
            if (_disposed) return;
            _logCache.HandleLogCacheImmediate();
        }

        /// <summary>
        /// Asynchronously saves all current log entries to the cache file.
        /// </summary>
        /// <returns>A task that completes when the save operation is finished.</returns>
        public Task SaveToFile()
        {
            if (_disposed) return Task.CompletedTask;
            return _logCache.HandleLogCache();
        }

        /// <summary>
        /// Asynchronously loads log entries from the cache file and replaces the current log entries.
        /// </summary>
        /// <returns>A task that completes when the load operation is finished.</returns>
        public async Task LoadFromFile()
        {
            if (_disposed) return;
            var logWrapper = await _logCache.GetCachedLogEntriesAsync();
            lock (_lockObject)
            {
                _logEntries = new ConcurrentQueue<LogEntry>(logWrapper?.Entries ?? new LogEntry[0]);
            }
        }

        /// <summary>
        /// Asynchronously handles application quit by saving log entries to file and cleaning up resources.
        /// </summary>
        /// <returns>A task that completes when the quit handling is finished.</returns>
        public async Task HandleQuit()
        {
            if (_disposed) return;
            SaveToFileImmediate();
            await _logCache.HandleQuit();
        }

        public LogEntry[] GetAllLogs()
        {
            lock (_lockObject)
            {
                return _logEntries.ToArray();
            }
        }

        public void Subscribe()
        {
            lock (_lockObject)
            {
                if (!_isSubscribed && !_disposed)
                {
                    Application.logMessageReceivedThreaded += OnLogMessageReceived;
                    _isSubscribed = true;
                }
            }
        }

        void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            try
            {
                var logEntry = new LogEntry(
                    message: message,
                    stackTrace: stackTrace,
                    logType: type);

                lock (_lockObject)
                {
                    _logEntries.Enqueue(logEntry);
                }
            }
            catch
            {
                // Ignore logging errors to prevent recursive issues
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_isSubscribed)
            {
                Application.logMessageReceivedThreaded -= OnLogMessageReceived;
                _isSubscribed = false;
            }
            _logCache.Dispose();
        }
    }
}

