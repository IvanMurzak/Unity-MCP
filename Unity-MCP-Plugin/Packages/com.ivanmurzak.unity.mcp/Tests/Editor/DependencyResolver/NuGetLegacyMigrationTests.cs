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
            Assert.AreEqual(0, result.RemovedItems.Count);
        }

        [Test]
        public void Run_NoLegacyState_ReturnsNoLegacyState_OnUnversionedFlatInstall()
        {
            // Steady-state install in the unversioned flat layout: no legacy
            // {Id}.{Version}/ directories, no versioned-filename {stem}.{v}.dll
            // siblings — the migration must short-circuit and touch nothing.
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
            Assert.AreEqual(2, result.RemovedItems.Count);
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
            Assert.AreEqual(0, second.RemovedItems.Count);
        }

        [Test]
        public void Run_MixedLegacyState_MigratesLegacyDirsAndVersionedFiles_LeavesUnrelatedContent()
        {
            // Mixed install: a legacy {Id}.{Version}/ folder, a flat
            // versioned-filename DLL, and unrelated user content. The migration
            // sweeps both legacy artifacts in one pass and leaves the user's
            // directory + unversioned canonical untouched.
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");
            File.WriteAllText(Path.Combine(_installPath, "Microsoft.Bcl.Memory.10.0.3.dll"), "flat-versioned-leftover");
            File.WriteAllText(Path.Combine(_installPath, "Microsoft.Bcl.Memory.dll"), "canonical");
            Directory.CreateDirectory(Path.Combine(_installPath, "ReadMe"));
            File.WriteAllText(Path.Combine(_installPath, "ReadMe", "notes.txt"), "user notes");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, result.Outcome);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "Microsoft.Bcl.Memory.10.0.3.dll")),
                "Versioned-filename flat DLLs are now legacy and must be swept.");
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "Microsoft.Bcl.Memory.dll")),
                "Unversioned canonical filenames must not be touched by migration.");
            Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "ReadMe")),
                "Non-package directories must not be touched by migration.");
        }

        [Test]
        public void Run_VersionedFilenameDllsAtRoot_AreSwept()
        {
            // Migration from the pre-unversioned-filename resolver: any flat
            // {stem}.{numericVersion}.dll at the install root is by
            // definition stale once the canonical filename is just {stem}.dll.
            // The migration sweeps them in the same pass that removes
            // legacy {Id}.{Version}/ directories.
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.1.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "McpPlugin.6.2.1.dll.meta"), "meta");
            File.WriteAllText(Path.Combine(_installPath, "System.Text.Json.8.0.5.dll"), "stale");
            File.WriteAllText(Path.Combine(_installPath, "System.Text.Json.8.0.5.dll.meta"), "meta");
            // Unversioned canonicals must NOT be touched.
            File.WriteAllText(Path.Combine(_installPath, "ReadMe.dll"), "user-content");
            File.WriteAllText(Path.Combine(_installPath, "Foo.dll"), "canonical");

            var result = NuGetLegacyMigration.Run(_installPath);

            Assert.AreEqual(NuGetLegacyMigration.Outcome.Migrated, result.Outcome);
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.1.dll")));
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "McpPlugin.6.2.1.dll.meta")),
                ".meta sidecar must be deleted alongside the DLL.");
            Assert.IsFalse(File.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "ReadMe.dll")),
                "Unversioned filenames are the new canonical and must be preserved.");
            Assert.IsTrue(File.Exists(Path.Combine(_installPath, "Foo.dll")));
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
            Assert.AreEqual(3, result.RemovedItems.Count);
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Foo.Bar.1.0.0-preview")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Baz.2.3.4+build.42")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "Qux.1.0.0-rc.1")));
        }

