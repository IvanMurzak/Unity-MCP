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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using Xunit;

namespace com.IvanMurzak.Unity.MCP.EngineFree.Tests
{
    /// <summary>
    /// Walks up from the test assembly's output directory to find a repo-relative file, so a
    /// source-text pin does not depend on the test runner's working directory. The output lives at
    /// <c>Unity-MCP-Plugin/Tests~/EngineFree/bin/&lt;config&gt;/net8.0/</c>, five levels below the repo root.
    /// </summary>
    static class RepoFile
    {
        public static string? Find(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        public static string Require(string relativePath)
        {
            var path = Find(relativePath);
            Assert.True(path != null,
                $"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}. " +
                "Did the test project move relative to the repo root?");
            return path!;
        }
    }

    /// <summary>
    /// The NuGet pin + asmdef-gate invariant of issue #957, asserted over the REAL constants in
    /// <see cref="NuGetConfig"/> rather than over a copy of them.
    ///
    /// <para>WHY THIS MATTERS. The plugin's asmdefs are gated behind <c>defineConstraints</c> whose
    /// defines live in the CONSUMER's ProjectSettings and therefore survive a package upgrade: they
    /// record that <i>some</i> NuGet DLL set was restored, never <i>which</i>. Raise a pin without
    /// bumping <see cref="NuGetConfig.DependencyGenerationDefine"/> and the outgoing AppDomain leaves
    /// the gate open, Unity compiles the new sources against the old DLLs, the compile failure blocks
    /// the domain reload, and the project dead-locks in Safe Mode. That is what shipped in 0.88.0.</para>
    ///
    /// <para><c>.github/scripts/check_nuget_gate.py</c> guards this on every PR. These tests
    /// RE-IMPLEMENT its rule in C# over the compiled-in constants (deliberately not by shelling out to
    /// Python): the CI script parses <c>NuGetConfig.cs</c> as TEXT with a regex, so it and this suite
    /// fail for genuinely different reasons — the script catches an unblessed pin change, these catch a
    /// change that stops the script's regexes from matching what the compiler actually sees.</para>
    /// </summary>
    public class NuGetPinGateTests
    {
        const string LockRelativePath = ".github/nuget-gate.lock";

