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
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Operations that gate Unity's next compile pass to include only the
    /// resolver itself: wiping <c>Library/ScriptAssemblies/</c> (forces a
    /// full rebuild) and toggling <see cref="NuGetConfig.ReadyDefine"/>
    /// (main plugin asmdefs gate on it, so stripping it makes them skip
    /// compile until the resolver re-adds it on a healthy install).
    /// </summary>
    public static class RecompileGate
    {
        const string Tag = "[RecompileGate]";

        /// <summary>
        /// Belt-and-braces reset before swapping plugin source under the
        /// editor: forces a clean recompile (<see cref="Wipe"/>) AND blocks
        /// the main plugin asmdefs from compiling until the resolver
        /// re-validates the DLL set (<see cref="RemoveReadyDefine"/>). Use
        /// this when an unrelated user-asmdef compile error could otherwise
        /// keep the OLD plugin AppDomain loaded and prevent the new
        /// resolver code from running.
        /// </summary>
        public static void Reset()
        {
            Wipe();
            RemoveReadyDefine();
        }

        /// <summary>
        /// Best-effort recursive delete of <c>Library/ScriptAssemblies/</c>;
        /// falls back to per-file delete when the atomic call hits a locked
        /// file (loaded plugin DLLs are mmapped on Windows).
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
        /// Adds every <see cref="NuGetConfig.GateDefines"/> entry to every supported build target
        /// group, and strips stale dependency-generation defines left by an older package version.
        /// Applying it only to the active group would let target switching reintroduce compile
        /// failures in assemblies gated by <c>defineConstraints</c>.
        /// </summary>
        public static void EnsureReadyDefine()
        {
            var changed = ForEachTarget(TrySyncGateDefines);
            if (changed.Count > 0)
                Debug.Log($"{Tag} Synced gate defines ({string.Join(", ", NuGetConfig.GateDefines)}) for: {string.Join(", ", changed)}.");
        }

        /// <summary>Strips every gate define from every supported build target group.</summary>
        public static void RemoveReadyDefine()
        {
            var changed = ForEachTarget(TryRemoveGateDefines);
            if (changed.Count > 0)
                Debug.Log($"{Tag} Removed gate defines from: {string.Join(", ", changed)}; resolver will re-add them once the DLL set is healthy.");
        }

        /// <summary>
        /// Applies <paramref name="modify"/> to every supported build target
        /// group plus <see cref="NamedBuildTarget.Server"/> (a distinct
        /// target not reachable via <see cref="BuildTargetGroup"/>).
        /// Returns the names of targets the modifier reported as changed.
        /// </summary>
        static List<string> ForEachTarget(Func<NamedBuildTarget, bool> modify)
        {
            var changed = new List<string>();

            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;

                NamedBuildTarget target;
                try { target = NamedBuildTarget.FromBuildTargetGroup(group); }
                catch { continue; }

                if (modify(target))
                    changed.Add(target.TargetName);
            }

            if (modify(NamedBuildTarget.Server))
                changed.Add(NamedBuildTarget.Server.TargetName);

            return changed;
        }

        /// <summary>
        /// Brings <paramref name="target"/>'s define list to exactly "all current gate defines
        /// present, no stale dependency-generation define present". Dropping the stale generation
        /// matters on a DOWNGRADE: without it a project that once satisfied a newer generation would
        /// keep that define forever and the older package's gate would never re-close.
        /// </summary>
        static bool TrySyncGateDefines(NamedBuildTarget target)
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);

                var next = new List<string>(defines.Length + NuGetConfig.GateDefines.Length);
                foreach (var define in defines)
                {
                    // Drop superseded generations; keep the current one (re-added below in order).
                    if (define.StartsWith(NuGetConfig.DependencyGenerationDefinePrefix, StringComparison.Ordinal))
                        continue;
                    if (Array.IndexOf(NuGetConfig.GateDefines, define) >= 0)
                        continue;
                    next.Add(define);
                }
                next.AddRange(NuGetConfig.GateDefines);

                if (SameDefines(defines, next))
                    return false;

                PlayerSettings.SetScriptingDefineSymbols(target, next.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool TryRemoveGateDefines(NamedBuildTarget target)
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);

                var next = new List<string>(defines.Length);
                foreach (var define in defines)
                {
                    if (define.StartsWith(NuGetConfig.DependencyGenerationDefinePrefix, StringComparison.Ordinal))
                        continue;
                    if (Array.IndexOf(NuGetConfig.GateDefines, define) >= 0)
                        continue;
                    next.Add(define);
                }

                if (SameDefines(defines, next))
                    return false;

                PlayerSettings.SetScriptingDefineSymbols(target, next.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Order-sensitive equality — used to avoid a no-op write that would dirty ProjectSettings.</summary>
        static bool SameDefines(string[] current, List<string> next)
        {
            if (current.Length != next.Count)
                return false;
            for (var i = 0; i < current.Length; i++)
            {
                if (!string.Equals(current[i], next[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
