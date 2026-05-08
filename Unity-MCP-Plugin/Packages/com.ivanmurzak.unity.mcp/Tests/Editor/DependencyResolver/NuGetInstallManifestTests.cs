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
