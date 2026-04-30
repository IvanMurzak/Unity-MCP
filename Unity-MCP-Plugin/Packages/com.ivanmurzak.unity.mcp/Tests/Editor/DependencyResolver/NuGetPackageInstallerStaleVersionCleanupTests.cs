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
    /// Regression coverage for issue #703: when the resolver installs a package whose
    /// version was bumped (commonly a transitive like ReflectorNet on a Unity-package
    /// upgrade), any prior <c>{Id}.&lt;oldVersion&gt;/</c> directory left on disk from a
    /// previous session must be removed at install time so the C# compiler does not see
    /// duplicate copies of the same assembly.
    ///
    /// <see cref="NuGetPackageInstaller.RemoveStaleSiblingVersions"/> is the helper that
    /// performs this scan. These tests exercise it directly against a temp directory; the
    /// actual <c>Install()</c> path wires the real <c>NuGetConfig.InstallPath</c> in.
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
        public void RemoveStaleSiblingVersions_DeletesPriorLowerVersionAndItsMeta()
        {
            // Issue #703 scenario: ReflectorNet 5.0.0 installed in a previous session,
            // user upgraded the Unity package, new dep graph resolves ReflectorNet 5.1.1.
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "5.0.0");
            // The new directory does not need to exist yet — the helper runs BEFORE
            // extraction in Install(), so this mirrors the real cross-session upgrade flow.

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed, "Helper must report that it removed something.");
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.0.0")),
                "Stale lower-version directory must be removed.");
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.0.0.meta")),
                "Stale .meta sidecar must be removed alongside its directory.");
        }

        [Test]
        public void RemoveStaleSiblingVersions_PreservesSameVersionDirectory()
        {
            // Idempotency: re-running the resolver on an up-to-date project must NOT delete
            // the directory at the configured version. Otherwise every restore would force
            // a needless re-extraction.
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "5.1.1");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsFalse(removed);
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.1.1")));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.1.1.meta")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_PreservesOtherPackages()
        {
            // The scan must not touch directories belonging to a different package Id.
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "5.0.0");
            CreateFakePackageDir("System.Text.Json", "8.0.5");
            CreateFakePackageDir("Microsoft.AspNetCore.SignalR.Client", "8.0.15");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.0.0")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")),
                "Unrelated package must be untouched.");
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "Microsoft.AspNetCore.SignalR.Client.8.0.15")),
                "Package with dots in its Id must not be matched against a different package's Id.");
        }

        [Test]
        public void RemoveStaleSiblingVersions_HandlesPackageIdsWithDots()
        {
            // Bug-shaped regression check: ExtractPackageIdFromDirName must split correctly
            // for IDs that contain dots, so a stale "Microsoft.AspNetCore.SignalR.Common.10.0.3"
            // is correctly recognised as belonging to "Microsoft.AspNetCore.SignalR.Common".
            CreateFakePackageDir("Microsoft.AspNetCore.SignalR.Common", "10.0.3");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "Microsoft.AspNetCore.SignalR.Common", "8.0.15");

            Assert.IsTrue(removed);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Microsoft.AspNetCore.SignalR.Common.10.0.3")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_DeletesMultipleStaleVersionsAtOnce()
        {
            // A user who upgraded across several Unity-MCP releases without ever running
            // Restore() in the middle could legitimately have two or more stale directories
            // for the same package Id on disk. The helper must clean ALL of them, not just one.
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "4.9.0");
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "5.0.0");
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "5.1.0");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.4.9.0")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.0.0")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.5.1.0")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_DeletesHigherVersionTooWhenItIsNotTheKeepVersion()
        {
            // The cleanup contract is "delete everything that is not the keepVersion", not
            // "delete only lower versions". A bad partial restore could legitimately leave a
            // higher-version directory on disk that is NOT the one the current closure
            // resolves to (e.g. the closure pinned a lower version explicitly to match a
            // peer dependency), and we need to remove it for the same duplicate-assembly
            // reason as a stale lower version.
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "6.0.0");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.IvanMurzak.ReflectorNet.6.0.0")));
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
        public void RemoveStaleSiblingVersions_ReturnsFalse_WhenInstallPathHasNoMatchingSiblings()
        {
            // Steady-state restore: the install path is full of unrelated packages and the
            // current package is not on disk at any version yet (it is about to be extracted).
            // The helper must do nothing and report so.
            CreateFakePackageDir("System.Text.Json", "8.0.5");
            CreateFakePackageDir("Microsoft.AspNetCore.SignalR.Client", "8.0.15");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsFalse(removed);
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "Microsoft.AspNetCore.SignalR.Client.8.0.15")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_MatchesPackageIdCaseInsensitively()
        {
            // The rest of the resolver matches package IDs case-insensitively (see
            // installedThisSession's StringComparer.OrdinalIgnoreCase, RemoveUnnecessaryPackages,
            // IsSkipped). Real-world directory names typically preserve the canonical casing
            // of the NuGet ID, but defensive case-insensitivity protects against e.g. a
            // hand-edited Plugins folder where someone lowered the casing.
            CreateFakePackageDir("com.ivanmurzak.reflectornet", "5.0.0");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "com.ivanmurzak.reflectornet.5.0.0")));
        }

        [Test]
        public void RemoveStaleSiblingVersions_IgnoresDirectoriesWithUnparseableNames()
        {
            // The install path can contain non-package directories (e.g. an editor-generated
            // backup folder, a user-dropped file). The helper relies on
            // ExtractPackageIdFromDirName which returns null for unparseable names; those
            // directories must be ignored, not crash the scan and not be deleted.
            Directory.CreateDirectory(Path.Combine(_installPath, "not-a-package"));
            Directory.CreateDirectory(Path.Combine(_installPath, "ReadMe"));
            CreateFakePackageDir("com.IvanMurzak.ReflectorNet", "5.0.0");

            var removed = NuGetPackageInstaller.RemoveStaleSiblingVersions(
                _installPath, "com.IvanMurzak.ReflectorNet", "5.1.1");

            Assert.IsTrue(removed, "The legitimately stale package dir must still be removed.");
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "not-a-package")),
                "Non-package directories must not be touched.");
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "ReadMe")));
        }

        /// <summary>
        /// Creates a fake <c>{Id}.{Version}/</c> directory with a single dummy DLL inside,
        /// plus a sibling <c>.meta</c> file, mirroring the on-disk shape the real resolver
        /// produces. The DLL content is not parsed by the helper; only the filename / extension
        /// matter for any downstream checks (and this helper's scan does not even look at file
        /// contents — it identifies stale siblings purely by directory name).
        /// </summary>
        void CreateFakePackageDir(string id, string version)
        {
            var dirName = $"{id}.{version}";
            var dirPath = Path.Combine(_installPath, dirName);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, $"{id}.dll"), "dummy");
            File.WriteAllText(dirPath + ".meta", "fileFormatVersion: 2\nguid: 00000000000000000000000000000000\n");
        }
    }
}
