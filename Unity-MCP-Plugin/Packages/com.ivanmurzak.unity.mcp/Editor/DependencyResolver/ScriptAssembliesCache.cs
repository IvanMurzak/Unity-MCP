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
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
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

        /// <summary>
        /// Strips <see cref="NuGetConfig.ReadyDefine"/> from every build target
        /// group. Main plugin asmdefs are gated on this define, so removing it
        /// makes them skip compilation on the next pass — only the
        /// DependencyResolver assembly compiles, runs migration, and re-adds
        /// the define when the DLL set is healthy. Belt-and-braces against
        /// the case where the resolver itself can't compile through some
        /// unrelated user-asmdef error and the project gets stuck.
        /// </summary>
        public static void RemoveReadyDefine()
        {
            var removed = false;

            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;

                NamedBuildTarget target;
                try { target = NamedBuildTarget.FromBuildTargetGroup(group); }
                catch { continue; }

                if (TryRemoveDefine(target))
                    removed = true;
            }

            if (TryRemoveDefine(NamedBuildTarget.Server))
                removed = true;

            if (removed)
                Debug.Log($"{Tag} Removed '{NuGetConfig.ReadyDefine}' scripting define; resolver will re-add it once the DLL set is healthy.");
        }

        static bool TryRemoveDefine(NamedBuildTarget target)
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                if (!defines.Contains(NuGetConfig.ReadyDefine))
                    return false;

                PlayerSettings.SetScriptingDefineSymbols(
                    target,
                    defines.Where(d => d != NuGetConfig.ReadyDefine).ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
