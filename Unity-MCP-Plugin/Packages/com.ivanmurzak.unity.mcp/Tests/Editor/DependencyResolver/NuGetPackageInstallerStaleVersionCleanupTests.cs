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
    /// Regression coverage for issue #703 (stale-sibling cleanup) ported to the
    /// post-#733 flat-layout install model. The on-disk layout is now
    /// <c>{installPath}/{stem}.{packageVersion}.dll</c> with a
    /// <c>.nuget-installed.json</c> manifest at the root that maps package IDs
    /// to their owned DLLs and the version those DLLs were installed at.
    /// <see cref="NuGetPackageInstaller.RemoveStaleSiblingVersions"/> drives
    /// stale-version removal off that manifest; these tests exercise it
    /// directly against a temp directory.
    /// </summary>
    [TestFixture]
    public class NuGetPackageInstallerStaleVersionCleanupTests
    {
        string _installPath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _installPath = Path.Combine(
                Path.GetTempPath(),
                "UnityMcp-NuGetInstaller-" + Path.GetRandomFileName());
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
        public void RemoveStaleSiblingVersions_DeletesPriorVersionDllAndItsMeta()
        {
            // Issue #703 scenario, ported to flat layout: ReflectorNet 5.0.0 is
            // recorded in the manifest from a previous session, the user upgraded
            // the Unity package, the new dep graph resolves ReflectorNet 5.1.1.
            CreateFlatPackage("com.IvanMurzak.ReflectorNet", "5.0.0", "ReflectorNet.5.0.0.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed, "Helper must report that it removed something.");
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "ReflectorNet.5.0.0.dll")),
                "Stale versioned DLL must be removed.");
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "ReflectorNet.5.0.0.dll.meta")),
                "Stale .meta sidecar must be removed alongside its DLL.");
            // Manifest must reflect the removal.
            var manifest = NuGetInstallManifest.Load(_installPath);
            Assert.IsFalse(manifest.Packages.ContainsKey("com.IvanMurzak.ReflectorNet"));
        }

        [Test]
        public void RemoveStaleSiblingVersions_PreservesSameVersionEntry()
        {
            // Idempotency: re-running the resolver on an up-to-date project must NOT delete
            // the entry at the configured version. Otherwise every restore would force
            // a needless re-extraction.
            CreateFlatPackage("com.IvanMurzak.ReflectorNet", "5.1.1", "ReflectorNet.5.1.1.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsFalse(removed);
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "ReflectorNet.5.1.1.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "ReflectorNet.5.1.1.dll.meta")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_PreservesOtherPackages()
        {
            // The scan must not touch entries belonging to a different package Id.
            CreateFlatPackage("com.IvanMurzak.ReflectorNet", "5.0.0", "ReflectorNet.5.0.0.dll");
            CreateFlatPackage("System.Text.Json", "8.0.5", "System.Text.Json.8.0.5.dll");
            CreateFlatPackage("Microsoft.AspNetCore.SignalR.Client", "8.0.15",
                "Microsoft.AspNetCore.SignalR.Client.8.0.15.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            var manifest = NuGetInstallManifest.Load(_installPath);
            Assert.IsFalse(manifest.Packages.ContainsKey("com.IvanMurzak.ReflectorNet"));
            Assert.IsTrue(manifest.Packages.ContainsKey("System.Text.Json"));
            Assert.IsTrue(manifest.Packages.ContainsKey("Microsoft.AspNetCore.SignalR.Client"),
                "Package with dots in its Id must not be matched against a different package's Id.");
        }

        [Test]
        public void RemoveStaleSiblingVersions_HandlesPackageIdsWithDots()
        {
            CreateFlatPackage("Microsoft.AspNetCore.SignalR.Common", "10.0.3",
                "Microsoft.AspNetCore.SignalR.Common.10.0.3.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "Microsoft.AspNetCore.SignalR.Common", "8.0.15");

            Assert.IsTrue(removed);
            var manifest = NuGetInstallManifest.Load(_installPath);
            Assert.IsFalse(manifest.Packages.ContainsKey("Microsoft.AspNetCore.SignalR.Common"));
        }

        [Test]
        public void RemoveStaleSiblingVersions_DeletesHigherVersionTooWhenItIsNotTheKeepVersion()
        {
            // The cleanup contract is "delete everything that is not the keepVersion", not
            // "delete only lower versions".
            CreateFlatPackage("com.IvanMurzak.ReflectorNet", "6.0.0", "ReflectorNet.6.0.0.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "ReflectorNet.6.0.0.dll")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_ReturnsFalse_WhenInstallPathDoesNotExist()
        {
            // First-ever restore on a brand-new project: the install dir is created lazily
            // by the restorer. The helper must not crash and must report no work done.
            var doesNotExist = Path.Combine(_installPath, "does-not-exist");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                doesNotExist, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsFalse(removed);
        }

        [Test]
        public void RemoveStaleSiblingVersions_ReturnsFalse_WhenManifestHasNoMatchingEntry()
        {
            CreateFlatPackage("System.Text.Json", "8.0.5", "System.Text.Json.8.0.5.dll");
            CreateFlatPackage("Microsoft.AspNetCore.SignalR.Client", "8.0.15",
                "Microsoft.AspNetCore.SignalR.Client.8.0.15.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsFalse(removed);
            var manifest = NuGetInstallManifest.Load(_installPath);
            Assert.IsTrue(manifest.Packages.ContainsKey("System.Text.Json"));
            Assert.IsTrue(manifest.Packages.ContainsKey("Microsoft.AspNetCore.SignalR.Client"));
        }

        [Test]
        public void RemoveStaleSiblingVersions_MatchesPackageIdCaseInsensitively()
        {
            // Comparisons throughout the resolver are case-insensitive on the package ID.
            CreateFlatPackage("com.ivanmurzak.reflectornet", "5.0.0", "ReflectorNet.5.0.0.dll");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            var manifest = NuGetInstallManifest.Load(_installPath);
            Assert.IsFalse(manifest.Packages.ContainsKey("com.ivanmurzak.reflectornet"));
        }

        /// <summary>
        /// Creates a fake flat-layout entry: writes a single DLL named
        /// <paramref name="dllName"/> (already in the
        /// <c>{stem}.{packageVersion}.dll</c> form), a sibling <c>.meta</c>,
        /// and updates the manifest to associate it with <paramref name="id"/>
        /// at <paramref name="version"/>.
        /// </summary>
        void CreateFlatPackage(string id, string version, string dllName)
        {
            var dllPath = Path.Combine(_installPath, dllName);
            File.WriteAllText(dllPath, "dummy");
            File.WriteAllText(dllPath + ".meta", "fileFormatVersion: 2\nguid: 00000000000000000000000000000000\n");

            var manifest = NuGetInstallManifest.Load(_installPath);
            var entry = new InstalledPackage(version);
            entry.Dlls.Add(dllName);
            manifest.Packages[id] = entry;
            NuGetInstallManifest.Save(_installPath, manifest);
        }
    }
}
