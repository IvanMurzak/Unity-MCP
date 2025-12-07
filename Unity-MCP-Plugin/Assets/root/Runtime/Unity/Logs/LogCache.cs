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
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using R3;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    internal class LogCache : IDisposable
    {
        readonly LogUtils _logUtils;
        readonly string _cacheFilePath;
        readonly string _cacheFileName;
        readonly string _cacheFile;
        readonly JsonSerializerOptions _jsonOptions;
        readonly SemaphoreSlim _fileLock = new(1, 1);
        readonly CancellationTokenSource _shutdownCts = new();
        bool _saving = false;
        int _lastSavedCount = 0;

        IDisposable? timerSubscription;

        internal LogCache(LogUtils logUtils, string? cacheFilePath = null, string? cacheFileName = null, JsonSerializerOptions? jsonOptions = null)
        {
            if (!MainThread.Instance.IsMainThread)
                throw new Exception($"{nameof(LogCache)} must be initialized on the main thread.");

            _logUtils = logUtils;
            _cacheFilePath = cacheFilePath ?? (Application.isEditor
                ? $"{Path.GetDirectoryName(Application.dataPath)}/Temp/mcp-server"
                : $"{Application.persistentDataPath}/Temp/mcp-server");

            _cacheFileName = cacheFileName ?? (Application.isEditor
                ? "editor-logs.txt"
                : "player-logs.txt");

            _cacheFile = $"{Path.Combine(_cacheFilePath, _cacheFileName)}";
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
            };

            timerSubscription = Observable.Timer(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)
                )
                .Subscribe(x =>
                {
                    if (!_saving && !_shutdownCts.IsCancellationRequested)
                        Task.Run(HandleLogCache, _shutdownCts.Token);
                });
        }

        public async Task HandleQuit()
        {
            if (!_shutdownCts.IsCancellationRequested)
                _shutdownCts.Cancel();
            timerSubscription?.Dispose();
            await HandleLogCache();
        }

        public async Task HandleLogCache()
        {
            var logs = _logUtils.GetAllLogs();
            if (logs.Length < _lastSavedCount)
            {
                _lastSavedCount = 0;
            }

            if (logs.Length > _lastSavedCount)
            {
                var newLogs = new LogEntry[logs.Length - _lastSavedCount];
                Array.Copy(logs, _lastSavedCount, newLogs, 0, newLogs.Length);
                await AppendCacheEntriesAsync(newLogs);
                _lastSavedCount = logs.Length;
            }
        }

        public void HandleLogCacheImmediate()
        {
            var logs = _logUtils.GetAllLogs();
            if (logs.Length < _lastSavedCount)
            {
                _lastSavedCount = 0;
            }

            if (logs.Length > _lastSavedCount)
            {
                var newLogs = new LogEntry[logs.Length - _lastSavedCount];
                Array.Copy(logs, _lastSavedCount, newLogs, 0, newLogs.Length);
                AppendCacheEntries(newLogs);
                _lastSavedCount = logs.Length;
            }
        }

        Task AppendCacheEntriesAsync(LogEntry[] entries)
        {
            return Task.Run(async () =>
            {
                await _fileLock.WaitAsync();
                try
                {
                    AppendCacheToFile(entries);
                }
                finally
                {
                    _fileLock.Release();
                }
            });
        }

        void AppendCacheEntries(LogEntry[] entries)
        {
            _fileLock.Wait();
            try
            {
                AppendCacheToFile(entries);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        void AppendCacheToFile(LogEntry[] entries)
        {
            _saving = true;

            if (!Directory.Exists(_cacheFilePath))
                Directory.CreateDirectory(_cacheFilePath);

            using (var fileStream = new FileStream(_cacheFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096, useAsync: false))
            {
                foreach (var entry in entries)
                {
                    System.Text.Json.JsonSerializer.Serialize(fileStream, entry, _jsonOptions);
                    fileStream.WriteByte((byte)'\n');
                }
                fileStream.Flush();
            }
            _saving = false;
        }

        public void ClearCacheFile()
        {
            _fileLock.Wait();
            try
            {
                File.Delete(_cacheFile);

                if (File.Exists(_cacheFile))
                    Debug.LogError($"Failed to delete cache file: {_cacheFile}");

                _lastSavedCount = 0;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task<LogWrapper?> GetCachedLogEntriesAsync()
        {
            return Task.Run(async () =>
            {
                await _fileLock.WaitAsync();
                try
                {
                    if (!File.Exists(_cacheFile))
                        return null;

                    var entries = new System.Collections.Generic.List<LogEntry>();
                    using (var reader = new StreamReader(_cacheFile))
                    {
                        string? line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                try
                                {
                                    var entry = System.Text.Json.JsonSerializer.Deserialize<LogEntry>(line, _jsonOptions);
                                    if (entry != null) entries.Add(entry);
                                }
                                catch { }
                            }
                        }
                    }
                    _lastSavedCount = entries.Count;
                    return new LogWrapper { Entries = entries.ToArray() };
                }
                finally
                {
                    _fileLock.Release();
                }
            });
        }

        public void Dispose()
        {
            if (_shutdownCts.IsCancellationRequested) return;

            timerSubscription?.Dispose();
            timerSubscription = null;

            if (!_shutdownCts.IsCancellationRequested)
                _shutdownCts.Cancel();
            _shutdownCts.Dispose();

            _fileLock.Dispose();
        }

        ~LogCache() => Dispose();
    }
}