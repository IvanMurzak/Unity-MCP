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
using System.Runtime.InteropServices;
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
            // Already-migrated project: only flat-layout (versioned) DLLs and
            // the manifest are present. Migration must short-circuit, not
            // delete anything.
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.10.0.3.dll"), "dummy");
            File.WriteAllText(Path.Combine(_installPath, "System.Memory.10.0.3.dll.meta"), "meta");
            File.WriteAllText(Path.Combine(_installPath, ".nuget-installed.json"), "{}");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.NoLegacyState, result.Outcome);
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "System.Memory.10.0.3.dll")));
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
            File.WriteAllText(Path.Combine(_installPath, "Microsoft.Bcl.Memory.10.0.3.dll"), "flat-already");
            Directory.CreateDirectory(Path.Combine(_installPath, "ReadMe"));
            File.WriteAllText(Path.Combine(_installPath, "ReadMe", "notes.txt"), "user notes");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, result.Outcome);
            Assert.AreEqual(1, result.RemovedDirectories.Count);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "Microsoft.Bcl.Memory.10.0.3.dll")),
                "Flat-layout DLLs already present must not be deleted by migration.");
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "ReadMe")),
                "Non-package directories must not be touched by migration.");
        }

        [Test]
        public void Run_LegacyDirWithSemVerPrerelease_RemovedAsLegacy()
        {
            // NuGet folder names can contain SemVer prerelease / build-metadata
            // suffixes (e.g. `Microsoft.AspNetCore.SignalR.Client.8.0.15-preview`).
            // System.Version.TryParse rejects those tails, so an earlier version
            // of the migration silently left those folders on disk and the user
            // ended up with both the legacy nested DLL and the new flat-layout
            // DLL coexisting — duplicate-assembly compile errors. Pin the
            // SemVer-shape fallback in `ExtractPackageIdFromDirName` so the
            // migration removes them.
            CreateLegacyPackage("Foo.Bar", "1.0.0-preview", "Foo.Bar.dll");
            CreateLegacyPackage("Baz", "2.3.4+build.42", "Baz.dll");
            CreateLegacyPackage("Qux", "1.0.0-rc.1", "Qux.dll");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, result.Outcome);
            Assert.AreEqual(3, result.RemovedDirectories.Count);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Foo.Bar.1.0.0-preview")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Baz.2.3.4+build.42")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Qux.1.0.0-rc.1")));
        }

#if UNITY_EDITOR_WIN
        // Windows-only: this test simulates the abort path that Windows' exclusive
        // file-share lock triggers when a DLL is loaded by another process. Linux
        // and macOS use advisory locking — `FileShare.None` does not prevent
        // `Directory.Delete(recursive: true)` there, so the migration completes
        // successfully and the assertion fails. Unity's CI runs in a Linux Docker
        // container; gating on `UNITY_EDITOR_WIN` keeps the assertion honest
        // without adding a runtime branch in production code.
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
#endif

#if !UNITY_EDITOR_WIN
        // Unix counterpart of Run_FileLock_AbortsAndLeavesLegacyIntact. The migration
        // catches both `IOException` (Windows file-lock) and `UnauthorizedAccessException`
        // (Unix permission denial / antivirus / read-only flags) and treats them
        // identically — both yield `Outcome.AbortedFileLock`. Here we deny write on
        // the install-path parent via `chmod 0o500`, which makes `Directory.Delete`
        // throw `UnauthorizedAccessException` on the final rmdir of the now-empty
        // legacy subdirectory.
        //
        // Self-skips when running as root (e.g. inside the GameCI Unity Docker
        // container), because the kernel bypasses chmod-based denial for uid=0
        // and the test would falsely fail. Local non-root Unix dev / CI agents
        // running as a regular user exercise the abort path for real.
        [Test]
        public void Run_PermissionDenied_AbortsAndLeavesLegacyIntact()
        {
            if (geteuid() == 0)
                Assert.Ignore("chmod-based permission denial is bypassed by the kernel for uid=0; run this test as a non-root user.");

            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");

            LogAssert.Expect(UnityEngine.LogType.Error, new Regex(@"\[NuGet\] Migration to flat layout aborted"));

            const uint READ_EXEC_ONLY = 0b101_000_000; // 0o500 — r-x------
            const uint READ_WRITE_EXEC = 0b111_000_000; // 0o700 — rwx------ (TearDown-friendly)
            if (chmod(_installPath, READ_EXEC_ONLY) != 0)
                Assert.Inconclusive($"chmod {_installPath} -> 0o500 failed (errno={Marshal.GetLastWin32Error()})");

            try
            {
                var result = NuGetLegacyMigration.Run(_installPath);

                Assert.AreEqual(NuGetLegacyMigration.Outcome.AbortedFileLock, result.Outcome);
                Assert.IsNotNull(result.FailedDirectory);
                Assert.IsNotNull(result.FailureMessage);
                // Legacy folder still on disk (possibly emptied of contents, but
                // the directory entry itself remains because we couldn't unlink
                // it from a read-only parent) — recoverable state.
                Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            }
            finally
            {
                // Always restore so [TearDown]'s recursive delete of `_installPath` succeeds.
                chmod(_installPath, READ_WRITE_EXEC);
            }
        }

        [DllImport("libc", SetLastError = true)]
        static extern int chmod(string pathname, uint mode);

        [DllImport("libc", SetLastError = true)]
        static extern uint geteuid();
#endif

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
