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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Configures PluginImporter settings for NuGet DLLs.
    ///
    /// Handles four cases:
    ///   1. Unity provides the DLL + we need it in builds → include in builds, exclude from editor
    ///   2. Unity provides the DLL + editor-only → disable entirely
    ///   3. We provide the DLL + we need it in builds → include everywhere
    ///   4. We provide the DLL + editor-only → editor only
    ///
    /// Case 1 is critical: assemblies like System.Diagnostics.DiagnosticSource are
    /// available in the Unity Editor but NOT included in player builds automatically.
    /// Our NuGet copy must be included in builds while excluded from editor to avoid duplicates.
    ///
    /// Since #733: DLLs sit FLAT under <see cref="NuGetConfig.InstallPath"/> with the
    /// versioned filename pattern <c>{stem}.{packageVersion}.dll</c>. The package → DLL
    /// owner mapping comes from <see cref="NuGetInstallManifest"/> (we can't infer the
    /// package ID from the on-disk path anymore — every DLL sits in the same folder).
    /// </summary>
    static class NuGetPluginConfigurator
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Configures PluginImporter for every DLL recorded in the NuGet install manifest.
        /// Called after packages are installed/restored.
        /// </summary>
        public static void ConfigureAll()
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return;

            var manifest = NuGetInstallManifest.Load(NuGetConfig.InstallPath);

            // Batch importer changes so Unity performs a single reimport pass at the end
            // instead of one reimport per DLL (which was dominating editor startup time
            // on projects with many NuGet packages).
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (packageId, entry) in manifest.Packages)
                {
                    var includeInBuild = ShouldIncludeInBuild(packageId);
                    foreach (var dll in entry.Dlls)
                    {
                        var dllPath = Path.Combine(NuGetConfig.InstallPath, dll);
                        if (!File.Exists(dllPath))
                            continue;

                        // Convert to Unity asset path (forward slashes, relative to project)
                        var assetPath = dllPath.Replace('\\', '/');
                        ConfigureDll(assetPath, includeInBuild);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Configures a single DLL's PluginImporter settings.
        /// </summary>
        public static void ConfigureDll(string assetPath, bool includeInBuild)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
                return;

            // The DLL filename carries the package version as a tail segment
            // (e.g. "System.Memory.10.0.3.dll"). Strip it so the assembly-
            // resolver lookup matches the assembly's manifest name, not the
            // on-disk filename.
            var fileName = Path.GetFileName(assetPath);
            var assemblyName = NuGetInstallManifest.TryParseInstalledDllName(fileName, out var stem, out _) && stem != null
                ? stem
                : Path.GetFileNameWithoutExtension(assetPath);

            var unityProvidesIt = UnityAssemblyResolver.IsAlreadyImported(assemblyName);

            bool anyPlatform;
            bool excludeEditor;
            bool editorOnly;

            if (unityProvidesIt && includeInBuild)
            {
                // Unity provides this DLL in the editor, but builds need our copy.
                anyPlatform = true;
                excludeEditor = true;
                editorOnly = false;
            }
            else if (unityProvidesIt)
            {
                // Unity provides it and we don't need it in builds — disable entirely.
                anyPlatform = false;
                excludeEditor = false;
                editorOnly = false;
            }
            else if (includeInBuild)
            {
                // Runtime DLL not provided by Unity: include everywhere.
                anyPlatform = true;
                excludeEditor = false;
                editorOnly = false;
            }
            else
            {
                // Editor-only DLL not provided by Unity.
                anyPlatform = false;
                excludeEditor = false;
                editorOnly = true;
            }

            // Check if settings need to change
            var currentAnyPlatform = importer.GetCompatibleWithAnyPlatform();
            var currentEditor = importer.GetCompatibleWithEditor();
            var currentExcludeEditor = importer.GetExcludeEditorFromAnyPlatform();

            // When Any Platform is on, the Editor flag must also track !excludeEditor —
            // otherwise a stale Editor=0 left over from Unity's initial import silently
            // survives (this was the cause of "Unloading broken assembly" on startup).
            var expectedEditor = anyPlatform ? !excludeEditor : editorOnly;
            var needsChange = currentAnyPlatform != anyPlatform
                           || currentExcludeEditor != excludeEditor
                           || currentEditor != expectedEditor;

            if (!needsChange)
                return;

            if (anyPlatform)
            {
                importer.SetCompatibleWithAnyPlatform(true);
                importer.SetExcludeEditorFromAnyPlatform(excludeEditor);
                // Explicitly sync the individual Editor platform flag. Unity's initial import
                // sometimes leaves Editor at enabled=0 even when Any Platform is on without
                // Exclude Editor; without this call, the stale 0 persists in the .meta and
                // Editor-side loading fails (e.g., "Unloading broken assembly ..." for DLLs
                // whose transitive deps are also editor-disabled).
                importer.SetCompatibleWithEditor(!excludeEditor);
            }
            else
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(editorOnly);
            }

            importer.SaveAndReimport();
            Debug.Log($"{Tag} Configured '{assemblyName}': anyPlatform={anyPlatform}, excludeEditor={excludeEditor}, editorOnly={editorOnly}");
        }

        /// <summary>
        /// Sets the file's <see cref="PluginImporter"/> compatibility to "no
        /// platforms" so Unity unloads the DLL on the next domain reload,
        /// freeing the OS file handle. Used by the legacy-folder migration
        /// and the stale-flat sweep to recover from the case where Unity has
        /// the assembly loaded into the editor AppDomain and the file is
        /// therefore locked. Safe to call on non-DLL files or paths Unity
        /// hasn't imported (no-op).
        /// </summary>
        public static void DisableImporter(string filePath)
        {
            if (!filePath.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
                return;

            // Asset paths use forward slashes and are project-relative;
            // Windows Path APIs may have inserted '\' so normalise here.
            var assetPath = filePath.Replace('\\', '/');

            try
            {
                if (!(AssetImporter.GetAtPath(assetPath) is PluginImporter importer))
                    return;

                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SaveAndReimport();

                Debug.Log($"{Tag} Disabled PluginImporter for locked '{assetPath}'; deletion will be retried after the next domain reload.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Could not disable PluginImporter for '{assetPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Best-effort <see cref="File.Delete"/> that swallows missing-file
        /// races and logs a warning on any other failure. Shared between the
        /// migration and per-package install paths so the two surfaces don't
        /// drift on what counts as "best effort".
        /// </summary>
        internal static void TryDeleteFile(string path)
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

        /// <summary>
        /// Determines if a DLL should be included in game builds based on its
        /// owning package ID. Configured packages use their IncludeInBuild flag.
        /// Transitive dependencies default to included (runtime packages depend on them).
        /// </summary>
        static bool ShouldIncludeInBuild(string packageId)
        {
            foreach (var package in NuGetConfig.Packages)
            {
                if (string.Equals(packageId, package.Id, System.StringComparison.OrdinalIgnoreCase))
                    return package.IncludeInBuild;
            }

            // Transitive dependency — include in builds by default.
            return true;
        }
    }
}
