/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.  |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Helpers for invalidating Unity's <c>Library/ScriptAssemblies/</c> cache.
    /// Wiping the cache forces Unity to recompile every assembly from scratch
    /// on the next compile attempt — used after operations that swap plugin
    /// source code under the editor (package update, force-resolve) so the
    /// post-operation domain reload can't accidentally keep stale plugin
    /// assemblies loaded in the AppDomain.
    /// </summary>
    public static class ScriptAssembliesCache
    {
        const string Tag = "[ScriptAssembliesCache]";

        /// <summary>
        /// Best-effort recursive delete of <c>Library/ScriptAssemblies/</c>.
        /// Tries the atomic <see cref="Directory.Delete(string, bool)"/> first
        /// and falls back to per-file deletion when a single file is locked
        /// (the currently-loaded plugin DLLs are mmapped into the editor
        /// AppDomain, so on Windows their file handles may still be held).
        /// Whatever survives the wipe will be overwritten by Unity's next
        /// recompile anyway; the goal is just to maximize the chance of a
        /// clean rebuild without requiring the user to close Unity and
        /// delete the folder by hand.
        /// </summary>
        public static void Wipe()
        {
            // Application.dataPath is the absolute path to Assets/; Library/
            // is its sibling at the project root.
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies"));
            if (!Directory.Exists(path))
                return;

            try
            {
                Directory.Delete(path, recursive: true);
                Debug.Log($"{Tag} Wiped {path} — Unity will recompile every assembly on the next compile.");
                return;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Fall through to per-file best-effort.
            }

            var locked = 0;
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(file); }
                catch { locked++; }
            }
            Debug.LogWarning($"{Tag} Could not fully wipe {path} ({locked} locked file(s) survived); Unity will recompile what it can on the next compile.");
        }
    }
}
