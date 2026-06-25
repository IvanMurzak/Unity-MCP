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

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Pure-managed (no UnityEngine / UnityEditor API) download-integrity logic for the shared
    /// <c>GameDev-MCP-Server</c> binary: the <c>SHA256SUMS</c> manifest URL builder, the coreutils-format
    /// parser, the exact-key RID digest lookup, the case-insensitive digest compare, and the single
    /// fail-closed <see cref="VerifyZipChecksum"/> verdict. The editor manager
    /// (<see cref="McpServerManager"/>.<c>DownloadAndUnpackBinary</c>) calls this BEFORE
    /// <c>ZipFile.ExtractToDirectory</c> / <c>Process.Start</c> — so a downloaded server zip is NEVER
    /// extracted or launched unless its SHA256 matches the release's published <c>SHA256SUMS</c> manifest
    /// (issue #841). A compromised release asset or a trusted-CA MITM would otherwise yield arbitrary code
    /// execution on the developer's machine.
    ///
    /// <para>
    /// Keeping this logic here — rather than inline in <see cref="McpServerManager"/> (which touches
    /// UnityEngine/UnityEditor APIs in nearly every method) — makes every decision unit-testable in an
    /// EditMode/NUnit test with no running editor and no real download: each method below is a deterministic
    /// string/enum/dictionary transform. The HTTP fetch + SHA256 compute + file IO that surround this verdict
    /// live in the editor-coupled manager. Mirrors Godot-MCP's <c>GodotMcpServerView</c> checksum seam
    /// (Godot leg = PR #193).
    /// </para>
    /// </summary>
    public static class McpServerChecksum
    {
        /// <summary>
        /// The name of the integrity manifest asset attached to every GameDev-MCP-Server release: a standard
        /// coreutils <c>sha256sum</c> output file listing one <c>&lt;hex&gt;␠␠&lt;filename&gt;</c> line per
        /// per-RID server zip. LIVE on the pinned <c>v8.0.0</c> release (and every future release).
        /// </summary>
        public const string Sha256SumsAssetName = "SHA256SUMS";

        /// <summary>
        /// The URL of a release's <c>SHA256SUMS</c> manifest — the SIBLING of the per-RID zip
        /// (<see cref="McpServerManager.ExecutableZipUrl"/>) under the SAME <c>v&lt;serverVersion&gt;</c>
        /// release tag:
        /// <c>https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v&lt;serverVersion&gt;/SHA256SUMS</c>.
        /// The downloaded zip's SHA256 is verified against this manifest BEFORE extraction/execution
        /// (fail-closed). Uses <see cref="McpServerManager.ServerReleaseTag"/> so the manifest tag can never
        /// drift from the zip tag. Pure string build — unit-testable with no editor.
        /// </summary>
        public static string Sha256SumsUrl(string serverVersion)
            => $"https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/{McpServerManager.ServerReleaseTag(serverVersion)}/{Sha256SumsAssetName}";

        /// <summary>
        /// The <c>SHA256SUMS</c> manifest URL for the PINNED <see cref="McpServerManager.ServerVersion"/> —
        /// the production path, taking NO version parameter so the manifest tag can never drift from the zip
        /// tag (which also pins <see cref="McpServerManager.ServerVersion"/>).
        /// </summary>
        public static string Sha256SumsUrl()
            => Sha256SumsUrl(McpServerManager.ServerVersion);

        /// <summary>
        /// Parse a coreutils <c>sha256sum</c> manifest into a <c>{filename → lowercase-hex-digest}</c> map.
        /// The exact LIVE format is one line per file: a 64-character lowercase hex digest, then TWO spaces
        /// (the coreutils text-mode separator), then the file name —
        /// <c>844d4ad8…53319␠␠gamedev-mcp-server-linux-x64.zip</c>. Tolerances applied (so a hand-edited or
        /// CRLF manifest still parses, while a malformed one yields no usable entry):
        /// <list type="bullet">
        /// <item>CRLF and bare-LF line endings; blank lines skipped.</item>
        /// <item>Leading/trailing whitespace on each line trimmed.</item>
        /// <item>The coreutils binary-mode <c>'*'</c> marker before the filename (<c>&lt;hex&gt; *&lt;name&gt;</c>)
        /// is stripped.</item>
        /// <item>A line whose first token is NOT a 64-char hex string, or which has no filename, is SKIPPED
        /// (it never produces a spurious entry — fail-closed at the lookup layer).</item>
        /// </list>
        /// Digests are normalized to lowercase; filenames are kept verbatim (case-sensitive, matching the
        /// asset names). On a duplicate filename the LAST entry wins. Never throws — a null/empty/garbage
        /// input yields an empty map. Pure managed; unit-testable with no editor.
        /// </summary>
        public static IReadOnlyDictionary<string, string> ParseSha256Sums(string? sha256SumsText)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(sha256SumsText))
                return map;

            foreach (var rawLine in sha256SumsText!.Replace("\r\n", "\n").Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                // Split into the digest token and the remainder (the filename). The coreutils separator is two
                // spaces, but we split on the FIRST run of whitespace so a single-space or tab variant still
                // parses — the digest token is fixed-width 64 hex, the filename is everything after.
                var sepIndex = -1;
                for (var i = 0; i < line.Length; i++)
                {
                    if (line[i] == ' ' || line[i] == '\t')
                    {
                        sepIndex = i;
                        break;
                    }
                }
                if (sepIndex <= 0 || sepIndex >= line.Length - 1)
                    continue;

                var digestToken = line.Substring(0, sepIndex);
                if (!IsHex64(digestToken))
                    continue;

                var fileName = line.Substring(sepIndex + 1).TrimStart(' ', '\t');
                // coreutils binary-mode marker: `<hex> *<name>`. Strip a single leading '*'.
                if (fileName.StartsWith("*", StringComparison.Ordinal))
                    fileName = fileName.Substring(1);
                fileName = fileName.Trim();
                if (fileName.Length == 0)
                    continue;

                map[fileName] = digestToken.ToLowerInvariant();
            }

            return map;
        }

        /// <summary>True when <paramref name="value"/> is exactly 64 ASCII hex characters (a SHA256 hex digest).</summary>
        static bool IsHex64(string value)
        {
            if (value.Length != 64)
                return false;
            foreach (var c in value)
            {
                var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Look up the expected SHA256 digest for <paramref name="assetZipName"/> (e.g.
        /// <c>gamedev-mcp-server-win-x64.zip</c>) in a parsed <see cref="ParseSha256Sums"/> map. The lookup is
        /// EXACT-key Ordinal — <c>linux-x64</c> never cross-matches <c>linux-arm64</c>, <c>win-x64</c> never
        /// cross-matches <c>win-x86</c> (no substring/prefix match). Returns the lowercase hex digest, or null
        /// when the manifest has no entry for that asset (the MISSING-entry fail-closed case). Pure managed.
        /// </summary>
        public static string? LookupDigest(
            IReadOnlyDictionary<string, string> parsedSha256Sums,
            string assetZipName)
        {
            if (parsedSha256Sums == null || string.IsNullOrEmpty(assetZipName))
                return null;
            return parsedSha256Sums.TryGetValue(assetZipName, out var digest) ? digest : null;
        }

        /// <summary>
        /// Case-insensitive hex-digest equality (both sides trimmed). A null/empty/whitespace digest on either
        /// side is NEVER a match (fail-closed: an unknown digest must not pass). Pure managed.
        /// </summary>
        public static bool DigestMatches(string? expectedHexDigest, string? actualHexDigest)
        {
            if (string.IsNullOrWhiteSpace(expectedHexDigest) || string.IsNullOrWhiteSpace(actualHexDigest))
                return false;
            return string.Equals(expectedHexDigest!.Trim(), actualHexDigest!.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The verdict of verifying a downloaded zip against a release <c>SHA256SUMS</c> manifest.
        /// </summary>
        public enum ChecksumVerdict
        {
            /// <summary>The manifest parsed, contained this asset's entry, and the digest matched. SAFE to extract/execute.</summary>
            Verified,

            /// <summary>The manifest text was missing/empty/unparsable (no usable entries). Fail-closed.</summary>
            ManifestUnparsable,

            /// <summary>The manifest parsed but had no line for this asset's zip name. Fail-closed.</summary>
            MissingEntry,

            /// <summary>The manifest's entry for this asset did NOT match the downloaded zip's digest. Fail-closed.</summary>
            DigestMismatch
        }

        /// <summary>
        /// The single fail-closed integrity decision the editor manager calls BEFORE
        /// <c>ZipFile.ExtractToDirectory</c> / <c>Process.Start</c>: parse the release's <c>SHA256SUMS</c>,
        /// find the entry for <paramref name="assetZipName"/>, and compare it (case-insensitive hex) against
        /// the locally-computed SHA256 of the downloaded zip (<paramref name="actualZipHexDigest"/>). Returns
        /// <see cref="ChecksumVerdict.Verified"/> ONLY when the manifest parsed, contained the asset, and the
        /// digest matched; every other outcome is a distinct fail-closed verdict the caller MUST treat as
        /// "do NOT extract, do NOT launch". Keeping this here (not inline in the editor manager) makes the
        /// entire decision unit-testable with no editor and no real download. Never throws.
        /// </summary>
        /// <param name="sha256SumsText">The raw downloaded <c>SHA256SUMS</c> manifest text.</param>
        /// <param name="assetZipName">This RID's zip name, e.g. <c>gamedev-mcp-server-win-x64.zip</c>.</param>
        /// <param name="actualZipHexDigest">The SHA256 of the downloaded zip, as lowercase/any-case hex.</param>
        public static ChecksumVerdict VerifyZipChecksum(string? sha256SumsText, string assetZipName, string? actualZipHexDigest)
        {
            var parsed = ParseSha256Sums(sha256SumsText);
            if (parsed.Count == 0)
                return ChecksumVerdict.ManifestUnparsable;

            var expected = LookupDigest(parsed, assetZipName);
            if (expected == null)
                return ChecksumVerdict.MissingEntry;

            return DigestMatches(expected, actualZipHexDigest)
                ? ChecksumVerdict.Verified
                : ChecksumVerdict.DigestMismatch;
        }

        /// <summary>
        /// A short, actionable human-readable reason for a non-<see cref="ChecksumVerdict.Verified"/> verdict,
        /// for the editor manager's fail-closed log line. Pure string transform.
        /// </summary>
        public static string ChecksumFailureReason(ChecksumVerdict verdict, string assetZipName) => verdict switch
        {
            ChecksumVerdict.ManifestUnparsable =>
                $"the downloaded {Sha256SumsAssetName} manifest was empty or unparsable",
            ChecksumVerdict.MissingEntry =>
                $"the {Sha256SumsAssetName} manifest has no entry for '{assetZipName}'",
            ChecksumVerdict.DigestMismatch =>
                $"the downloaded '{assetZipName}' SHA256 did not match the {Sha256SumsAssetName} manifest entry",
            _ => "the checksum was verified"
        };
    }
}
