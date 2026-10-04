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
        /// <remarks>
        /// The default implementation is <see cref="QuerySince"/>'s, built on <see cref="QueryAsync"/> instead.
        /// </remarks>
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
                        return await QueryAsync(maxEntries, logTypeFilter, includeStackTrace, lastMinutes).ConfigureAwait(false);
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
        /// <see cref="Query"/>: an implementer written before it existed still compiles and still answers.
        /// The built-in storages override it with a single scan under their file lock.
        /// <para>
        /// The default reads the newest entries through <see cref="Query"/> (no type filter), doubling the window
        /// until it reaches the cursor or holds everything stored, then applies the cursor, the type filter, the
        /// sort and the limit itself. "Every sequence ever issued" is taken as the highest sequence the storage
        /// returns.
        /// </para>
        /// <para>
        /// If the storage assigns no sequences at all (its newest entry has <see cref="LogEntry.Sequence"/> 0, as
        /// with any implementer written before sequences existed), no cursor can be honoured. The default then
        /// returns exactly what <see cref="Query"/> returns for the same arguments - the newest page, i.e. the
        /// <c>sinceSequence = 0</c> answer. That is what the contract's restart backstop leads the caller to
        /// anyway (every returned sequence is below its cursor, so it re-reads from 0), and it never presents
        /// the oldest stored entries as "new".
        /// </para>
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
                        return Query(maxEntries, logTypeFilter, includeStackTrace, lastMinutes);
                }
            }
        }

        void Clear();
    }

    /// <summary>
    /// The cursor arithmetic behind <see cref="ILogStorage"/>'s default <c>QuerySince</c> implementations.
    /// </summary>
    static class LogStorageCursor
    {
        internal enum Step { Grow, Done, Unsequenced }

        internal static int Grow(int window)
            => window > int.MaxValue / 2 ? int.MaxValue : window * 2;

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

            // Complete once the window holds everything stored, or reaches down to the cursor.
            var complete = newest.Length < window
                || window == int.MaxValue
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
