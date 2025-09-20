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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Utils;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Package
    {
        [McpPluginTool
        (
            "Package_Remove",
            Title = "Remove Package"
        )]
        [Description(@"Remove Unity packages from the project by modifying the manifest.json file. 
Supports removing multiple packages at once. Returns the result for each package (success or warning if not found).
Automatically refreshes the AssetDatabase and handles domain reload scenarios.")]
        public static async Task<ResponseCallTool> Remove
        (
            [Description("Array of package IDs to remove. Example: ['com.unity.postprocessing', 'org.nuget.system.text.json']")]
            string[] packageIds,

            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error(Error.InvalidRequestID());

            return await MainThread.Instance.RunAsync(async () =>
            {
                if (McpPluginUnity.IsLogActive(LogLevel.Info))
                    Debug.Log($"[Package_Remove] Preparing to remove packages: {string.Join(", ", packageIds)}");

                try
                {
                    var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
                    
                    if (!File.Exists(manifestPath))
                        return ResponseCallTool.Error(Error.ManifestNotFound(manifestPath)).SetRequestID(requestId);

                    var results = new List<string>();
                    var packagesRemoved = false;

                    // Read and parse manifest.json
                    var jsonText = File.ReadAllText(manifestPath);
                    var manifestJson = JSONObject.Parse(jsonText);
                    
                    if (manifestJson == null)
                        return ResponseCallTool.Error(Error.ManifestParseError(manifestPath)).SetRequestID(requestId);

                    var dependencies = manifestJson["dependencies"];
                    if (dependencies == null)
                    {
                        // No dependencies section - all packages are "not found"
                        foreach (var packageId in packageIds)
                            results.Add(Error.PackageNotFound(packageId));
                    }
                    else
                    {
                        // Process each package ID
                        foreach (var packageId in packageIds)
                        {
                            if (dependencies[packageId] != null)
                            {
                                // Package found - remove it
                                dependencies.Remove(packageId);
                                results.Add($"[Success] Package {packageId} removed");
                                packagesRemoved = true;

                                if (McpPluginUnity.IsLogActive(LogLevel.Info))
                                    Debug.Log($"[Package_Remove] Removed package: {packageId}");
                            }
                            else
                            {
                                // Package not found - add warning
                                results.Add(Error.PackageNotFound(packageId));
                            }
                        }
                    }

                    // If at least one package was removed, save the manifest and refresh
                    if (packagesRemoved)
                    {
                        // Save the modified manifest
                        File.WriteAllText(manifestPath, manifestJson.ToString(2).Replace("\" : ", "\": "));
                        
                        if (McpPluginUnity.IsLogActive(LogLevel.Info))
                            Debug.Log("[Package_Remove] Manifest.json updated, refreshing AssetDatabase...");

                        // Store results for domain reload handling  
                        PackageRemovalTracker.StoreResults(requestId, results);

                        // Refresh AssetDatabase - this may trigger domain reload
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                        // Return Processing status - the actual result will be sent after refresh/compilation
                        return ResponseCallTool.Processing().SetRequestID(requestId);
                    }
                    else
                    {
                        // No packages were removed, return results immediately
                        var resultMessage = string.Join("\n", results);
                        
                        if (McpPluginUnity.IsLogActive(LogLevel.Info))
                            Debug.Log($"[Package_Remove] No packages removed. Results: {resultMessage}");

                        return ResponseCallTool.Success(resultMessage).SetRequestID(requestId);
                    }
                }
                catch (Exception ex)
                {
                    if (McpPluginUnity.IsLogActive(LogLevel.Error))
                        Debug.LogException(ex);

                    return ResponseCallTool.Error($"[Error] Package removal failed: {ex.Message}").SetRequestID(requestId);
                }
            }).Unwrap();
        }
    }

    // Helper class to track package removal results across domain reloads
    [InitializeOnLoad]
    public static class PackageRemovalTracker
    {
        private static readonly Dictionary<string, List<string>> _pendingResults = new Dictionary<string, List<string>>();

        static PackageRemovalTracker()
        {
            // Subscribe to compilation events to handle domain reload completion
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.update += OnEditorUpdate;
        }

        public static void StoreResults(string requestId, List<string> results)
        {
            _pendingResults[requestId] = results;
            
            if (McpPluginUnity.IsLogActive(LogLevel.Info))
                Debug.Log($"[PackageRemovalTracker] Stored results for request {requestId}: {results.Count} items");
        }

        public static List<string>? GetResults(string requestId)
        {
            if (_pendingResults.TryGetValue(requestId, out var results))
            {
                _pendingResults.Remove(requestId);
                return results;
            }
            return null;
        }

        public static bool HasPendingRequest(string requestId)
        {
            return _pendingResults.ContainsKey(requestId);
        }

        private static void OnAfterAssemblyReload()
        {
            // After domain reload, check if we have any pending package removal operations to complete
            if (_pendingResults.Count > 0)
            {
                if (McpPluginUnity.IsLogActive(LogLevel.Info))
                    Debug.Log($"[PackageRemovalTracker] Domain reload completed, processing {_pendingResults.Count} pending requests");

                // Wait a frame for Unity to settle after domain reload, then process results
                EditorApplication.delayCall += ProcessPendingResults;
            }
        }

        private static void OnEditorUpdate()
        {
            // Check if compilation has finished and we have pending requests
            if (_pendingResults.Count > 0 && !EditorApplication.isCompiling)
            {
                if (McpPluginUnity.IsLogActive(LogLevel.Info))
                    Debug.Log("[PackageRemovalTracker] Compilation finished, processing pending requests");

                ProcessPendingResults();
                
                // Unsubscribe from update to avoid repeated processing
                EditorApplication.update -= OnEditorUpdate;
                EditorApplication.delayCall += () => EditorApplication.update += OnEditorUpdate;
            }
        }

        private static async void ProcessPendingResults()
        {
            var requestsToProcess = new List<KeyValuePair<string, List<string>>>(_pendingResults);
            _pendingResults.Clear();

            foreach (var kvp in requestsToProcess)
            {
                var requestId = kvp.Key;
                var results = kvp.Value;
                var resultMessage = string.Join("\n", results);

                if (McpPluginUnity.IsLogActive(LogLevel.Info))
                    Debug.Log($"[PackageRemovalTracker] Sending completion for request {requestId}: {resultMessage}");

                try
                {
                    // Check if there were any compilation errors
                    var hasCompilationErrors = EditorUtility.scriptCompilationFailed;
                    
                    ResponseCallTool response;
                    if (hasCompilationErrors)
                    {
                        response = ResponseCallTool.Error($"[Error] Package removal completed but compilation failed. Check the console for compilation errors.\n\nPackage removal results:\n{resultMessage}")
                            .SetRequestID(requestId);
                    }
                    else
                    {
                        response = ResponseCallTool.Success(resultMessage)
                            .SetRequestID(requestId);
                    }

                    await McpPluginUnity.NotifyToolRequestCompleted(response);
                }
                catch (Exception ex)
                {
                    if (McpPluginUnity.IsLogActive(LogLevel.Error))
                        Debug.LogError($"[PackageRemovalTracker] Failed to send completion notification for request {requestId}: {ex.Message}");
                }
            }
        }
    }
}