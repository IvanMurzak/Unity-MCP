/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public static class LogCache
    {
        static string _cacheFilePath = $"{Application.dataPath}/Temp~/mcp-server/editor-logs.txt";
        static double _lastSnapshot = 0f;
        static double _snapshotInterval = 2f;

        public static void HandleLogCache()
        {
            if (_lastSnapshot + _snapshotInterval < EditorApplication.timeSinceStartup)
            {
                if (LogUtils.LogEntries > 0)
                {
                    var logs = LogUtils.GetAllLogs();
                    CacheLogEntries(logs);
                }
                _lastSnapshot = EditorApplication.timeSinceStartup;
            }
        }


        public static void CacheLogEntries(LogEntry[] entries)
        {
            using (FileStream stream = File.Create(_cacheFilePath))
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, entries);
            }
        }

        public static ConcurrentQueue<LogEntry> GetCachedLogEntries()
        {
            if (!File.Exists(_cacheFilePath))
            {
                return new();
            }
            using (FileStream stream = File.OpenRead(_cacheFilePath))
            {
                var formatter = new BinaryFormatter();
                LogEntry[] entries = formatter.Deserialize(stream) as LogEntry[];
                return new ConcurrentQueue<LogEntry>(entries);
            }
        }
    }
}