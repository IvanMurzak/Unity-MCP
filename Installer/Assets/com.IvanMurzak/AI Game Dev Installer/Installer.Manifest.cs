/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System.IO;
using System.Linq;
using UnityEngine;
using com.IvanMurzak.Unity.MCP.Installer.SimpleJSON;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.IvanMurzak.Unity.MCP.Installer.Tests")]
namespace com.IvanMurzak.Unity.MCP.Installer
{
    public static partial class Installer
    {
        static string ManifestPath => Path.Combine(Application.dataPath, "../Packages/manifest.json");

        // Property names
        public const string Dependencies = "dependencies";
        public const string ScopedRegistries = "scopedRegistries";
        public const string Name = "name";
        public const string Url = "url";
        public const string Scopes = "scopes";

        // Property values
        public const string RegistryName = "package.openupm.com";
        public const string RegistryUrl = "https://package.openupm.com";
        public static readonly string[] PackageIds = new string[] {
            "com.ivanmurzak",            // Ivan Murzak's OpenUPM packages
            "extensions.unity",          // Ivan Murzak's OpenUPM packages (older)
            "org.nuget.com.ivanmurzak",  // Ivan Murzak's NuGet packages
            "org.nuget.microsoft",       // Microsoft NuGet packages
            "org.nuget.system",          // Microsoft NuGet packages
            "org.nuget.r3"               // R3 package NuGet package
        };

        /// <summary>
        /// Determines if the version should be updated. Only update if installer version is higher than current version.
        /// </summary>
        /// <param name="currentVersion">Current package version string</param>
        /// <param name="installerVersion">Installer version string</param>
        /// <returns>True if version should be updated (installer version is higher), false otherwise</returns>

        internal static bool ShouldUpdateVersion(string currentVersion, string installerVersion)
        {
            if (string.IsNullOrEmpty(currentVersion))
                return true; // No current version, should install

            if (string.IsNullOrEmpty(installerVersion))
                return false; // No installer version, don't change

            try
            {
                // Try to parse as System.Version (semantic versioning)
                var current = new System.Version(currentVersion);
                var installer = new System.Version(installerVersion);

                // Only update if installer version is higher than current version
                return installer > current;
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"Failed to parse versions '{currentVersion}' or '{installerVersion}' as System.Version.");
                // If version parsing fails, fall back to string comparison
                // This ensures we don't break if version format is unexpected
                return string.Compare(installerVersion, currentVersion, System.StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        public static void AddScopedRegistryIfNeeded(string manifestPath, int indent = 2)
        {
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"{manifestPath} not found!");
                return;
            }
            var jsonText = File.ReadAllText(manifestPath)
                .Replace("{ }", "{\n}")
                .Replace("{}", "{\n}")
                .Replace("[ ]", "[\n]")
                .Replace("[]", "[\n]");

            var manifestJson = JSONObject.Parse(jsonText);
            if (manifestJson == null)
            {
                Debug.LogError($"Failed to parse {manifestPath} as JSON.");
                return;
            }

            var modified = false;

            // --- Add scoped registries if needed
            var scopedRegistries = manifestJson[ScopedRegistries];
            if (scopedRegistries == null)
            {
                manifestJson[ScopedRegistries] = new JSONArray();
                modified = true;
            }

            // --- Add OpenUPM registry if needed
            var openUpmRegistry = scopedRegistries!.Linq
                .Select(kvp => kvp.Value)
                .Where(r => r.Linq
                    .Any(p => p.Key == Name && p.Value == RegistryName))
                .FirstOrDefault();

            if (openUpmRegistry == null)
            {
                scopedRegistries.Add(openUpmRegistry = new JSONObject
                {
                    [Name] = RegistryName,
                    [Url] = RegistryUrl,
                    [Scopes] = new JSONArray()
                });
                modified = true;
            }

            // --- Add missing scopes
            var scopes = openUpmRegistry[Scopes];
            if (scopes == null)
            {
                openUpmRegistry[Scopes] = scopes = new JSONArray();
                modified = true;
            }
            foreach (var packageId in PackageIds)
            {
                var existingScope = scopes!.Linq
                    .Select(kvp => kvp.Value)
                    .Where(value => value == packageId)
                    .FirstOrDefault();
                if (existingScope == null)
                {
                    scopes.Add(packageId);
                    modified = true;
                }
            }

            // --- Package Dependency (Version-aware installation)
            // Only update version if installer version is higher than current version
            // This prevents downgrades when users manually update to newer versions
            var dependencies = manifestJson[Dependencies];
            if (dependencies == null)
            {
                manifestJson[Dependencies] = dependencies = new JSONObject();
                modified = true;
            }

            // Only update version if installer version is higher than current version
            var currentVersion = dependencies[PackageId];
            if (currentVersion == null || ShouldUpdateVersion(currentVersion, Version))
            {
                dependencies[PackageId] = Version;
                modified = true;
            }

            // --- Write changes back to manifest
            if (modified)
                File.WriteAllText(manifestPath, manifestJson.ToString(indent).Replace("\" : ", "\": "));
        }
    }
}