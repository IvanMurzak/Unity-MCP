#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.CodeExec
{
    public sealed class CodeRunnerOptions
    {
        /// <summary>Namespaces every snippet gets. Hosts add their own (UnityEngine, Godot, ...).</summary>
        public IReadOnlyList<string> DefaultUsings { get; set; } = new[] { "System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks" };

        /// <summary>Folder that relative include paths resolve against.</summary>
        public string IncludeRoot { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>Defaults to <see cref="AssemblyHosts.CreateDefault"/>.</summary>
        public IAssemblyHost? Host { get; set; }

        /// <summary>Defaults to <see cref="LoadedAssembliesReferenceProvider"/>.</summary>
        public IReferenceProvider? References { get; set; }

        /// <summary>
        /// Turns the snippet's return value into something that outlives the snippet (text, a DTO, JSON).
        /// Runs BEFORE an unloadable snippet is released, so it must not return the snippet's own objects.
        /// </summary>
        public Func<object?, object?> ValueConverter { get; set; } = value => value?.ToString();

        public IList<ILogCapture> LogCaptures { get; set; } = new List<ILogCapture> { new ConsoleLogCapture() };

        /// <summary>Compiled snippets kept for identical requests (least recently used are dropped).</summary>
        public int CacheCapacity { get; set; } = 64;
    }

    public sealed class CodeRunnerStats
    {
        internal long executions, cacheHits, loadedAssemblies;
        public long Executions => Interlocked.Read(ref executions);
        public long CacheHits => Interlocked.Read(ref cacheHits);

        /// <summary>Snippet assemblies loaded into a host that cannot unload them (Mono). They stay until the domain goes.</summary>
        public long LoadedAssemblies => Interlocked.Read(ref loadedAssemblies);
    }

    /// <summary>The compiled form of a request. Produced by <see cref="CodeRunner.Prepare"/> (any thread).</summary>
    public sealed class PreparedSnippet
    {
        internal PreparedSnippet(ExecuteRequest request, ExecuteResult? failure, CodeRunner.CacheEntry? entry, bool cacheHit, long compileMs)
        {
            Request = request;
            Failure = failure;
            Entry = entry;
            CacheHit = cacheHit;
            CompileMs = compileMs;
        }

        internal ExecuteRequest Request { get; }
        internal ExecuteResult? Failure { get; }
        internal CodeRunner.CacheEntry? Entry { get; }
        public bool Succeeded => Failure == null;
        public bool CacheHit { get; }
        public long CompileMs { get; }
    }

    /// <summary>
    /// Engine-agnostic pipeline: includes → compile (auto-import retry) → load → invoke → convert → unload.
    /// <see cref="Prepare"/> is thread-safe and can run on a background thread; <see cref="Run"/> / <see cref="RunAsync"/>
    /// invoke the snippet on the calling thread (the host's main thread) and must not run concurrently.
    /// </summary>
    public sealed class CodeRunner
    {
        public const string SnippetAssemblyPrefix = "CodeExec.Snippet";
        const int MaxImportRounds = 3;
        static int _assemblyCounter;

        readonly ICSharpCompiler _compiler;
        readonly CodeRunnerOptions _options;
        readonly IAssemblyHost _host;
        readonly IReferenceProvider _references;
        readonly object _cacheLock = new object();
        readonly Dictionary<string, LinkedListNode<(string Key, CacheEntry Entry)>> _cache = new Dictionary<string, LinkedListNode<(string Key, CacheEntry Entry)>>(StringComparer.Ordinal);
        readonly LinkedList<(string Key, CacheEntry Entry)> _lru = new LinkedList<(string Key, CacheEntry Entry)>();

        // Mono cannot unload: an evicted entry whose assembly is already loaded would be loaded AGAIN on the next
        // identical request. Such entries move here (without their image bytes) and are never dropped.
        readonly Dictionary<string, CacheEntry> _pinned = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);

        // The reference list instance is stable while nothing loads; hash it once per instance, not per request.
        IReadOnlyList<string>? _hashedReferences;
        string _referencesHash = "";

        public CodeRunner(ICSharpCompiler compiler, CodeRunnerOptions? options = null)
        {
            _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            _options = options ?? new CodeRunnerOptions();
            _host = _options.Host ?? AssemblyHosts.CreateDefault();
            _references = _options.References ?? new LoadedAssembliesReferenceProvider();
        }

        public IAssemblyHost Host => _host;
        public string CompilerVersion => _compiler.CompilerVersion;
        public CodeRunnerStats Stats { get; } = new CodeRunnerStats();

        public ExecuteResult Execute(ExecuteRequest request) => Run(Prepare(request));

        public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default) =>
            RunAsync(Prepare(request), cancellationToken);

        public PreparedSnippet Prepare(ExecuteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var stopwatch = Stopwatch.StartNew();
            PreparedSnippet Fail(ErrorKind kind, string error) =>
                new PreparedSnippet(request, ExecuteResult.Fail(kind, error), null, false, stopwatch.ElapsedMilliseconds);

            if (string.IsNullOrWhiteSpace(request.Code))
                return Fail(ErrorKind.InvalidRequest, "code is empty");
            if (string.IsNullOrWhiteSpace(request.EntryType) && !string.IsNullOrWhiteSpace(request.EntryMethod))
                return Fail(ErrorKind.InvalidRequest, "entry method given without entry type");

            List<ParameterDeclaration> parameters;
            List<SourceFile> includes;
            try
            {
                parameters = request.Parameters.Select(p => new ParameterDeclaration(p.Name, TypeNames.ToCSharp(p.Type))).ToList();
                includes = IncludeResolver.Read(request.Include, _options.IncludeRoot);
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException)
            {
                return Fail(ErrorKind.InvalidRequest, e.Message);
            }

            var usings = _options.DefaultUsings.Concat(request.Usings).Select(NormalizeUsing).Where(u => u.Length > 0).Distinct(StringComparer.Ordinal).ToList();
            var compileRequest = new CompileRequest
            {
                Code = request.Code,
                Includes = includes,
                ReferencePaths = _references.GetReferencePaths(),
                Parameters = parameters,
                EntryType = string.IsNullOrWhiteSpace(request.EntryType) ? null : request.EntryType,
                EntryMethod = string.IsNullOrWhiteSpace(request.EntryMethod) ? null : request.EntryMethod,
            };

            var key = CacheKey(compileRequest, usings, request.AutoImport, ReferencesHash(compileRequest.ReferencePaths));
            var cached = FromCache(key);
            if (cached != null)
            {
                Interlocked.Increment(ref Stats.cacheHits);
                return new PreparedSnippet(request, null, cached, true, stopwatch.ElapsedMilliseconds);
            }

            var autoImported = new List<string>();
            var ambiguous = new List<CodeDiagnostic>();
            CompileResult compiled;
            for (var round = 0; ; round++)
            {
                compileRequest.Usings = usings;
                compileRequest.AssemblyName = SnippetAssemblyPrefix + Interlocked.Increment(ref _assemblyCounter) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                compiled = _compiler.Compile(compileRequest);
                if (compiled.Success || !request.AutoImport || compiled.UnresolvedNames.Count == 0 || round == MaxImportRounds)
                    break;

                var added = new List<string>();
                ambiguous.Clear();
                foreach (var pair in TypeIndex.FindNamespaces(compiled.UnresolvedNames))
                {
                    if (pair.Value.Count == 1)
                    {
                        if (!usings.Contains(pair.Value[0]))
                            added.Add(pair.Value[0]);
                    }
                    else
                    {
                        ambiguous.Add(new CodeDiagnostic
                        {
                            Id = "CE0001",
                            File = "snippet.cs",
                            Message = $"'{pair.Key}' exists in several namespaces: {string.Join(", ", pair.Value)}. Add the right using.",
                        });
                    }
                }
                if (added.Count == 0)
                    break;
                usings.AddRange(added);
                autoImported.AddRange(added);
            }

            if (compiled.Image == null)
            {
                var failure = ExecuteResult.Fail(ErrorKind.Compile, "Compilation failed");
                failure.Diagnostics = compiled.Diagnostics.Concat(ambiguous).ToList();
                failure.AutoImported = autoImported;
                failure.CompileMs = stopwatch.ElapsedMilliseconds;
                return new PreparedSnippet(request, failure, null, false, failure.CompileMs);
            }

            var entry = new CacheEntry(compileRequest.AssemblyName, compiled, autoImported);
            ToCache(key, entry);
            return new PreparedSnippet(request, null, entry, false, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>Invokes a prepared snippet synchronously. An unfinished asynchronous result is a <see cref="ErrorKind.Timeout"/>: use <see cref="RunAsync"/>.</summary>
        public ExecuteResult Run(PreparedSnippet prepared)
        {
            if (prepared.Failure != null)
                return prepared.Failure;
            var run = Start(prepared);
            if (run.Pending != null)
                run.Fail(ErrorKind.Timeout, "The snippet is asynchronous and did not finish synchronously; run it with RunAsync.");
            return run.Finish();
        }

        /// <summary>Invokes a prepared snippet; an asynchronous result is awaited (continuations stay on the caller's context).</summary>
        public async Task<ExecuteResult> RunAsync(PreparedSnippet prepared, CancellationToken cancellationToken = default)
        {
            if (prepared.Failure != null)
                return prepared.Failure;
            var run = Start(prepared);
            if (run.Pending is Task task)
            {
                var timeout = prepared.Request.TimeoutMs > 0 ? prepared.Request.TimeoutMs : Timeout.Infinite;
                Task finished;
                using (var delay = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    finished = await Task.WhenAny(task, Task.Delay(timeout, delay.Token));
                    delay.Cancel(); // the snippet finished first: do not keep the timer alive for the rest of the timeout
                }
                if (finished != task)
                    run.Fail(ErrorKind.Timeout, cancellationToken.IsCancellationRequested
                        ? "Canceled while waiting for the snippet's result; the snippet may still be running."
                        : $"No result within {prepared.Request.TimeoutMs} ms; the snippet may still be running.");
            }
            return run.Finish();
        }

        Execution Start(PreparedSnippet prepared)
        {
            Interlocked.Increment(ref Stats.executions);
            var entry = prepared.Entry!;
            var run = new Execution(this, prepared);
            try
            {
                run.Loaded = entry.Loaded ?? _host.Load(entry.Image!, entry.Symbols, entry.AssemblyName);
                if (!_host.SupportsUnload && entry.Loaded == null)
                {
                    // Mono cannot unload: keep it so identical requests reuse it; the bytes are no longer needed.
                    entry.Loaded = run.Loaded;
                    entry.Image = null;
                    entry.Symbols = null;
                    Interlocked.Increment(ref Stats.loadedAssemblies);
                }

                var count = prepared.Request.Parameters.Count;
                var type = run.Loaded.Assembly.GetType(entry.EntryType, throwOnError: false);
                var method = type?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == entry.EntryMethod && m.GetParameters().Length == count);
                if (method == null)
                {
                    run.Fail(ErrorKind.EntryPointNotFound, $"Static method '{entry.EntryType}.{entry.EntryMethod}' with {count} parameter(s) not found.");
                    return run;
                }

                var declared = new DeclaredResult(method.ReturnType);
                run.ReturnType = entry.ResultType ?? declared.YieldsName; // the compiler knows a snippet's real result type
                run.BeginCapture(_options.LogCaptures);
                run.SetResult(declared, method.Invoke(null, prepared.Request.Parameters.Select(p => p.Value).ToArray()));
            }
            catch (Exception e)
            {
                // Anything (a generic entry method, a byref-like return, a bad image) is the snippet's failure: Start must
                // always hand back a run, so Finish restores the log captures and releases the assembly.
                run.Fail(ErrorKind.Runtime, FormatException(e, run.Loaded?.Assembly));
            }
            return run;
        }

        /// <summary>
        /// The entry point's declared result, classified once: awaitable or not (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;)
        /// and the type it yields (<c>Task</c> → <c>void</c>, <c>Task&lt;T&gt;</c> → <c>T</c>).
        /// </summary>
        internal readonly struct DeclaredResult
        {
            public DeclaredResult(Type declared)
            {
                Declared = declared;
                var generic = declared.IsGenericType ? declared.GetGenericTypeDefinition() : null;
                var name = (generic ?? declared).FullName;
                // ValueTask is not part of netstandard2.0: recognise it by name.
                IsValueTask = name == "System.Threading.Tasks.ValueTask" || name == "System.Threading.Tasks.ValueTask`1";
                IsTask = IsValueTask || typeof(Task).IsAssignableFrom(declared);
                HasResult = IsTask && generic != null;
                Yields = !IsTask ? declared : HasResult ? declared.GetGenericArguments()[0] : typeof(void);
            }

            public Type Declared { get; }
            public bool IsTask { get; }
            public bool IsValueTask { get; }
            public bool HasResult { get; }
            public Type Yields { get; }

            /// <summary><c>System.Int32</c>, <c>System.Collections.Generic.List&lt;System.Int32&gt;</c> (same shape as the compiler's).</summary>
            public string YieldsName
            {
                get
                {
                    try { return TypeNames.ToCSharp(Yields).Replace("global::", ""); }
                    catch (ArgumentException) { return Yields.FullName ?? Yields.Name; } // by-ref / pointer results
                }
            }
        }

        /// <summary><c>Type: message</c> plus only the stack frames of the snippet's own assembly (snippet and includes).</summary>
        internal static string FormatException(Exception exception, Assembly? snippet)
        {
            while ((exception is TargetInvocationException || exception is AggregateException) && exception.InnerException != null)
                exception = exception.InnerException;
            var sb = new StringBuilder(exception.GetType().FullName).Append(": ").Append(exception.Message);
            if (snippet == null)
                return sb.ToString();
            foreach (var frame in new StackTrace(exception, true).GetFrames() ?? Array.Empty<StackFrame>())
            {
                var method = frame.GetMethod();
                if (method?.DeclaringType?.Assembly != snippet)
                    continue;
                sb.AppendLine().Append("   at ").Append(method.DeclaringType.FullName).Append('.').Append(method.Name);
                var file = frame.GetFileName();
                if (!string.IsNullOrEmpty(file))
                    sb.Append(" in ").Append(file).Append(":line ").Append(frame.GetFileLineNumber());
            }
            return sb.ToString();
        }

        static string NormalizeUsing(string value)
        {
            var u = (value ?? "").Trim();
            if (u.StartsWith("using ", StringComparison.Ordinal))
                u = u.Substring(6).Trim();
            return u.TrimEnd(';').Trim();
        }

        string ReferencesHash(IReadOnlyList<string> references)
        {
            lock (_cacheLock)
            {
                if (!ReferenceEquals(references, _hashedReferences))
                {
                    _referencesHash = Hash(references);
                    _hashedReferences = references;
                }
                return _referencesHash;
            }
        }

        static string CacheKey(CompileRequest request, IEnumerable<string> usings, bool autoImport, string referencesHash)
        {
            var parts = new List<string?> { request.Code, request.EntryType, request.EntryMethod, autoImport ? "1" : "0", referencesHash };
            parts.AddRange(usings);
            parts.Add("|");
            foreach (var p in request.Parameters) { parts.Add(p.TypeName); parts.Add(p.Name); }
            parts.Add("|");
            foreach (var f in request.Includes) { parts.Add(f.Path); parts.Add(f.Text); }
            return Hash(parts);
        }

        static string Hash(IEnumerable<string?> parts)
        {
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var separator = new byte[] { 0 };
                foreach (var part in parts)
                {
                    hash.AppendData(Encoding.UTF8.GetBytes(part ?? ""));
                    hash.AppendData(separator);
                }
                return Convert.ToBase64String(hash.GetHashAndReset());
            }
        }

        CacheEntry? FromCache(string key)
        {
            lock (_cacheLock)
            {
                if (_pinned.TryGetValue(key, out var pinned))
                    return pinned;
                if (!_cache.TryGetValue(key, out var node))
                    return null;
                _lru.Remove(node);
                _lru.AddFirst(node);
                return node.Value.Entry;
            }
        }

        void ToCache(string key, CacheEntry entry)
        {
            if (_options.CacheCapacity <= 0)
                return;
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(key) || _pinned.ContainsKey(key))
                    return;
                _cache[key] = _lru.AddFirst((key, entry));
                while (_cache.Count > _options.CacheCapacity)
                {
                    var (lastKey, lastEntry) = _lru.Last!.Value;
                    _lru.RemoveLast();
                    _cache.Remove(lastKey);
                    if (lastEntry.Loaded != null)
                        _pinned[lastKey] = lastEntry;
                }
            }
        }

        internal sealed class CacheEntry
        {
            public CacheEntry(string assemblyName, CompileResult compiled, IReadOnlyList<string> autoImported)
            {
                AssemblyName = assemblyName;
                Image = compiled.Image;
                Symbols = compiled.Symbols;
                EntryType = compiled.EntryType;
                EntryMethod = compiled.EntryMethod;
                ResultType = compiled.ResultType;
                AutoImported = autoImported;
            }

            public string AssemblyName { get; }

            /// <summary>Dropped once a non-unloadable host has loaded the assembly for good.</summary>
            public byte[]? Image { get; set; }
            public byte[]? Symbols { get; set; }
            public string EntryType { get; }
            public string EntryMethod { get; }
            public string? ResultType { get; }
            public IReadOnlyList<string> AutoImported { get; }

            /// <summary>Set only for hosts that cannot unload (Mono): the assembly is loaded once and reused.</summary>
            public ILoadedAssembly? Loaded { get; set; }
        }

        /// <summary>State of one invocation, from load to unload.</summary>
        sealed class Execution
        {
            readonly CodeRunner _runner;
            readonly PreparedSnippet _prepared;
            readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            readonly List<LogEntry> _logs = new List<LogEntry>();
            readonly List<IDisposable> _captures = new List<IDisposable>();
            ErrorKind _errorKind;
            string? _error;
            object? _raw;
            Task? _task;
            bool _taskHasResult;

            public Execution(CodeRunner runner, PreparedSnippet prepared)
            {
                _runner = runner;
                _prepared = prepared;
            }

            public ILoadedAssembly? Loaded { get; set; }
            public string? ReturnType { get; set; }

            /// <summary>The snippet's task while its result is still to come; null for synchronous results.</summary>
            public Task? Pending { get; private set; }

            /// <summary>Records the invocation result, judged by the DECLARED return type: awaitables become the task to await.</summary>
            public void SetResult(DeclaredResult declared, object? raw)
            {
                if (raw != null && declared.IsValueTask)
                    raw = declared.Declared.GetMethod("AsTask", Type.EmptyTypes)!.Invoke(raw, null);
                if (raw is Task task && declared.IsTask)
                {
                    _task = task;
                    _taskHasResult = declared.HasResult;
                    Pending = task.IsCompleted ? null : task;
                }
                _raw = raw;
            }

            public void Fail(ErrorKind kind, string error)
            {
                if (_errorKind != ErrorKind.None)
                    return;
                _errorKind = kind;
                _error = error;
            }

            public void BeginCapture(IList<ILogCapture> captures)
            {
                foreach (var capture in captures)
                    _captures.Add(capture.Begin(entry => { lock (_logs) _logs.Add(entry); }));
            }

            public ExecuteResult Finish()
            {
                object? value = null;
                try
                {
                    if (_errorKind == ErrorKind.None)
                        value = _runner._options.ValueConverter(Result());
                }
                catch (Exception e)
                {
                    Fail(ErrorKind.Runtime, FormatException(e, Loaded?.Assembly));
                }
                finally
                {
                    for (var i = _captures.Count - 1; i >= 0; i--)
                        _captures[i].Dispose();
                    if (_runner._host.SupportsUnload)
                        Loaded?.Dispose();
                    _raw = null;
                    _task = null;
                    Pending = null;
                }

                List<LogEntry> logs;
                lock (_logs) logs = new List<LogEntry>(_logs);
                return new ExecuteResult
                {
                    Success = _errorKind == ErrorKind.None,
                    Value = value,
                    ReturnType = ReturnType,
                    Logs = logs,
                    ErrorKind = _errorKind,
                    Error = _error,
                    AutoImported = _prepared.Entry!.AutoImported,
                    CompileMs = _prepared.CompileMs,
                    ExecuteMs = _stopwatch.ElapsedMilliseconds,
                    CacheHit = _prepared.CacheHit,
                };
            }

            /// <summary>A finished task → its value (its exception is rethrown as is); anything else unchanged.</summary>
            object? Result()
            {
                if (_task == null)
                    return _raw;
                _task.GetAwaiter().GetResult();
                return _taskHasResult ? _task.GetType().GetProperty("Result")!.GetValue(_task) : null;
            }
        }
    }
}
