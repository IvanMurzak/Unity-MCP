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
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Pins the pure-managed, fail-closed download-integrity logic (<see cref="McpServerChecksum"/>'s
    /// SHA256SUMS URL builder, coreutils-format parser, exact-key RID digest lookup/compare, and the single
    /// <see cref="McpServerChecksum.VerifyZipChecksum"/> verdict) that the editor manager
    /// (<see cref="McpServerManager"/>.<c>DownloadAndUnpackBinary</c>) calls BEFORE
    /// <c>ZipFile.ExtractToDirectory</c> / <c>Process.Start</c> — so a downloaded server zip is NEVER extracted
    /// or launched unless its SHA256 matches the release's published <c>SHA256SUMS</c> manifest (issue #841).
    /// Every assertion is a deterministic string/enum/dictionary transform with NO running editor and NO real
    /// download. The HTTP fetch + SHA256 compute + file IO that surround this verdict live in the editor-only
    /// manager. Mirrors Godot-MCP's GodotMcpServerChecksumTests (Godot leg = PR #193).
    ///
    /// <para>
    /// The <see cref="LiveV8Sha256Sums"/> fixture is the VERBATIM content of the real <c>v8.0.0</c> release
    /// manifest (the version this plugin pins) at
    /// https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v8.0.0/SHA256SUMS — so the parser is
    /// proven against the exact two-space coreutils format that production will see, and the win-x64 digest is
    /// the real one (operator-confirmable: <c>gh release download v8.0.0 --pattern gamedev-mcp-server-win-x64.zip</c>
    /// then <c>sha256sum</c> equals <see cref="LiveWinX64Digest"/>). Updating the pinned
    /// <see cref="McpServerManager.ServerVersion"/> in future does not invalidate these tests: they assert the
    /// FORMAT contract, not a moving digest.
    /// </para>
    /// </summary>
    public class McpServerChecksumTests
    {
        /// <summary>
        /// The verbatim live <c>SHA256SUMS</c> from the GameDev-MCP-Server <c>v8.0.0</c> release — standard
        /// coreutils output: <c>&lt;64-lowercase-hex&gt;␠␠&lt;filename&gt;</c>, one line per RID zip, LF-terminated.
        /// All 7 published RIDs are present.
        /// </summary>
        const string LiveV8Sha256Sums =
            "5f17508e92812fbf9522eb552641d21dc2383fc2f6cf371f5413ad06c9820282  gamedev-mcp-server-linux-arm64.zip\n" +
            "844d4ad8cd152df44287341235ca2ae67cdb69b496252678eb6491f0bdc53319  gamedev-mcp-server-linux-x64.zip\n" +
            "ad0f50042dfa1edde26a9f26968538146ba792cc0188a47f6bfc1ae573bb513e  gamedev-mcp-server-osx-arm64.zip\n" +
            "d25993216e610401c8925716d9ad0f8ecaf3dc93443b12cfd057a75495ef9952  gamedev-mcp-server-osx-x64.zip\n" +
            "702f1d708c25dde6a58d3335c7adb92aa5fe36be618003821ceb040a9b59c51b  gamedev-mcp-server-win-arm64.zip\n" +
            "7383638dbc1cad84cf3b85617405c29c7885a51e34b1ef7b8b8864d0656814cb  gamedev-mcp-server-win-x64.zip\n" +
            "b171e1d8318d0ce4e88d30a5e86ad1cac1acea946ef1a71cd410a27f917c9799  gamedev-mcp-server-win-x86.zip\n";

        /// <summary>The real win-x64 digest from the live v8.0.0 manifest (verbatim).</summary>
        const string LiveWinX64Digest = "7383638dbc1cad84cf3b85617405c29c7885a51e34b1ef7b8b8864d0656814cb";

        const string WinX64Asset = "gamedev-mcp-server-win-x64.zip";

        // --- SHA256SUMS URL builder (sibling of the zip URL, same v-tag) ---

        [Test]
        public void Sha256SumsUrl_IsSiblingOfZipUrlUnderSameVTag()
        {
            // The manifest MUST live under the same v<version> release tag as the zip, with the fixed
            // `SHA256SUMS` asset name — so the integrity manifest can never drift from the binary it covers.
            var sumsUrl = McpServerChecksum.Sha256SumsUrl("8.0.0");
            Assert.AreEqual(
                "https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v8.0.0/SHA256SUMS",
                sumsUrl);
        }

        [Test]
        public void Sha256SumsUrl_DefaultOverload_PinsServerVersion()
        {
            var sumsUrl = McpServerChecksum.Sha256SumsUrl();
            Assert.AreEqual(McpServerChecksum.Sha256SumsUrl(McpServerManager.ServerVersion), sumsUrl);
            StringAssert.Contains($"/releases/download/v{McpServerManager.ServerVersion}/SHA256SUMS", sumsUrl);
        }

        [Test]
        public void Sha256SumsUrl_PrePrefixedVersion_NotDoublePrefixed()
        {
            Assert.AreEqual(
                "https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v8.0.0/SHA256SUMS",
                McpServerChecksum.Sha256SumsUrl("v8.0.0"));
        }

        [Test]
        public void Sha256SumsUrl_SharesReleaseDirectoryWithZipUrl()
        {
            // The manifest URL shares the release-download directory prefix with the per-RID zip URL — so a
            // verified manifest can never be fetched from a different release than the downloaded zip.
            var sumsUrl = McpServerChecksum.Sha256SumsUrl(McpServerManager.ServerVersion);
            var zipUrl = McpServerManager.ExecutableZipUrl;
            var zipDir = zipUrl.Substring(0, zipUrl.LastIndexOf('/') + 1);
            StringAssert.StartsWith(zipDir, sumsUrl);
        }

        // --- Parser: the exact two-space coreutils line format ---

        [Test]
        public void ParseSha256Sums_LiveManifest_ParsesAllSevenEntriesWithLowercaseDigests()
        {
            var map = McpServerChecksum.ParseSha256Sums(LiveV8Sha256Sums);

            Assert.AreEqual(7, map.Count);
            // The RIGHT RID is selected among the 7 — win-x64 maps to ITS digest, not a neighbour's.
            Assert.AreEqual(LiveWinX64Digest, map[WinX64Asset]);
            Assert.AreEqual("844d4ad8cd152df44287341235ca2ae67cdb69b496252678eb6491f0bdc53319",
                map["gamedev-mcp-server-linux-x64.zip"]);
            Assert.AreEqual("b171e1d8318d0ce4e88d30a5e86ad1cac1acea946ef1a71cd410a27f917c9799",
                map["gamedev-mcp-server-win-x86.zip"]);
            // Every value is a 64-char lowercase hex digest.
            foreach (var digest in map.Values)
            {
                Assert.AreEqual(64, digest.Length);
                Assert.AreEqual(digest.ToLowerInvariant(), digest);
            }
        }

        [Test]
        public void ParseSha256Sums_TolerantOfCrlfAndBlankLinesAndUppercaseHex()
        {
            const string text =
                "\r\n" +
                "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789  gamedev-mcp-server-win-x64.zip\r\n" +
                "\r\n";
            var map = McpServerChecksum.ParseSha256Sums(text);
            Assert.AreEqual(1, map.Count);
            // Digest normalized to lowercase.
            Assert.AreEqual("abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
                map[WinX64Asset]);
        }

        [Test]
        public void ParseSha256Sums_StripsBinaryModeStarMarker()
        {
            // coreutils binary mode emits `<hex> *<name>`; the '*' is not part of the filename.
            const string text =
                "7383638dbc1cad84cf3b85617405c29c7885a51e34b1ef7b8b8864d0656814cb *gamedev-mcp-server-win-x64.zip\n";
            var map = McpServerChecksum.ParseSha256Sums(text);
            Assert.IsTrue(map.ContainsKey(WinX64Asset));
            Assert.AreEqual(LiveWinX64Digest, map[WinX64Asset]);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   \n  \n")]                                              // only whitespace
        [TestCase("not-a-digest  gamedev-mcp-server-win-x64.zip\n")]        // first token isn't 64-hex
        [TestCase("7383638dbc1cad84cf3b85617405c29c7885a51e34b1ef7b8b8864d065681\n")] // hex too short, no filename
        [TestCase("zzz3638dbc1cad84cf3b85617405c29c7885a51e34b1ef7b8b8864d0656814cb  x.zip\n")] // non-hex chars
        public void ParseSha256Sums_MalformedOrEmpty_YieldsNoUsableEntry(string? text)
        {
            var map = McpServerChecksum.ParseSha256Sums(text);
            Assert.AreEqual(0, map.Count);
        }

        // --- Lookup + compare (exact-key RID, no cross-match) ---

        [Test]
        public void LookupDigest_ReturnsEntryForRid_NullWhenMissing()
        {
            var map = McpServerChecksum.ParseSha256Sums(LiveV8Sha256Sums);
            Assert.AreEqual(LiveWinX64Digest, McpServerChecksum.LookupDigest(map, WinX64Asset));
            Assert.IsNull(McpServerChecksum.LookupDigest(map, "gamedev-mcp-server-freebsd-x64.zip"));
        }

        [Test]
        public void LookupDigest_ExactKey_DoesNotCrossMatchSiblingRids()
        {
            // linux-x64 must NOT cross-match linux-arm64; win-x64 must NOT cross-match win-x86. Each RID's
            // digest is distinct, so a substring/prefix match would surface the wrong digest — the exact-key
            // Ordinal lookup is what keeps a win-x86 zip from being "verified" against the win-x64 digest.
            var map = McpServerChecksum.ParseSha256Sums(LiveV8Sha256Sums);

            Assert.AreEqual("844d4ad8cd152df44287341235ca2ae67cdb69b496252678eb6491f0bdc53319",
                McpServerChecksum.LookupDigest(map, "gamedev-mcp-server-linux-x64.zip"));
            Assert.AreEqual("5f17508e92812fbf9522eb552641d21dc2383fc2f6cf371f5413ad06c9820282",
                McpServerChecksum.LookupDigest(map, "gamedev-mcp-server-linux-arm64.zip"));
            Assert.AreNotEqual(
                McpServerChecksum.LookupDigest(map, "gamedev-mcp-server-win-x64.zip"),
                McpServerChecksum.LookupDigest(map, "gamedev-mcp-server-win-x86.zip"));
        }

        [Test]
        public void DigestMatches_IsCaseInsensitive_AndFailsClosedOnNullOrEmpty()
        {
            Assert.IsTrue(McpServerChecksum.DigestMatches(LiveWinX64Digest, LiveWinX64Digest.ToUpperInvariant()));
            Assert.IsTrue(McpServerChecksum.DigestMatches("  " + LiveWinX64Digest + "  ", LiveWinX64Digest));
            Assert.IsFalse(McpServerChecksum.DigestMatches(LiveWinX64Digest, null));
            Assert.IsFalse(McpServerChecksum.DigestMatches(null, LiveWinX64Digest));
            Assert.IsFalse(McpServerChecksum.DigestMatches("", ""));
            Assert.IsFalse(McpServerChecksum.DigestMatches(LiveWinX64Digest, "deadbeef"));
        }

        // --- The single fail-closed verdict (what the editor manager calls) ---

        [Test]
        public void VerifyZipChecksum_RealPair_IsVerified()
        {
            // The real win-x64 digest against the real live manifest → SAFE to extract/execute. This is the
            // exact pair production sees.
            var verdict = McpServerChecksum.VerifyZipChecksum(LiveV8Sha256Sums, WinX64Asset, LiveWinX64Digest);
            Assert.AreEqual(McpServerChecksum.ChecksumVerdict.Verified, verdict);
        }

        [Test]
        public void VerifyZipChecksum_RealPair_VerifiedWithUppercaseComputedDigest()
        {
            // The editor manager computes the digest via BitConverter.ToString(...).Replace("-", "") which
            // emits UPPER-case hex; the verdict must accept it (case-insensitive compare).
            var verdict = McpServerChecksum.VerifyZipChecksum(
                LiveV8Sha256Sums, WinX64Asset, LiveWinX64Digest.ToUpperInvariant());
            Assert.AreEqual(McpServerChecksum.ChecksumVerdict.Verified, verdict);
        }

        [Test]
        public void VerifyZipChecksum_TamperedDigest_IsRejected()
        {
            // A tampered/compromised zip whose SHA256 differs by a single nibble must be REJECTED (fail-closed).
            var tampered = "0" + LiveWinX64Digest.Substring(1);
            var verdict = McpServerChecksum.VerifyZipChecksum(LiveV8Sha256Sums, WinX64Asset, tampered);
            Assert.AreEqual(McpServerChecksum.ChecksumVerdict.DigestMismatch, verdict);
        }

        [Test]
        public void VerifyZipChecksum_MissingEntryForRid_IsRejected()
        {
            // The manifest parsed fine but has no line for THIS RID's asset → fail-closed (MissingEntry).
            var verdict = McpServerChecksum.VerifyZipChecksum(
                LiveV8Sha256Sums, "gamedev-mcp-server-freebsd-x64.zip", LiveWinX64Digest);
            Assert.AreEqual(McpServerChecksum.ChecksumVerdict.MissingEntry, verdict);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage with no valid sha lines\n")]
        public void VerifyZipChecksum_UnparsableManifest_IsRejected(string? manifest)
        {
            // An empty/unparsable manifest must NEVER pass (do not execute an unverified binary).
            var verdict = McpServerChecksum.VerifyZipChecksum(manifest, WinX64Asset, LiveWinX64Digest);
            Assert.AreEqual(McpServerChecksum.ChecksumVerdict.ManifestUnparsable, verdict);
        }

        [Test]
        public void ChecksumFailureReason_NamesTheActionableCause()
        {
            // The editor fail-closed log line must be actionable: it names the manifest, the asset, and the cause.
            StringAssert.Contains(WinX64Asset,
                McpServerChecksum.ChecksumFailureReason(McpServerChecksum.ChecksumVerdict.DigestMismatch, WinX64Asset));
            StringAssert.Contains(WinX64Asset,
                McpServerChecksum.ChecksumFailureReason(McpServerChecksum.ChecksumVerdict.MissingEntry, WinX64Asset));
            StringAssert.Contains("SHA256SUMS",
                McpServerChecksum.ChecksumFailureReason(McpServerChecksum.ChecksumVerdict.ManifestUnparsable, WinX64Asset));
        }

        // --- The asset-name builder the manager passes to the verifier ---

        [Test]
        public void ExecutableZipName_IsTrailingSegmentOfZipUrl()
        {
            // The verified asset name MUST be the exact key looked up in SHA256SUMS, and the same trailing
            // segment as the downloaded zip URL — so the verified name can never drift from the downloaded name.
            var zipUrl = McpServerManager.ExecutableZipUrl;
            var trailing = zipUrl.Substring(zipUrl.LastIndexOf('/') + 1);
            Assert.AreEqual(trailing, McpServerManager.ExecutableZipName);
            StringAssert.StartsWith("gamedev-mcp-server-", McpServerManager.ExecutableZipName);
            StringAssert.EndsWith(".zip", McpServerManager.ExecutableZipName);
        }
    }
}
