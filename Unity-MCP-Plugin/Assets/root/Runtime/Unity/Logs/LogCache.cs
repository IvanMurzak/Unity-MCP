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
using R3;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    internal class LogCache : IDisposable
    {
        readonly string _cacheFilePath;
        readonly string _cacheFileName;
        readonly string _cacheFile;
        readonly JsonSerializerOptions _jsonOptions;
        readonly SemaphoreSlim _fileLock = new(1, 1);
        readonly CancellationTokenSource _shutdownCts = new();

        IDisposable? timerSubscription;

        internal LogCache(string? cacheFilePath = null, string? cacheFileName = null, JsonSerializerOptions? jsonOptions = null)
        {
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
                    if (!_shutdownCts.IsCancellationRequested)
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
            if (LogUtils.LogEntries > 0)
            {
                var logs = LogUtils.GetAllLogs();
                await CacheLogEntriesAsync(logs);
            }
        }

        public async Task CacheLogEntriesAsync(LogEntry[] entries)
        {
            await _fileLock.WaitAsync();
            try
            {
                await Task.Run(() =>
                {
                    var data = new LogWrapper { Entries = entries };
                    var json = JsonSerializer.Serialize(data, _jsonOptions);

                    if (!Directory.Exists(_cacheFilePath))
                        Directory.CreateDirectory(_cacheFilePath);

                    // Atomic File Write
                    File.WriteAllText(_cacheFile + ".tmp", json);
                    if (File.Exists(_cacheFile))
                        File.Delete(_cacheFile);
                    File.Move(_cacheFile + ".tmp", _cacheFile);
                });
            }
            finally
            {
                _fileLock.Release();
            }
        }
        public async Task<LogWrapper?> GetCachedLogEntriesAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    if (!File.Exists(_cacheFile))
                        return null;

                    var json = File.ReadAllText(_cacheFile);
                    return JsonSerializer.Deserialize<LogWrapper>(json, _jsonOptions);
                });
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public void Dispose()
        {
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