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
    /// loaded into the editor AppDomain, <c>File.Delete</c> throws
    /// <see cref="IOException"/>. The migration is best-effort per file —
    /// the locked file's <see cref="PluginImporter"/> is disabled so the next
    /// domain reload unloads the assembly and unlocks the file, then the next
    /// migration pass deletes it. The caller proceeds with the rest of the
    /// restore regardless: even if a legacy folder still survives, the
    /// downstream stale-flat sweep + extraction can still make progress on
    /// whatever the migration freed up.
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
            /// At least one legacy directory could not be fully removed
            /// because of a file lock or other I/O error. The migration has
            /// already done what it could (deleted any unlocked siblings,
            /// disabled the PluginImporter on locked files); the caller may
            /// proceed and the next migration pass will pick up the rest.
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
        /// <paramref name="installPath"/>. Flat-layout DLLs and the manifest
        /// are left untouched.
        ///
        /// <para>
        /// Best-effort per-file deletion: a single locked DLL inside one
        /// legacy folder no longer aborts the entire migration. Files that
        /// can be deleted are deleted; files that can't (Windows
        /// <c>FileShare.None</c> lock when Unity has the assembly loaded into
        /// the editor AppDomain, antivirus quarantine, read-only flags) are
        /// logged and have their <c>PluginImporter</c> disabled so the next
        /// domain reload unloads them and unlocks the file — the next
        /// migration pass then completes the deletion. The caller may always
        /// continue past this method; the downstream stale-flat sweep
        /// (<see cref="NuGetPackageInstaller.RemoveStaleVersionDllsByStem"/>)
        /// and extraction can still make progress on whatever the migration
        /// freed up.
        /// </para>
        ///
        /// <para>
        /// Outcomes: <see cref="Outcome.NoLegacyState"/> when no legacy
        /// directory was found; <see cref="Outcome.Migrated"/> when every
        /// legacy directory was fully removed;
        /// <see cref="Outcome.AbortedFileLock"/> when at least one directory
        /// could not be fully removed. <see cref="Result.FailedDirectory"/>
        /// reports the first such directory; the warning log enumerates them
        /// all.
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

            string? firstFailedDir = null;
            string? firstFailureMessage = null;

            foreach (var dir in legacyDirs)
            {
                Debug.Log($"{Tag} Migrating legacy install: removing {dir} (replaced by flat layout in #733)");

                if (TryRemoveDirectoryRecursive(dir, out var failureMessage))
                {
                    TryDeleteFile(dir + ".meta");
                    removed.Add(dir);
                    continue;
                }

                if (firstFailedDir == null)
                {
                    firstFailedDir = dir;
                    firstFailureMessage = failureMessage;
                }
                Debug.LogWarning(BuildLockMessage(dir, failureMessage));
            }

            if (firstFailedDir != null)
                return new Result(Outcome.AbortedFileLock, removed, firstFailedDir, firstFailureMessage);

            Debug.Log($"{Tag} Migration complete: {removed.Count} legacy directories removed; flat layout active");
            return new Result(Outcome.Migrated, removed);
        }

        /// <summary>
        /// Best-effort recursive delete that does not throw. Returns true only
        /// when the directory is gone after the call. On a per-file
        /// IOException / UnauthorizedAccessException, disables the file's
        /// <see cref="PluginImporter"/> (when applicable) so the next domain
        /// reload unloads it and the next migration pass can finish — without
        /// this, a single locked DLL inside an otherwise-deletable folder
        /// would keep the user pinned in the duplicate-assembly state forever.
        /// </summary>
        static bool TryRemoveDirectoryRecursive(string dir, out string? failureMessage)
        {
            failureMessage = null;

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                if (!TryRemoveDirectoryRecursive(subDir, out var subFailure))
                    failureMessage ??= subFailure;
            }

            foreach (var file in Directory.GetFiles(dir))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    failureMessage ??= ex.Message;
                    NuGetPluginConfigurator.DisableImporter(file);
                }
            }

            try
            {
                Directory.Delete(dir);
                return true;
            }
            catch (Exception ex)
            {
                failureMessage ??= ex.Message;
                return false;
            }
        }

        static void TryDeleteFile(string path)
        {
            if (!File.Exists(path))
                return;
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Failed to delete '{path}': {ex.Message}");
            }
        }

        static string BuildLockMessage(string directory, string? reason)
        {
            return
                $"{Tag} Could not fully remove legacy install directory:\n" +
                $"  {directory}\n\n" +
                (reason != null ? $"  Reason: {reason}\n\n" : "") +
                "On Windows this typically means a DLL inside the legacy folder is loaded " +
                "into the editor AppDomain. The PluginImporter for the locked DLL has been " +
                "disabled so the next domain reload will unload it; the next migration pass " +
                "will retry the deletion. Restart Unity if the warning persists.";
        }
    }
}
