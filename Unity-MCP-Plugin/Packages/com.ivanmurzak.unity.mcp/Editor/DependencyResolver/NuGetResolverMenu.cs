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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Adds a Unity Editor menu item that forces a full NuGet DLL re-resolve on demand.
    /// The automatic resolver in <see cref="NuGetDependencyResolver"/> only runs the full
    /// restore path when <see cref="NuGetPackageRestorer.AllPackagesInstalled"/> reports
    /// something is missing. This menu item bypasses that quick-check so users can recover
    /// from an inconsistent on-disk DLL state without manually deleting Assets/Plugins/NuGet.
    /// </summary>
    static class NuGetResolverMenu
    {
        const string Tag = NuGetConfig.LogTag;
        const string MenuPath = "Tools/AI Game Developer/Dependencies/Force Resolve NuGet DLLs";

        [MenuItem(MenuPath, priority = 1050)]
        public static void ForceResolve()
        {
            Debug.Log($"{Tag} Force resolve requested — running full restore...");

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
    }
}
