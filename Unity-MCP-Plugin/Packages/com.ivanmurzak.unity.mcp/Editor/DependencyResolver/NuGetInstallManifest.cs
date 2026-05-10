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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// On-disk manifest for the flat-layout NuGet install path. Replaces the
    /// directory-name-as-source-of-truth model that the per-package
    /// <c>{Id}.{Version}/</c> layout used pre-#733.
    ///
    /// File location: <c>{installPath}/.nuget-installed.json</c>.
    /// Schema:
    /// <code>
    /// {
    ///   "packages": {
    ///     "Microsoft.AspNetCore.Http.Connections.Client": {
    ///       "version": "8.0.15",
    ///       "dlls": ["Microsoft.AspNetCore.Http.Connections.Client.8.0.15.dll"]
    ///     },
    ///     "Microsoft.Bcl.Memory": {
    ///       "version": "10.0.3",
    ///       "dlls": [
    ///         "System.Memory.10.0.3.dll",
    ///         "System.Buffers.10.0.3.dll",
    ///         "System.Runtime.CompilerServices.Unsafe.10.0.3.dll"
    ///       ]
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// The manifest is the primary source of truth for "which DLL belongs to
    /// which package at which version". Filename parsing
    /// (<see cref="TryParseInstalledDllName"/>) is the disaster-recovery
    /// fallback — even if the manifest is deleted, the on-disk filenames
    /// carry enough metadata for <see cref="TryRebuildFromDisk"/> to
    /// reconstruct one.
    /// </summary>
    static class NuGetInstallManifest
    {
        const string Tag = NuGetConfig.LogTag;
        public const string FileName = ".nuget-installed.json";

        /// <summary>
        /// Returns the absolute path to the manifest file under the supplied
        /// install path.
        /// </summary>
        public static string GetPath(string installPath) =>
            Path.Combine(installPath, FileName);

        /// <summary>
        /// Loads the manifest from disk. Returns an empty manifest when the file
        /// is missing or unparseable; the caller is responsible for treating
        /// that as "needs full restore".
        /// </summary>
        public static InstallManifest Load(string installPath)
        {
            var path = GetPath(installPath);
            if (!File.Exists(path))
                return new InstallManifest();

            try
            {
                var json = File.ReadAllText(path);
                return ManifestJsonParser.Parse(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Manifest at '{path}' could not be parsed ({ex.Message}); treating as empty and rebuilding.");
                return new InstallManifest();
            }
        }

        /// <summary>
        /// Writes the manifest to disk atomically. Creates the install directory
        /// if it does not exist. Writes to a sibling <c>.tmp</c> file first and
        /// then moves it into place, so a crash or power loss mid-write cannot
        /// leave a truncated/empty <c>.nuget-installed.json</c> behind.
        /// </summary>
        public static void Save(string installPath, InstallManifest manifest)
        {
            if (!Directory.Exists(installPath))
                Directory.CreateDirectory(installPath);

            var path = GetPath(installPath);
            var tempPath = path + ".tmp";
            var json = ManifestJsonParser.Serialize(manifest);

            File.WriteAllText(tempPath, json);
            try
            {
                // Atomic rename: delete the destination first so File.Move (the
                // .NET Standard 2.0-compatible overload that Unity's bundled
                // Mono ships) does not throw IOException("destination exists").
                // The delete-then-move sequence is not strictly atomic on
                // Windows, but it removes the truncated-file failure mode the
                // bug report calls out (a crash mid-WriteAllText would leave an
                // empty/partial .nuget-installed.json; the staging-file
                // approach guarantees the destination is either the previous
                // valid file or the freshly serialized one — never partial).
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tempPath, path);
            }
            catch
            {
                // Best-effort cleanup of the staging file if the rename fails.
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch { /* ignore cleanup error — surface the original failure */ }
                throw;
            }
        }

        /// <summary>
        /// Rebuilds an in-memory manifest by scanning the install directory
        /// for the legacy versioned DLL filename shape
        /// <c>{stem}.{version}.dll</c>. Unversioned canonical filenames are
        /// skipped (we can't recover their version from disk; if the manifest
        /// is missing, the next <c>Restore()</c> re-extracts every package
        /// from .nupkg cache and rebuilds the manifest authoritatively).
        ///
        /// Used when <c>.nuget-installed.json</c> is missing, corrupt, or
        /// out-of-sync with disk (the disaster-recovery path). The caller is
        /// responsible for persisting the rebuilt manifest via <see cref="Save"/>.
        ///
        /// <para>
        /// Filename-only rebuild cannot recover the package ID for multi-DLL
        /// packages where each DLL stem differs from the package ID (e.g.
        /// <c>Microsoft.Bcl.Memory</c> ships <c>System.Memory.10.0.3.dll</c>).
        /// For such DLLs the rebuild treats the DLL stem itself as the package
        /// ID; the next full restore reconciles the synthetic IDs against the
        /// dependency closure (the closure is the authoritative source). This
        /// is enough to satisfy the steady-state "manifest deleted, no
        /// re-extraction needed" disaster-recovery acceptance criterion for
        /// the pre-unversioned-filename layouts that still ship versioned
        /// filenames on disk.
        /// </para>
        /// </summary>
        public static InstallManifest TryRebuildFromDisk(string installPath)
        {
            var manifest = new InstallManifest();
            if (!Directory.Exists(installPath))
                return manifest;

            foreach (var dllPath in Directory.GetFiles(installPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(dllPath);
                if (!TryParseInstalledDllName(fileName, out var stem, out var version) || stem == null || version == null)
                    continue;

                if (!manifest.Packages.TryGetValue(stem, out var entry))
                {
                    entry = new InstalledPackage(version);
                    manifest.Packages[stem] = entry;
                }
                else if (!string.Equals(entry.Version, version, StringComparison.OrdinalIgnoreCase))
                {
                    // Multiple versions of the same DLL stem on disk shouldn't
                    // happen in a healthy install. If it does, latest-wins for
                    // the rebuild and the next Restore() pass will reconcile
                    // and clean up the loser.
                    entry = new InstalledPackage(version);
                    manifest.Packages[stem] = entry;
                }
                if (!entry.Dlls.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    entry.Dlls.Add(fileName);
            }

            return manifest;
        }

        /// <summary>
        /// Splits a versioned install filename of the form
        /// <c>{stem}.{version}.dll</c> back into its stem and version parts.
        /// Returns false for canonical unversioned names (e.g. <c>System.Memory.dll</c>)
        /// or names whose version-tail does not parse as a System.Version —
        /// useful as a "is this a leftover from the pre-unversioned-filename
        /// layout?" predicate.
        ///
        /// The version segment is matched as <c>\d+(\.\d+){1,3}</c> at the
        /// tail before <c>.dll</c>. The match is greedy on the tail so that
        /// <c>System.Memory.10.0.3.dll</c> splits as stem=<c>System.Memory</c>
        /// + version=<c>10.0.3</c>, not stem=<c>System.Memory.10.0</c> +
        /// version=<c>3</c>.
        /// </summary>
        public static bool TryParseInstalledDllName(string fileName, out string? stem, out string? version)
        {
            stem = null;
            version = null;

            if (string.IsNullOrEmpty(fileName))
                return false;
            if (!fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return false;

            var noExt = fileName.Substring(0, fileName.Length - 4);
            var match = VersionTailPattern.Match(noExt);
            if (!match.Success)
                return false;

            var versionString = match.Groups[1].Value;
            if (!System.Version.TryParse(versionString, out _))
                return false;

            var stemEnd = match.Index;
            if (stemEnd <= 0)
                return false;

            stem = noExt.Substring(0, stemEnd);
            version = versionString;
            return true;
        }

        // Anchor a 1-4 segment numeric tail (each segment 1-9 digits — System.Version's
        // ceiling) to the end of the stem. Used to detect / peel the version off
        // filenames left over from the pre-unversioned-filename layout.
        static readonly Regex VersionTailPattern = new(@"\.(\d+(?:\.\d+){1,3})$", RegexOptions.Compiled);
    }

    /// <summary>
    /// In-memory representation of <see cref="NuGetInstallManifest"/>'s on-disk schema.
    /// </summary>
    sealed class InstallManifest
    {
        /// <summary>
        /// Map of NuGet package ID → installed entry. Comparison is
        /// case-insensitive to match the rest of the resolver.
        /// </summary>
        public Dictionary<string, InstalledPackage> Packages { get; }
            = new(StringComparer.OrdinalIgnoreCase);

        public InstallManifest Clone()
        {
            var copy = new InstallManifest();
            foreach (var kv in Packages)
                copy.Packages[kv.Key] = new InstalledPackage(kv.Value.Version, kv.Value.Dlls);
            return copy;
        }
    }

    /// <summary>
    /// One entry in <see cref="InstallManifest.Packages"/>.
    /// </summary>
    sealed class InstalledPackage
    {
        /// <summary>The package version this entry was installed at.</summary>
        public string Version { get; set; }

        /// <summary>
        /// DLL filenames (relative to the install path) the package owns at this
        /// version. Each filename is of the form <c>{stem}.{version}.dll</c>.
        /// </summary>
        public List<string> Dlls { get; }

        public InstalledPackage(string version)
        {
            Version = version;
            Dlls = new List<string>();
        }

        public InstalledPackage(string version, IEnumerable<string> dlls)
        {
            Version = version;
            Dlls = new List<string>(dlls);
        }
    }

    /// <summary>
    /// Hand-rolled JSON read/write for <see cref="InstallManifest"/>. Avoids a
    /// dependency on <c>System.Text.Json</c> / <c>Newtonsoft.Json</c> so the
    /// DependencyResolver assembly stays the only one that compiles before any
    /// NuGet package is on disk (the same invariant <c>NuGetDependencyResolver</c>
    /// is built against).
    /// </summary>
    static class ManifestJsonParser
    {
        public static string Serialize(InstallManifest manifest)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"packages\": {");

            // Stable ordering — keeps the file diff-friendly across runs.
            var ordered = manifest.Packages
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                var (id, entry) = (ordered[i].Key, ordered[i].Value);
                sb.Append("    \"").Append(JsonEscape(id)).Append("\": {");
                sb.AppendLine();
                sb.Append("      \"version\": \"").Append(JsonEscape(entry.Version)).Append("\",");
                sb.AppendLine();
                sb.Append("      \"dlls\": [");
                if (entry.Dlls.Count == 0)
                {
                    sb.Append("]");
                }
                else
                {
                    sb.AppendLine();
                    for (var j = 0; j < entry.Dlls.Count; j++)
                    {
                        sb.Append("        \"").Append(JsonEscape(entry.Dlls[j])).Append("\"");
                        if (j < entry.Dlls.Count - 1)
                            sb.Append(",");
                        sb.AppendLine();
                    }
                    sb.Append("      ]");
                }
                sb.AppendLine();
                sb.Append("    }");
                if (i < ordered.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  }");
            sb.Append("}");
            return sb.ToString();
        }

        public static InstallManifest Parse(string json)
        {
            var manifest = new InstallManifest();
            var parser = new MicroJsonParser(json);
            var root = parser.ParseValue() as Dictionary<string, object?>;
            if (root == null)
                return manifest;

            if (!root.TryGetValue("packages", out var packagesObj) || packagesObj is not Dictionary<string, object?> packages)
                return manifest;

            foreach (var (id, valueObj) in packages)
            {
                if (valueObj is not Dictionary<string, object?> entryObj)
                    continue;

                var version = entryObj.TryGetValue("version", out var versionObj) ? versionObj as string : null;
                if (string.IsNullOrEmpty(version))
                    continue;

                var entry = new InstalledPackage(version!);
                if (entryObj.TryGetValue("dlls", out var dllsObj) && dllsObj is List<object?> dlls)
                {
                    foreach (var dllObj in dlls)
                    {
                        if (dllObj is string dll)
                            entry.Dlls.Add(dll);
                    }
                }

                manifest.Packages[id] = entry;
            }

            return manifest;
        }

        static string JsonEscape(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Minimal JSON parser. Handles objects, arrays, strings, numbers,
        /// booleans, and null. Sufficient for the manifest schema; not a
        /// general-purpose parser.
        /// </summary>
        sealed class MicroJsonParser
        {
            readonly string _src;
            int _pos;

            public MicroJsonParser(string src) { _src = src; _pos = 0; }

            public object? ParseValue()
            {
                SkipWhitespace();
                if (_pos >= _src.Length)
                    return null;
                var c = _src[_pos];
                return c switch
                {
                    '{' => ParseObject(),
                    '[' => ParseArray(),
                    '"' => ParseString(),
                    't' or 'f' => ParseBool(),
                    'n' => ParseNull(),
                    _ => ParseNumber(),
                };
            }

            Dictionary<string, object?> ParseObject()
            {
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                Expect('{');
                SkipWhitespace();
                if (Peek() == '}') { _pos++; return result; }

                while (true)
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    var value = ParseValue();
                    result[key] = value;
                    SkipWhitespace();
                    if (Peek() == ',') { _pos++; continue; }
                    if (Peek() == '}') { _pos++; return result; }
                    throw new FormatException($"Expected ',' or '}}' at {_pos}");
                }
            }

            List<object?> ParseArray()
            {
                var result = new List<object?>();
                Expect('[');
                SkipWhitespace();
                if (Peek() == ']') { _pos++; return result; }

                while (true)
                {
                    var value = ParseValue();
                    result.Add(value);
                    SkipWhitespace();
                    if (Peek() == ',') { _pos++; continue; }
                    if (Peek() == ']') { _pos++; return result; }
                    throw new FormatException($"Expected ',' or ']' at {_pos}");
                }
            }

            string ParseString()
            {
                Expect('"');
                var sb = new StringBuilder();
                while (_pos < _src.Length)
                {
                    var c = _src[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (_pos >= _src.Length) break;
                        var esc = _src[_pos++];
                        switch (esc)
                        {
                            case '\\': sb.Append('\\'); break;
                            case '"': sb.Append('"'); break;
                            case '/': sb.Append('/'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'u':
                                if (_pos + 4 > _src.Length)
                                    throw new FormatException("Truncated \\u escape");
                                sb.Append((char)Convert.ToInt32(_src.Substring(_pos, 4), 16));
                                _pos += 4;
                                break;
                            default: sb.Append(esc); break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                throw new FormatException("Unterminated string");
            }

            bool ParseBool()
            {
                if (_src.Length - _pos >= 4 && _src.Substring(_pos, 4) == "true") { _pos += 4; return true; }
                if (_src.Length - _pos >= 5 && _src.Substring(_pos, 5) == "false") { _pos += 5; return false; }
                throw new FormatException($"Expected bool at {_pos}");
            }

            object? ParseNull()
            {
                if (_src.Length - _pos >= 4 && _src.Substring(_pos, 4) == "null") { _pos += 4; return null; }
                throw new FormatException($"Expected null at {_pos}");
            }

            double ParseNumber()
            {
                var start = _pos;
                if (Peek() == '-') _pos++;
                while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.' || _src[_pos] == 'e' || _src[_pos] == 'E' || _src[_pos] == '+' || _src[_pos] == '-'))
                    _pos++;
                return double.Parse(_src.Substring(start, _pos - start), System.Globalization.CultureInfo.InvariantCulture);
            }

            void Expect(char expected)
            {
                if (_pos >= _src.Length || _src[_pos] != expected)
                    throw new FormatException($"Expected '{expected}' at {_pos}");
                _pos++;
            }

            char Peek() => _pos < _src.Length ? _src[_pos] : '\0';

            void SkipWhitespace()
            {
                while (_pos < _src.Length && char.IsWhiteSpace(_src[_pos]))
                    _pos++;
            }
        }
    }
}
