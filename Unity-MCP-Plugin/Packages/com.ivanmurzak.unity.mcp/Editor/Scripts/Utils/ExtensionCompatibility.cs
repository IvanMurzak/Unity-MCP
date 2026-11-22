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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Whether an installed extension can work with this plugin, and which published version can.
    /// </summary>
    /// <remarks>
    /// An extension's assemblies carry the plugin's dependency-generation define in their
    /// <c>defineConstraints</c> (Unity-MCP#957), so an extension built for another generation is
    /// skipped by Unity without an error: its tools are simply missing. The define in the installed
    /// asmdefs is therefore the exact compatibility marker, readable without the network.
    /// Engine-free on purpose: no Unity API, so the engine-free test project compiles this file.
    /// </remarks>
    internal static class ExtensionCompatibility
    {
        private static readonly Regex StableVersion = new(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

        /// <summary>
        /// The asmdefs Unity compiles in a consumer project. Test, hidden (<c>~</c>) and dot folders are
        /// not entered: Unity ignores the last two, and a <c>file:</c> package may sit at a repository root.
        /// </summary>
        internal static IEnumerable<string> ShippedAsmdefs(string folder)
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.asmdef"))
                yield return file;
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(child);
                if (name == "Tests" || name.EndsWith("~") || name.StartsWith("."))
                    continue;
                foreach (var file in ShippedAsmdefs(child))
                    yield return file;
            }
        }

        /// <summary>The dependency-generation defines an asmdef is constrained on.</summary>
        internal static IEnumerable<string> GenerationDefines(string asmdefText, string definePrefix)
            => Regex.Matches(asmdefText, "\"(" + Regex.Escape(definePrefix) + "\\d+)\"").Select(match => match.Groups[1].Value);

        /// <summary>
        /// True when any of the extension's asmdefs requires a generation other than
        /// <paramref name="currentDefine"/>. An extension without the gate is not judged.
        /// </summary>
        internal static bool IsBuiltForAnotherGeneration(IEnumerable<string> asmdefTexts, string definePrefix, string currentDefine)
            => asmdefTexts.SelectMany(text => GenerationDefines(text, definePrefix)).Any(define => define != currentDefine);

        /// <summary>
        /// The highest published stable version whose plugin floor is not above <paramref name="pluginVersion"/>,
        /// from an npm-style registry document. Never a version that would pull a newer plugin in:
        /// each plugin release that moves the generation ships its extensions with that release as
        /// their floor, so this is the version built for the installed plugin, when one exists.
        /// </summary>
        internal static string? HighestVersionFor(string registryJson, string pluginPackageId, string pluginVersion)
        {
            if (!Version.TryParse(pluginVersion, out var plugin))
                return null;
            try
            {
                using var document = JsonDocument.Parse(registryJson);
                if (!document.RootElement.TryGetProperty("versions", out var versions) || versions.ValueKind != JsonValueKind.Object)
                    return null;

                Version? best = null;
                foreach (var entry in versions.EnumerateObject())
                {
                    if (!StableVersion.IsMatch(entry.Name)
                        || entry.Value.ValueKind != JsonValueKind.Object
                        || !entry.Value.TryGetProperty("dependencies", out var dependencies)
                        || dependencies.ValueKind != JsonValueKind.Object
                        || !dependencies.TryGetProperty(pluginPackageId, out var floorNode)
                        || floorNode.ValueKind != JsonValueKind.String
                        || !StableVersion.IsMatch(floorNode.GetString()!))
                        continue;

                    var version = Version.Parse(entry.Name);
                    if (Version.Parse(floorNode.GetString()!) <= plugin && (best == null || version > best))
                        best = version;
                }
                return best?.ToString(3);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// The version to move an installed extension to: <see cref="HighestVersionFor"/> when it is
        /// newer than the installed one, else null (nothing newer is published for this plugin).
        /// </summary>
        internal static string? UpdateTargetFor(string installedVersion, string registryJson, string pluginPackageId, string pluginVersion)
        {
            var target = HighestVersionFor(registryJson, pluginPackageId, pluginVersion);
            return target != null && Version.TryParse(installedVersion, out var installed) && Version.Parse(target) > installed
                ? target
                : null;
        }
    }
}
