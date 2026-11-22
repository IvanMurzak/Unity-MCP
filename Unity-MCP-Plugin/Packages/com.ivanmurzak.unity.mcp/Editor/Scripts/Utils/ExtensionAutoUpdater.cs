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
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.DependencyResolver;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// On plugin startup: finds installed extensions built for another plugin generation (Unity skips
    /// their assemblies without an error, so their tools are missing) and moves them to the published
    /// version built for this plugin. This is what keeps the extensions working when only the plugin
    /// was updated, for example through Package Manager.
    /// </summary>
    /// <remarks>
    /// The check is offline and cheap: the registered packages are already in memory and only the
    /// asmdefs of packages that depend on the plugin are read. The registry is asked only when a
    /// stale extension was found. An extension that cannot be updated automatically is named in a
    /// console warning with what to do.
    /// </remarks>
    internal static class ExtensionAutoUpdater
    {
        private const string PluginPackageId = ServerBinaryUpdateWatcher.UnityMcpPackageName;
        private const string Tag = "[Unity-MCP]";

        // SessionState, not EditorPrefs: a state nothing could be done about is looked at again in
        // the next editor session, and within one session after RetrySeconds.
        private const string KeyHandledState = "MCP_ExtensionAutoUpdater_HandledState";
        private const string KeyRetryAt = "MCP_ExtensionAutoUpdater_RetryAt";
        private const float RetrySeconds = 30 * 60;

        // The update tick, not delayCall: delayCall waits for the editor window to get focus.
        public static void Init() => EditorApplication.update += Run;

        private static void Run()
        {
            // A compile in progress ends in a domain reload that would drop the work started here.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            EditorApplication.update -= Run;

            // Entering Play Mode reloads the domain too: a package change must not start under it.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            List<(string PackageId, string Name, string Version, bool CanAutoUpdate)> stale;
            try
            {
                stale = FindStaleExtensions(PackageInfo.GetAllRegisteredPackages());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Could not check the installed extensions against this plugin: {ex.Message}");
                return;
            }
            if (stale.Count == 0)
                return;

            // Every domain reload lands here again. One state is acted on (and warned about) once,
            // then again after RetrySeconds: a matching extension may have been published meanwhile.
            var state = UnityMcpPlugin.Version + "|" + string.Join(",", stale.Select(extension => $"{extension.PackageId}@{extension.Version}"));
            if (SessionState.GetString(KeyHandledState, string.Empty) == state
                && EditorApplication.timeSinceStartup < SessionState.GetFloat(KeyRetryAt, 0))
                return;

            _ = UpdateAsync(stale, state);
        }

        /// <summary>
        /// The installed packages that depend on the plugin and whose assemblies require another
        /// dependency generation than this plugin sets. Only a first-party extension installed from
        /// the package registry can be updated automatically.
        /// </summary>
        private static List<(string PackageId, string Name, string Version, bool CanAutoUpdate)> FindStaleExtensions(IEnumerable<PackageInfo> packages)
        {
            var catalog = MainWindowEditor.ExtensionCatalog.ToDictionary(data => data.PackageId);
            var stale = new List<(string, string, string, bool)>();
            foreach (var package in packages)
            {
                var firstParty = catalog.TryGetValue(package.name, out var data);
                if (package.name == PluginPackageId
                    || (!firstParty && !package.dependencies.Any(dependency => dependency.name == PluginPackageId))
                    || string.IsNullOrEmpty(package.resolvedPath) || !Directory.Exists(package.resolvedPath))
                    continue;

                if (ExtensionCompatibility.IsBuiltForAnotherGeneration(
                        ExtensionCompatibility.ShippedAsmdefs(package.resolvedPath).Select(File.ReadAllText),
                        RecompileGate.DependencyGenerationDefinePrefix,
                        RecompileGate.DependencyGenerationDefine))
                    stale.Add((package.name, firstParty ? data!.Name : package.displayName, package.version,
                        firstParty && package.source == PackageSource.Registry));
            }
            return stale;
        }

        private static async Task UpdateAsync(IReadOnlyList<(string PackageId, string Name, string Version, bool CanAutoUpdate)> stale, string state)
        {
            try
            {
                var pluginVersion = UnityMcpPlugin.Version;
                // A headless editor (CI) and a team that turned update notifications off keep their
                // manifest as it is: they are told what to do instead.
                var automatic = !Application.isBatchMode && !UpdateChecker.IsDisabledForProject;

                var candidates = stale.Where(extension => extension.CanAutoUpdate).ToArray();
                var documents = await Task.WhenAll(candidates.Select(extension => UpdateChecker.FetchRegistryDocumentAsync(extension.PackageId)));

                var updates = new List<(string PackageId, string InstalledVersion, string? TargetVersion)>();
                var moves = new List<string>();
                for (var i = 0; i < candidates.Length; i++)
                {
                    var (packageId, name, version, _) = candidates[i];
                    var target = documents[i] == null ? null : ExtensionCompatibility.UpdateTargetFor(version, documents[i]!, PluginPackageId, pluginVersion);
                    if (documents[i] == null)
                        Warn(name, version, packageId, "Its published versions could not be read (no connection?). It is checked again later.");
                    else if (target == null)
                        Warn(name, version, packageId, $"No version built for plugin {pluginVersion} is published. Update the plugin to its latest version; if it already is, the matching extension is still being published and is checked again later.");
                    else if (!automatic)
                        Warn(name, version, packageId, $"Version {target} is built for this plugin: update it in the Extensions section of the AI Game Developer window.");
                    else
                    {
                        updates.Add((packageId, version, target));
                        moves.Add($"{name} {version} -> {target}");
                    }
                }
                foreach (var (packageId, name, version, _) in stale.Where(extension => !extension.CanAutoUpdate))
                    Warn(name, version, packageId, "It is not a registry install of a first-party extension, so it is left alone: update it where it comes from.");

                if (updates.Count > 0)
                {
                    Debug.Log($"{Tag} Updating extensions built for another plugin version, so their tools work with plugin {pluginVersion}: {string.Join(", ", moves)}.");
                    await PackageUtils.AddPackagesAsync(UpdateChecker.BuildExtensionUpdateSpecs(updates, ReadManifestDependencies(), Application.unityVersion));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Could not update the extensions built for another plugin version ({ex.Message}). Their tools stay missing until they are updated in the Extensions section of the AI Game Developer window.");
            }
            // Marked only here, once the state was acted on. A domain reload in the middle drops this
            // task with the old domain, and the next startup must start over rather than find the
            // state already handled.
            SessionState.SetString(KeyHandledState, state);
            SessionState.SetFloat(KeyRetryAt, (float)EditorApplication.timeSinceStartup + RetrySeconds);
        }

        private static JsonObject? ReadManifestDependencies()
        {
            try
            {
                return JsonNode.Parse(File.ReadAllText(ExtensionPanel.ManifestPath))?["dependencies"]?.AsObject();
            }
            catch (Exception)
            {
                return null; // Only the optional Unity package floors are read from it.
            }
        }

        private static void Warn(string name, string version, string packageId, string whatToDo)
            => Debug.LogWarning($"{Tag} Extension '{name}' {version} ({packageId}) is built for another version of the plugin, "
                + $"so Unity does not compile it and its tools are missing. {whatToDo}");
    }
}
