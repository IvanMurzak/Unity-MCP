#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
#if NET8_0_OR_GREATER
using System.IO;
using System.Runtime.Loader;
#endif

namespace com.IvanMurzak.CodeExec
{
    /// <summary>An emitted snippet assembly while it is in use. Disposing it releases it when the host can unload.</summary>
    public interface ILoadedAssembly : IDisposable
    {
        Assembly Assembly { get; }
    }

    /// <summary>Loads emitted images. Hosts differ only in whether they can unload them.</summary>
    public interface IAssemblyHost
    {
        bool SupportsUnload { get; }
        ILoadedAssembly Load(byte[] image, byte[]? symbols, string name);
    }

    /// <summary>
    /// Plain <c>Assembly.Load</c> — the only option on Mono (Unity) and .NET Framework. Nothing is unloaded until the
    /// domain goes away, which is why <see cref="CodeRunner"/> keeps loaded assemblies for identical requests.
    /// </summary>
    public sealed class DefaultAssemblyHost : IAssemblyHost
    {
        static int _resolverInstalled;

        public DefaultAssemblyHost()
        {
            // Assemblies loaded from bytes are not bound automatically when a snippet references them (.NET Framework,
            // Mono). Answer only for snippets, so binding failures elsewhere in the host stay untouched.
            if (Interlocked.Exchange(ref _resolverInstalled, 1) == 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
                    args.RequestingAssembly == null || LoadedAssemblies.IsSnippet(args.RequestingAssembly)
                        ? LoadedAssemblies.Find(new AssemblyName(args.Name).Name)
                        : null;
            }
        }

        public bool SupportsUnload => false;

        public ILoadedAssembly Load(byte[] image, byte[]? symbols, string name) =>
            new Loaded(symbols == null ? Assembly.Load(image) : Assembly.Load(image, symbols));

        sealed class Loaded : ILoadedAssembly
        {
            public Loaded(Assembly assembly) { Assembly = assembly; }
            public Assembly Assembly { get; }
            public void Dispose() { }
        }
    }

#if NET8_0_OR_GREATER
    /// <summary>
    /// One collectible <see cref="AssemblyLoadContext"/> per execution (CoreCLR: Godot, BRUTAL, plain .NET).
    /// A reference binds to the assembly the process ALREADY runs, in whatever context it lives (in Godot the game
    /// assembly is in Godot's own collectible context), BEFORE the default context is asked: a host's
    /// <c>AssemblyLoadContext.Default.Resolving</c> handler would otherwise load a second copy from disk, with fresh
    /// statics and, for engine bindings, uninitialized native interop. Only assemblies loaded nowhere yet fall back to
    /// the default context.
    /// </summary>
    public sealed class CollectibleAssemblyHost : IAssemblyHost
    {
        public bool SupportsUnload => true;

        public ILoadedAssembly Load(byte[] image, byte[]? symbols, string name)
        {
            var context = new SnippetContext("CodeExec:" + name);
            using var imageStream = new MemoryStream(image);
            using var symbolStream = symbols == null ? null : new MemoryStream(symbols);
            return new Loaded(context, context.LoadFromStream(imageStream, symbolStream));
        }

        sealed class SnippetContext : AssemblyLoadContext
        {
            public SnippetContext(string name) : base(name, isCollectible: true) { }

            // null = loaded nowhere yet: the usual default-context lookup.
            protected override Assembly? Load(AssemblyName assemblyName) => LoadedAssemblies.Find(assemblyName);
        }

        sealed class Loaded : ILoadedAssembly
        {
            AssemblyLoadContext? _context;

            public Loaded(AssemblyLoadContext context, Assembly assembly)
            {
                _context = context;
                Assembly = assembly;
            }

            public Assembly Assembly { get; }

            public void Dispose()
            {
                _context?.Unload();
                _context = null;
            }
        }
    }
#endif

    public static class AssemblyHosts
    {
        /// <summary>Collectible on CoreCLR builds of this library, plain load on netstandard2.0 (Unity / Mono).</summary>
        public static IAssemblyHost CreateDefault()
        {
#if NET8_0_OR_GREATER
            return new CollectibleAssemblyHost();
#else
            return new DefaultAssemblyHost();
#endif
        }
    }

    /// <summary>The host's own assemblies: loaded, not dynamic, not snippets. One place for that filter.</summary>
    static class LoadedAssemblies
    {
        public static bool IsSnippet(Assembly assembly) =>
            assembly.FullName?.StartsWith(CodeRunner.SnippetAssemblyPrefix, StringComparison.Ordinal) == true;

        public static IEnumerable<Assembly> Host()
        {
#if NET8_0_OR_GREATER
            // A context leaves AssemblyLoadContext.All once Unload() starts; its assemblies stay listed until collected.
            // They are on their way out (a host reload): never compile or bind against them.
            var live = new HashSet<AssemblyLoadContext>(AssemblyLoadContext.All);
#endif
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || IsSnippet(assembly))
                    continue;
#if NET8_0_OR_GREATER
                var context = AssemblyLoadContext.GetLoadContext(assembly);
                if (context != null && !live.Contains(context))
                    continue;
#endif
                yield return assembly;
            }
        }

        public static Assembly? Find(string? simpleName)
        {
            if (simpleName == null)
                return null;
            foreach (var assembly in Host())
            {
                if (string.Equals(SimpleName(assembly), simpleName, StringComparison.OrdinalIgnoreCase))
                    return assembly;
            }
            return null;
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// The loaded host assembly a reference means, or null (loaded nowhere compatible: the default-context lookup).
        /// A candidate has the same simple name, a version no lower than the referenced one, and the same culture and
        /// public key token when the reference names them. Deterministic order: the exact identity, then the copy in the
        /// default context, then the higher version, then the later loaded (a host reload lists the new copy last).
        /// </summary>
        public static Assembly? Find(AssemblyName reference)
        {
            var simpleName = reference.Name;
            if (simpleName == null)
                return null;
            var fullName = reference.FullName;
            var version = reference.Version;
            var culture = reference.CultureName;
            var token = reference.GetPublicKeyToken();
            Assembly? best = null;
            AssemblyName? bestName = null;
            int bestRank = -1;
            foreach (var assembly in Host())
            {
                if (!string.Equals(SimpleName(assembly), simpleName, StringComparison.OrdinalIgnoreCase))
                    continue;
                var name = assembly.GetName();
                if (version != null && name.Version != null && name.Version < version)
                    continue; // older than what the snippet compiled against: members may be missing
                if (!string.IsNullOrEmpty(culture) && !string.Equals(name.CultureName, culture, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (token != null && token.Length > 0 && !SameBytes(token, name.GetPublicKeyToken()))
                    continue;
                var rank = (string.Equals(assembly.FullName, fullName, StringComparison.OrdinalIgnoreCase) ? 2 : 0)
                    + (AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.Default ? 1 : 0);
                if (rank > bestRank || (rank == bestRank && !(name.Version < bestName!.Version)))
                {
                    best = assembly;
                    bestName = name;
                    bestRank = rank;
                }
            }
            return best;
        }

        static bool SameBytes(byte[] a, byte[]? b)
        {
            if (b == null || a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
#endif

        /// <summary>The simple name from the runtime-cached <see cref="Assembly.FullName"/> (no AssemblyName allocation).</summary>
        public static string SimpleName(Assembly assembly)
        {
            var full = assembly.FullName ?? "";
            var comma = full.IndexOf(',');
            return comma < 0 ? full : full.Substring(0, comma);
        }
    }
}
