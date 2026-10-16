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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
            lock (_fileMutex)
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
        }
        public override Task FlushAsync()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(FlushAsync));
                return Task.CompletedTask;
            }
            lock (_fileMutex)
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
            return Task.CompletedTask;
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
            lock (_fileMutex)
            {
                fileWriteStream?.Close();
                fileWriteStream?.Dispose();
                fileWriteStream = null;
                _logEntriesBufferLength = 0;

                if (File.Exists(filePath))
                    File.Delete(filePath);

                if (File.Exists(filePath))
                    _logger.LogError("Failed to delete cache file: {file}", filePath);
            }
        }

        protected override IEnumerable<LogEntry> ReadNewestFirst(DateTime? cutoffTime)
        {
            // Not-yet-written entries first (newest at the end of the buffer): all of them are newer than the file.
            for (int i = _logEntriesBufferLength - 1; i >= 0; i--)
            {
                var entry = _logEntriesBuffer[i];
                if (cutoffTime.HasValue && entry.Timestamp < cutoffTime.Value)
                    yield break;

                yield return entry;
            }

            foreach (var entry in base.ReadNewestFirst(cutoffTime))
                yield return entry;
        }

        ~BufferedFileLogStorage() => Dispose();
    }
}