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
    /// References resolve against assemblies already loaded in the process: in Godot the game assembly lives in
    /// Godot's own collectible context, not in the default one.
    /// </summary>
    public sealed class CollectibleAssemblyHost : IAssemblyHost
    {
        public bool SupportsUnload => true;

        public ILoadedAssembly Load(byte[] image, byte[]? symbols, string name)
        {
            var context = new AssemblyLoadContext("CodeExec:" + name, isCollectible: true);
            context.Resolving += ResolveFromLoaded;
            using var imageStream = new MemoryStream(image);
            using var symbolStream = symbols == null ? null : new MemoryStream(symbols);
            return new Loaded(context, context.LoadFromStream(imageStream, symbolStream));
        }

        static Assembly? ResolveFromLoaded(AssemblyLoadContext context, AssemblyName name) => LoadedAssemblies.Find(name.Name);

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
                if (_context == null)
                    return;
                _context.Resolving -= ResolveFromLoaded;
                _context.Unload();
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
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic && !IsSnippet(assembly))
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

        /// <summary>The simple name from the runtime-cached <see cref="Assembly.FullName"/> (no AssemblyName allocation).</summary>
        public static string SimpleName(Assembly assembly)
        {
            var full = assembly.FullName ?? "";
            var comma = full.IndexOf(',');
            return comma < 0 ? full : full.Substring(0, comma);
        }
    }
}