        /// <summary>
        /// The order-insensitive, whitespace-insensitive digest of a pinned set — byte-for-byte the
        /// canonical form <c>check_nuget_gate.py:pins_digest</c> hashes: <c>"id@version"</c> per pin,
        /// sorted ORDINALLY (Python's <c>sorted()</c> on <c>str</c> is code-point order), joined with
        /// <c>"\n"</c>, hashed as UTF-8 SHA-256, rendered lower-case hex.
        /// </summary>
        internal static string PinsDigest(IEnumerable<(string Id, string Version)> pins)
        {
            var canonical = string.Join("\n", pins
                .Select(p => $"{p.Id}@{p.Version}")
                .OrderBy(s => s, StringComparer.Ordinal));

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        static (string Id, string Version)[] LivePins()
            => NuGetConfig.Packages.Select(p => (p.Id, p.Version)).ToArray();

        static JsonElement ReadLock()
        {
            var json = File.ReadAllText(RepoFile.Require(LockRelativePath));
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        // ── The pins themselves ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Packages_AreNonEmpty_UniquelyIdentified_AndEveryVersionParses()
        {
            var pins = LivePins();
            Assert.NotEmpty(pins);

            // A duplicate id would make the resolver's install/removal bookkeeping ambiguous and would
            // hash into the same digest twice, so pin it here.
            var duplicates = pins.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();
            Assert.Empty(duplicates);

            // NuGet version shape: numeric core plus an optional prerelease/build suffix. The resolver
            // lower-cases the version straight into a flat-container URL, so anything that is not a real
            // version yields a 404 at install time rather than a build error here.
            var shape = new Regex(@"^\d+\.\d+\.\d+(\.\d+)?(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$");
            foreach (var (id, version) in pins)
            {
                Assert.False(string.IsNullOrWhiteSpace(id));
                Assert.True(shape.IsMatch(version), $"Pin '{id}' has a non-version string: '{version}'.");

                var numericCore = version.Split('-', '+')[0];
                Assert.True(Version.TryParse(numericCore, out _),
                    $"Pin '{id}' version '{version}' has an unparseable numeric core '{numericCore}'.");
            }
        }

        // ── The gate defines ───────────────────────────────────────────────────────────────────────

        [Fact]
        public void GenerationDefine_IsPresent_PrefixedAndDistinctFromTheReadyDefine()
        {
            Assert.False(string.IsNullOrWhiteSpace(NuGetConfig.ReadyDefine));
            Assert.False(string.IsNullOrWhiteSpace(NuGetConfig.DependencyGenerationDefine));

            // The two gates are ANDed by Unity. If they were the same symbol the generation gate would
            // be a no-op and #957 would be reachable again with a green CI.
            Assert.NotEqual(NuGetConfig.ReadyDefine, NuGetConfig.DependencyGenerationDefine);

            // RecompileGate.EnsureReadyDefine strips every define with this prefix EXCEPT the current
            // generation one, so a generation define outside the prefix could never be cleaned up.
            Assert.StartsWith(NuGetConfig.DependencyGenerationDefinePrefix,
                NuGetConfig.DependencyGenerationDefine, StringComparison.Ordinal);
            Assert.NotEqual(NuGetConfig.DependencyGenerationDefinePrefix,
                NuGetConfig.DependencyGenerationDefine);

            // The ready define must NOT sit under the generation prefix, or the stale-define sweep would
            // strip the ready gate itself.
            Assert.False(NuGetConfig.ReadyDefine.StartsWith(
                NuGetConfig.DependencyGenerationDefinePrefix, StringComparison.Ordinal));
        }

        [Fact]
        public void GateDefines_AreExactlyTheReadyAndGenerationPair()
        {
            // The asmdefs are constrained on this array as a SET; a define present in one place and
            // missing from the array is a half-open gate.
            Assert.Equal(
                new[] { NuGetConfig.ReadyDefine, NuGetConfig.DependencyGenerationDefine },
                NuGetConfig.GateDefines);
        }

        // ── The CI rule, re-implemented ────────────────────────────────────────────────────────────

        [Fact]
        public void PinnedSet_MatchesTheBlessedLock_AndTheLockPairsTheCurrentGenerationDefine()
        {
            var blessed = ReadLock();

            // Rule 1 of check_nuget_gate.py: the lock records a BLESSED PAIRING of (pins digest,
            // generation define). Change the pins and the digest moves; leave the define alone and the
            // pairing no longer matches — which is exactly the shipped-in-0.88.0 mistake.
            Assert.Equal(NuGetConfig.DependencyGenerationDefine,
                blessed.GetProperty("dependencyGenerationDefine").GetString());
            Assert.Equal(PinsDigest(LivePins()), blessed.GetProperty("pinsSha256").GetString());
        }

        [Fact]
        public void Digest_AgreesWithThePythonCanonicalForm_ForTheBlessedPinList()
        {
            // Cross-check of the re-implementation itself: the lock's `pins` array is written by
            // check_nuget_gate.py from the SAME pins it hashed. Feeding that array back through the C#
            // digest must reproduce the recorded hash — so if this C# canonicalization ever diverges
            // from the Python one (separator, casing, sort order), this reddens even while the test
            // above still passes by comparing two consistently-wrong values.
            var blessed = ReadLock();
            var fromLock = blessed.GetProperty("pins")
                .EnumerateArray()
                .Select(e =>
                {
                    var text = e.GetString()!;
                    var at = text.LastIndexOf('@');
                    return (Id: text.Substring(0, at), Version: text.Substring(at + 1));
                })
                .ToArray();

            Assert.NotEmpty(fromLock);
            Assert.Equal(blessed.GetProperty("pinsSha256").GetString(), PinsDigest(fromLock));
        }

        [Fact]
        public void Digest_IsOrderInsensitive_ButChangesWhenAnyPinVersionChanges()
        {
            var pins = LivePins();
            var baseline = PinsDigest(pins);

            // NEGATIVE half — reordering must NOT move the digest. `Packages` is a hand-edited array and
            // a re-sort of it is a routine, meaningless diff; a digest that moved on reorder would make
            // the gate fire on a change that cannot cause #957.
            Assert.Equal(baseline, PinsDigest(pins.Reverse().ToArray()));

            // POSITIVE half — the property the gate rests on: a pin version change is ALWAYS visible in
            // the digest, so the lock comparison above cannot stay green through an unblessed bump. This
            // is asserted for EVERY pin, not just a convenient one.
            foreach (var (id, version) in pins)
            {
                var mutated = pins
                    .Select(p => p.Id == id ? (p.Id, Version: version + ".99") : p)
                    .ToArray();
                Assert.NotEqual(baseline, PinsDigest(mutated));
            }
        }

        // ── DoD 6: this csproj is a SECOND pin location; keep it in lock-step with the resolver ────

        [Theory]
        [InlineData("com.IvanMurzak.McpPlugin")]
        [InlineData("com.IvanMurzak.ReflectorNet")]
        public void PinnedPackageVersions_MatchNuGetConfig(string packageId)
        {
            // EngineFree.csproj consumes McpPlugin/ReflectorNet from NuGet, so it is a pin location the
            // release train must bump alongside NuGetConfig.Packages. A missed bump would leave this
            // suite compiling the plugin's sources against the PREVIOUS framework DLLs while every other
            // signal says the pin moved — the failure would be silent and the green run misleading.
            //
            // The observed value is read from assembly metadata MSBuild emits from the real
            // <PackageReference> item rather than from a literal re-typed here, so the guard cannot
            // drift from the csproj.
            //
            // Measured: under a workspace-source override (a root Directory.Build.targets whose
            // `<PackageReference Update>` rows pin `[8.3.0-ws.g<sha8>]` with UseWorkspaceSources=true)
            // this metadata still reports the DECLARED pin, because Directory.Build.targets is
            // imported AFTER the ItemGroup that captures it. That is the behaviour this guard wants:
            // an override is a deliberate, transient substitution, and it must not redden a check
            // about what the RELEASE TRAIN has to bump.
            var metadata = typeof(NuGetPinGateTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Pin." + packageId);

            Assert.True(metadata != null,
                $"EngineFree.csproj no longer exports assembly metadata 'Pin.{packageId}'. " +
                "The pin-parity guard is inert without it — restore the <AssemblyMetadata> item.");
            Assert.False(string.IsNullOrWhiteSpace(metadata!.Value),
                $"Assembly metadata 'Pin.{packageId}' is empty — the MSBuild item transform in " +
                "EngineFree.csproj stopped resolving, so this guard would compare nothing.");

            var resolverPin = NuGetConfig.Packages.FirstOrDefault(p => p.Id == packageId);
            Assert.True(resolverPin.Id == packageId,
                $"'{packageId}' is no longer pinned in NuGetConfig.Packages — the plugin and this test " +
                "project must consume the same framework version.");

            Assert.Equal(resolverPin.Version, metadata.Value);
        }

        // ── NuGetPackage / NuGetCache derivations ──────────────────────────────────────────────────

        [Fact]
        public void DownloadUrl_IsTheLowerCasedFlatContainerPath()
        {
            var package = new NuGetPackage("com.IvanMurzak.McpPlugin", "8.3.0", includeInBuild: true);

            // The v3 flat container is case-sensitive and serves lower-cased ids/versions only; a
            // mixed-case URL 404s at install time, which surfaces to a user as a broken first import.
            Assert.Equal(
                $"{NuGetConfig.NuGetBaseUrl}/com.ivanmurzak.mcpplugin/8.3.0/com.ivanmurzak.mcpplugin.8.3.0.nupkg",
                package.DownloadUrl);

            // The on-disk cache name keeps the ORIGINAL casing — the two are deliberately different.
            Assert.Equal("com.IvanMurzak.McpPlugin.8.3.0.nupkg", package.CacheFileName);
            Assert.Equal("com.IvanMurzak.McpPlugin 8.3.0", package.ToString());
        }

        [Fact]
        public void CachedPath_IsUnderTheConfiguredCacheDirectory()
        {
            var package = new NuGetPackage("R3", "1.3.0", includeInBuild: true);
            var cached = NuGetCache.GetCachedPath(package);

            Assert.Equal(Path.Combine(NuGetConfig.CachePath, "R3.1.3.0.nupkg"), cached);

            // Library/ is wiped by Unity and untracked by git — the cache must never land in the asset
            // pipeline, and the install path must never land in Library/.
            Assert.StartsWith("Library", NuGetConfig.CachePath, StringComparison.Ordinal);
            Assert.StartsWith("Assets/", NuGetConfig.InstallPath, StringComparison.Ordinal);
        }

        [Fact]
        public void TargetFrameworkPriority_PrefersNetstandard21_AndEndsWithTheRootLibFallback()
        {
            var priority = NuGetConfig.TargetFrameworkPriority;

            // McpPlugin.dll ships netstandard2.1 for Unity; picking a net4x asset first would install a
            // DLL Unity's runtime cannot load the same way, which is how MissingMethodException at play
            // time starts.
            Assert.Equal("netstandard2.1", priority[0]);
            Assert.Equal(string.Empty, priority[priority.Length - 1]);
            Assert.Equal(priority.Length, priority.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
