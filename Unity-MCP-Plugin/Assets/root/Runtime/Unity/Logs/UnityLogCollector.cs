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
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Collects Unity log messages and manages saving/loading them to/from a cache file.
    /// </summary>
    public class UnityLogCollector : IDisposable
    {
        readonly ILogStorage _logStorage;
        bool _disposed = false;

        public UnityLogCollector(ILogStorage logStorage)
        {
            if (!MainThread.Instance.IsMainThread)
                throw new Exception($"{nameof(UnityLogCollector)} must be initialized on the main thread.");

            _logStorage = logStorage ?? throw new ArgumentNullException(nameof(logStorage));

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        public void Clear()
        {
            if (_disposed)
                return;

            _logStorage.Clear();
        }

        /// <summary>
        /// Synchronously saves all current log entries to the cache file.
        /// </summary>
        /// <returns>A task that completes when the save operation is finished.</returns>
        public void Save()
        {
            if (_disposed)
                return;

            _logStorage.Flush();
        }

        /// <summary>
        /// Asynchronously saves all current log entries to the cache file.
        /// </summary>
        /// <returns>A task that completes when the save operation is finished.</returns>
        public Task SaveAsync()
        {
            if (_disposed)
                return Task.CompletedTask;

            return _logStorage.FlushAsync();
        }

        public Task<LogEntry[]> QueryAsync(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            return _logStorage.QueryAsync(maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
        }

        public LogEntry[] Query(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            return _logStorage.Query(maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
        }

        void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            try
            {
                var logEntry = new LogEntry(
                    message: message,
                    stackTrace: stackTrace,
                    logType: type);

                _logStorage.Append(logEntry);
            }
            catch
            {
                // Ignore logging errors to prevent recursive issues
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Save();

            _disposed = true;

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            _logStorage.Dispose();
        }
    }
}

