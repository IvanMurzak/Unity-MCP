/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// One-shot migration from the legacy per-package NuGet install layout
    /// (<c>{Id}.{Version}/{Dll}.dll</c>) to the flat versioned-filename layout
    /// (<c>{stem}.{packageVersion}.dll</c>).
    ///
    /// Runs at the start of every <see cref="NuGetPackageRestorer.Restore"/>
    /// pass. After the first successful migration the install directory has no
    /// legacy <c>{Id}.{Version}/</c> directories left, so subsequent runs
    /// short-circuit at the detection probe.
    ///
    /// Failure mode (Windows file lock — see issue #733): if a legacy DLL is
    /// loaded into the editor AppDomain, <c>Directory.Delete</c> may throw
    /// <see cref="IOException"/>. The migration aborts the entire restore
    /// cycle in that case (see <see cref="MigrationAbortedException"/>) — a
    /// partial migration would leave the project in the duplicate-assembly
    /// state we're explicitly trying to prevent.
    ///
    /// <para>
    /// Asset GUIDs in legacy <c>.meta</c> files are intentionally NOT preserved
    /// onto the new flat-layout meta files: these DLLs are referenced via
    /// asmdef <c>precompiledReferences</c> (filename-based) rather than via
    /// GUID, so the link survives the rename without the migration carrying
    /// per-meta GUID state forward. Unity regenerates fresh meta files on the
    /// next asset import.
    /// </para>
    ///
    /// <para>
    /// <b>Plugin-exclusive directory contract:</b> the install path
    /// (<see cref="NuGetConfig.InstallPath"/>, by default
    /// <c>Assets/Plugins/NuGet/</c>) MUST be reserved exclusively for this
    /// resolver. Any directory directly underneath whose name parses as
    /// <c>{Id}.{System.Version}</c> is treated as a legacy NuGet artifact and
    /// removed with <c>Directory.Delete(recursive: true)</c>. A user-authored
    /// folder dropped here whose name happens to match that pattern (e.g.
    /// <c>MyTools.1.0/</c>) would be silently destroyed. Do not co-locate
    /// non-NuGet content with the install path.
    /// </para>
    /// </summary>
    static class NuGetLegacyMigration
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Result code returned by <see cref="Run"/>.
        /// </summary>
        public enum Outcome
        {
            /// <summary>No legacy directories were found; nothing to do.</summary>
            NoLegacyState,

            /// <summary>One or more legacy directories were detected and removed.</summary>
            Migrated,

            /// <summary>
            /// A legacy directory could not be removed because of a file lock
            /// or other I/O error. The caller MUST abort the entire restore
            /// cycle without writing any new DLLs.
            /// </summary>
            AbortedFileLock,
        }

        /// <summary>
        /// Outcome of a successful run that returned <see cref="Outcome.Migrated"/>.
        /// </summary>
        public readonly struct Result
        {
            public Outcome Outcome { get; }
            public IReadOnlyList<string> RemovedDirectories { get; }
            public string? FailedDirectory { get; }
            public string? FailureMessage { get; }

            public Result(Outcome outcome, IReadOnlyList<string> removedDirectories, string? failedDirectory = null, string? failureMessage = null)
            {
                Outcome = outcome;
                RemovedDirectories = removedDirectories;
                FailedDirectory = failedDirectory;
                FailureMessage = failureMessage;
            }
        }

        /// <summary>
        /// Detects and removes legacy <c>{Id}.{Version}/</c> directories under
        /// <paramref name="installPath"/>. The flat-layout DLLs (if any) and
        /// the manifest are left untouched.
        ///
        /// <para>
        /// On a clean install or on the second run after a successful first
        /// migration this returns <see cref="Outcome.NoLegacyState"/> and does
        /// no I/O beyond the directory enumeration.
        /// </para>
        ///
        /// <para>
        /// On a file-lock failure, this returns <see cref="Outcome.AbortedFileLock"/>
        /// with the offending directory and message. The caller MUST abort the
        /// rest of the restore — the legacy DLLs are still on disk and writing
        /// flat-layout DLLs alongside them would brick the project with
        /// duplicate-assembly compile errors.
        /// </para>
        /// </summary>
        public static Result Run(string installPath)
        {
            var removed = new List<string>();
            if (!Directory.Exists(installPath))
                return new Result(Outcome.NoLegacyState, removed);

            // First pass: detect legacy directories without deleting anything.
            // Splitting detection from deletion lets us short-circuit on a
            // clean install with a single Directory.GetDirectories call.
            var legacyDirs = new List<string>();
            foreach (var dir in Directory.GetDirectories(installPath))
            {
                var dirName = Path.GetFileName(dir);
                var packageId = NuGetPackageInstaller.ExtractPackageIdFromDirName(dirName);
                if (packageId == null)
                    continue;
                legacyDirs.Add(dir);
            }

            if (legacyDirs.Count == 0)
                return new Result(Outcome.NoLegacyState, removed);

            Debug.Log($"{Tag} Legacy install detected: {legacyDirs.Count} directories to migrate to flat layout (issue #733).");

            foreach (var dir in legacyDirs)
            {
                var dirName = Path.GetFileName(dir);
                Debug.Log($"{Tag} Migrating legacy install: removing {dir} (replaced by flat layout in #733)");

                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException ex)
                {
                    // Common on Windows when a DLL inside the legacy directory
                    // is loaded into the running editor AppDomain.
                    var message = BuildLockMessage(dir, ex);
                    Debug.LogError(message);
                    return new Result(Outcome.AbortedFileLock, removed, dir, message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Antivirus / permissions / read-only flags. Same recovery
                    // story as a file lock — abort, surface, retry next reload.
                    var message = BuildLockMessage(dir, ex);
                    Debug.LogError(message);
                    return new Result(Outcome.AbortedFileLock, removed, dir, message);
                }

                var metaFile = dir + ".meta";
                if (File.Exists(metaFile))
                {
                    try { File.Delete(metaFile); }
                    catch (Exception ex)
                    {
                        // Losing a .meta file isn't load-bearing on the
                        // upgrade path — Unity will regenerate it. Log and
                        // keep going so the rest of the migration can finish.
                        Debug.LogWarning($"{Tag} Failed to delete legacy meta '{metaFile}': {ex.Message}");
                    }
                }

                removed.Add(dir);
            }

            Debug.Log($"{Tag} Migration complete: {removed.Count} legacy directories removed; flat layout active");
            return new Result(Outcome.Migrated, removed);
        }

        static string BuildLockMessage(string directory, Exception ex)
        {
            return
                $"{Tag} Migration to flat layout aborted — could not remove legacy install directory:\n" +
                $"  {directory}\n\n" +
                $"  Reason: {ex.Message}\n\n" +
                "On Windows this typically means a DLL inside the legacy folder is loaded into " +
                "the editor AppDomain. Restart Unity to drop the AppDomain and the migration " +
                "will retry on the next domain reload. The legacy folders are intentionally left " +
                "intact; no flat-layout DLLs will be written until migration succeeds (otherwise " +
                "the project would end up with duplicate copies of every assembly).";
        }
    }
}
