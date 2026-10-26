#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.CodeExec
{
    /// <summary>Output limits (spec/contract.md §6): a response goes straight into an agent's context.</summary>
    public static class OutputLimits
    {
        public const int DefaultValueChars = 20000;
        public const int DefaultLogChars = 20000;
        public const int DefaultLogLineChars = 2000;
        public const int DefaultErrorChars = 20000;

        /// <summary>What one log entry costs on top of its message: its level and line break, as hosts print them.</summary>
        public const int LogEntryChars = 12;

        /// <summary>A traceback keeps the first and the last this many of the snippet's frames.</summary>
        public const int FramesKept = 10;

        /// <summary>Inner (chained) exceptions reported after the error itself.</summary>
        public const int MaxCauses = 5;

        /// <summary>Compile diagnostics returned at most; one more entry says how many were left out.</summary>
        public const int MaxDiagnostics = 20;

        internal const string ValueHint = "return a slice or a summary";
        internal const string ErrorHint = "raise a shorter message";

        /// <summary>
        /// The first <paramref name="limit"/> characters plus <c>... [truncated: N of M characters shown; hint]</c>;
        /// never splits a surrogate pair. <paramref name="limit"/> 0 = no limit.
        /// </summary>
        public static string Clip(string text, int limit, string hint)
        {
            if (limit <= 0 || text.Length <= limit)
                return text;
            var keep = char.IsHighSurrogate(text[limit - 1]) ? limit - 1 : limit;
            return text.Substring(0, keep) + "... [truncated: " + keep + " of " + text.Length + " characters shown; " + hint + "]";
        }

        /// <summary>The first <see cref="MaxDiagnostics"/> diagnostics, plus a CE0002 entry counting the rest.</summary>
        internal static IReadOnlyList<CodeDiagnostic> Diagnostics(IEnumerable<CodeDiagnostic> diagnostics)
        {
            var all = diagnostics.ToList();
            if (all.Count <= MaxDiagnostics)
                return all;
            var kept = all.Take(MaxDiagnostics).ToList();
            kept.Add(new CodeDiagnostic
            {
                Id = "CE0002",
                Message = $"{all.Count - MaxDiagnostics} more diagnostic(s) left out: fix these first.",
            });
            return kept;
        }
    }

    /// <summary>
    /// The log budget of spec §6, applied as entries arrive so a runaway print loop cannot grow memory: the first entries up
    /// to 3/4 of the budget, then the most recent ones that still fit, with one entry saying how many were left out.
    /// Thread-safe (host log hooks may fire on other threads).
    /// </summary>
    sealed class LogBudget
    {
        readonly int _max, _maxLine;
        readonly List<LogEntry> _head = new List<LogEntry>();
        readonly Queue<LogEntry> _tail = new Queue<LogEntry>();
        bool _headOpen = true;
        long _headUsed, _tailUsed;
        int _leftOut;
        bool _clipped;

        public LogBudget(int maxChars, int maxLineChars)
        {
            _max = maxChars;
            _maxLine = maxLineChars;
        }

        public void Add(LogEntry entry)
        {
            if (entry.Message == null) // a host hook's empty message
                entry = new LogEntry(entry.Level, "");
            var clipped = OutputLimits.Clip(entry.Message, _maxLine, "print less");
            lock (_head)
            {
                if (!ReferenceEquals(clipped, entry.Message))
                {
                    _clipped = true;
                    entry = new LogEntry(entry.Level, clipped);
                }
                if (_max <= 0)
                {
                    _head.Add(entry);
                    return;
                }
                var size = Cost(entry);
                if (_headOpen && _headUsed + size <= _max * 3L / 4)
                {
                    _head.Add(entry);
                    _headUsed += size;
                    return;
                }
                _headOpen = false;
                _tail.Enqueue(entry);
                _tailUsed += size;
                while (_tail.Count > 0 && _headUsed + _tailUsed > _max)
                {
                    _tailUsed -= Cost(_tail.Dequeue());
                    _leftOut++;
                }
            }
        }

        /// <summary>The kept entries; <paramref name="truncated"/> when a line was clipped or entries were left out.</summary>
        public List<LogEntry> Entries(out bool truncated)
        {
            lock (_head)
            {
                truncated = _clipped || _leftOut > 0;
                var entries = new List<LogEntry>(_head);
                if (_leftOut > 0)
                    entries.Add(new LogEntry("warning", $"... {_leftOut} log line(s) left out (output over {_max} characters): print less, or return a summary"));
                entries.AddRange(_tail);
                return entries;
            }
        }

        static long Cost(LogEntry entry) => entry.Message.Length + OutputLimits.LogEntryChars;
    }
}
