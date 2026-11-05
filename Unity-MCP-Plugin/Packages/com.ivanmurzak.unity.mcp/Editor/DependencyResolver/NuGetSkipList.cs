/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Pure helpers behind <see cref="NuGetProjectSettings.EffectiveSkipPackages"/>: merging the
    /// built-in skip list with the project-level one, and finding skipped packages the project
    /// does not actually provide. Kept free of Unity API so it is unit-testable against plain inputs.
    /// </summary>
    static class NuGetSkipList
    {
        /// <summary>
        /// Merges the built-in and project-level skip lists into one case-insensitive set of
        /// NuGet package IDs. Entries are trimmed; blank entries are dropped.
        /// </summary>
        public static HashSet<string> Merge(IEnumerable<string> builtIn, IEnumerable<string> project)
        {
            var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddAll(merged, builtIn);
            AddAll(merged, project);
            return merged;
        }

        /// <summary>
        /// Returns the skipped package IDs for which <paramref name="isProvidedByProject"/> is
        /// false. A package is expected to be provided under an assembly with the same name as
        /// its ID; packages whose assembly name differs from the ID are reported as unprovided,
        /// so the caller must treat the result as a warning, not a hard failure.
        /// </summary>
        public static List<string> FindUnprovided(IEnumerable<string> skipPackages, Func<string, bool> isProvidedByProject)
        {
            var unprovided = new List<string>();
            foreach (var packageId in skipPackages)
            {
                if (!isProvidedByProject(packageId))
                    unprovided.Add(packageId);
            }
            return unprovided;
        }

        static void AddAll(HashSet<string> target, IEnumerable<string> source)
        {
            foreach (var entry in source)
            {
                var trimmed = entry?.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    target.Add(trimmed!);
            }
        }
    }
}