#if UNITY_EDITOR_WIN
        // Windows-only: simulates a `FileShare.None` lock that Unity holds on a
        // DLL it has loaded into the editor AppDomain. Linux/macOS use advisory
        // locking — `FileShare.None` does not prevent `File.Delete` there — so
        // gating on `UNITY_EDITOR_WIN` keeps the assertion honest. The
        // post-best-effort migration no longer aborts the entire restore on a
        // single locked file: the call returns `AbortedFileLock` to surface
        // that some directory is still on disk, but the caller may continue
        // safely (the locked file's PluginImporter is disabled so the next
        // domain reload unloads it and the next migration pass cleans it up).
        [Test]
        public void Run_FileLock_LeavesBlockedFolderIntactAndReportsAbortedFileLock()
        {
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");
            var lockedDll = Path.Combine(_installPath, "System.Text.Json.8.0.5", "System.Text.Json.dll");

            // Best-effort migration logs a warning (not an error) for blocked folders.
            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex(@"\[NuGet\] Could not fully remove legacy install directory"));

            using (var lockHandle = new FileStream(lockedDll, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var result = NuGetLegacyMigration.Run(_installPath);

                Assert.AreEqual(NuGetLegacyMigration.Outcome.AbortedFileLock, result.Outcome);
                Assert.IsNotNull(result.FirstFailedItem);
                Assert.IsNotNull(result.FirstFailureMessage);
                // Blocked folder still on disk — the next reload picks it back up.
                Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")));
            }
        }

        [Test]
        public void Run_FileLock_OneFolderBlocked_OtherFoldersAreStillRemoved()
        {
            // Mixed state: a blocked folder must NOT prevent unblocked folders
            // from being cleaned up — that's the whole point of best-effort.
            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");
            CreateLegacyPackage("R3", "1.3.0", "R3.dll");
            var lockedDll = Path.Combine(_installPath, "System.Text.Json.8.0.5", "System.Text.Json.dll");

            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex(@"\[NuGet\] Could not fully remove legacy install directory"));

            using (var lockHandle = new FileStream(lockedDll, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var result = NuGetLegacyMigration.Run(_installPath);

                Assert.AreEqual(NuGetLegacyMigration.Outcome.AbortedFileLock, result.Outcome);
                Assert.AreEqual(1, result.RemovedItems.Count,
                    "The unblocked folder must still be removed even though a sibling folder was locked.");
                Assert.IsTrue(Directory.Exists(Path.Combine(_installPath, "System.Text.Json.8.0.5")),
                    "Blocked folder remains on disk for retry on the next pass.");
                Assert.IsFalse(Directory.Exists(Path.Combine(_installPath, "R3.1.3.0")),
                    "Unblocked folder must have been removed.");
            }
        }
#endif

#if !UNITY_EDITOR_WIN
        // Unix counterpart of Run_FileLock_LeavesBlockedFolderIntactAndReportsAbortedFileLock.
        // The migration catches both `IOException` (Windows file-lock) and
        // `UnauthorizedAccessException` (Unix permission denial / antivirus /
        // read-only flags) inside its per-file delete loop. Here we deny write
        // on the install-path parent via `chmod 0o500`, which makes
        // `File.Delete` and the trailing `Directory.Delete` both throw
        // `UnauthorizedAccessException`. Self-skips when running as root.
        [Test]
        public void Run_PermissionDenied_LeavesBlockedFolderIntactAndReportsAbortedFileLock()
        {
            if (geteuid() == 0)
                Assert.Ignore("chmod-based permission denial is bypassed by the kernel for uid=0; run this test as a non-root user.");

            CreateLegacyPackage("System.Text.Json", "8.0.5", "System.Text.Json.dll");

            LogAssert.Expect(UnityEngine.LogType.Warning, new Regex(@"\[NuGet\] Could not fully remove legacy install directory"));

            const uint READ_EXEC_ONLY = 0b101_000_000; // 0o500 — r-x------
            const uint READ_WRITE_EXEC = 0b111_000_000; // 0o700 — rwx------ (TearDown-friendly)
            if (chmod(_installPath, READ_EXEC_ONLY) != 0)
                Assert.Inconclusive($"chmod {_installPath} -> 0o500 failed (errno={Marshal.GetLastWin32Error()})");

            try
            {
                var result = NuGetLegacyMigration.Run(_installPath);

                Assert.AreEqual(NuGetLegacyMigration.Outcome.AbortedFileLock, result.Outcome);
                Assert.IsNotNull(result.FirstFailedItem);
                Assert.IsNotNull(result.FirstFailureMessage);
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
