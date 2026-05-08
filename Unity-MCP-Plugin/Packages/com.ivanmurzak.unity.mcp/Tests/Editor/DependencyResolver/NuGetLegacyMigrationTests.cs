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
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine.TestTools;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.DependencyResolverTests
{
    /// <summary>
    /// Coverage for issue #733's mandatory legacy → flat-layout migration. Every
    /// existing user upgrading to the new resolver still has DLLs sitting under
    /// <c>Assets/Plugins/NuGet/{Id}.{Version}/</c> from a pre-fix install. The
    /// migration MUST delete those folders before any flat-layout DLL is
    /// written, in the same restore cycle, otherwise the project ends up with
    /// duplicate copies of every assembly and Unity's compiler errors with
    /// CS0436 / CS0433.
    /// </summary>
    [TestFixture]
    public class NuGetLegacyMigrationTests
    {
        string _installPath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _installPath = Path.Combine(
                Path.GetTempPath(),
                "UnityMcp-Migration-" + Path.GetRandomFileName());
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
        public void Run_NoLegacyState_ReturnsNoLegacyState_OnEmptyInstallPath()
        {
            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.NoLegacyState, result.Outcome);
            Assert.AreEqual(0, result.RemovedDirectories.Count);
        }

        [Test]
        public void Run_NoLegacyState_ReturnsNoLegacyState_OnFlatOnlyInstall()
        {
            // Already-migrated project: only flat-layout DLLs and the manifest
            // are present. Migration must short-circuit, not delete anything.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.dll.meta"), "meta");
            File.WriteAllText(Path.Combine(_installPath, ".nuget-installed.json"), "{}");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.NoLegacyState, result.Outcome);
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "System.Memory.dll")));
        }

        [Test]
        public void Run_HappyPath_RemovesLegacyDirectoriesAndMetas()
        {
            // Seed several legacy directories: single-DLL package and a multi-DLL package.
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");
            CreateLegacyPackage("Microsoft.Bcl.Memory", "10.0.3",
                "System.Memory.dll", "System.Buffers.dll", "System.Runtime.CompilerServices.Unsafe.dll");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, result.Outcome);
            Assert.AreEqual(2, result.RemovedDirectories.Count);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5.meta")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Microsoft.Bcl.Memory.10.0.3")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "Microsoft.Bcl.Memory.10.0.3.meta")));
        }

        [Test]
        public void Run_Idempotent_SecondRunIsNoOp()
        {
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");

            var first = NuGetLegacyMigration.Run(_installPath);
            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, first.Outcome);

            var second = NuGetLegacyMigration.Run(_installPath);
            Assert.AreEqual(NuGetLegacyMigration.Outcome.NoLegacyState, second.Outcome);
            Assert.AreEqual(0, second.RemovedDirectories.Count);
        }

        [Test]
        public void Run_PartialLegacyState_MigratesOnlyLegacyDirsAndIgnoresFlatDllsOrUnrelatedDirectories()
        {
            // Mixed install: some legacy folders, some flat-layout DLLs already
            // present, plus a non-package directory the user dropped in. The
            // migration must remove only the legacy folders.
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");
            File.WriteAllText(Path.Combine(_installPath, "Microsoft.Bcl.Memory.dll"), "flat-already");
            Directory.CreateDirectory(Path.Combine(_installPath, "ReadMe"));
            File.WriteAllText(Path.Combine(_installPath, "ReadMe", "notes.txt"), "user notes");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, result.Outcome);
            Assert.AreEqual(1, result.RemovedDirectories.Count);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "Microsoft.Bcl.Memory.dll")),
                "Flat-layout DLLs already present must not be deleted by migration.");
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "ReadMe")),
                "Non-package directories must not be touched by migration.");
        }

        [Test]
        public void Run_FileLock_AbortsAndLeavesLegacyIntact()
        {
            // Simulate a Windows file-lock failure. The migration must abort,
            // surface the failure, and leave the legacy directory intact so the
            // project is in a recoverable state — not partially migrated.
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");
            var lockedDll = Path.Combine(_installPath, "System.Text.Json.8.0.5", "System.Text.Json.dll");

            // The migration intentionally surfaces the abort via Debug.LogError
            // so the user sees it in the Unity Console. Unity's test framework
            // treats ANY error log as a test failure unless it's explicitly
            // declared expected.
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex(@"\[NuGet\] Migration to flat layout aborted"));

            using (var lockHandle = new FileStream(lockedDll, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var result = NuGetLegacyMigration.Run(_installPath);

                Assert.AreEqual(NuGetLegacyMigration.Outcome.AbortedFileLock, result.Outcome);
                Assert.IsNotNull(result.FailedDirectory);
                Assert.IsNotNull(result.FailureMessage);
                // Legacy folder still on disk — recoverable state.
                Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            }
        }

        /// <summary>
        /// Creates a fake legacy <c>{Id}.{Version}/</c> directory with the
        /// given DLL filenames inside, plus the sibling <c>.meta</c>.
        /// </summary>
        void CreateLegacyPackage(string id, string version, params string[] dllNames)
        {
            var dirName = $"{id}.{version}";
            var dirPath = Path.Combine(_installPath, dirName);
            Directory.CreateDirectory(dirPath);
            foreach (var dll in dllNames)
                File.WriteAllText(Path.Combine(dirPath, dll), "dummy");
            File.WriteAllText(dirPath + ".meta", "fileFormatVersion: 2\nguid: 00000000000000000000000000000000\n");
        }
    }
}
