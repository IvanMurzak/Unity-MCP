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
    /// </summary>
    static class NuGetPluginConfigurator
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Configures PluginImporter for all DLLs in the NuGet install directory.
        /// Called after packages are installed/restored.
        /// </summary>
        public static void ConfigureAll()
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return;

            var dlls = Directory.GetFiles(NuGetConfig.InstallPath, "*.dll", SearchOption.AllDirectories);

            // Batch importer changes so Unity performs a single reimport pass at the end
            // instead of one reimport per DLL (which was dominating editor startup time
            // on projects with many NuGet packages).
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var dllPath in dlls)
                {
                    // Convert to Unity asset path (forward slashes, relative to project)
                    var assetPath = dllPath.Replace('\\', '/');
                    ConfigureDll(assetPath);
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
        public static void ConfigureDll(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
                return;

            var dllName = Path.GetFileNameWithoutExtension(assetPath);
            var unityProvidesIt = UnityAssemblyResolver.IsAlreadyImported(dllName);
            var includeInBuild = ShouldIncludeInBuild(assetPath);

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
            Debug.Log($"{Tag} Configured '{dllName}': anyPlatform={anyPlatform}, excludeEditor={excludeEditor}, editorOnly={editorOnly}");
        }

        /// <summary>
        /// Determines if a DLL should be included in game builds.
        /// Explicitly configured packages use their IncludeInBuild flag.
        /// Transitive dependencies default to included (runtime packages depend on them).
        /// </summary>
        static bool ShouldIncludeInBuild(string dllPath)
        {
            var dirName = Path.GetFileName(Path.GetDirectoryName(dllPath));
            if (dirName == null)
                return true;

            // Extract the package ID from the directory name (e.g., "System.Text.Json.8.0.5" → "System.Text.Json")
            // so we match the exact package ID rather than any prefix (which would confuse
            // "Microsoft.Extensions.Logging" with "Microsoft.Extensions.Logging.Abstractions").
            var extractedId = NuGetPackageInstaller.ExtractPackageIdFromDirName(dirName);
            if (extractedId == null)
                return true;

            foreach (var package in NuGetConfig.Packages)
            {
                if (string.Equals(extractedId, package.Id, System.StringComparison.OrdinalIgnoreCase))
                    return package.IncludeInBuild;
            }

            // Transitive dependency — include in builds by default.
            return true;
        }
    }
}
