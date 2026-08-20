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
    /// Loads the top-level NuGet dependency pins from the <c>nuget-dependencies.json</c> manifest
    /// that ships INSIDE the UPM package, next to this resolver's sources.
    ///
    /// <para>Why a data file when the same pins exist in <see cref="NuGetConfig.Packages"/>:
    /// the compiled pins describe the package version the RUNNING assembly was built from. When the
    /// user upgrades the Unity-MCP package, UPM fires <c>Events.registeredPackages</c> from the
    /// still-alive previous AppDomain (see Unity-MCP#707) — i.e. from an assembly whose compiled
    /// pins are the OLD package's. Restoring with the old pins leaves stale DLLs on disk, the
    /// post-update recompile fails against them (e.g. CS0117 on an outdated McpPlugin.dll), the
    /// failed compile blocks the domain reload, and the resolver never gets another chance — the
    /// project is wedged (and the MCP server neither updates nor starts) until the user repairs the
    /// DLLs by hand. Reading the pins from the just-written package FILES gives the old-domain
    /// handler the NEW requirements, so the restore converges in one pass.</para>
    ///
    /// <para>The manifest and the compiled pins are kept in lockstep by a unit test
    /// (<c>NuGetDependencyManifestTests.ShippedManifest_MatchesCompiledPins</c>) — bump PRs that
    /// touch one but not the other fail CI.</para>
    ///
    /// <para>Design invariant of the DependencyResolver assembly: zero external dependencies (it
    /// must always compile, even when the main plugin cannot) — hence <see cref="JsonUtility"/>
    /// instead of a JSON library.</para>
    /// </summary>
    static class NuGetDependencyManifest
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>File name of the shipped dependency manifest.</summary>
        public const string FileName = "nuget-dependencies.json";

        /// <summary>Path of the manifest inside a package root folder.</summary>
        public static string GetPath(string packageRootPath)
            => Path.Combine(packageRootPath, "Editor", "DependencyResolver", FileName);

        /// <summary>
        /// Reads the dependency manifest from a package root folder. Returns null when the file is
        /// missing, unreadable, or unparsable — the caller then falls back to the compiled pins,
        /// which is exactly the pre-manifest behavior. Never throws.
        /// </summary>
        public static NuGetPackage[]? TryLoad(string? packageRootPath)
        {
            if (string.IsNullOrEmpty(packageRootPath))
                return null;

            try
            {
                var path = GetPath(packageRootPath);
                if (!File.Exists(path))
                    return null;

                return TryParse(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Failed to read '{FileName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses the manifest JSON. Returns null when the JSON is malformed, declares no packages,
        /// or any entry is missing its id/version — a partially-valid manifest is rejected as a
        /// whole so the caller never restores against a truncated pin list. Never throws.
        /// </summary>
        internal static NuGetPackage[]? TryParse(string json)
        {
            ManifestDto? dto;
            try
            {
                dto = JsonUtility.FromJson<ManifestDto>(json);
            }
            catch
            {
                return null;
            }

            if (dto?.packages == null || dto.packages.Length == 0)
                return null;

            var result = new NuGetPackage[dto.packages.Length];
            for (var i = 0; i < dto.packages.Length; i++)
            {
                var entry = dto.packages[i];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.id)
                    || string.IsNullOrWhiteSpace(entry.version))
                {
                    return null;
                }

                result[i] = new NuGetPackage(entry.id!.Trim(), entry.version!.Trim(), entry.includeInBuild);
            }
            return result;
        }

        // JsonUtility DTOs — field names must match the JSON keys exactly.
#pragma warning disable CS0649 // fields are assigned by JsonUtility
        [Serializable]
        class ManifestDto
        {
            public EntryDto[]? packages;
        }

        [Serializable]
        class EntryDto
        {
            public string? id;
            public string? version;
            public bool includeInBuild;
        }
#pragma warning restore CS0649
    }
}
