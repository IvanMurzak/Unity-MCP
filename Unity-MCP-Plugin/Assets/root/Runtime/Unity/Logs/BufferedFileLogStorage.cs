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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// A file-based log storage that uses the base class's lock-free ConcurrentQueue for buffering.
    /// Append operations are lock-free and safe to call from any thread (including Unity's log callback threads).
    /// This class extends the base with optional flush threshold support.
    /// </summary>
    public class BufferedFileLogStorage : FileLogStorage
    {
        protected readonly int _flushEntriesThreshold;
        private int _appendsSinceLastFlush;

        public BufferedFileLogStorage(
            ILogger? logger = null,
            int flushEntriesThreshold = 100,
            string? cacheFilePath = null,
            string? cacheFileName = null,
            int fileBufferSize = 4096,
            int maxFileSizeMB = DefaultMaxFileSizeMB,
            JsonSerializerOptions? jsonOptions = null)
            : base(logger, cacheFilePath, cacheFileName, fileBufferSize, maxFileSizeMB, jsonOptions)
        {
            if (flushEntriesThreshold <= 0)
                throw new ArgumentOutOfRangeException(nameof(flushEntriesThreshold), "Flush entries threshold must be greater than zero.");

            _flushEntriesThreshold = flushEntriesThreshold;
            _appendsSinceLastFlush = 0;
        }

        /// <summary>
        /// Appends log entries using lock-free ConcurrentQueue from base class.
        /// Triggers an async flush when threshold is reached.
        /// </summary>
        public override void Append(params LogEntry[] entries)
        {
            base.Append(entries);

            // Track appends and trigger async flush when threshold is reached
            // Using Interlocked for thread-safe increment
            var count = System.Threading.Interlocked.Add(ref _appendsSinceLastFlush, entries.Length);
            if (count >= _flushEntriesThreshold)
            {
                System.Threading.Interlocked.Exchange(ref _appendsSinceLastFlush, 0);
                // Fire-and-forget async flush - don't block the calling thread
                _ = Task.Run(() =>
                {
                    try { Flush(); }
                    catch { /* Ignore flush errors in background */ }
                });
            }
        }

        public override void Flush()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(Flush));
                return;
            }

            _fileLock.Wait();
            try
            {
                // Flush all pending entries from the concurrent queue
                FlushPendingEntries();
                fileWriteStream?.Flush();
                System.Threading.Interlocked.Exchange(ref _appendsSinceLastFlush, 0);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public override async Task FlushAsync()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(FlushAsync));
                return;
            }

            await _fileLock.WaitAsync();
            try
            {
                // Flush all pending entries from the concurrent queue
                FlushPendingEntries();
                if (fileWriteStream != null)
                    await fileWriteStream.FlushAsync();
                System.Threading.Interlocked.Exchange(ref _appendsSinceLastFlush, 0);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Closes and disposes the current file stream if open. Clears the log cache file.
        /// </summary>
        public override void Clear()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(Clear));
                return;
            }

            _fileLock.Wait();
            try
            {
                // Clear pending entries without writing them
                while (_pendingEntries.TryDequeue(out _)) { }

                fileWriteStream?.Close();
                fileWriteStream?.Dispose();
                fileWriteStream = null;
                System.Threading.Interlocked.Exchange(ref _appendsSinceLastFlush, 0);

                if (File.Exists(filePath))
                    File.Delete(filePath);

                if (File.Exists(filePath))
                    _logger.LogError("Failed to delete cache file: {file}", filePath);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public override LogEntry[] Query(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(Query));
                return Array.Empty<LogEntry>();
            }

            _fileLock.Wait();
            try
            {
                return QueryInternal(maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        protected override LogEntry[] QueryInternal(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            var result = new List<LogEntry>();
            var cutoffTime = lastMinutes > 0
                ? DateTime.Now.AddMinutes(-lastMinutes)
                : DateTime.MinValue;

            // 1. Get from pending queue (newest entries not yet flushed to file)
            // Convert to array to get a snapshot of pending entries
            var pendingArray = _pendingEntries.ToArray();
            for (int i = pendingArray.Length - 1; i >= 0; i--)
            {
                var entry = pendingArray[i];
                if (logTypeFilter.HasValue && entry.LogType != logTypeFilter.Value)
                    continue;

                if (lastMinutes > 0 && entry.Timestamp < cutoffTime)
                {
                    return result.AsEnumerable().Reverse().ToArray();
                }

                result.Add(entry);
                if (result.Count >= maxEntries)
                    return result.AsEnumerable().Reverse().ToArray();
            }

            // 2. Exit if we already have enough entries
            var neededLogsCount = maxEntries - result.Count;
            if (neededLogsCount <= 0)
                return result.AsEnumerable().Reverse().ToArray();

            result.Reverse();

            // 3. Get from file
            var fileEntries = base.QueryInternal(neededLogsCount, logTypeFilter, includeStackTrace, lastMinutes);
            result.AddRange(fileEntries);

            return result.ToArray();
        }

        ~BufferedFileLogStorage() => Dispose();
    }
}