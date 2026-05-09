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
    /// Forces Unity to recompile every assembly on the next compile by wiping
    /// <c>Library/ScriptAssemblies/</c>. Used after operations that swap plugin
    /// source code (package update, force-resolve) so the post-operation reload
    /// can't keep stale plugin assemblies loaded in the AppDomain.
    /// </summary>
    public static class ScriptAssembliesCache
    {
        const string Tag = "[ScriptAssembliesCache]";

        /// <summary>
        /// Best-effort recursive delete; falls back to per-file delete when
        /// the atomic call hits a locked file (loaded plugin DLLs are mmapped
        /// on Windows). Whatever survives gets overwritten by Unity's next
        /// recompile.
        /// </summary>
        public static void Wipe()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "ScriptAssemblies"));
            if (!Directory.Exists(path))
                return;

            try
            {
                Directory.Delete(path, recursive: true);
                Debug.Log($"{Tag} Wiped {path} — Unity will recompile every assembly on the next compile.");
                return;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }

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
