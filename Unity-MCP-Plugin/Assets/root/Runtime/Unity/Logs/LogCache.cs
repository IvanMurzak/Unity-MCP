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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
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
        static readonly SemaphoreSlim _fileLock = new(1, 1);
        static readonly CancellationTokenSource _shutdownCts = new();
        static readonly TaskCompletionSource<bool> _shutdownTcs = new();

        static bool _initialized = false;
        static IDisposable? _timerSubscription;
        static LogCache? _instance;
        static readonly object _lock = new object();

        public static void HandleQuit()
        {
            if (!_shutdownCts.IsCancellationRequested)
                _shutdownCts.Cancel();
            _timerSubscription?.Dispose();
            var lastLogTask = HandleLogCache();
            lastLogTask.ContinueWith(_ => _shutdownTcs.TrySetResult(true));
        }

        public static bool HasInstance => _instance != null;
        public static LogCache Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new LogCache();
                    return _instance!;
                }
            }
        }

        private LogCache()
        {
            if (_initialized || Application.isBatchMode)
                return;

            _timerSubscription = Observable.Timer(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1)
                )
                .Subscribe(x =>
                {
                    if (!_shutdownCts.IsCancellationRequested)
                        Task.Run(HandleLogCache, _shutdownCts.Token);
                });

            _initialized = true;
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
                var data = JsonUtility.ToJson(new LogWrapper { entries = entries }.ToJson(null));

                if (!Directory.Exists(_cacheFilePath))
                    Directory.CreateDirectory(_cacheFilePath);

                // Atomic File Write
                await File.WriteAllTextAsync(_cacheFile + ".tmp", data);
                if (File.Exists(_cacheFile))
                    File.Delete(_cacheFile);
                File.Move(_cacheFile + ".tmp", _cacheFile);
            }
            finally
            {
                _fileLock.Release();
            }
        }
        public static async Task<ConcurrentQueue<LogEntry>> GetCachedLogEntriesAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_cacheFile))
                {
                    return new ConcurrentQueue<LogEntry>();
                }
                var json = await File.ReadAllTextAsync(_cacheFile);
                return await Task.Run(() =>
                {
                    var wrapper = JsonUtility.FromJson<LogWrapper>(json);
                    return new ConcurrentQueue<LogEntry>(wrapper.entries);
                });
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public void Dispose()
        {
            _timerSubscription?.Dispose();
            _timerSubscription = null;

            if (!_shutdownCts.IsCancellationRequested)
                _shutdownCts.Cancel();
            _shutdownCts.Dispose();
        }

        ~LogCache() => Dispose();
    }
}