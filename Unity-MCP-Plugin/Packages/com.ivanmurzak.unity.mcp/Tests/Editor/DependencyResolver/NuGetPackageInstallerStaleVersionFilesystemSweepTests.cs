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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Coverage for <see cref="NuGetPackageInstaller.RemoveStaleVersionDllsByStem"/>:
    /// the filesystem-driven companion to the manifest-driven
    /// <c>RemoveStaleSiblingVersions</c>. Catches stale
    /// <c>{stem}.&lt;olderVersion&gt;.dll</c> files (and unversioned
    /// <c>{stem}.dll</c> legacy artifacts) that the manifest knows nothing
    /// about — manifest deletion, partial-restore failure, AV quarantine, or
    /// manual disk edits all leave the manifest out of sync with disk, and
    /// without this sweep the resolver would write the new version alongside
    /// the stale one and brick the project with duplicate-assembly errors.
    /// </summary>
    [TestFixture]
    public class NuGetPackageInstallerStaleVersionFilesystemSweepTests
    {
        string _installPath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _installPath = Path.Combine(
                Path.GetTempPath(),
                "UnityMcp-StaleSweep-" + Path.GetRandomFileName());
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
        public void RemoveStaleVersionDllsByStem_DeletesOlderVersionAndItsMeta()
        {
            // Project upgraded com.IvanMurzak.McpPlugin from 6.2.0 → 6.2.1, but
            // the manifest entry was wiped out. The filesystem sweep MUST find
            // the orphan McpPlugin.6.2.0.dll and remove it before extraction
            // writes McpPlugin.6.2.1.dll alongside.
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.0.dll.meta"), "meta");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.6.2.1.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.0.dll.meta")),
                ".meta sidecar must be deleted alongside its DLL.");
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_DeletesUnversionedLegacyDll()
        {
            // Even older legacy: a bare {stem}.dll without a version tail —
            // produced by some pre-flat-layout manual install. Same removal
            // contract: it cannot coexist with the new versioned form.
            File.WriteAllText(Path.Combine(_installPath, "ReflectorNet.dll"), "very-legacy");
            File.WriteAllText(Path.Combine(_installPath, "ReflectorNet.dll.meta"), "meta");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/ReflectorNet.dll", "ReflectorNet.5.1.1.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "5.1.1", packageId: "com.IvanMurzak.ReflectorNet");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "ReflectorNet.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "ReflectorNet.dll.meta")));
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_PreservesCanonicalCurrentVersionFile()
        {
            // The current-version filename must survive the sweep — extraction
            // overwrites it (or alreadyOnDisk short-circuits cleanly).
            var canonical = "System.Text.Json.8.0.5.dll";
            File.WriteAllText(Path.Combine(_installPath, canonical), "current");
            File.WriteAllText(Path.Combine(_installPath, canonical + ".meta"), "meta");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/System.Text.Json.dll", canonical),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "8.0.5", packageId: "System.Text.Json");

            Assert.IsFalse(removed, "Nothing to clean up when only the canonical file is on disk.");
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, canonical)));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, canonical + ".meta")));
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_HandlesMultipleStaleVersionsForSameStem()
        {
            // Multiple stale versions on disk (e.g. user kept upgrading without
            // restart, leaving a chain of unwanted files). All non-canonical
            // versions get swept.
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.1.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.dll"), "stale-unversioned");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.6.2.1.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.1.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.dll")));
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_HandlesMultipleStems_ForMultiDllPackage()
        {
            // A package shipping multiple DLLs (e.g. Microsoft.Bcl.Memory ships
            // System.Memory + System.Buffers + System.Runtime.CompilerServices.Unsafe).
            // Every shipped stem gets its own scan; stale versions of each are
            // removed independently.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.9.0.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "System.Buffers.9.0.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.5.0.0.dll"), "stale");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/System.Memory.dll", "System.Memory.10.0.3.dll"),
                Plan("lib/netstandard2.0/System.Buffers.dll", "System.Buffers.10.0.3.dll"),
                Plan("lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll", "System.Runtime.CompilerServices.Unsafe.10.0.3.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "10.0.3", packageId: "Microsoft.Bcl.Memory");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "System.Memory.9.0.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "System.Buffers.9.0.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.5.0.0.dll")));
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_DoesNotMatchSiblingPackageDllWithLongerStem()
        {
            // Cross-stem boundary: a package shipping McpPlugin.dll must NOT
            // sweep McpPlugin.Common.<v>.dll on disk — that file belongs to
            // a sibling package. The regex requires either ".dll" or a
            // numeric version tail directly after the stem; "Common" is
            // neither, so the regex's stem boundary holds.
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.Common.6.2.0.dll"), "sibling-package");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.Common.6.2.0.dll.meta"), "meta");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.6.2.1.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsFalse(removed);
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "McpPlugin.Common.6.2.0.dll")),
                "DLL belonging to a different package's stem must not be deleted by the McpPlugin sweep.");
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_DoesNotMatchNonNumericTailAfterStem()
        {
            // Defensive: a third-party file the user dropped in like
            // McpPlugin.Foo.dll has a non-numeric tail and must survive the
            // sweep. (The regex demands a numeric-only version tail.)
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.Foo.dll"), "third-party");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.6.2.1.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsFalse(removed);
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "McpPlugin.Foo.dll")));
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_NoOpWhenInstallPathIsEmpty()
        {
            // Brand-new install: nothing on disk yet, sweep is a no-op.
            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.6.2.1.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsFalse(removed);
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_NoOpWhenPlannedSetIsEmpty()
        {
            // Development-only / framework-incompatible package paths produce
            // an empty planned set; the sweep must be a no-op so we don't
            // accidentally start deleting unrelated DLLs.
            File.WriteAllText(Path.Combine(_installPath, "Some.Other.1.0.0.dll"), "unrelated");

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, new List<PlannedDll>(), keepVersion: "1.0.0", packageId: "AnyPackage");

            Assert.IsFalse(removed);
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "Some.Other.1.0.0.dll")));
        }

        static PlannedDll Plan(string entryFullName, string canonicalFileName) =>
            new PlannedDll(entryFullName, canonicalFileName, Path.Combine("/ignored", canonicalFileName));
    }
}
