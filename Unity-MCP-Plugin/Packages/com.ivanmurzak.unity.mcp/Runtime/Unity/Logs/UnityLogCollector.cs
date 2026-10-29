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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Collects Unity log messages and manages saving/loading them to/from a cache file.
    /// </summary>
    public class UnityLogCollector : IDisposable
    {
        private static readonly Regex RichTextRegex = new Regex(@"</?(b|i|size|color|material|quad|a)\b[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        readonly ILogStorage _logStorage;
        readonly ThreadSafeBool _isDisposed = new(false);

        public UnityLogCollector(ILogStorage logStorage)
        {
            if (!MainThread.Instance.IsMainThread)
                throw new Exception($"{GetType().GetTypeShortName()} constructor must be initialized on the main thread.");

            _logStorage = logStorage ?? throw new ArgumentNullException(nameof(logStorage));

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        public void Clear()
        {
            if (_isDisposed.Value)
                return;

            _logStorage.Clear();
        }

        /// <summary>
        /// Synchronously saves all current log entries to the cache file.
        /// </summary>
        public void Save()
        {
            if (_isDisposed.Value)
                return;

            _logStorage.Flush();
        }

        /// <summary>
        /// Asynchronously saves all current log entries to the cache file.
        /// </summary>
        /// <returns>A task that completes when the save operation is finished.</returns>
        public Task SaveAsync()
        {
            if (_isDisposed.Value)
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

        public Task<LogEntry[]> QuerySinceAsync(
            long sinceSequence,
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            return _logStorage.QuerySinceAsync(sinceSequence, maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
        }

        public LogEntry[] QuerySince(
            long sinceSequence,
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            return _logStorage.QuerySince(sinceSequence, maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
        }

        /// <summary>
        /// McpPlugin's McpToolManager logs every tool response it sends ("[AI] McpToolManager Error / Success / Processing
        /// Response to AI: …"). These echoes are not captured: an agent checking "errors after my change" would find its own
        /// failed calls, and the large responses would push real entries out of the stored window.
        /// </summary>
        internal static bool IsToolResponseEcho(string? message) =>
            message != null && message.Contains("McpToolManager ") && message.Contains(" Response to AI:");

        /// <summary>
        /// McpPlugin also logs every tool (and prompt) call that fails (RunTool: "Tool execution failed for '…'" and the other refusals
        /// below), as an error line preceded by the exception itself. The agent already has that answer: like the response
        /// echoes, neither entry is captured (an agent reading "errors after my change" found dozens of its own retries).
        /// </summary>
        internal static bool IsToolFailureEcho(string? message) =>
            message != null && ToolFailureEchoRegex.IsMatch(message);

        static readonly Regex ToolFailureEchoRegex = new Regex(
            @"^(Tool execution failed for '|Prompt execution failed for '|Parameter validation failed for (tool|prompt) '|JSON parameter parsing failed for (tool|prompt) '|Parameter count mismatch for (tool|prompt) ')",
            RegexOptions.Compiled);

        // Debug.Log* raises logMessageReceivedThreaded synchronously on the logging thread: a flag set around the calls marks
        // their entries (the exception entry carries no text that would identify it).
        [ThreadStatic] static bool notCaptured;

        /// <summary>Runs <paramref name="log"/> (Debug.Log* calls) without capturing what it logs (no closure: logging is hot).</summary>
        internal static void LogUncaptured<T1, T2, T3>(Action<T1, T2, T3> log, T1 a, T2 b, T3 c)
        {
            var was = notCaptured;
            notCaptured = true;
            try { log(a, b, c); }
            finally { notCaptured = was; }
        }

        void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            try
            {
                if (notCaptured)
                    return;

                // Strip rich text tags
                var cleanMessage = RichTextRegex.Replace(message, string.Empty);

                if (IsToolResponseEcho(cleanMessage))
                    return;

                var logEntry = new LogEntry(
                    message: cleanMessage,
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
            if (!_isDisposed.TrySetTrue())
                return; // already disposed

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            _logStorage.Dispose();

            GC.SuppressFinalize(this);
        }

        // The storage closes itself from its own finalizer, without a flush; disposing it here would flush.
        ~UnityLogCollector() => Application.logMessageReceivedThreaded -= OnLogMessageReceived;
    }
}

