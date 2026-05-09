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
    /// the per-package filesystem sweep that runs before extraction. With the
    /// unversioned flat layout the canonical filename is just <c>{stem}.dll</c>;
    /// any sibling matching <c>{stem}.{numericVersion}.dll</c> is by definition
    /// a leftover from the pre-unversioned-filename resolver and must be
    /// removed before re-extraction so Unity does not see two assemblies with
    /// the same manifest name.
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
        public void RemoveStaleVersionDllsByStem_DeletesVersionedFilenameLeftoverAndItsMeta()
        {
            // Project upgraded from the pre-unversioned-filename resolver. The
            // old canonical was McpPlugin.6.2.0.dll; the new canonical is
            // McpPlugin.dll. The sweep MUST find the orphan versioned-filename
            // file and remove it before extraction overwrites McpPlugin.dll.
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.0.dll.meta"), "meta");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.0.dll.meta")),
                ".meta sidecar must be deleted alongside its DLL.");
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_PreservesCanonicalUnversionedFile()
        {
            // The canonical {stem}.dll is what extraction will write; the
            // sweep must NOT delete it (whether it's already up-to-date bytes
            // or about to be overwritten).
            var canonical = "System.Text.Json.dll";
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
        public void RemoveStaleVersionDllsByStem_HandlesMultipleVersionedLeftoversForSameStem()
        {
            // A user that bumped the package version repeatedly across a
            // chain of pre-unversioned-filename releases ends up with several
            // {stem}.{version}.dll files. All of them must be swept; only
            // the canonical {stem}.dll survives (re-extracted by the caller).
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.1.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.dll"), "current-canonical");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.dll"),
            };

            var removed = NuGetPackageInstaller.RemoveStaleVersionDllsByStem(
                _installPath, planned, keepVersion: "6.2.1", packageId: "com.IvanMurzak.McpPlugin");

            Assert.IsTrue(removed);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.1.0.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.0.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "McpPlugin.dll")),
                "The canonical unversioned filename must be preserved.");
        }

        [Test]
        public void RemoveStaleVersionDllsByStem_HandlesMultipleStems_ForMultiDllPackage()
        {
            // A package shipping multiple DLLs (e.g. Microsoft.Bcl.Memory ships
            // System.Memory + System.Buffers + System.Runtime.CompilerServices.Unsafe).
            // Every shipped stem gets its own sweep; versioned-filename
            // leftovers of each are removed independently.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.9.0.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "System.Buffers.9.0.0.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "System.Runtime.CompilerServices.Unsafe.5.0.0.dll"), "stale");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/System.Memory.dll", "System.Memory.dll"),
                Plan("lib/netstandard2.0/System.Buffers.dll", "System.Buffers.dll"),
                Plan("lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll", "System.Runtime.CompilerServices.Unsafe.dll"),
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
            // a sibling package. The TryParseInstalledDllName parse yields
            // stem=McpPlugin.Common, which is NOT in originalStems for the
            // McpPlugin-only package.
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.Common.6.2.0.dll"), "sibling-package");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.Common.6.2.0.dll.meta"), "meta");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.dll"),
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
            // Defensive: a third-party file with a non-numeric tail like
            // McpPlugin.Foo.dll must survive (parses to stem=McpPlugin.Foo,
            // not in originalStems for the McpPlugin-only package).
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.Foo.dll"), "third-party");

            var planned = new List<PlannedDll>
            {
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.dll"),
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
                Plan("lib/netstandard2.0/McpPlugin.dll", "McpPlugin.dll"),
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
