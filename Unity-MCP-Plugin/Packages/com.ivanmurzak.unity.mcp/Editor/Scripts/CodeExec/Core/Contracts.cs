#nullable enable
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.CodeExec
{
    /// <summary>Why an execution failed. Mirrors <c>errorKind</c> in spec/contract.md.</summary>
    public enum ErrorKind
    {
        None,
        InvalidRequest,
        Compile,
        EntryPointNotFound,
        Runtime,
        Timeout,
        NotAvailable,
    }

    /// <summary>An argument passed to the snippet. <see cref="Value"/> is already deserialized by the host.</summary>
    public sealed class SnippetParameter
    {
        public SnippetParameter(string name, Type type, object? value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
        }

        public string Name { get; }
        public Type Type { get; }
        public object? Value { get; }
    }

    /// <summary>One execution request (spec/contract.md §1).</summary>
    public sealed class ExecuteRequest
    {
        public string Code { get; set; } = "";

        /// <summary>Extra namespaces on top of <see cref="CodeRunnerOptions.DefaultUsings"/>. Aliases and <c>static</c> allowed.</summary>
        public IReadOnlyList<string> Usings { get; set; } = Array.Empty<string>();

        /// <summary>Files or folders (recursive, <c>*.cs</c>). Relative paths resolve against <see cref="CodeRunnerOptions.IncludeRoot"/>.</summary>
        public IReadOnlyList<string> Include { get; set; } = Array.Empty<string>();

        /// <summary>Full-source mode: the code is compiled verbatim and this static method is invoked.</summary>
        public string? EntryType { get; set; }

        public string? EntryMethod { get; set; }

        public IReadOnlyList<SnippetParameter> Parameters { get; set; } = Array.Empty<SnippetParameter>();

        /// <summary>Upper bound for awaiting an asynchronous result; 0 = no bound. Never aborts running code.</summary>
        public int TimeoutMs { get; set; }

        /// <summary>Retry with a namespace when an unknown simple name resolves to exactly one (spec §3.3).</summary>
        public bool AutoImport { get; set; } = true;
    }

    public sealed class LogEntry
    {
        public LogEntry(string level, string message)
        {
            Level = level;
            Message = message;
        }

        /// <summary><c>info</c>, <c>warning</c> or <c>error</c>.</summary>
        public string Level { get; }
        public string Message { get; }
        public override string ToString() => Level + ": " + Message;
    }

    public sealed class CodeDiagnostic
    {
        public string Id { get; set; } = "";
        public string Severity { get; set; } = "error";
        /// <summary><c>snippet.cs</c>, an include's relative path, or empty for code the agent did not write.</summary>
        public string File { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; } = "";
        public override string ToString() => $"{File}({Line},{Column}): {Severity} {Id}: {Message}";
    }

    /// <summary>Execution outcome (spec/contract.md §2).</summary>
    public sealed class ExecuteResult
    {
        public bool Success { get; set; }

        /// <summary>The result after <see cref="CodeRunnerOptions.ValueConverter"/>; never an object of the snippet's own types.</summary>
        public object? Value { get; set; }

        /// <summary>
        /// Result type, <c>Task&lt;T&gt;</c> → <c>T</c>: <c>System.Void</c> when nothing is returned; the static type of a
        /// snippet's result expression (<c>System.Int32</c>); <c>System.Object</c> for a snippet that returns through
        /// <c>return</c> statements; the method's type in full-source mode. Null when the entry point was never reached.
        /// </summary>
        public string? ReturnType { get; set; }

        public IReadOnlyList<LogEntry> Logs { get; set; } = Array.Empty<LogEntry>();
        public ErrorKind ErrorKind { get; set; }
        public string? Error { get; set; }
        public IReadOnlyList<CodeDiagnostic> Diagnostics { get; set; } = Array.Empty<CodeDiagnostic>();
        public IReadOnlyList<string> AutoImported { get; set; } = Array.Empty<string>();
        public long CompileMs { get; set; }
        public long ExecuteMs { get; set; }
        public bool CacheHit { get; set; }

        internal static ExecuteResult Fail(ErrorKind kind, string error) => new ExecuteResult { ErrorKind = kind, Error = error };
    }
}
