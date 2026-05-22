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
        /// Adds <see cref="NuGetConfig.ReadyDefine"/> to every supported
        /// build target group. Applying it only to the active group would
        /// let target switching reintroduce compile failures in assemblies
        /// gated by <c>defineConstraints</c>.
        /// </summary>
        public static void EnsureReadyDefine()
        {
            var changed = ForEachTarget(TryAddReadyDefine);
            if (changed.Count > 0)
                Debug.Log($"{Tag} Added '{NuGetConfig.ReadyDefine}' for: {string.Join(", ", changed)}.");
        }

        /// <summary>Strips <see cref="NuGetConfig.ReadyDefine"/> from every supported build target group.</summary>
        public static void RemoveReadyDefine()
        {
            var changed = ForEachTarget(TryRemoveReadyDefine);
            if (changed.Count > 0)
                Debug.Log($"{Tag} Removed '{NuGetConfig.ReadyDefine}' from: {string.Join(", ", changed)}; resolver will re-add it once the DLL set is healthy.");
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

        static bool TryAddReadyDefine(NamedBuildTarget target)
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                if (Array.IndexOf(defines, NuGetConfig.ReadyDefine) >= 0)
                    return false;

                var newDefines = new string[defines.Length + 1];
                Array.Copy(defines, newDefines, defines.Length);
                newDefines[defines.Length] = NuGetConfig.ReadyDefine;

                PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool TryRemoveReadyDefine(NamedBuildTarget target)
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                var index = Array.IndexOf(defines, NuGetConfig.ReadyDefine);
                if (index < 0)
                    return false;

                var newDefines = new string[defines.Length - 1];
                Array.Copy(defines, 0, newDefines, 0, index);
                Array.Copy(defines, index + 1, newDefines, index, defines.Length - index - 1);

                PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
