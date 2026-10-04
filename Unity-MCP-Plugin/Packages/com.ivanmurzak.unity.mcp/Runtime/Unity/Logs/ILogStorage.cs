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
using System.Linq;
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

        /// <inheritdoc cref="QuerySince"/>
        /// <remarks>Default: the same as <see cref="QuerySince"/>'s, but built on <see cref="QueryAsync"/>.</remarks>
        async Task<LogEntry[]> QuerySinceAsync(
            long sinceSequence,
            int maxEntries = 100,
            UnityEngine.LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (maxEntries <= 0)
                return Array.Empty<LogEntry>();

            for (var window = maxEntries; ; window = LogStorageCursor.Grow(window))
            {
                var newest = await QueryAsync(window, null, includeStackTrace, lastMinutes).ConfigureAwait(false);
                switch (LogStorageCursor.Select(newest, window, sinceSequence, maxEntries, logTypeFilter, out var page))
                {
                    case LogStorageCursor.Step.Done:
                        return page;
                    case LogStorageCursor.Step.Unsequenced:
                        return LogStorageCursor.IsQueryAnswer(window, maxEntries, logTypeFilter)
                            ? newest
                            : await QueryAsync(maxEntries, logTypeFilter, includeStackTrace, lastMinutes).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Entries newer than <paramref name="sinceSequence"/>, oldest first (cursor, then filters, then sort, then
        /// limit - an overflowing result is the OLDEST page, so the next call continues without a gap).
        /// A cursor above every sequence ever issued is treated as "before everything".
        /// </summary>
        /// <remarks>
        /// Added after the first release of this interface, so it has a default implementation built only on
        /// <see cref="Query"/>: an implementer written before it existed still compiles and still answers. The
        /// built-in storages override it. "Every sequence ever issued" is taken as the highest one stored.
        /// A storage that assigns no sequences (<see cref="LogEntry.Sequence"/> 0) cannot honour a cursor; the
        /// default then returns exactly what <see cref="Query"/> returns for the same arguments - the newest page,
        /// i.e. the <c>sinceSequence = 0</c> answer.
        /// </remarks>
        LogEntry[] QuerySince(
            long sinceSequence,
            int maxEntries = 100,
            UnityEngine.LogType? logTypeFilter = null,
            bool includeStackTrace = false,
            int lastMinutes = 0)
        {
            if (maxEntries <= 0)
                return Array.Empty<LogEntry>();

            for (var window = maxEntries; ; window = LogStorageCursor.Grow(window))
            {
                var newest = Query(window, null, includeStackTrace, lastMinutes);
                switch (LogStorageCursor.Select(newest, window, sinceSequence, maxEntries, logTypeFilter, out var page))
                {
                    case LogStorageCursor.Step.Done:
                        return page;
                    case LogStorageCursor.Step.Unsequenced:
                        return LogStorageCursor.IsQueryAnswer(window, maxEntries, logTypeFilter)
                            ? newest
                            : Query(maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
                }
            }
        }

        void Clear();
    }

    /// <summary>
    /// The cursor arithmetic behind <see cref="ILogStorage"/>'s default <c>QuerySince</c> implementations: read the
    /// newest entries unfiltered, doubling the window until it reaches the cursor or holds everything stored, then
    /// apply the cursor, the type filter, the sort and the limit here.
    /// </summary>
    static class LogStorageCursor
    {
        internal enum Step { Grow, Done, Unsequenced }

        internal static int Grow(int window)
            => window > int.MaxValue / 2 ? int.MaxValue : window * 2;

        /// <summary>
        /// Whether the unfiltered read of <paramref name="window"/> entries is already what
        /// <c>Query(maxEntries, logTypeFilter, ...)</c> would return, so the unsequenced fallback need not re-read.
        /// </summary>
        internal static bool IsQueryAnswer(int window, int maxEntries, UnityEngine.LogType? logTypeFilter)
            => window == maxEntries && !logTypeFilter.HasValue;

        /// <param name="newest">What <see cref="ILogStorage.Query"/> returned for <paramref name="window"/> entries, unfiltered.</param>
        internal static Step Select(
            LogEntry[] newest,
            int window,
            long sinceSequence,
            int maxEntries,
            UnityEngine.LogType? logTypeFilter,
            out LogEntry[] page)
        {
            page = Array.Empty<LogEntry>();
            if (newest.Length == 0)
                return Step.Done;

            var highest = newest.Max(entry => entry.Sequence);
            if (highest <= 0)
                return Step.Unsequenced;

            // A cursor above everything issued means the counter restarted: read from the beginning.
            var cursor = sinceSequence > highest ? 0 : Math.Max(sinceSequence, 0);

            // Complete once the window holds everything stored, or reaches down to the cursor. Terminates: Grow
            // saturates at int.MaxValue, which no array's Length reaches.
            var complete = newest.Length < window
                || newest.Min(entry => entry.Sequence) <= cursor;
            if (!complete)
                return Step.Grow;

            page = newest
                .Where(entry => entry.Sequence > cursor)
                .Where(entry => !logTypeFilter.HasValue || entry.LogType == logTypeFilter.Value)
                .OrderBy(entry => entry.Sequence)
                .Take(maxEntries)
                .ToArray();
            return Step.Done;
        }
    }
}
