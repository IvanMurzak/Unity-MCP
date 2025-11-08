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
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using R3;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public class LogCache : IDisposable
    {
        static readonly string _cacheFilePath = Application.isEditor
            ? $"{Path.GetDirectoryName(Application.dataPath)}/Temp/mcp-server"
            : $"{Application.persistentDataPath}/Temp/mcp-server";

        static readonly string _cacheFileName = "editor-logs.txt";
        static readonly string _cacheFile = $"{Path.Combine(_cacheFilePath, _cacheFileName)}";
        static readonly object _lock = new();
        static readonly SemaphoreSlim _fileLock = new(1, 1);
        static readonly CancellationTokenSource _shutdownCts = new();
        static readonly TaskCompletionSource<bool> _shutdownTcs = new();
        static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            AllowTrailingCommas = true,
        };

        static bool isInitialized;
        static LogCache? instance;
        static IDisposable? timerSubscription;

        public static void HandleQuit()
        {
            if (!_shutdownCts.IsCancellationRequested)
                _shutdownCts.Cancel();
            timerSubscription?.Dispose();
            var lastLogTask = HandleLogCache();
            lastLogTask.ContinueWith(_ => _shutdownTcs.TrySetResult(true));
        }

        public static bool HasInstance => instance != null;
        public static LogCache Instance
        {
            get
            {
                lock (_lock)
                {
                    instance ??= new LogCache();
                    return instance!;
                }
            }
        }

        private LogCache()
        {
            if (isInitialized || Application.isBatchMode)
                return;

            timerSubscription = Observable.Timer(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)
                )
                .Subscribe(x =>
                {
                    if (!_shutdownCts.IsCancellationRequested)
                        Task.Run(HandleLogCache, _shutdownCts.Token);
                });

            isInitialized = true;
        }

        public static async Task HandleLogCache()
        {
            if (LogUtils.LogEntries > 0)
            {
                var logs = LogUtils.GetAllLogs();
                await CacheLogEntriesAsync(logs);
            }
        }

        public static async Task CacheLogEntriesAsync(LogEntry[] entries)
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
        public static async Task<LogWrapper?> GetCachedLogEntriesAsync()
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
        }

        ~LogCache() => Dispose();
    }
}