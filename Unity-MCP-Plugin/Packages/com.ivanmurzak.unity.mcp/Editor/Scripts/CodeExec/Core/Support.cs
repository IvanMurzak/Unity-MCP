#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace com.IvanMurzak.CodeExec
{
    /// <summary>Simple type name → namespaces that define a public type with that name (spec §3.3).</summary>
    public static class TypeIndex
    {
        // Per assembly, built once: listing exported types is slow on Mono. Weak keys let collectible assemblies go.
        static readonly ConditionalWeakTable<Assembly, Dictionary<string, List<string>>> Index =
            new ConditionalWeakTable<Assembly, Dictionary<string, List<string>>>();

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> FindNamespaces(IEnumerable<string> simpleNames)
        {
            var found = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            var wanted = simpleNames.Distinct(StringComparer.Ordinal).ToList();
            foreach (var assembly in LoadedAssemblies.Host())
            {
                var index = Index.GetValue(assembly, Build);
                foreach (var name in wanted)
                {
                    if (!index.TryGetValue(name, out var namespaces))
                        continue;
                    if (!found.TryGetValue(name, out var set))
                        found[name] = set = new SortedSet<string>(StringComparer.Ordinal);
                    set.UnionWith(namespaces);
                }
            }
            return found.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.ToList(), StringComparer.Ordinal);
        }

        static Dictionary<string, List<string>> Build(Assembly assembly)
        {
            var index = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            Type[] types;
            try { types = assembly.GetExportedTypes(); }
            catch (Exception) { return index; } // assemblies whose dependencies are missing cannot list their types
            var coreLibrary = assembly == typeof(object).Assembly;
            foreach (var type in types)
            {
                if (type.IsNested || string.IsNullOrEmpty(type.Namespace) || (coreLibrary && IsInternalNamespace(type.Namespace!)))
                    continue;
                var name = TypeNames.StripArity(type.Name, out _);
                if (!index.TryGetValue(name, out var namespaces))
                    index[name] = namespaces = new List<string>();
                if (!namespaces.Contains(type.Namespace!))
                    namespaces.Add(type.Namespace!);
            }
            return index;
        }

        /// <summary>
        /// CoreLib's <c>Internal</c> / <c>Internal.*</c> types are public only for the runtime's own use
        /// (<c>Internal.Console</c> writes past <c>Console.SetOut</c>). Never a guess; an explicit using still works.
        /// </summary>
        static bool IsInternalNamespace(string ns) =>
            ns.StartsWith("Internal", StringComparison.Ordinal) && (ns.Length == 8 || ns[8] == '.');
    }

    /// <summary>
    /// Includes (spec §1): inside the include root unless the host allows otherwise; files must be <c>.cs</c>; folders
    /// contribute their <c>*.cs</c> recursively. Paths are reported relative to the root.
    /// </summary>
    static class IncludeResolver
    {
        public static List<SourceFile> Read(IReadOnlyList<string> include, string root, bool allowOutsideRoot)
        {
            var files = new List<SourceFile>();
            if (include.Count == 0)
                return files; // the root is not even needed (a host may leave it empty)
            var seen = new HashSet<string>(PathComparer);
            var prefix = WithSeparator(Path.GetFullPath(root));
            foreach (var entry in include)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    throw new ArgumentException("include entry is empty");
                var full = Path.GetFullPath(Path.IsPathRooted(entry) ? entry : Path.Combine(root, entry));
                if (!allowOutsideRoot && !IsUnder(prefix, WithSeparator(full))) // the root itself counts
                    throw new ArgumentException("include is outside the include root: " + entry);
                IEnumerable<string> paths;
                if (Directory.Exists(full))
                    paths = Directory.GetFiles(full, "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal);
                else if (File.Exists(full))
                {
                    if (!full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("include is not a .cs file: " + entry);
                    paths = new[] { full };
                }
                else
                    throw new FileNotFoundException("include not found: " + entry);
                foreach (var path in paths)
                {
                    if (seen.Add(path))
                        files.Add(new SourceFile(Relative(prefix, path), File.ReadAllText(path)));
                }
            }
            return files;
        }

        // Windows paths are case-insensitive; elsewhere a differently cased path may be another folder.
        static readonly StringComparison PathComparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        static readonly StringComparer PathComparer = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        static string WithSeparator(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;

        static bool IsUnder(string prefix, string path) => path.StartsWith(prefix, PathComparison);

        static string Relative(string prefix, string path) => (IsUnder(prefix, path) ? path.Substring(prefix.Length) : path).Replace('\\', '/');
    }

    /// <summary>
    /// Whether a snippet assembly keeps state between runs if it is reused: a static field (an auto-property's or an
    /// event's backing field included) declared by one of the agent's own types, unless it is <c>readonly</c> and of an
    /// immutable type (spec §3.5): a lookup table or a value computed once is not state. The compiler's own types (lambda
    /// caches, array-initializer data) are stateless for this purpose.
    /// </summary>
    static class StaticState
    {
        public static bool Has(Assembly assembly)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            try
            {
                // The compiler's own types (lambda caches, array-initializer data, collection-expression wrappers) are skipped.
                var types = assembly.GetTypes().Where(t => !t.IsDefined(typeof(CompilerGeneratedAttribute), false)).ToList();
                // A collection view the snippet implements (or derives) itself may keep state in its instance: then no view
                // type is trusted to be immutable.
                var trustViews = !types.Any(ImplementsView);
                foreach (var type in types)
                {
                    if (type.GetFields(flags).Any(f => !f.IsLiteral && !(f.IsInitOnly && IsImmutable(f.FieldType, trustViews))))
                        return true;
                }
                return false;
            }
            catch (Exception) // a type or attribute that cannot load: cannot tell, so be safe and load fresh copies
            {
                return true;
            }
        }

        static readonly HashSet<Type> ImmutableValues = new HashSet<Type>
        {
            typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid),
        };

        // Read-only views: their contents cannot change through the declared type (a cast back to a mutable one can).
        static readonly HashSet<string> ReadOnlyViews = new HashSet<string>
        {
            "System.Collections.Generic.IReadOnlyCollection`1", "System.Collections.Generic.IReadOnlyList`1",
            "System.Collections.Generic.IReadOnlyDictionary`2", "System.Collections.Generic.IReadOnlySet`1",
            "System.Collections.ObjectModel.ReadOnlyCollection`1", "System.Collections.ObjectModel.ReadOnlyDictionary`2",
        };

        /// <summary>Immutable by declared type (spec §3.5). Arrays and pointer-sized integers (native memory) are mutable.</summary>
        static bool IsImmutable(Type type, bool trustViews)
        {
            if ((type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr)) || type.IsEnum || ImmutableValues.Contains(type))
                return true;
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
                return IsImmutable(underlying, trustViews);
            return trustViews && IsView(type) && type.GetGenericArguments().All(t => IsImmutable(t, trustViews));
        }

        static bool IsView(Type type)
        {
            if (!type.IsGenericType)
                return false;
            var definition = type.GetGenericTypeDefinition();
            // Nested types there are the mutable builders and enumerators.
            return (definition.Namespace == "System.Collections.Immutable" && !definition.IsNested) || ReadOnlyViews.Contains(definition.FullName ?? "");
        }

        static bool ImplementsView(Type type)
        {
            for (var t = (Type?)type; t != null; t = t.BaseType)
            {
                if (IsView(t))
                    return true;
            }
            return type.GetInterfaces().Any(IsView);
        }
    }

    /// <summary>Formats a runtime <see cref="Type"/> as C# source: <c>global::System.Collections.Generic.List&lt;global::System.Int32&gt;</c>.</summary>
    public static class TypeNames
    {
        public static string ToCSharp(Type type)
        {
            if (type.IsByRef || type.IsPointer || type.IsGenericParameter)
                throw new ArgumentException("unsupported parameter type: " + type);
            if (type.IsArray)
                return ToCSharp(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";

            var arguments = type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes;
            var chain = new List<Type>();
            for (var t = type; t != null; t = t.DeclaringType)
                chain.Insert(0, t);

            var sb = new StringBuilder("global::");
            if (!string.IsNullOrEmpty(chain[0].Namespace))
                sb.Append(chain[0].Namespace).Append('.');
            var used = 0;
            for (var i = 0; i < chain.Count; i++)
            {
                if (i > 0)
                    sb.Append('.');
                sb.Append(StripArity(chain[i].Name, out var arity));
                if (arity > 0)
                {
                    sb.Append('<').Append(string.Join(", ", arguments.Skip(used).Take(arity).Select(ToCSharp))).Append('>');
                    used += arity;
                }
            }
            return sb.ToString();
        }

        /// <summary><c>List`1</c> → <c>List</c> (arity 1).</summary>
        internal static string StripArity(string name, out int arity)
        {
            var tick = name.IndexOf('`');
            arity = tick > 0 ? int.Parse(name.Substring(tick + 1)) : 0;
            return tick > 0 ? name.Substring(0, tick) : name;
        }
    }

    /// <summary>Collects what a snippet writes while it runs. Hosts add their own (Unity log, Godot print, ...).</summary>
    public interface ILogCapture
    {
        IDisposable Begin(Action<LogEntry> sink);
    }

    /// <summary>Redirects <see cref="Console.Out"/> / <see cref="Console.Error"/> for the duration of a run.</summary>
    public sealed class ConsoleLogCapture : ILogCapture
    {
        public IDisposable Begin(Action<LogEntry> sink)
        {
            var output = Console.Out;
            var error = Console.Error;
            var outWriter = new LineWriter(line => sink(new LogEntry("info", line)));
            var errWriter = new LineWriter(line => sink(new LogEntry("error", line)));
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            return new Restore(() =>
            {
                outWriter.Flush();
                errWriter.Flush();
                Console.SetOut(output);
                Console.SetError(error);
            });
        }

        sealed class LineWriter : TextWriter
        {
            readonly Action<string> _line;
            readonly StringBuilder _buffer = new StringBuilder();
            public LineWriter(Action<string> line) { _line = line; }
            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value) => Write(value.ToString());

            public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));

            public override void Write(string? value)
            {
                if (string.IsNullOrEmpty(value))
                    return;
                var start = 0;
                for (int newline; (newline = value!.IndexOf('\n', start)) >= 0; start = newline + 1)
                {
                    _buffer.Append(value, start, newline - start);
                    EmitLine();
                }
                _buffer.Append(value, start, value.Length - start);
            }

            public override void Flush()
            {
                if (_buffer.Length > 0)
                    EmitLine();
            }

            void EmitLine()
            {
                if (_buffer.Length > 0 && _buffer[_buffer.Length - 1] == '\r')
                    _buffer.Length--;
                _line(_buffer.ToString());
                _buffer.Clear();
            }
        }
    }

    sealed class Restore : IDisposable
    {
        Action? _action;
        public Restore(Action action) { _action = action; }

        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }
}
