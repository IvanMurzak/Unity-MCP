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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class FileLogStorage : ILogStorage, IDisposable
    {
        protected const int DefaultMaxFileSizeMB = 512;

        protected readonly ILogger _logger;
        protected readonly string _cacheFilePath;
        protected readonly string _cacheFileName;
        protected readonly string _cacheFile;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected readonly SemaphoreSlim _fileLock = new(1, 1);
        protected readonly int _fileBufferSize;
        protected readonly long _maxFileSizeBytes;
        protected readonly ThreadSafeBool _isDisposed = new(false);

        protected FileStream? fileWriteStream;

        public FileLogStorage(
            ILogger? logger = null,
            string? cacheFilePath = null,
            string? cacheFileName = null,
            int fileBufferSize = 4096,
            int maxFileSizeMB = DefaultMaxFileSizeMB,
            JsonSerializerOptions? jsonOptions = null)
        {
            if (!MainThread.Instance.IsMainThread)
                throw new Exception($"{nameof(FileLogStorage)} must be initialized on the main thread.");

            if (fileBufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(fileBufferSize), "File buffer size must be greater than zero.");

            if (maxFileSizeMB <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFileSizeMB), "Max file size must be greater than zero.");

            _logger = logger ?? UnityLoggerFactory.LoggerFactory.CreateLogger(GetType().GetTypeShortName());

            _cacheFilePath = cacheFilePath ?? (Application.isEditor
                ? $"{Path.GetDirectoryName(Application.dataPath)}/Temp/mcp-server"
                : $"{Application.persistentDataPath}/Temp/mcp-server");

            _cacheFileName = cacheFileName ?? (Application.isEditor
                ? "ai-editor-logs.txt"
                : "ai-player-logs.txt");

            _cacheFile = $"{Path.Combine(_cacheFilePath, _cacheFileName)}";
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            _fileBufferSize = fileBufferSize;
            _maxFileSizeBytes = maxFileSizeMB * 1024L * 1024L;

            fileWriteStream = CreateWriteStream();
        }

        protected virtual FileStream CreateWriteStream()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(CreateWriteStream));
                throw new ObjectDisposedException(GetType().GetTypeShortName());
            }
            if (!Directory.Exists(_cacheFilePath))
                Directory.CreateDirectory(_cacheFilePath);
            return new FileStream(_cacheFile, FileMode.Append, FileAccess.Write, FileShare.Read, bufferSize: _fileBufferSize, useAsync: false);
        }

        public virtual void Flush()
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
                fileWriteStream?.Flush();
            }
            finally
            {
                _fileLock.Release();
            }
        }
        public virtual async Task FlushAsync()
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
                if (fileWriteStream != null)
                    await fileWriteStream.FlushAsync();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public virtual Task AppendAsync(params LogEntry[] entries)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(AppendAsync));
                return Task.CompletedTask;
            }
            return Task.Run(async () =>
            {
                await _fileLock.WaitAsync();
                try
                {
                    AppendInternal(entries);
                }
                finally
                {
                    _fileLock.Release();
                }
            });
        }

        public virtual void Append(params LogEntry[] entries)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(Append));
                return;
            }
            _fileLock.Wait();
            try
            {
                AppendInternal(entries);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        protected virtual void AppendInternal(params LogEntry[] entries)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(AppendInternal));
                return;
            }
            fileWriteStream ??= CreateWriteStream();

            // Check if file size limit reached and reset if needed
            if (fileWriteStream.Length >= _maxFileSizeBytes)
            {
                ResetLogFile();
            }

            foreach (var entry in entries)
            {
                System.Text.Json.JsonSerializer.Serialize(fileWriteStream, entry, _jsonOptions);
                fileWriteStream.WriteByte((byte)'\n');
            }
            fileWriteStream.Flush();
        }

        /// <summary>
        /// Resets the log file by deleting it and creating a new one.
        /// Called when file size limit is reached.
        /// </summary>
        protected virtual void ResetLogFile()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(ResetLogFile));
                return;
            }

            _logger.LogInformation("Log file size limit reached ({maxSizeMB}MB). Resetting log file.",
                _maxFileSizeBytes / (1024 * 1024));

            fileWriteStream?.Flush();
            fileWriteStream?.Close();
            fileWriteStream?.Dispose();
            fileWriteStream = null;

            if (File.Exists(_cacheFile))
                File.Delete(_cacheFile);

            fileWriteStream = CreateWriteStream();
        }

        /// <summary>
        /// Closes and disposes the current file stream if open. Clears the log cache file.
        /// </summary>
        public virtual void Clear()
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

        public virtual Task<LogEntry[]> QueryAsync(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(QueryAsync));
                return Task.FromResult(Array.Empty<LogEntry>());
            }
            return Task.Run(() => Query(maxEntries, logTypeFilter, includeStackTrace, lastMinutes));
        }

        public virtual LogEntry[] Query(
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

        protected virtual LogEntry[] QueryInternal(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (!File.Exists(_cacheFile))
                return Array.Empty<LogEntry>();

            using (var fileStream = new FileStream(_cacheFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var cutoffTime = lastMinutes > 0
                    ? DateTime.Now.AddMinutes(-lastMinutes)
                    : (DateTime?)null;

                var allLogs = ReadLogEntriesFromLinesInReverse(fileStream, cutoffTime);

                // Apply log type filter
                if (logTypeFilter.HasValue)
                {
                    allLogs = allLogs
                        .Where(log => log.LogType == logTypeFilter.Value);
                }

                // Take the most recent entries (up to maxEntries)
                var filteredLogs = allLogs
                    .Take(maxEntries)
                    .Reverse()
                    .ToArray();

                return filteredLogs;
            }
        }

        protected virtual IEnumerable<LogEntry> ReadLogEntriesFromLinesInReverse(FileStream fileStream, DateTime? cutoffTime = null)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(ReadLogEntriesFromLinesInReverse));
                yield break;
            }
            var position = fileStream.Length;
            if (position == 0) yield break;

            var buffer = new byte[_fileBufferSize];
            var lineBuffer = new List<byte>();

            while (position > 0)
            {
                var bytesToRead = (int)Math.Min(position, _fileBufferSize);
                position -= bytesToRead;
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Read(buffer, 0, bytesToRead);

                for (int i = bytesToRead - 1; i >= 0; i--)
                {
                    var b = buffer[i];
                    if (b == '\n')
                    {
                        if (lineBuffer.Count > 0)
                        {
                            lineBuffer.Reverse();
                            var logEntry = DeserializeLogEntry(lineBuffer);
                            if (logEntry != null)
                            {
                                if (cutoffTime.HasValue && logEntry.Timestamp < cutoffTime.Value)
                                    yield break;

                                yield return logEntry;
                            }
                            lineBuffer.Clear();
                        }
                    }
                    else if (b == '\r')
                    {
                        // Ignore \r
                    }
                    else
                    {
                        lineBuffer.Add(b);
                    }
                }
            }

            if (lineBuffer.Count > 0)
            {
                lineBuffer.Reverse();
                var logEntry = DeserializeLogEntry(lineBuffer);
                if (logEntry != null)
                {
                    if (cutoffTime.HasValue && logEntry.Timestamp < cutoffTime.Value)
                        yield break;

                    yield return logEntry;
                }
            }
        }

        protected virtual LogEntry? DeserializeLogEntry(List<byte> jsonBytes)
        {
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes.ToArray());
            return DeserializeLogEntry(json);
        }

        protected virtual LogEntry? DeserializeLogEntry(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<LogEntry>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public virtual void Dispose()
        {
            if (!_isDisposed.TrySetTrue())
                return; // already disposed

            Flush();

            fileWriteStream?.Close();
            fileWriteStream?.Dispose();
            fileWriteStream = null;

            _fileLock.Dispose();

            GC.SuppressFinalize(this);
        }

        ~FileLogStorage() => Dispose();
    }
}