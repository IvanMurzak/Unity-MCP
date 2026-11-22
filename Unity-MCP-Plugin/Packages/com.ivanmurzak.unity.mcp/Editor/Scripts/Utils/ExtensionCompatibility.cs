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
    /// <summary>How an installed extension stands with the installed plugin.</summary>
    internal enum ExtensionFit
    {
        /// <summary>Unity compiles it with this plugin.</summary>
        Works,

        /// <summary>
        /// Unity compiles it, but nothing ties its assemblies to this plugin release: after the next
        /// plugin update it would compile while the plugin itself is still skipped, and fail.
        /// </summary>
        Unprotected,

        /// <summary>Built for another plugin release: Unity skips it without an error and its tools are missing.</summary>
        Skipped,
    }

    /// <summary>
    /// Whether an installed extension can work with this plugin, and which published version can.
    /// </summary>
    /// <remarks>
    /// An extension works with exactly one plugin release. Its assemblies carry the plugin's
    /// dependency-generation define in their <c>defineConstraints</c> (Unity-MCP#957) and a
    /// <c>versionDefines</c> entry that holds only for that plugin version, so with any other plugin
    /// Unity skips them without an error: the tools are simply missing. Both are static data in the
    /// installed asmdefs, readable without the network; the published version built for a plugin is the
    /// one whose dependency on the plugin names that plugin version.
    /// Engine-free on purpose: no Unity API, so the engine-free test project compiles this file.
    /// </remarks>
    internal static class ExtensionCompatibility
    {
        private static readonly Regex StableVersion = new(@"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

        // A version may carry a pre-release or build suffix (1.14.0-rc.1): such a build of the plugin
        // stands for its release, so the numeric part is what is compared.
        private static readonly Regex VersionCore = new(@"^\s*(\d+(?:\.\d+){0,2})(?:[-+].*)?\s*$", RegexOptions.Compiled);

        /// <summary>
        /// The asmdefs Unity compiles in a consumer project. Test, hidden (<c>~</c>) and dot folders are
        /// not entered: Unity ignores the last two. The walk stops <paramref name="depth"/> folders
        /// down: a <c>file:</c> package may sit at a repository root with a whole project beside it,
        /// and this runs at every domain reload. The asmdefs of a package sit within three folders.
        /// </summary>
        internal static IEnumerable<string> ShippedAsmdefs(string folder, int depth = 6)
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.asmdef"))
                yield return file;
            if (depth == 0)
                yield break;
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(child);
                if (name == "Tests" || name.EndsWith("~") || name.StartsWith("."))
                    continue;
                foreach (var file in ShippedAsmdefs(child, depth - 1))
                    yield return file;
            }
        }

        /// <summary>
        /// How the extension with these asmdefs stands with plugin <paramref name="pluginVersion"/>, whose
        /// assemblies are gated on <paramref name="currentDefine"/>. An asmdef without the generation gate
        /// (an extension older than the gate, a third-party one) is not judged.
        /// </summary>
        internal static ExtensionFit FitOf(IEnumerable<string> asmdefTexts, string definePrefix, string currentDefine, string pluginPackageId, string pluginVersion)
        {
            var generation = new Regex("^" + Regex.Escape(definePrefix) + @"\d+$");
            var unprotected = false;
            foreach (var text in asmdefTexts)
            {
                try
                {
                    using var document = JsonDocument.Parse(text);
                    var root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                        continue;
                    var constraints = Strings(root, "defineConstraints").ToArray();
                    var generations = constraints.Where(constraint => generation.IsMatch(constraint)).ToArray();
                    if (generations.Any(define => define != currentDefine))
                        return ExtensionFit.Skipped;

                    var pairings = PluginExpressions(root, pluginPackageId, constraints).ToArray();
                    if (pairings.Any(expression => !Satisfies(expression, pluginVersion)))
                        return ExtensionFit.Skipped;
                    unprotected |= generations.Length > 0 && pairings.Length == 0;
                }
                catch (JsonException)
                {
                    // Unity would not compile it either; nothing can be said about it here.
                }
            }
            return unprotected ? ExtensionFit.Unprotected : ExtensionFit.Works;
        }

        private static IEnumerable<string> Strings(JsonElement asmdef, string property)
            => asmdef.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
                ? array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)
                : Enumerable.Empty<string>();

        /// <summary>The version expressions on the plugin package that the assembly requires to hold.</summary>
        private static IEnumerable<string> PluginExpressions(JsonElement asmdef, string pluginPackageId, string[] constraints)
        {
            if (!asmdef.TryGetProperty("versionDefines", out var entries) || entries.ValueKind != JsonValueKind.Array)
                yield break;
            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String && name.GetString() == pluginPackageId
                    && entry.TryGetProperty("define", out var define) && define.ValueKind == JsonValueKind.String && constraints.Contains(define.GetString())
                    && entry.TryGetProperty("expression", out var expression) && expression.ValueKind == JsonValueKind.String)
                    yield return expression.GetString()!;
            }
        }

        /// <summary>
        /// Unity's version-define expression: <c>1.2.3</c> (that version or newer), <c>[1.2.3]</c> (exactly),
        /// or an interval such as <c>[1.2.0,1.3.0)</c>. An expression that cannot be read is not judged.
        /// </summary>
        internal static bool Satisfies(string expression, string version)
        {
            if (!TryParse(version, out var actual))
                return true;
            expression = expression.Trim();
            if (expression.Length == 0)
                return true;

            var opens = expression[0] == '[' || expression[0] == '(';
            var closes = expression[expression.Length - 1] == ']' || expression[expression.Length - 1] == ')';
            if (!opens && !closes)
                return !TryParse(expression, out var minimum) || actual >= minimum;
            if (!opens || !closes)
                return true;

            var bounds = expression.Substring(1, expression.Length - 2).Split(',');
            if (bounds.Length == 1)
                return !TryParse(bounds[0], out var exact) || actual == exact;
            if (bounds.Length != 2)
                return true;
            if (TryParse(bounds[0], out var lower) && (expression[0] == '[' ? actual < lower : actual <= lower))
                return false;
            if (TryParse(bounds[1], out var upper) && (expression[expression.Length - 1] == ']' ? actual > upper : actual >= upper))
                return false;
            return true;
        }

        private static bool TryParse(string text, out Version version)
        {
            version = new Version(0, 0, 0);
            var match = VersionCore.Match(text);
            if (!match.Success)
                return false;
            var parts = match.Groups[1].Value.Split('.');
            var numbers = new int[3];
            for (var i = 0; i < parts.Length; i++)
            {
                // TryParse: the pattern lets a number through that an int cannot hold.
                if (!int.TryParse(parts[i], out numbers[i]))
                    return false;
            }
            version = new Version(numbers[0], numbers[1], numbers[2]);
            return true;
        }

        /// <summary>
        /// The highest published stable version built for plugin <paramref name="pluginVersion"/>, from an
        /// npm-style registry document: the one whose dependency on the plugin names exactly that version.
        /// Every plugin release ships its own extension versions, so a version with any other plugin
        /// dependency is skipped by Unity under this plugin.
        /// </summary>
        internal static string? VersionFor(string registryJson, string pluginPackageId, string pluginVersion)
        {
            if (!TryParse(pluginVersion, out var plugin))
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
                        || !StableVersion.IsMatch(floorNode.GetString()!)
                        || !TryParse(entry.Name, out var version)
                        || !TryParse(floorNode.GetString()!, out var floor))
                        continue;

                    if (floor == plugin && (best == null || version > best))
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
        /// The version to move an installed extension to: <see cref="VersionFor"/> when it is newer than the
        /// installed one, or, with <paramref name="anyDirection"/> (the installed one does not work with this
        /// plugin, so an older one that does is better), when it is simply another one. Else null.
        /// </summary>
        internal static string? UpdateTargetFor(string installedVersion, string registryJson, string pluginPackageId, string pluginVersion, bool anyDirection = false)
        {
            var target = VersionFor(registryJson, pluginPackageId, pluginVersion);
            if (target == null)
                return null;
            var published = Version.Parse(target);
            // An installed version that cannot be ordered (a pre-release) is never "older" than a release.
            var ordered = Version.TryParse(installedVersion, out var installed);
            if (ordered && published > installed)
                return target;
            return anyDirection && (!ordered || published != installed) ? target : null;
        }
    }
}
