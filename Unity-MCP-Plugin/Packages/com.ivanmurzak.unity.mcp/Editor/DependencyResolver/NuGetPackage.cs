/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Represents a NuGet package dependency with its ID and version.
    /// </summary>
    readonly struct NuGetPackage
    {
        /// <summary>NuGet package ID (e.g., "System.Text.Json").</summary>
        public readonly string Id;

        /// <summary>Package version (e.g., "10.0.3").</summary>
        public readonly string Version;
        public readonly NuGetVersionRange? VersionRange;

        /// <summary>
        /// If true, this DLL is included in game builds (all platforms).
        /// If false, the DLL is editor-only (excluded from builds via PluginImporter).
        /// </summary>
        public readonly bool IncludeInBuild;

        public NuGetPackage(string id, string version, bool includeInBuild = false)
        {
            Id = id;
            Version = version;
            IncludeInBuild = includeInBuild;
            VersionRange = null;
        }

        NuGetPackage(string id, NuGetVersionRange range)
        {
            Id = id;
            Version = range.CandidateVersion;
            IncludeInBuild = false;
            VersionRange = range;
        }

        internal static NuGetPackage Dependency(string id, string range)
            => new NuGetPackage(id, NuGetVersionRange.Parse(range));

        /// <summary>
        /// The .nupkg download URL from NuGet v3 flat container API.
        /// </summary>
        public string DownloadUrl
        {
            get
            {
                var lowerId = Id.ToLowerInvariant();
                var lowerVersion = Version.ToLowerInvariant();
                return $"{NuGetConfig.NuGetBaseUrl}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";
            }
        }

        /// <summary>
        /// Cached .nupkg filename.
        /// </summary>
        public string CacheFileName => $"{Id}.{Version}.nupkg";

        public override string ToString() => $"{Id} {Version}";
    }

    readonly struct NuGetVersionRange
    {
        public readonly string Text;
        readonly string? _minimum;
        readonly string? _maximum;
        readonly bool _includeMinimum;
        readonly bool _includeMaximum;

        public string CandidateVersion
        {
            get
            {
                var parts = Parts(_minimum ?? _maximum!);
                var version = $"{parts.core[0]}.{parts.core[1]}.{parts.core[2]}";
                if (parts.core[3] != 0) version += "." + parts.core[3];
                if (parts.pre.Length > 0) version += "-" + string.Join(".", parts.pre);
                return version;
            }
        }

        NuGetVersionRange(string text, string? minimum, string? maximum, bool includeMinimum, bool includeMaximum)
        {
            Text = text;
            _minimum = minimum;
            _maximum = maximum;
            _includeMinimum = includeMinimum;
            _includeMaximum = includeMaximum;
        }

        public static NuGetVersionRange Parse(string text)
        {
            var value = text.Trim();
            string? minimum;
            string? maximum = null;
            var includeMinimum = true;
            var includeMaximum = false;
            if (value.StartsWith("[") || value.StartsWith("("))
            {
                if (!value.EndsWith("]") && !value.EndsWith(")"))
                    throw new FormatException($"Invalid NuGet version range '{text}'.");
                var bounds = value.Substring(1, value.Length - 2).Split(',');
                includeMinimum = value[0] == '[';
                includeMaximum = value[value.Length - 1] == ']';
                if (bounds.Length == 1 && includeMinimum && includeMaximum)
                    minimum = maximum = bounds[0].Trim();
                else if (bounds.Length == 2)
                {
                    minimum = bounds[0].Trim();
                    maximum = bounds[1].Trim();
                    if (minimum.Length == 0) minimum = null;
                    if (maximum.Length == 0) maximum = null;
                }
                else
                    throw new FormatException($"Invalid NuGet version range '{text}'.");
            }
            else
                minimum = value;

            if (minimum == null && maximum == null)
                throw new FormatException($"NuGet version range '{text}' has no concrete bound; configure an explicit version.");
            if (minimum != null) CompareVersions(minimum, minimum);
            if (maximum != null) CompareVersions(maximum, maximum);
            if (minimum != null && maximum != null)
            {
                var order = CompareVersions(minimum, maximum);
                if (order > 0 || (order == 0 && (!includeMinimum || !includeMaximum)))
                    throw new FormatException($"NuGet version range '{text}' is empty.");
            }
            return new NuGetVersionRange(text, minimum, maximum, includeMinimum, includeMaximum);
        }

        public bool Contains(string version)
        {
            var lower = _minimum == null ? 1 : CompareVersions(version, _minimum);
            var upper = _maximum == null ? -1 : CompareVersions(version, _maximum);
            return (lower > 0 || (lower == 0 && _includeMinimum))
                && (upper < 0 || (upper == 0 && _includeMaximum));
        }

        public static int CompareVersions(string a, string b)
        {
            var left = Parts(a);
            var right = Parts(b);
            for (var i = 0; i < 4; i++)
                if (left.core[i] != right.core[i])
                    return left.core[i].CompareTo(right.core[i]);
            if (left.pre.Length == 0 || right.pre.Length == 0)
                return left.pre.Length == 0 ? (right.pre.Length == 0 ? 0 : 1) : -1;
            for (var i = 0; i < Math.Min(left.pre.Length, right.pre.Length); i++)
            {
                var l = left.pre[i];
                var r = right.pre[i];
                var ln = l.All(char.IsDigit);
                var rn = r.All(char.IsDigit);
                int order;
                if (ln && rn)
                {
                    l = l.TrimStart('0');
                    r = r.TrimStart('0');
                    order = l.Length != r.Length ? l.Length.CompareTo(r.Length) : string.CompareOrdinal(l, r);
                }
                else
                    order = ln != rn ? (ln ? -1 : 1) : string.Compare(l, r, StringComparison.OrdinalIgnoreCase);
                if (order != 0)
                    return Math.Sign(order);
            }
            return left.pre.Length.CompareTo(right.pre.Length);
        }

        static (int[] core, string[] pre) Parts(string version)
        {
            var value = version.Split('+')[0];
            var dash = value.IndexOf('-');
            var core = (dash < 0 ? value : value.Substring(0, dash)).Split('.');
            var pre = dash < 0 ? Array.Empty<string>() : value.Substring(dash + 1).Split('.');
            var numbers = new int[4];
            if (core.Length > 4 || pre.Any(p => p.Length == 0 || p.Any(c => !char.IsLetterOrDigit(c) && c != '-')))
                throw new FormatException($"Invalid NuGet version '{version}'.");
            for (var i = 0; i < core.Length; i++)
                if (!int.TryParse(core[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                    throw new FormatException($"Invalid NuGet version '{version}'.");
            return (numbers, pre);
        }
    }

    static class NuGetPackagePlanner
    {
        internal static IReadOnlyList<string> Resolve(IEnumerable<NuGetPackage> roots,
            Func<NuGetPackage, List<NuGetPackage>> dependencies, Func<string, bool> skip,
            out Dictionary<string, string> selection)
        {
            var rootList = roots.Where(r => !skip(r.Id)).ToList();
            var selected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            selection = selected;
            for (var round = 0; round < 32; round++)
            {
                var requested = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var constraints = new Dictionary<string, List<(NuGetVersionRange range, string source)>>(StringComparer.OrdinalIgnoreCase);
                var candidates = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var order = new List<string>();
                void Request(NuGetPackage package, string source)
                {
                    if (skip(package.Id)) return;
                    if (!requested.ContainsKey(package.Id))
                    {
                        order.Add(package.Id);
                        constraints[package.Id] = new List<(NuGetVersionRange, string)>();
                        candidates[package.Id] = new List<string>();
                    }
                    candidates[package.Id].Add(package.Version);
                    if (package.VersionRange is NuGetVersionRange range)
                        constraints[package.Id].Add((range, source));
                    else
                        constraints[package.Id].Add((NuGetVersionRange.Parse(package.Version), source));
                    var ordered = candidates[package.Id].OrderByDescending(v => v,
                        Comparer<string>.Create(NuGetVersionRange.CompareVersions)).ToList();
                    requested[package.Id] = ordered.FirstOrDefault(v => constraints[package.Id].All(c => c.range.Contains(v)))
                        ?? ordered[0];
                }
                foreach (var root in rootList) Request(root, "configured packages");
                for (var i = 0; i < order.Count; i++)
                {
                    var id = order[i];
                    var version = selected.TryGetValue(id, out var previous) ? previous : requested[id];
                    if (constraints[id].Any(c => !c.range.Contains(version)))
                        continue;
                    foreach (var dependency in dependencies(new NuGetPackage(id, version)))
                        Request(dependency, $"{id} {version}");
                }
                var stable = requested.Count == selected.Count
                    && requested.All(r => selected.TryGetValue(r.Key, out var v) && string.Equals(v, r.Value, StringComparison.OrdinalIgnoreCase));
                selected = requested;
                selection = selected;
                if (!stable) continue;
                return constraints.SelectMany(pair => pair.Value
                    .Where(c => !c.range.Contains(requested[pair.Key]))
                    .Select(c => $"{pair.Key} {requested[pair.Key]} does not satisfy {c.range.Text} required by {c.source}. " +
                        "Configure a version satisfying every dependency range; automatic feed version discovery is not supported."))
                    .ToList();
            }
            return new[] { "NuGet dependency selection did not converge after 32 rounds; no packages were installed." };
        }
    }
}
