#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace com.IvanMurzak.CodeExec
{
    /// <summary>
    /// Supplies the metadata references (assembly file paths) a snippet compiles against. Returning the SAME list instance
    /// while nothing changed lets <see cref="CodeRunner"/> and the compiler skip re-hashing and re-reading references.
    /// </summary>
    public interface IReferenceProvider
    {
        IReadOnlyList<string> GetReferencePaths();
    }

    /// <summary>
    /// Every host assembly loaded in the process that has a file on disk, one per simple name. The list is rebuilt only
    /// when the set of host assemblies changes (snippet loads and unloads do not count). No event subscriptions: a
    /// long-lived handler would pin a host that reloads its own code (Godot's collectible editor context).
    /// </summary>
    public sealed class LoadedAssembliesReferenceProvider : IReferenceProvider
    {
        readonly Func<Assembly, string?>? _locationResolver;
        readonly object _lock = new object();
        IReadOnlyList<string> _paths = Array.Empty<string>();
        long _signature = -1;

        /// <param name="locationResolver">
        /// Host hook for assemblies loaded from memory (empty <see cref="Assembly.Location"/>): Godot loads the game
        /// assembly from a stream, so the host points at the DLL it was built to. Without it such assemblies are
        /// invisible to snippets.
        /// </param>
        public LoadedAssembliesReferenceProvider(Func<Assembly, string?>? locationResolver = null)
        {
            _locationResolver = locationResolver;
        }

        public IReadOnlyList<string> GetReferencePaths()
        {
            long signature = 0;
            foreach (var assembly in LoadedAssemblies.Host())
                signature = signature * 31 + RuntimeHelpers.GetHashCode(assembly);
            lock (_lock)
            {
                if (signature != _signature)
                {
                    _paths = Build();
                    _signature = signature;
                }
                return _paths;
            }
        }

        IReadOnlyList<string> Build()
        {
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in LoadedAssemblies.Host())
            {
                var name = LoadedAssemblies.SimpleName(assembly);
                if (byName.ContainsKey(name))
                    continue;
                var location = LocationOf(assembly);
                if (location.Length > 0 && File.Exists(location))
                    byName[name] = location;
            }
#if NET8_0_OR_GREATER
            AddFrameworkFacades(byName);
#endif
            return new List<string>(byName.Values);
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Framework assemblies a snippet may use before the host ever touched them: the default usings and
        /// <c>Console</c>. Without System.Console, <c>Console</c> would not resolve, and auto-import must not reach for
        /// CoreLib's public <c>Internal.Console</c>, whose output bypasses log capture.
        /// </summary>
        static readonly string[] FrameworkFacades = { "System.Runtime", "System.Console", "System.Collections", "System.Linq" };

        void AddFrameworkFacades(Dictionary<string, string> byName)
        {
            // CoreLib itself, never the host resolver: that one maps in-memory HOST assemblies, not the runtime.
            var directory = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? "";
            if (directory.Length == 0)
                return; // single-file or in-memory runtime: nothing on disk to point at
            foreach (var name in FrameworkFacades)
            {
                var path = Path.Combine(directory, name + ".dll");
                if (!byName.ContainsKey(name) && File.Exists(path))
                    byName[name] = path;
            }
        }
#endif

        string LocationOf(Assembly assembly)
        {
            string location;
            try { location = assembly.Location; }
            catch (NotSupportedException) { location = ""; }
            if (location.Length == 0 && _locationResolver != null)
                location = _locationResolver(assembly) ?? "";
            return location;
        }
    }
}
