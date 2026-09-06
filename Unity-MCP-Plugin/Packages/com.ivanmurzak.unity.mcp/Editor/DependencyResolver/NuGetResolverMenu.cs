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
using UnityEditor.PackageManager;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Adds Unity Editor menu items that force a full dependency re-resolve on demand.
    /// The automatic resolver in <see cref="NuGetDependencyResolver"/> only runs the full
    /// restore path when <see cref="NuGetPackageRestorer.AllPackagesInstalled"/> reports
    /// something is missing, AND it needs a successful domain reload to fire at all — which
    /// Unity refuses while ANY script in the project has a compile error. If the project's
    /// on-disk NuGet DLL set (or, separately, Unity's own <c>Library/PackageCache</c> copy of
    /// this package) is already stale/inconsistent when the Editor starts — e.g. because
    /// <c>Packages/manifest.json</c> was bumped past several versions while the Editor was
    /// closed, or a prior compile-error session never got a chance to self-heal — neither
    /// <see cref="NuGetDependencyResolver"/>'s [InitializeOnLoad] path nor its
    /// registeredPackages-event path (#707) ever runs, and the project is stuck showing
    /// CS0115 / CS0103 errors in <c>UnityMcpPlugin.Config.cs</c> or <c>Editor/Scripts/**</c>
    /// that look like source bugs but are purely a stale on-disk cache. These menu items give
    /// users a one-click way out without hand-editing <c>manifest.json</c> or deleting
    /// folders themselves.
    /// </summary>
    static class NuGetResolverMenu
    {
        const string Tag = NuGetConfig.LogTag;
        const string MenuPath = "Tools/AI Game Developer/Dependencies/Force Resolve NuGet DLLs";
        const string ReimportMenuPath = "Tools/AI Game Developer/Dependencies/Force Reimport Package Cache (fixes ghost CS0103 errors)";

        [MenuItem(MenuPath, priority = 1050)]
        public static void ForceResolve()
        {
            Debug.Log($"{Tag} Force resolve requested — running full restore...");

            // Force a clean recompile so the AssetDatabase.Refresh() at the
            // bottom runs against fresh assemblies, not the stale AppDomain.
            RecompileGate.Reset();

            try
            {
                var changed = NuGetPackageRestorer.Restore();
                NuGetPluginConfigurator.ConfigureAll();

                if (changed)
                {
                    Debug.Log($"{Tag} Force resolve complete. Refreshing AssetDatabase...");
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.Log($"{Tag} Force resolve complete. No changes needed.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Force resolve failed: {ex}");
            }
        }

        /// <summary>
        /// Deletes this package's own extracted copy under <c>Library/PackageCache</c> and asks
        /// the Package Manager to re-resolve. Unlike <see cref="ForceResolve"/> (which only
        /// touches the NuGet DLL layer under <c>Assets/Plugins/NuGet</c>), this targets a
        /// different failure mode: Unity's own AssetDatabase occasionally imports a package
        /// folder without including every <c>.cs</c> file in the Roslyn compile args, even
        /// though the files and their <c>.meta</c> siblings are present and well-formed on
        /// disk (observed on Unity 6000.7.0a2; symptom is CS0103 "does not exist in the
        /// current context" for types defined a few files away in the same assembly, e.g.
        /// <c>Editor/Scripts/Services/*.cs</c>). Neither a plain AssetDatabase.Refresh() nor an
        /// Editor restart is reliably enough to fix this — the folder needs to be deleted and
        /// re-extracted from scratch via a genuine Package Manager resolve. A plain
        /// AssetDatabase.Refresh() after deleting the folder is NOT enough either: UPM's own
        /// resolve pass only re-extracts a package when it detects a manifest/dependency
        /// change, not merely because the destination folder went missing.
        /// </summary>
        [MenuItem(ReimportMenuPath, priority = 1051)]
        public static void ForceReimportPackageCache()
        {
            Debug.Log($"{Tag} Force reimport requested — deleting cached package copy and asking UPM to re-resolve...");

            try
            {
                var packageCacheDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
                if (Directory.Exists(packageCacheDir))
                {
                    // Exact-id match only ("com.ivanmurzak.unity.mcp@<hash>") — must not touch
                    // sibling sub-packages like "com.ivanmurzak.unity.mcp.terrain@<hash>", which
                    // share the prefix but are separate packages with their own cache entries.
                    foreach (var dir in Directory.GetDirectories(packageCacheDir, "com.ivanmurzak.unity.mcp@*"))
                    {
                        Directory.Delete(dir, recursive: true);
                        Debug.Log($"{Tag} Deleted stale package cache: {dir}");
                    }
                }

                RecompileGate.Reset();
                Client.Resolve();
                Debug.Log($"{Tag} Force reimport requested from Package Manager. This runs asynchronously — watch the Console for the resulting recompile.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Force reimport failed: {ex}");
            }
        }
    }
}
