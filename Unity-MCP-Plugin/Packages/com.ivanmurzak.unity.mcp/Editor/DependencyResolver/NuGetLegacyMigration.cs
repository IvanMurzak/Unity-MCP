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
        /// Outcome of a <see cref="Run"/> call. <see cref="FirstFailedItem"/>
        /// is the first directory or file the migration could not fully remove
        /// (others get logged); <see cref="RemovedItems"/> lists everything
        /// that did get cleaned up.
        /// </summary>
        public readonly struct Result
        {
            public Outcome Outcome { get; }
            public IReadOnlyList<string> RemovedItems { get; }
            public string? FirstFailedItem { get; }
            public string? FirstFailureMessage { get; }

            public Result(Outcome outcome, IReadOnlyList<string> removedItems, string? firstFailedItem = null, string? firstFailureMessage = null)
            {
                Outcome = outcome;
                RemovedItems = removedItems;
                FirstFailedItem = firstFailedItem;
                FirstFailureMessage = firstFailureMessage;
            }
        }

        /// <summary>
        /// Migrates the install path off pre-#733 layouts to the canonical
        /// unversioned flat layout: removes legacy <c>{Id}.{Version}/</c>
        /// directories AND flat <c>{stem}.{numericVersion}.dll</c> files.
        /// Best-effort — see the class doc for the locked-file recovery
        /// contract.
        /// </summary>
        public static Result Run(string installPath)
        {
            var removed = new List<string>();
            if (!Directory.Exists(installPath))
                return new Result(Outcome.NoLegacyState, removed);

            var legacyDirs = DetectLegacyDirectories(installPath);
            var versionedFiles = DetectVersionedFilenameDlls(installPath);

            if (legacyDirs.Count == 0 && versionedFiles.Count == 0)
                return new Result(Outcome.NoLegacyState, removed);

            Debug.Log($"{Tag} Legacy install detected: {legacyDirs.Count} directories, {versionedFiles.Count} versioned-filename DLLs to migrate to the unversioned flat layout.");

            string? firstFailedItem = null;
            string? firstFailureMessage = null;

            foreach (var dir in legacyDirs)
            {
                Debug.Log($"{Tag} Removing legacy directory: {dir}");
                if (TryRemoveDirectoryRecursive(dir, out var failureMessage))
                {
                    NuGetPluginConfigurator.TryDeleteFile(dir + ".meta");
                    removed.Add(dir);
                    continue;
                }
                firstFailedItem ??= dir;
                firstFailureMessage ??= failureMessage;
                Debug.LogWarning(BuildLockMessage(dir, failureMessage ?? "(unknown)"));
            }

            foreach (var file in versionedFiles)
            {
                Debug.Log($"{Tag} Removing versioned-filename DLL: {Path.GetFileName(file)} (version moved into the manifest).");
                if (TryDeleteVersionedFile(file, out var failureMessage))
                {
                    removed.Add(file);
                    continue;
                }
                firstFailedItem ??= file;
                firstFailureMessage ??= failureMessage;
                Debug.LogWarning(BuildLockMessage(file, failureMessage ?? "(unknown)"));
            }

            if (firstFailedItem != null)
                return new Result(Outcome.AbortedFileLock, removed, firstFailedItem, firstFailureMessage);

            Debug.Log($"{Tag} Migration complete: {removed.Count} legacy items removed; unversioned flat layout active.");
            return new Result(Outcome.Migrated, removed);
        }

        static List<string> DetectLegacyDirectories(string installPath)
        {
            var result = new List<string>();
            foreach (var dir in Directory.GetDirectories(installPath))
            {
                var dirName = Path.GetFileName(dir);
                if (NuGetPackageInstaller.ExtractPackageIdFromDirName(dirName) != null)
                    result.Add(dir);
            }
            return result;
        }

        /// <summary>
        /// Returns every <c>{stem}.{numericVersion}.dll</c> at the install
        /// root — these are flat-layout files written by an earlier resolver
        /// that embedded the package version in the filename. With the
        /// version now living in the manifest, every such file is by
        /// definition stale and the canonical replacement is <c>{stem}.dll</c>.
        /// </summary>
        static List<string> DetectVersionedFilenameDlls(string installPath)
        {
            var result = new List<string>();
            foreach (var dllPath in Directory.GetFiles(installPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(dllPath);
                if (NuGetInstallManifest.TryParseInstalledDllName(fileName, out _, out _))
                    result.Add(dllPath);
            }
            return result;
        }

        static bool TryDeleteVersionedFile(string filePath, out string? failureMessage)
        {
            failureMessage = null;
            try
            {
                File.Delete(filePath);
                NuGetPluginConfigurator.TryDeleteFile(filePath + ".meta");
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                failureMessage = ex.Message;
                NuGetPluginConfigurator.DisableImporter(filePath);
                return false;
            }
        }

        /// <summary>
        /// Best-effort recursive delete; returns true only when the directory
        /// is gone after the call. Locked file → disable importer so the next
        /// reload unloads it and the next migration pass finishes.
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

        static string BuildLockMessage(string directory, string reason)
        {
            return
                $"{Tag} Could not fully remove legacy install directory:\n" +
                $"  {directory}\n\n" +
                $"  Reason: {reason}\n\n" +
                "On Windows this typically means a DLL inside the legacy folder is loaded " +
                "into the editor AppDomain. The PluginImporter for the locked DLL has been " +
                "disabled so the next domain reload will unload it; the next migration pass " +
                "will retry the deletion. Restart Unity if the warning persists.";
        }
    }
}
