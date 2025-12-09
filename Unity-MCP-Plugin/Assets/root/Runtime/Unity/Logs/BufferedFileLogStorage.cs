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
    public class BufferedFileLogStorage : FileLogStorage
    {
        protected readonly int _flushEntriesThreshold;
        protected readonly LogEntry[] _logEntriesBuffer;
        protected int _logEntriesBufferLength;

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
            _logEntriesBuffer = new LogEntry[flushEntriesThreshold];
            _logEntriesBufferLength = 0;
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
                // Flush buffered entries to file
                if (_logEntriesBufferLength > 0)
                {
                    var entriesToFlush = new LogEntry[_logEntriesBufferLength];
                    Array.Copy(_logEntriesBuffer, entriesToFlush, _logEntriesBufferLength);
                    base.AppendInternal(entriesToFlush);
                    _logEntriesBufferLength = 0;
                }
                fileWriteStream?.Flush();
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
                // Flush buffered entries to file
                if (_logEntriesBufferLength > 0)
                {
                    var entriesToFlush = new LogEntry[_logEntriesBufferLength];
                    Array.Copy(_logEntriesBuffer, entriesToFlush, _logEntriesBufferLength);
                    base.AppendInternal(entriesToFlush);
                    _logEntriesBufferLength = 0;
                }

                if (fileWriteStream != null)
                    await fileWriteStream.FlushAsync();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        protected override void AppendInternal(params LogEntry[] entries)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(AppendInternal));
                return;
            }
            if (_logEntriesBufferLength >= _flushEntriesThreshold)
            {
                base.AppendInternal(_logEntriesBuffer);
                _logEntriesBufferLength = 0;
            }
            foreach (var entry in entries)
            {
                _logEntriesBuffer[_logEntriesBufferLength] = entry;
                _logEntriesBufferLength++;

                if (_logEntriesBufferLength >= _flushEntriesThreshold)
                {
                    base.AppendInternal(_logEntriesBuffer);
                    _logEntriesBufferLength = 0;
                }
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
                fileWriteStream?.Close();
                fileWriteStream?.Dispose();
                fileWriteStream = null;
                _logEntriesBufferLength = 0;

                if (File.Exists(_cacheFile))
                    File.Delete(_cacheFile);

                if (File.Exists(_cacheFile))
                    _logger.LogError("Failed to delete cache file: {file}", _cacheFile);
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
                ? System.DateTime.Now.AddMinutes(-lastMinutes)
                : System.DateTime.MinValue;

            // 1. Get from buffer (Newest are at the end of buffer)
            for (int i = _logEntriesBufferLength - 1; i >= 0; i--)
            {
                var entry = _logEntriesBuffer[i];
                if (logTypeFilter.HasValue && entry.LogType != logTypeFilter.Value)
                    continue;

                if (lastMinutes > 0)
                {
                    if (entry.Timestamp < cutoffTime)
                    {
                        return result.AsEnumerable().Reverse().ToArray();
                    }
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