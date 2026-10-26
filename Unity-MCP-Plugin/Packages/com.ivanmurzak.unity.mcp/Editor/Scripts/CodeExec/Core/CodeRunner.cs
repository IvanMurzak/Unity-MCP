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

        /// <summary>Accept includes outside <see cref="IncludeRoot"/> (absolute paths, <c>..</c>). Off by default (spec §1).</summary>
        public bool AllowIncludeOutsideRoot { get; set; }

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
        internal PreparedSnippet(ExecuteRequest request, ExecuteResult? failure, CodeRunner.CacheEntry? entry, bool cacheHit, long compileMs,
            IReadOnlyList<SnippetParameter>? parameters = null)
        {
            Request = request;
            Parameters = parameters ?? request.Parameters;
            Failure = failure;
            Entry = entry;
            CacheHit = cacheHit;
            CompileMs = compileMs;
        }

        internal ExecuteRequest Request { get; }

        /// <summary>The arguments the snippet is invoked with: the request's, or those bound by <see cref="WithValues"/>.</summary>
        internal IReadOnlyList<SnippetParameter> Parameters { get; }
        internal ExecuteResult? Failure { get; }
        internal CodeRunner.CacheEntry? Entry { get; }
        public bool Succeeded => Failure == null;
        public bool CacheHit { get; }
        public long CompileMs { get; }

        /// <summary>
        /// The same compiled snippet with the parameter values supplied now (same order as the request's parameters): a host
        /// compiles off its main thread with the parameter TYPES and binds values (e.g. live engine objects) on the main
        /// thread. A wrong count yields an InvalidRequest snippet; values convert as in Execute.
        /// </summary>
        public PreparedSnippet WithValues(IReadOnlyList<object?> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (Failure != null)
                return this;
            var declared = Parameters;
            if (values.Count != declared.Count)
            {
                // Only the count is checked here; values convert exactly as in Execute (reflection Invoke: null -> default,
                // primitive widening), and a value that does not fit is a Runtime error on both paths.
                var failure = ExecuteResult.Fail(ErrorKind.InvalidRequest, $"{values.Count} value(s) given for {declared.Count} parameter(s).");
                failure.CompileMs = CompileMs;
                failure.CacheHit = CacheHit;
                return new PreparedSnippet(Request, failure, null, CacheHit, CompileMs);
            }

            var bound = declared.Select((p, i) => new SnippetParameter(p.Name, p.Type, values[i])).ToList();
            return new PreparedSnippet(Request, null, Entry, CacheHit, CompileMs, bound);
        }
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

        // Compiled images, least recently used dropped first.
        readonly Dictionary<string, LinkedListNode<CacheEntry>> _cache = new Dictionary<string, LinkedListNode<CacheEntry>>(StringComparer.Ordinal);
        readonly LinkedList<CacheEntry> _lru = new LinkedList<CacheEntry>();

        // Hosts that cannot unload (Mono): the one loaded entry per cache key, never dropped. A key is loaded at most once,
        // whatever the order of Prepare / Run / eviction, so identical requests never load a second permanent assembly.
        readonly Dictionary<string, CacheEntry> _loaded = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);

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
            PreparedSnippet Fail(ErrorKind kind, string error)
            {
                // An early error can quote the request (an include path): the error limit applies to it too.
                var failure = ExecuteResult.Fail(kind, OutputLimits.Clip(error, Math.Max(request.MaxErrorChars, 0), OutputLimits.ErrorHint));
                if (!ReferenceEquals(failure.Error, error))
                    failure.Truncated = new[] { "error" };
                return new PreparedSnippet(request, failure, null, false, stopwatch.ElapsedMilliseconds);
            }

            if (string.IsNullOrWhiteSpace(request.Code))
                return Fail(ErrorKind.InvalidRequest, "code is empty");
            if (string.IsNullOrWhiteSpace(request.EntryType) && !string.IsNullOrWhiteSpace(request.EntryMethod))
                return Fail(ErrorKind.InvalidRequest, "entry method given without entry type");
            if (request.MaxValueChars < 0 || request.MaxLogChars < 0 || request.MaxLogLineChars < 0 || request.MaxErrorChars < 0)
                return Fail(ErrorKind.InvalidRequest, "output limits must be >= 0 (0 = no limit)");

            List<ParameterDeclaration> parameters;
            List<SourceFile> includes;
            try
            {
                parameters = request.Parameters.Select(p => new ParameterDeclaration(p.Name, TypeNames.ToCSharp(p.Type))).ToList();
                includes = IncludeResolver.Read(request.Include, _options.IncludeRoot, _options.AllowIncludeOutsideRoot);
            }
            catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException
                || e is NotSupportedException || e is System.Security.SecurityException) // bad include paths (`a.cs:zz` on .NET Framework)
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
                failure.Diagnostics = OutputLimits.Diagnostics(ambiguous.Concat(compiled.Diagnostics)); // the actionable CE0001 first
                failure.AutoImported = autoImported;
                failure.CompileMs = stopwatch.ElapsedMilliseconds;
                return new PreparedSnippet(request, failure, null, false, failure.CompileMs);
            }

            var entry = new CacheEntry(key, compileRequest.AssemblyName, compiled, autoImported);
            ToCache(entry);
            return new PreparedSnippet(request, null, entry, false, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Invokes a prepared snippet synchronously. An unfinished asynchronous result is a <see cref="ErrorKind.Timeout"/>:
        /// use <see cref="RunAsync"/>. Values supplied at run time: <see cref="PreparedSnippet.WithValues"/>.
        /// </summary>
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
                var parameters = prepared.Parameters;
                run.Loaded = Load(entry);

                var type = run.Loaded.Assembly.GetType(entry.EntryType, throwOnError: false);
                var method = EntryMethod(type, entry.EntryMethod, parameters, out var problem);
                if (method == null)
                {
                    run.Fail(ErrorKind.EntryPointNotFound, $"Static method '{entry.EntryType}.{entry.EntryMethod}' with {parameters.Count} parameter(s) {problem}.");
                    return run;
                }

                var declared = new DeclaredResult(method.ReturnType);
                run.ReturnType = entry.ResultType ?? declared.YieldsName; // the compiler knows a snippet's real result type
                run.BeginCapture(_options.LogCaptures);
                run.SetResult(declared, method.Invoke(null, parameters.Select(p => p.Value).ToArray()));
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
        /// Loads the entry's assembly. A host that cannot unload (Mono) keeps ONE loaded entry per cache key: it loads
        /// outside the cache lock, publishes under it, and reuses whatever was published first. The image bytes are dropped
        /// and the key leaves the LRU, which only holds images still to load. A snippet with static state of its own is
        /// never published: each run loads a fresh copy, so no run sees another run's statics (spec §3.5).
        /// </summary>
        ILoadedAssembly Load(CacheEntry entry)
        {
            if (_host.SupportsUnload)
                return _host.Load(entry.Image!, entry.Symbols, entry.AssemblyName);
            byte[] image;
            byte[]? symbols;
            lock (_cacheLock)
            {
                if (_loaded.TryGetValue(entry.Key, out var published))
                    return published.Loaded!;
                image = entry.Image!; // non-null until published, and it is not published yet
                symbols = entry.Symbols;
            }

            var loaded = _host.Load(image, symbols, entry.AssemblyName);
            Interlocked.Increment(ref Stats.loadedAssemblies); // permanent, even when a concurrent run wins below
            entry.HasStaticState ??= StaticState.Has(loaded.Assembly); // the same for every copy: reflect once
            if (entry.HasStaticState == true)
                return loaded; // stays an image in the LRU: the next identical request loads it again
            lock (_cacheLock)
            {
                if (_loaded.TryGetValue(entry.Key, out var published))
                    return published.Loaded!; // a concurrent run of the same key won; ours stays loaded, unused
                entry.Loaded = loaded;
                entry.Image = null;
                entry.Symbols = null;
                _loaded[entry.Key] = entry;
                if (_cache.TryGetValue(entry.Key, out var node))
                {
                    _lru.Remove(node);
                    _cache.Remove(entry.Key);
                }
                return loaded;
            }
        }

        /// <summary>
        /// The static entry method: the .NET binder picks the most specific overload for the parameter types; with no exact
        /// fit, the single method of that name and parameter count. Null with <paramref name="problem"/> otherwise.
        /// </summary>
        static MethodInfo? EntryMethod(Type? type, string name, IReadOnlyList<SnippetParameter> parameters, out string problem)
        {
            problem = "not found";
            if (type == null)
                return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            try
            {
                var exact = type.GetMethod(name, flags, null, parameters.Select(p => p.Type).ToArray(), null);
                if (exact != null)
                    return exact;
            }
            catch (AmbiguousMatchException)
            {
                problem = "is ambiguous for the given parameter types";
                return null;
            }
            var byCount = type.GetMethods(flags).Where(m => m.Name == name && m.GetParameters().Length == parameters.Count).ToList();
            if (byCount.Count > 1)
                problem = "has several overloads and none fits the given parameter types";
            return byCount.Count == 1 ? byCount[0] : null;
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

        /// <summary>
        /// <c>Type: message</c> plus only the stack frames of the snippet's own assembly (snippet and includes): the first and
        /// the last <see cref="OutputLimits.FramesKept"/> of them; then each inner exception the same way after
        /// <c> ---&gt; </c>, at most <see cref="OutputLimits.MaxCauses"/>. Never throws, whatever the exception's members do.
        /// </summary>
        internal static string FormatException(Exception exception, Assembly? snippet)
        {
            while ((exception is TargetInvocationException || exception is AggregateException) && exception.InnerException != null)
                exception = exception.InnerException;
            var sb = new StringBuilder();
            AppendException(sb, exception, snippet);
            var inner = exception.InnerException;
            for (var depth = 0; inner != null && depth < OutputLimits.MaxCauses; depth++, inner = inner.InnerException)
                AppendException(sb.AppendLine().Append(" ---> "), inner, snippet);
            if (inner != null)
                sb.AppendLine().Append(" ---> ... more inner exception(s) left out");
            return sb.ToString();
        }

        static void AppendException(StringBuilder sb, Exception exception, Assembly? snippet)
        {
            string message;
            try { message = exception.Message; }
            catch (Exception) { message = "<unprintable message>"; }
            sb.Append(exception.GetType().FullName).Append(": ").Append(message);
            if (snippet == null)
                return;
            var frames = new List<string>();
            try
            {
                foreach (var frame in new StackTrace(exception, true).GetFrames() ?? Array.Empty<StackFrame>())
                {
                    var method = frame.GetMethod();
                    if (method?.DeclaringType?.Assembly != snippet)
                        continue;
                    var line = "   at " + method.DeclaringType.FullName + "." + method.Name;
                    var file = frame.GetFileName();
                    if (!string.IsNullOrEmpty(file))
                        line += " in " + file + ":line " + frame.GetFileLineNumber();
                    frames.Add(line);
                }
            }
            catch (Exception) { } // a stack that cannot be read: the message alone
            const int kept = OutputLimits.FramesKept;
            if (frames.Count > 2 * kept)
                frames = frames.Take(kept).Concat(new[] { $"   ... {frames.Count - 2 * kept} frame(s) left out" }).Concat(frames.Skip(frames.Count - kept)).ToList();
            foreach (var line in frames)
                sb.AppendLine().Append(line);
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
                if (_loaded.TryGetValue(key, out var loaded))
                    return loaded;
                if (!_cache.TryGetValue(key, out var node))
                    return null;
                _lru.Remove(node);
                _lru.AddFirst(node);
                return node.Value;
            }
        }

        void ToCache(CacheEntry entry)
        {
            if (_options.CacheCapacity <= 0)
                return;
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(entry.Key) || _loaded.ContainsKey(entry.Key))
                    return;
                _cache[entry.Key] = _lru.AddFirst(entry);
                while (_cache.Count > _options.CacheCapacity)
                {
                    _cache.Remove(_lru.Last!.Value.Key);
                    _lru.RemoveLast();
                }
            }
        }

        internal sealed class CacheEntry
        {
            public CacheEntry(string key, string assemblyName, CompileResult compiled, IReadOnlyList<string> autoImported)
            {
                Key = key;
                AssemblyName = assemblyName;
                Image = compiled.Image;
                Symbols = compiled.Symbols;
                EntryType = compiled.EntryType;
                EntryMethod = compiled.EntryMethod;
                ResultType = compiled.ResultType;
                AutoImported = autoImported;
            }

            public string Key { get; }
            public string AssemblyName { get; }

            /// <summary>Dropped once a non-unloadable host has loaded the assembly for good.</summary>
            public byte[]? Image { get; set; }
            public byte[]? Symbols { get; set; }
            public string EntryType { get; }
            public string EntryMethod { get; }
            public string? ResultType { get; }
            public IReadOnlyList<string> AutoImported { get; }

            /// <summary>Hosts that cannot unload: whether the snippet declares static state (then it is never reused).</summary>
            public bool? HasStaticState { get; set; }

            /// <summary>Set only for hosts that cannot unload (Mono): the assembly is loaded once and reused.</summary>
            public ILoadedAssembly? Loaded { get; set; }
        }

        /// <summary>State of one invocation, from load to unload.</summary>
        sealed class Execution
        {
            readonly CodeRunner _runner;
            readonly PreparedSnippet _prepared;
            readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            readonly LogBudget _logs;
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
                _logs = new LogBudget(prepared.Request.MaxLogChars, prepared.Request.MaxLogLineChars);
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
                    _captures.Add(capture.Begin(_logs.Add));
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

                // Output limits (spec §6): a text value, the logs and the error, in the order `truncated` lists them.
                var request = _prepared.Request;
                var truncated = new List<string>();
                if (value is string text && !ReferenceEquals(value = OutputLimits.Clip(text, request.MaxValueChars, OutputLimits.ValueHint), text))
                    truncated.Add("value");
                var logs = _logs.Entries(out var logsTruncated);
                if (logsTruncated)
                    truncated.Add("logs");
                var error = _error == null ? null : OutputLimits.Clip(_error, request.MaxErrorChars, OutputLimits.ErrorHint);
                if (!ReferenceEquals(error, _error))
                    truncated.Add("error");
                return new ExecuteResult
                {
                    Success = _errorKind == ErrorKind.None,
                    Value = value,
                    ReturnType = ReturnType,
                    Logs = logs,
                    ErrorKind = _errorKind,
                    Error = error,
                    Truncated = truncated,
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
