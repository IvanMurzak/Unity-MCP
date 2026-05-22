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
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP
{
    public interface ILogStorage : IDisposable
    {
        Task AppendAsync(params LogEntry[] entries);
        void Append(params LogEntry[] entries);

        Task FlushAsync();
        void Flush();

        Task<LogEntry[]> QueryAsync(
            int maxEntries = 100,
            UnityEngine.LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0);
        LogEntry[] Query(
            int maxEntries = 100,
            UnityEngine.LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0);

        void Clear();
    }
}