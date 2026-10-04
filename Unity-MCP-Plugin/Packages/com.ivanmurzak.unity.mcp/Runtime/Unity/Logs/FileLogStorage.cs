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

        /// <summary>
        /// While running, the sequence high-water mark is persisted in blocks of this size (a reserved ceiling),
        /// so a crash or an editor restart can never reissue a number, without a disk write per entry.
        /// An orderly Dispose persists the exact value instead.
        /// </summary>
        const long SequenceReservation = 256;

        protected readonly ILogger _logger;
        protected readonly string _directoryPath;
        protected readonly string _requestedFileName;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected readonly object _fileMutex = new();
        protected readonly int _fileBufferSize;
        protected readonly long _maxFileSizeBytes;
        protected readonly ThreadSafeBool _isDisposed = new(false);

        protected string fileName;
        protected string filePath;

        protected FileStream? fileWriteStream;

        /// <summary>
        /// Sidecar holding the sequence high-water mark. It always covers every number issued, so deleting the log
        /// file (Clear, ResetLogFile, the Temp/ wipe on Editor start) never restarts the counter.
        /// </summary>
        readonly string _sequenceFilePath;

        /// <summary>Highest sequence issued so far. Only touched under <see cref="_fileMutex"/>.</summary>
        long _sequence;

        /// <summary>Highest value known to be persisted in the sidecar. Only touched under <see cref="_fileMutex"/>.</summary>
        long _sequenceCeiling;

        public FileLogStorage(
            ILogger? logger = null,
            string? directoryPath = null,
            string? requestedFileName = null,
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

            _directoryPath = Path.GetFullPath(directoryPath ?? (Application.isEditor
                ? $"{Path.GetDirectoryName(Application.dataPath)}/Temp/mcp-server"
                : $"{Application.persistentDataPath}/Temp/mcp-server"));

            _requestedFileName = requestedFileName ?? (Application.isEditor
                ? "ai-editor-logs.txt"
                : "ai-player-logs.txt");

            if (!Directory.Exists(_directoryPath))
                Directory.CreateDirectory(_directoryPath);

            _fileBufferSize = fileBufferSize;
            _maxFileSizeBytes = maxFileSizeMB * 1024L * 1024L;

            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            fileWriteStream = CreateWriteStream(_requestedFileName, out fileName, out filePath);

            // Editor: Library/ survives the Temp/ wipe that happens on every Editor start. Not Library/mcp-server:
            // that is the server binary cache, deleted on every server update. A caller-chosen directory keeps its
            // sidecar next to the log; so does a player build (no Library there).
            var sequenceDirectory = directoryPath == null && Application.isEditor
                ? Path.GetFullPath($"{Path.GetDirectoryName(Application.dataPath)}/Library/mcp-logs")
                : _directoryPath;
            try
            {
                if (!Directory.Exists(sequenceDirectory))
                    Directory.CreateDirectory(sequenceDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create sequence directory {dir}. Using the log directory.", sequenceDirectory);
                sequenceDirectory = _directoryPath;
            }
            _sequenceFilePath = Path.Combine(sequenceDirectory, Path.GetFileNameWithoutExtension(_requestedFileName) + ".sequence");

            // Continue where the previous instance stopped: the larger of the persisted high-water mark
            // (survives Clear and a Temp/ wipe) and the newest entry still in the log file.
            lock (_fileMutex)
            {
                var persisted = ReadPersistedSequence();
                _sequence = Math.Max(persisted, ReadHighestSequenceInFile());
                _sequenceCeiling = persisted;

                // The log knows a higher number than the sidecar (the sidecar was lost): record it now, so the
                // sidecar alone covers every number issued before anything deletes the log.
                if (_sequence > persisted)
                    PersistSequence(_sequence);
            }
        }

        long ReadPersistedSequence()
        {
            try
            {
                if (File.Exists(_sequenceFilePath)
                    && long.TryParse(File.ReadAllText(_sequenceFilePath).Trim(), out var value)
                    && value > 0)
                    return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read sequence file {file}.", _sequenceFilePath);
            }
            return 0;
        }

        long ReadHighestSequenceInFile()
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                // Sequences ascend through the file, so the newest readable entry carries the highest one.
                foreach (var entry in ReadLogEntriesFromLinesInReverse(stream))
                    return Math.Max(entry.Sequence, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read the last sequence from {file}.", filePath);
            }
            return 0;
        }

        /// <summary>
        /// Persists <paramref name="value"/> to the sidecar. Writes a temp file and swaps it in, so a failed
        /// write can never truncate the existing high-water mark. Must be called under <see cref="_fileMutex"/>.
        /// </summary>
        void PersistSequence(long value)
        {
            // Recorded before the write: the warning below is itself a Unity log that re-enters Append on this
            // thread (the mutex is reentrant), and a failing disk must not make that recurse.
            _sequenceCeiling = value;
            try
            {
                var tempPath = _sequenceFilePath + ".tmp";
                File.WriteAllText(tempPath, value.ToString());
                if (File.Exists(_sequenceFilePath))
                    File.Replace(tempPath, _sequenceFilePath, null);
                else
                    File.Move(tempPath, _sequenceFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist sequence {value} to {file}.", value, _sequenceFilePath);
            }
        }

        /// <summary>
        /// Gives every entry its sequence number. Must be called under <see cref="_fileMutex"/>, in the same
        /// critical section that stores the entry, because the capture callback fires from many threads.
        /// </summary>
        void AssignSequences(LogEntry[] entries)
        {
            // Reserve first, assign after: a failing reservation logs a warning that re-enters Append, and that
            // nested entry must get its number before this batch is numbered, or the file would not ascend.
            if (_sequence + entries.Length > _sequenceCeiling)
                PersistSequence(_sequence + entries.Length + SequenceReservation);

            foreach (var entry in entries)
                entry.Sequence = ++_sequence;
        }

        protected virtual FileStream CreateWriteStream(string fileName, out string resultFileName, out string resultFilePath)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(CreateWriteStream));
                throw new ObjectDisposedException(GetType().GetTypeShortName());
            }

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var currentFileName = fileName;
            int incrementIndex = 1;

            while (true)
            {
                if (incrementIndex > 1000)
                    throw new Exception("Failed to create unique log file name after 1000 attempts.");

                try
                {
                    var filePath = Path.GetFullPath(Path.Combine(_directoryPath, currentFileName));

                    _logger.LogDebug("Creating log file stream: {file}", filePath);

                    var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize: _fileBufferSize, useAsync: false)
                        ?? throw new Exception("Failed to create file stream for log storage.");

                    resultFileName = currentFileName;
                    resultFilePath = filePath;

                    return stream;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create log file stream for {file}. Retrying with a different file name.",
                        currentFileName);
                    incrementIndex++;
                    currentFileName = $"{baseName}-{incrementIndex}{extension}";
                }
            }
        }

        public virtual void Flush()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(Flush));
                return;
            }
            lock (_fileMutex)
            {
                fileWriteStream?.Flush();
            }
        }
        public virtual Task FlushAsync()
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(FlushAsync));
                return Task.CompletedTask;
            }
            return Task.Run(() =>
            {
                if (_isDisposed.Value)
                {
                    _logger.LogWarning("{method} called but already disposed, ignored.",
                        nameof(FlushAsync));
                    return;
                }
                lock (_fileMutex)
                {
                    fileWriteStream?.Flush();
                }
            });
        }

        public virtual Task AppendAsync(params LogEntry[] entries)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(AppendAsync));
                return Task.CompletedTask;
            }
            return Task.Run(() =>
            {
                if (_isDisposed.Value)
                {
                    _logger.LogWarning("{method} called but already disposed, ignored.",
                        nameof(AppendAsync));
                    return;
                }
                lock (_fileMutex)
                {
                    AssignSequences(entries);
                    AppendInternal(entries);
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
            lock (_fileMutex)
            {
                // Numbered in the same critical section that stores the entries.
                AssignSequences(entries);
                AppendInternal(entries);
            }
        }

        /// <summary>Stores entries that already carry their sequence. Must be called under <see cref="_fileMutex"/>.</summary>
        protected virtual void AppendInternal(params LogEntry[] entries)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(AppendInternal));
                return;
            }
            fileWriteStream ??= CreateWriteStream(_requestedFileName, out fileName, out filePath);

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
            fileWriteStream?.Dispose();
            fileWriteStream = null;

            if (File.Exists(filePath))
                File.Delete(filePath);

            fileWriteStream = CreateWriteStream(_requestedFileName, out this.fileName, out filePath);
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
            lock (_fileMutex)
            {
                fileWriteStream?.Dispose();
                fileWriteStream = null;

                if (File.Exists(filePath))
                    File.Delete(filePath);

                if (File.Exists(filePath))
                    _logger.LogError("Failed to delete cache file: {file}", filePath);
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
            lock (_fileMutex)
            {
                return QueryInternal(maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
            }
        }

        protected virtual LogEntry[] QueryInternal(
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            var allLogs = ReadNewestFirst(CutoffTime(lastMinutes));

            // Apply log type filter
            if (logTypeFilter.HasValue)
            {
                allLogs = allLogs
                    .Where(log => log.LogType == logTypeFilter.Value);
            }

            // Take the most recent entries (up to maxEntries), back in chronological order
            return allLogs
                .Take(maxEntries)
                .Reverse()
                .Select(log => log.Clone(includeStackTrace))
                .ToArray();
        }

        static DateTime? CutoffTime(int lastMinutes)
            => lastMinutes > 0 ? DateTime.Now.AddMinutes(-lastMinutes) : (DateTime?)null;

        /// <summary>
        /// Every stored entry, newest first, ending at the first one older than <paramref name="cutoffTime"/>.
        /// Yields the storage's own instances (a buffered storage shares them with its writer): hand out copies.
        /// Must be enumerated under <see cref="_fileMutex"/>.
        /// </summary>
        protected virtual IEnumerable<LogEntry> ReadNewestFirst(DateTime? cutoffTime)
        {
            if (!File.Exists(filePath))
                yield break;

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            foreach (var entry in ReadLogEntriesFromLinesInReverse(fileStream, cutoffTime))
                yield return entry;
        }

        public virtual Task<LogEntry[]> QuerySinceAsync(
            long sinceSequence,
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(QuerySinceAsync));
                return Task.FromResult(Array.Empty<LogEntry>());
            }
            return Task.Run(() => QuerySince(sinceSequence, maxEntries, logTypeFilter, includeStackTrace, lastMinutes));
        }

        public virtual LogEntry[] QuerySince(
            long sinceSequence,
            int maxEntries = 100,
            LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (_isDisposed.Value)
            {
                _logger.LogWarning("{method} called but already disposed, ignored.",
                    nameof(QuerySince));
                return Array.Empty<LogEntry>();
            }
            lock (_fileMutex)
            {
                // A cursor above everything issued means the counter restarted: read from the beginning.
                var cursor = sinceSequence > _sequence ? 0 : Math.Max(sinceSequence, 0);
                if (maxEntries <= 0 || cursor == _sequence)
                    return Array.Empty<LogEntry>(); // nothing newer than the cursor exists

                // Read newest first down to the cursor, keeping only the OLDEST maxEntries matches.
                var page = new Queue<LogEntry>(); // no capacity: maxEntries comes from the caller and may be huge
                foreach (var entry in ReadNewestFirst(CutoffTime(lastMinutes)))
                {
                    // Sequences ascend through the storage, so the first entry at or below the cursor ends the scan.
                    // That also ends it at entries written before sequences existed (0).
                    if (entry.Sequence <= cursor)
                        break;

                    if (logTypeFilter.HasValue && entry.LogType != logTypeFilter.Value)
                        continue;

                    if (page.Count == maxEntries)
                        page.Dequeue(); // the newest kept so far; an older match takes its place
                    page.Enqueue(entry);
                }

                return page
                    .Reverse()
                    .Select(entry => entry.Clone(includeStackTrace))
                    .ToArray();
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

            try
            {
                Flush();
            }
            finally
            {
                lock (_fileMutex)
                {
                    fileWriteStream?.Dispose();
                    fileWriteStream = null;

                    // Orderly shutdown: replace the block reservation with the exact value, so a reopened
                    // storage continues right after the last number issued.
                    if (_sequence > 0)
                        PersistSequence(_sequence);
                }
            }

            GC.SuppressFinalize(this);
        }

        ~FileLogStorage() => Dispose();
    }
}