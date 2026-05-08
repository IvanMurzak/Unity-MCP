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
using System.IO;
using System.Linq;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Coverage for the on-disk manifest <c>.nuget-installed.json</c> introduced
    /// for the flat layout (issue #733). The manifest is the primary source of
    /// truth for "which DLL belongs to which package at which version", with
    /// versioned-filename parsing as the disaster-recovery fallback.
    /// </summary>
    [TestFixture]
    public class NuGetInstallManifestTests
    {
        string _installPath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _installPath = Path.Combine(
                Path.GetTempPath(),
                "UnityMcp-Manifest-" + Path.GetRandomFileName());
            Directory.CreateDirectory(_installPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_installPath))
            {
                try { Directory.Delete(_installPath, recursive: true); }
                catch { /* best-effort cleanup */ }
            }
        }

        [Test]
        public void Load_ReturnsEmptyManifest_WhenFileMissing()
        {
            var manifest = NuGetInstallManifest.Load(_installPath);
            Assert.AreEqual(0, manifest.Packages.Count);
        }

        [Test]
        public void SaveThenLoad_RoundTrips_AllFields()
        {
            var manifest = new InstallManifest();
            var entry = new InstalledPackage("8.0.15");
            entry.Dlls.Add("Microsoft.AspNetCore.SignalR.Client.8.0.15.dll");
            entry.Dlls.Add("Microsoft.AspNetCore.SignalR.Client.Core.8.0.15.dll");
            manifest.Packages["Microsoft.AspNetCore.SignalR.Client"] = entry;

            // Multi-DLL package with a different version.
            var multi = new InstalledPackage("10.0.3");
            multi.Dlls.Add("System.Memory.10.0.3.dll");
            multi.Dlls.Add("System.Buffers.10.0.3.dll");
            manifest.Packages["Microsoft.Bcl.Memory"] = multi;

            // Empty-DLL entry (development-only dependency).
            manifest.Packages["Microsoft.CodeAnalysis.Analyzers"] = new InstalledPackage("3.11.0");

            NuGetInstallManifest.Save(_installPath, manifest);

            var roundTrip = NuGetInstallManifest.Load(_installPath);

            Assert.AreEqual(3, roundTrip.Packages.Count);
            Assert.AreEqual("8.0.15", roundTrip.Packages["Microsoft.AspNetCore.SignalR.Client"].Version);
            Assert.AreEqual(2, roundTrip.Packages["Microsoft.AspNetCore.SignalR.Client"].Dlls.Count);
            Assert.AreEqual("10.0.3", roundTrip.Packages["Microsoft.Bcl.Memory"].Version);
            CollectionAssert.AreEquivalent(
                new[] { "System.Memory.10.0.3.dll", "System.Buffers.10.0.3.dll" },
                roundTrip.Packages["Microsoft.Bcl.Memory"].Dlls);
            Assert.AreEqual("3.11.0", roundTrip.Packages["Microsoft.CodeAnalysis.Analyzers"].Version);
            Assert.AreEqual(0, roundTrip.Packages["Microsoft.CodeAnalysis.Analyzers"].Dlls.Count);
        }

        [Test]
        public void Save_CreatesInstallDirectoryIfMissing()
        {
            var nested = Path.Combine(_installPath, "nested-not-yet-created");
            Assert.IsFalse(Directory.Exists(nested));

            var manifest = new InstallManifest();
            NuGetInstallManifest.Save(nested, manifest);

            Assert.IsTrue(Directory.Exists(nested));
            Assert.IsTrue(File.Exists(Path.Combine(nested, ".nuget-installed.json")));
        }

        [Test]
        public void Load_ReturnsEmptyManifest_OnMalformedJson_AndDoesNotThrow()
        {
            File.WriteAllText(Path.Combine(_installPath, ".nuget-installed.json"), "{ not valid json");

            var manifest = NuGetInstallManifest.Load(_installPath);

            Assert.AreEqual(0, manifest.Packages.Count);
        }

        [Test]
        public void Load_PreservesCaseInsensitiveLookup()
        {
            // The runtime resolver matches package IDs case-insensitively
            // throughout. The manifest must round-trip with the same property.
            var manifest = new InstallManifest();
            manifest.Packages["System.Text.Json"] = new InstalledPackage("8.0.5");
            NuGetInstallManifest.Save(_installPath, manifest);

            var loaded = NuGetInstallManifest.Load(_installPath);
            Assert.IsTrue(loaded.Packages.ContainsKey("system.text.json"),
                "Package ID lookup must be case-insensitive after a round-trip.");
            Assert.IsTrue(loaded.Packages.ContainsKey("SYSTEM.TEXT.JSON"));
        }

        [Test]
        public void Save_IsIdempotent_RoundTripIsByteForByte()
        {
            // Two saves of the same logical manifest must produce the exact
            // same bytes — keeps git diffs clean and avoids spurious
            // post-restore changes.
            var manifest = new InstallManifest();
            var entry = new InstalledPackage("8.0.5");
            entry.Dlls.Add("System.Text.Json.8.0.5.dll");
            manifest.Packages["System.Text.Json"] = entry;

            NuGetInstallManifest.Save(_installPath, manifest);
            var first = File.ReadAllBytes(Path.Combine(_installPath, ".nuget-installed.json"));

            // Round-trip and save again.
            var loaded = NuGetInstallManifest.Load(_installPath);
            NuGetInstallManifest.Save(_installPath, loaded);
            var second = File.ReadAllBytes(Path.Combine(_installPath, ".nuget-installed.json"));

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void TryRebuildFromDisk_ReproducesSingleDllPackagesFromVersionedFilenames()
        {
            // Disaster recovery (#733 acceptance criterion): user deletes
            // .nuget-installed.json. The next restore must rebuild the manifest
            // from on-disk versioned filenames, with no re-extraction needed.
            File.WriteAllText(Path.Combine(_installPath, "Microsoft.AspNetCore.Http.Connections.Client.8.0.15.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Text.Json.8.0.5.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "R3.1.3.0.dll"), "dummy");
            // Plus an unrelated file the rebuild must ignore.
            File.WriteAllText(Path.Combine(_installPath, "ReadMe.txt"), "user notes");

            var rebuilt = NuGetInstallManifest.TryRebuildFromDisk(_installPath);

            Assert.AreEqual(3, rebuilt.Packages.Count);
            Assert.AreEqual("8.0.15", rebuilt.Packages["Microsoft.AspNetCore.Http.Connections.Client"].Version);
            Assert.AreEqual("8.0.5", rebuilt.Packages["System.Text.Json"].Version);
            Assert.AreEqual("1.3.0", rebuilt.Packages["R3"].Version);
        }

        [Test]
        public void TryRebuildFromDisk_IgnoresLegacyUnversionedDllsAndNonDllFiles()
        {
            // Pre-flat-layout artifacts the user might still have on disk —
            // the parser must reject them so the rebuild stays consistent.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.dll"), "legacy unversioned");
            File.WriteAllText(Path.Combine(_installPath, "ReadMe.md"), "notes");

            var rebuilt = NuGetInstallManifest.TryRebuildFromDisk(_installPath);

            Assert.AreEqual(0, rebuilt.Packages.Count);
        }

        [Test]
        public void TryRebuildFromDisk_KeysMultiDllPackagesUnderSyntheticStems()
        {
            // Microsoft.Bcl.Memory is the canonical multi-DLL package: it ships
            // System.Memory, System.Buffers, System.Runtime.CompilerServices.Unsafe.
            // The disaster-recovery rebuild has no way to recover the real owner
            // from filenames alone, so it MUST key those DLLs under their own
            // stems as synthetic IDs. The follow-up MigrateSyntheticOwnerEntries
            // call (driven from NuGetPackageInstaller.Install) reconciles the
            // synthetic IDs back onto the real package ID — see the
            // ReconcilesSyntheticEntries_ForMultiDllPackage test below.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.10.0.3.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Buffers.10.0.3.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.10.0.3.dll"), "dummy");

            var rebuilt = NuGetInstallManifest.TryRebuildFromDisk(_installPath);

            Assert.AreEqual(3, rebuilt.Packages.Count);
            Assert.IsTrue(rebuilt.Packages.ContainsKey("System.Memory"));
            Assert.IsTrue(rebuilt.Packages.ContainsKey("System.Buffers"));
            Assert.IsTrue(rebuilt.Packages.ContainsKey("System.Runtime.CompilerServices.Unsafe"));
            Assert.IsFalse(rebuilt.Packages.ContainsKey("Microsoft.Bcl.Memory"),
                "Filename-only rebuild cannot recover the real owning package id; the test guards the synthetic-id contract that MigrateSyntheticOwnerEntries depends on.");
        }

        [Test]
        public void MigrateSyntheticOwnerEntries_ReconcilesMultiDllPackage_AndDoesNotTripCollisionCheck()
        {
            // End-to-end coverage of the post-rebuild reconciliation flow:
            //   1. Seed a multi-DLL package's flat-layout DLLs without a manifest.
            //   2. Run TryRebuildFromDisk — expect synthetic stem-keyed entries.
            //   3. Run MigrateSyntheticOwnerEntries with the real package id and a
            //      planned-DLL list that matches the on-disk filenames.
            //   4. Assert the synthetic entries are gone and the real package id
            //      now owns those same DLLs at the same version.
            // Without this fix, NuGetPackageInstaller.Install would log
            // "Refusing to install Microsoft.Bcl.Memory ..." and stick the user
            // on the disaster-recovery path forever.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.10.0.3.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Buffers.10.0.3.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.10.0.3.dll"), "dummy");

            var manifest = NuGetInstallManifest.TryRebuildFromDisk(_installPath);
            // Sanity: the rebuild produced synthetic entries (the [high] reproduction state).
            Assert.IsTrue(manifest.Packages.ContainsKey("System.Memory"));
            Assert.IsFalse(manifest.Packages.ContainsKey("Microsoft.Bcl.Memory"));

            var planned = new System.Collections.Generic.List<PlannedDll>
            {
                new PlannedDll("lib/net8.0/System.Memory.dll", "System.Memory.10.0.3.dll", Path.Combine(_installPath, "System.Memory.10.0.3.dll")),
                new PlannedDll("lib/net8.0/System.Buffers.dll", "System.Buffers.10.0.3.dll", Path.Combine(_installPath, "System.Buffers.10.0.3.dll")),
                new PlannedDll("lib/net8.0/System.Runtime.CompilerServices.Unsafe.dll", "System.Runtime.CompilerServices.Unsafe.10.0.3.dll", Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.10.0.3.dll")),
            };

            NuGetPackageInstaller.MigrateSyntheticOwnerEntries(manifest, "Microsoft.Bcl.Memory", "10.0.3", planned);

            Assert.IsFalse(manifest.Packages.ContainsKey("System.Memory"),
                "Synthetic stem-keyed entry must be removed after reconciliation.");
            Assert.IsFalse(manifest.Packages.ContainsKey("System.Buffers"));
            Assert.IsFalse(manifest.Packages.ContainsKey("System.Runtime.CompilerServices.Unsafe"));
            Assert.IsTrue(manifest.Packages.ContainsKey("Microsoft.Bcl.Memory"),
                "Real package id must own the migrated DLLs after reconciliation.");
            Assert.AreEqual("10.0.3", manifest.Packages["Microsoft.Bcl.Memory"].Version);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "System.Memory.10.0.3.dll",
                    "System.Buffers.10.0.3.dll",
                    "System.Runtime.CompilerServices.Unsafe.10.0.3.dll",
                },
                manifest.Packages["Microsoft.Bcl.Memory"].Dlls);
        }

        [Test]
        public void MigrateSyntheticOwnerEntries_DoesNotTouchUnrelatedPackages()
        {
            // Defense: the migrator must only strip entries whose DLL set is a
            // subset of the planned filenames. A real distinct package that
            // happens to share a version number must survive untouched.
            var manifest = new InstallManifest();
            var unrelatedEntry = new InstalledPackage("10.0.3");
            unrelatedEntry.Dlls.Add("Newtonsoft.Json.10.0.3.dll");
            manifest.Packages["Newtonsoft.Json"] = unrelatedEntry;

            var syntheticEntry = new InstalledPackage("10.0.3");
            syntheticEntry.Dlls.Add("System.Memory.10.0.3.dll");
            manifest.Packages["System.Memory"] = syntheticEntry;

            var planned = new System.Collections.Generic.List<PlannedDll>
            {
                new PlannedDll("lib/net8.0/System.Memory.dll", "System.Memory.10.0.3.dll", "/ignored/System.Memory.10.0.3.dll"),
            };

            NuGetPackageInstaller.MigrateSyntheticOwnerEntries(manifest, "Microsoft.Bcl.Memory", "10.0.3", planned);

            Assert.IsTrue(manifest.Packages.ContainsKey("Newtonsoft.Json"),
                "Unrelated same-version package must not be migrated away.");
            Assert.IsFalse(manifest.Packages.ContainsKey("System.Memory"),
                "Synthetic same-version entry whose DLLs match the planned set must be migrated.");
            Assert.IsTrue(manifest.Packages.ContainsKey("Microsoft.Bcl.Memory"));
        }

        [Test]
        public void MigrateSyntheticOwnerEntries_PartialDiskState_LeavesPlannedDllsMissingFromManifestEntry()
        {
            // Regression for the alreadyOnDisk gate: after MigrateSyntheticOwnerEntries
            // pulls only the synthetic entries that the disaster-recovery rebuild saw on
            // disk (a strict subset of the package's planned DLLs), the resulting
            // real-owner entry must NOT advertise the missing DLLs. NuGetPackageInstaller
            // gates `alreadyOnDisk` on `planned ⊆ manifestEntry.Dlls`, so an incomplete
            // migration here MUST yield an entry whose Dlls set fails that superset
            // check — otherwise extraction would be skipped and the missing files would
            // never be re-extracted.
            //
            // Scenario: Microsoft.Bcl.Memory ships 3 DLLs; only 2 survived on disk
            // (manifest deleted + one DLL lost to AV / partial cleanup). The user
            // re-runs at the same package version. The post-migration manifest entry
            // covers only the 2 surviving stems; planned still has 3.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.10.0.3.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Buffers.10.0.3.dll"), "dummy");
            // System.Runtime.CompilerServices.Unsafe.10.0.3.dll intentionally NOT seeded.

            var manifest = NuGetInstallManifest.TryRebuildFromDisk(_installPath);
            Assert.IsTrue(manifest.Packages.ContainsKey("System.Memory"));
            Assert.IsTrue(manifest.Packages.ContainsKey("System.Buffers"));
            Assert.IsFalse(manifest.Packages.ContainsKey("System.Runtime.CompilerServices.Unsafe"),
                "Sanity: the missing DLL must not appear in the rebuilt manifest.");

            var planned = new System.Collections.Generic.List<PlannedDll>
            {
                new PlannedDll("lib/net8.0/System.Memory.dll", "System.Memory.10.0.3.dll", Path.Combine(_installPath, "System.Memory.10.0.3.dll")),
                new PlannedDll("lib/net8.0/System.Buffers.dll", "System.Buffers.10.0.3.dll", Path.Combine(_installPath, "System.Buffers.10.0.3.dll")),
                new PlannedDll("lib/net8.0/System.Runtime.CompilerServices.Unsafe.dll", "System.Runtime.CompilerServices.Unsafe.10.0.3.dll", Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.10.0.3.dll")),
            };

            var migrated = NuGetPackageInstaller.MigrateSyntheticOwnerEntries(
                manifest, "Microsoft.Bcl.Memory", "10.0.3", planned);

            Assert.IsTrue(migrated, "Migration must signal that the manifest changed (caller must persist).");

            Assert.IsTrue(manifest.Packages.ContainsKey("Microsoft.Bcl.Memory"));
            var realEntry = manifest.Packages["Microsoft.Bcl.Memory"];
            CollectionAssert.AreEquivalent(
                new[] { "System.Memory.10.0.3.dll", "System.Buffers.10.0.3.dll" },
                realEntry.Dlls,
                "Migrated entry must reflect on-disk reality (2 of 3 DLLs), not the full planned set.");

            // Cross-check the gate the installer uses: planned ⊄ realEntry.Dlls, so
            // alreadyOnDisk would return false and the missing DLL gets re-extracted.
            var allPlannedRecorded = planned.TrueForAll(
                p => realEntry.Dlls.Contains(p.FileName, System.StringComparer.OrdinalIgnoreCase));
            Assert.IsFalse(allPlannedRecorded,
                "alreadyOnDisk gate would short-circuit incorrectly if the planned set were a subset of the migrated entry.");
        }

        [Test]
        public void MigrateSyntheticOwnerEntries_ReturnsFalse_WhenNothingMigrated()
        {
            // Caller persists only on a true return — ensure we don't churn the manifest
            // file when the migrator was a no-op.
            var manifest = new InstallManifest();
            var unrelatedEntry = new InstalledPackage("9.9.9");
            unrelatedEntry.Dlls.Add("Unrelated.9.9.9.dll");
            manifest.Packages["Unrelated"] = unrelatedEntry;

            var planned = new System.Collections.Generic.List<PlannedDll>
            {
                new PlannedDll("lib/net8.0/System.Memory.dll", "System.Memory.10.0.3.dll", "/ignored/System.Memory.10.0.3.dll"),
            };

            var migrated = NuGetPackageInstaller.MigrateSyntheticOwnerEntries(
                manifest, "Microsoft.Bcl.Memory", "10.0.3", planned);

            Assert.IsFalse(migrated);
        }

        [Test]
        public void MigrateSyntheticOwnerEntries_IgnoresEntriesAtDifferentVersion()
        {
            // A stem-keyed entry left at a DIFFERENT version is not part of the
            // reconciliation scope; it gets cleaned up by the stale-version
            // pass instead.
            var manifest = new InstallManifest();
            var olderSynthetic = new InstalledPackage("9.0.0");
            olderSynthetic.Dlls.Add("System.Memory.9.0.0.dll");
            manifest.Packages["System.Memory"] = olderSynthetic;

            var planned = new System.Collections.Generic.List<PlannedDll>
            {
                new PlannedDll("lib/net8.0/System.Memory.dll", "System.Memory.10.0.3.dll", "/ignored/System.Memory.10.0.3.dll"),
            };

            NuGetPackageInstaller.MigrateSyntheticOwnerEntries(manifest, "Microsoft.Bcl.Memory", "10.0.3", planned);

            Assert.IsTrue(manifest.Packages.ContainsKey("System.Memory"),
                "Different-version synthetic entry must be left alone (stale-version cleanup handles it).");
            Assert.IsFalse(manifest.Packages.ContainsKey("Microsoft.Bcl.Memory"),
                "No real-owner entry should be created when nothing was migrated.");
        }

        [Test]
        public void TryParseInstalledDllName_GreedilyConsumesEntireVersionTail()
        {
            // Regression check on the parser used by the disaster-recovery
            // rebuild: for "System.Memory.10.0.3.dll" the version is "10.0.3"
            // (not "0.3" or "3"), and the stem is "System.Memory".
            Assert.IsTrue(NuGetInstallManifest.TryParseInstalledDllName(
                "System.Memory.10.0.3.dll", out var stem, out var version));
            Assert.AreEqual("System.Memory", stem);
            Assert.AreEqual("10.0.3", version);
        }

        [Test]
        public void TryParseInstalledDllName_HandlesPackageStemsWithDots()
        {
            Assert.IsTrue(NuGetInstallManifest.TryParseInstalledDllName(
                "Microsoft.AspNetCore.Http.Connections.Client.8.0.15.dll", out var stem, out var version));
            Assert.AreEqual("Microsoft.AspNetCore.Http.Connections.Client", stem);
            Assert.AreEqual("8.0.15", version);
        }

        [Test]
        public void TryParseInstalledDllName_HandlesShortStemAndLongVersion()
        {
            // "R3.1.3.0.dll" → stem "R3", version "1.3.0".
            Assert.IsTrue(NuGetInstallManifest.TryParseInstalledDllName(
                "R3.1.3.0.dll", out var stem, out var version));
            Assert.AreEqual("R3", stem);
            Assert.AreEqual("1.3.0", version);
        }

        [Test]
        public void TryParseInstalledDllName_RejectsLegacyUnversionedFilename()
        {
            // No version tail → not a flat-layout install entry.
            Assert.IsFalse(NuGetInstallManifest.TryParseInstalledDllName(
                "System.Memory.dll", out _, out _));
        }

        [Test]
        public void TryParseInstalledDllName_RejectsMalformedTail()
        {
            // ".bar" tail isn't a System.Version.
            Assert.IsFalse(NuGetInstallManifest.TryParseInstalledDllName(
                "Foo.bar.dll", out _, out _));
        }

        [Test]
        public void TryParseInstalledDllName_RejectsNonDllExtension()
        {
            Assert.IsFalse(NuGetInstallManifest.TryParseInstalledDllName(
                "System.Memory.10.0.3.exe", out _, out _));
        }
    }
}
