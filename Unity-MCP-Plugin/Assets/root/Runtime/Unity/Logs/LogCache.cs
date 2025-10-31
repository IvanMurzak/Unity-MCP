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
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using R3;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public class LogCache
    {
        static string _cacheFilePath =
#if UNITY_EDITOR
            $"{Path.GetDirectoryName(Application.dataPath)}/Temp/mcp-server";
#else
            $"{Application.persistentDataPath}/Temp/mcp-server";
#endif

        static string _cacheFileName = "editor-logs.txt";
        static string _cacheFile = $"{Path.Combine(_cacheFilePath, _cacheFileName)}";
        static readonly SemaphoreSlim _fileLock = new(1, 1);
        static bool _initialized = false;
        private static CancellationTokenSource _shutdownCts = new();
        private static TaskCompletionSource<bool> _shutdownTcs = new();
        private static IDisposable? _timerSubscription;
        private static LogCache? _instance;
        private static readonly object _lock = new object();

        public static void HandleQuit()
        {
            _shutdownCts.Cancel();
            _timerSubscription?.Dispose();
            var lastLogTask = HandleLogCache();
            lastLogTask.ContinueWith(_ => _shutdownTcs.TrySetResult(true));
        }

        public static LogCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null && !Application.isBatchMode)
                        {
                            _instance = new LogCache();
                        }
                    }
                }
                return _instance!;
            }
        }

        private LogCache()
        {
            if (_initialized) return;

            _timerSubscription = Observable.Timer(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            )
            .Subscribe(x =>
            {
                if (!_shutdownCts.IsCancellationRequested)
                {
                    Task.Run(HandleLogCache, _shutdownCts.Token);
                }
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
                var data = JsonUtility.ToJson(new LogWrapper { entries = entries });
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
                    LogWrapper wrapper = JsonUtility.FromJson<LogWrapper>(json);
                    return new ConcurrentQueue<LogEntry>(wrapper.entries);
                });
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}