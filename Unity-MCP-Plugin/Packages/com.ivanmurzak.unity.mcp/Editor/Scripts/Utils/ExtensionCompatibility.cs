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
using System.Globalization;
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

        // A version as Unity orders package versions: up to three numbers, a pre-release suffix that
        // sorts it below its release (1.14.0-rc.1 < 1.14.0), build metadata that changes nothing.
        private static readonly Regex AnyVersion = new(@"^(\d+(?:\.\d+){0,2})(-[^+]+)?(?:\+.*)?$", RegexOptions.Compiled);

        // A plugin release: all three numbers, no pre-release suffix (the CLI's `pluginRelease` reads the
        // same shape). No extension is built for a pre-release plugin: Unity's `[1.14.0]` does not hold for it.
        private static readonly Regex Release = new(@"^(\d+\.\d+\.\d+)(?:\+.*)?$", RegexOptions.Compiled);

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
                    var generations = constraints.Where(constraint => IsGenerationDefine(constraint, definePrefix)).ToArray();
                    if (generations.Any(define => define != currentDefine))
                        return ExtensionFit.Skipped;

                    // Only an assembly behind the generation gate is the extension itself: a package
                    // may keep an optional assembly for certain plugin versions.
                    if (generations.Length == 0)
                        continue;
                    var pairings = PluginExpressions(root, pluginPackageId, constraints).ToArray();
                    if (pairings.Any(expression => !Satisfies(expression, pluginVersion)))
                        return ExtensionFit.Skipped;
                    unprotected |= pairings.Length == 0;
                }
                catch (JsonException)
                {
                    // Unity would not compile it either; nothing can be said about it here.
                }
            }
            return unprotected ? ExtensionFit.Unprotected : ExtensionFit.Works;
        }

        private static bool IsGenerationDefine(string define, string prefix)
            => define.Length > prefix.Length && define.StartsWith(prefix, StringComparison.Ordinal) && define.Skip(prefix.Length).All(char.IsDigit);

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
        /// Does Unity set the define of a version-define expression for package version <paramref name="version"/>?
        /// <c>1.2.3</c> (that version or newer), <c>[1.2.3]</c> (exactly), or an interval such as
        /// <c>[1.2.0,1.3.0)</c>; a pre-release sorts below its release. Unity leaves the define of an
        /// expression it cannot read unset (whitespace, <c>(1.2.3)</c>). A version that cannot be read is not judged.
        /// </summary>
        internal static bool Satisfies(string expression, string version)
        {
            if (!TryParse(version, out var actual))
                return true;
            if (expression.Length == 0)
                return false;

            var first = expression[0];
            var last = expression[expression.Length - 1];
            if (first != '[' && first != '(' && last != ']' && last != ')')
                return TryParse(expression, out var minimum) && Compare(actual, minimum) >= 0;
            if ((first != '[' && first != '(') || (last != ']' && last != ')'))
                return false;

            var bounds = expression.Substring(1, expression.Length - 2).Split(',');
            if (bounds.Length == 1)
                return first == '[' && last == ']' && TryParse(bounds[0], out var exact) && Compare(actual, exact) == 0;
            if (bounds.Length != 2 || !TryParse(bounds[0], out var lower) || !TryParse(bounds[1], out var upper))
                return false;
            var above = Compare(actual, lower);
            var below = Compare(upper, actual);
            return (first == '[' ? above >= 0 : above > 0) && (last == ']' ? below >= 0 : below > 0);
        }

        private static int Compare((Version Numbers, bool Prerelease) left, (Version Numbers, bool Prerelease) right)
        {
            var numbers = left.Numbers.CompareTo(right.Numbers);
            return numbers != 0 ? numbers : right.Prerelease.CompareTo(left.Prerelease);
        }

        private static bool TryParse(string text, out (Version Numbers, bool Prerelease) version)
        {
            version = (new Version(0, 0, 0), false);
            var match = AnyVersion.Match(text);
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
            version = (new Version(numbers[0], numbers[1], numbers[2]), match.Groups[2].Success);
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
            if (!TryParse(Release.Match(pluginVersion).Groups[1].Value, out var release))
                return null;
            var plugin = release.Numbers;
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

                    if (floor.Numbers == plugin && (best == null || version.Numbers > best))
                        best = version.Numbers;
                }
                return best?.ToString(3);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Was <paramref name="version"/> of a package published less than <paramref name="window"/> ago,
        /// by the <c>time</c> map of its npm-style registry document? A version whose publish time cannot
        /// be read counts as old: nothing that waits on a young release may wait for ever.
        /// </summary>
        internal static bool PublishedWithin(string registryJson, string version, DateTime utcNow, TimeSpan window)
        {
            try
            {
                using var document = JsonDocument.Parse(registryJson);
                return document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("time", out var times)
                    && times.ValueKind == JsonValueKind.Object
                    && times.TryGetProperty(version, out var time)
                    && time.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(time.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var published)
                    && utcNow - published < window;
            }
            catch (JsonException)
            {
                return false;
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
            if (target == null || !TryParse(target, out var published))
                return null;
            // An installed version that cannot be read is never "older" than a release.
            var ordered = TryParse(installedVersion, out var installed);
            if (ordered && Compare(published, installed) > 0)
                return target;
            return anyDirection && (!ordered || Compare(published, installed) != 0) ? target : null;
        }
    }
}
