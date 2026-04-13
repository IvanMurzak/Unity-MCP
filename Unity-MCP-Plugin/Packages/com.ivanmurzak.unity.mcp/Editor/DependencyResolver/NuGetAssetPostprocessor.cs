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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// Configures PluginImporter settings for bundled NuGet DLLs.
    ///
    /// Handles the key conflict scenario: Unity may ship a DLL as a built-in BCL assembly
    /// (e.g., System.Text.Json in Unity 6.5). When we bundle the same DLL, we must:
    ///   - Exclude our copy from the editor (Unity's built-in is used there)
    ///   - Include our copy in builds (Unity doesn't include it in player builds)
    ///
    /// Scans all DLLs in Plugins/NuGet/ and sets PluginImporter properties
    /// based on whether Unity already provides each assembly.
    /// </summary>
    static class NuGetPluginConfigurator
    {
        const string Tag = "[NuGet]";

        // Paths where bundled NuGet DLLs live (relative to project root).
        // Check both the package location and the Assets fallback.
        static readonly string[] SearchPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Plugins/NuGet",
            "Assets/Plugins/NuGet",
        };

        public static void ConfigureAll()
        {
            foreach (var searchPath in SearchPaths)
            {
                if (!Directory.Exists(searchPath))
                    continue;

                var dlls = Directory.GetFiles(searchPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dllPath in dlls)
                {
                    var assetPath = dllPath.Replace('\\', '/');
                    ConfigureDll(assetPath);
                }
            }
        }

        static void ConfigureDll(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
                return;

            var dllName = Path.GetFileNameWithoutExtension(assetPath);
            var unityProvidesIt = UnityAssemblyResolver.IsAlreadyImported(dllName);

            bool anyPlatform, excludeEditor, editorOnly;

            if (unityProvidesIt)
            {
                // Unity provides this DLL in the editor.
                // Include in builds (Unity doesn't ship it in player builds),
                // exclude from editor (avoid duplicate assemblies).
                anyPlatform = true;
                excludeEditor = true;
                editorOnly = false;
            }
            else
            {
                // Not provided by Unity — include everywhere.
                anyPlatform = true;
                excludeEditor = false;
                editorOnly = false;
            }

            var currentAny = importer.GetCompatibleWithAnyPlatform();
            var currentEditor = importer.GetCompatibleWithEditor();
            var currentExclude = importer.GetExcludeEditorFromAnyPlatform();

            var needsChange = currentAny != anyPlatform
                           || currentExclude != excludeEditor
                           || (!anyPlatform && currentEditor != editorOnly);

            if (!needsChange)
                return;

            if (anyPlatform)
            {
                importer.SetCompatibleWithAnyPlatform(true);
                importer.SetExcludeEditorFromAnyPlatform(excludeEditor);
            }
            else
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(editorOnly);
            }

            importer.SaveAndReimport();
            Debug.Log($"{Tag} Configured '{dllName}': excludeEditor={excludeEditor}");
        }
    }
}
