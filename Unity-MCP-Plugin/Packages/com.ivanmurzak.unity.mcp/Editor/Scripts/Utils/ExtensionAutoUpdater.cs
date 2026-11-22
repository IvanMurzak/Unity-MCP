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
    /// On plugin startup: finds installed extensions built for another plugin release (Unity skips
    /// their assemblies without an error, so their tools are missing) and moves them to the published
    /// version built for this plugin. This is what keeps the extensions working when only the plugin
    /// was changed, for example through Package Manager, or when one extension was moved to a version
    /// built for a newer plugin: Unity then runs that newer plugin (it resolves a package to the
    /// highest version anything in the project names) and skips the other extensions.
    /// </summary>
    /// <remarks>
    /// The check is offline and cheap: the registered packages are already in memory and only the
    /// asmdefs of packages that depend on the plugin are read. The registry is asked only when an
    /// extension needs a move. An extension that cannot be moved automatically is named in a
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

        // While an extension waits for its version to be published, Run stays subscribed and looks
        // again at this time: a session without a domain reload would otherwise never look again.
        private static double _lookAgainAt;

        /// <summary>An installed extension that should move to another version.</summary>
        private readonly struct Extension
        {
            public readonly string PackageId;
            public readonly string Name;
            public readonly string Version;

            /// <summary>The plugin version it is published for, when its package names one.</summary>
            public readonly string? PluginVersion;

            /// <summary>A first-party extension installed from the package registry.</summary>
            public readonly bool CanAutoUpdate;

            /// <summary>Unity skips it; otherwise it works and only lacks the tie to this plugin release.</summary>
            public readonly bool Skipped;

            public Extension(string packageId, string name, string version, string? pluginVersion, bool canAutoUpdate, bool skipped)
            {
                PackageId = packageId;
                Name = name;
                Version = version;
                PluginVersion = pluginVersion;
                CanAutoUpdate = canAutoUpdate;
                Skipped = skipped;
            }
        }

        // The update tick, not delayCall: delayCall waits for the editor window to get focus.
        public static void Init() => EditorApplication.update += Run;

        /// <summary>
        /// How an installed package stands with this plugin; read from its asmdefs, no network. A package
        /// whose files cannot be read is not judged: one unreadable package must not stop the others.
        /// </summary>
        internal static ExtensionFit FitOf(PackageInfo package)
        {
            if (string.IsNullOrEmpty(package.resolvedPath) || !Directory.Exists(package.resolvedPath))
                return ExtensionFit.Works;
            try
            {
                return ExtensionCompatibility.FitOf(
                    ExtensionCompatibility.ShippedAsmdefs(package.resolvedPath).Select(File.ReadAllText).ToArray(),
                    RecompileGate.DependencyGenerationDefinePrefix,
                    RecompileGate.DependencyGenerationDefine,
                    PluginPackageId,
                    UnityMcpPlugin.Version);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return ExtensionFit.Works;
            }
        }

        private static void Run()
        {
            if (EditorApplication.timeSinceStartup < _lookAgainAt)
                return;
            // A compile in progress ends in a domain reload that would drop the work started here,
            // and a package change must not start in Play Mode (it reloads the domain). Both are
            // waited for: leaving Play Mode reloads nothing, so no later startup would look again.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorApplication.update -= Run;

            List<Extension> found;
            try
            {
                found = FindExtensionsToMove(PackageInfo.GetAllRegisteredPackages());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Could not check the installed extensions against this plugin: {ex.Message}");
                return;
            }
            if (found.Count == 0)
                return;

            // Every domain reload lands here again. One state is acted on (and warned about) once,
            // then again after RetrySeconds: a matching extension may have been published meanwhile.
            var state = UnityMcpPlugin.Version + "|" + string.Join(",", found.Select(extension => $"{extension.PackageId}@{extension.Version}"));
            if (SessionState.GetString(KeyHandledState, string.Empty) == state
                && EditorApplication.timeSinceStartup < SessionState.GetFloat(KeyRetryAt, 0))
                return;

            if (Application.isBatchMode)
            {
                // A headless editor (CI) keeps its manifest as it is and may quit at any moment:
                // it is told what to do right here, without waiting for the registry.
                foreach (var extension in found)
                    Warn(extension, $"Set it to its version for plugin {UnityMcpPlugin.Version} in Packages/manifest.json (`unity-mcp-cli update --no-plugin --no-self` in the project does that).");
                MarkHandled(state);
                return;
            }

            _ = UpdateAsync(found, state);
        }

        /// <summary>
        /// The installed packages that depend on the plugin and that Unity skips under it, plus the
        /// first-party registry installs that work but are not tied to this plugin release yet (the
        /// next plugin update would break the compile on them). Only a first-party extension
        /// installed from the package registry can be moved automatically.
        /// </summary>
        private static List<Extension> FindExtensionsToMove(IEnumerable<PackageInfo> packages)
        {
            var catalog = MainWindowEditor.ExtensionCatalog.ToDictionary(data => data.PackageId);
            var found = new List<Extension>();
            foreach (var package in packages)
            {
                var firstParty = catalog.TryGetValue(package.name, out var data);
                var pluginVersion = package.dependencies.Where(dependency => dependency.name == PluginPackageId).Select(dependency => dependency.version).FirstOrDefault();
                if (package.name == PluginPackageId || (!firstParty && pluginVersion == null))
                    continue;

                var fit = FitOf(package);
                var canAutoUpdate = firstParty && package.source == PackageSource.Registry;
                if (fit == ExtensionFit.Skipped || (fit == ExtensionFit.Unprotected && canAutoUpdate))
                    found.Add(new Extension(package.name, firstParty ? data!.Name : package.displayName, package.version,
                        pluginVersion, canAutoUpdate, fit == ExtensionFit.Skipped));
            }
            return found;
        }

        private static async Task UpdateAsync(IReadOnlyList<Extension> found, string state)
        {
            var waiting = false;
            try
            {
                var pluginVersion = UnityMcpPlugin.Version;
                // A team that turned update notifications off keeps its manifest as it is: it is
                // told what to do instead.
                var automatic = !UpdateChecker.IsDisabledForProject;

                var candidates = found.Where(extension => extension.CanAutoUpdate).ToArray();
                var documents = await Task.WhenAll(candidates.Select(extension => UpdateChecker.FetchRegistryDocumentAsync(extension.PackageId)));

                // The registry took a while to answer and Play Mode may have been entered meanwhile.
                // Nothing awaits between here and the package request: Run waits for Play Mode to
                // end and starts over, with this state left unhandled.
                if (automatic && candidates.Length > 0 && EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Init();
                    return;
                }

                var updates = new List<(string PackageId, string InstalledVersion, string? TargetVersion)>();
                var moves = new List<string>();
                for (var i = 0; i < candidates.Length; i++)
                {
                    var extension = candidates[i];
                    // A skipped one follows the plugin in either direction; a working one only moves up.
                    var target = documents[i] == null ? null
                        : ExtensionCompatibility.UpdateTargetFor(extension.Version, documents[i]!, PluginPackageId, pluginVersion, anyDirection: extension.Skipped);
                    if (target != null && automatic)
                    {
                        updates.Add((extension.PackageId, extension.Version, target));
                        moves.Add($"{extension.Name} {extension.Version} -> {target}");
                    }
                    else if (target != null)
                        Warn(extension, $"Version {target} is built for this plugin: update it in the Extensions section of the AI Game Developer window.");
                    else if (!extension.Skipped)
                        continue; // It works: nothing to report until its version for this plugin can be installed.
                    else
                    {
                        waiting = true;
                        Warn(extension, documents[i] == null
                            ? "Its published versions could not be read (no connection?). It is checked again later."
                            : $"No other version built for plugin {pluginVersion} is published. Update the plugin to its latest version; if it already is, the matching extension is still being published and is checked again later.");
                    }
                }
                foreach (var extension in found.Where(extension => !extension.CanAutoUpdate))
                    Warn(extension, "It is not a registry install of a first-party extension, so it is left alone: update it where it comes from.");

                if (updates.Count > 0)
                {
                    Debug.Log($"{Tag} Moving the installed extensions to their versions built for plugin {pluginVersion} (an extension works with exactly one plugin release): {string.Join(", ", moves)}.");
                    await PackageUtils.AddPackagesAsync(UpdateChecker.BuildExtensionUpdateSpecs(updates, ReadManifestDependencies(), Application.unityVersion));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Could not move the installed extensions to their versions for this plugin ({ex.Message}). Update them in the Extensions section of the AI Game Developer window.");
            }
            // Marked only here, once the state was acted on. A domain reload in the middle drops this
            // task with the old domain, and the next startup must start over rather than find the
            // state already handled.
            MarkHandled(state);
            if (waiting)
            {
                _lookAgainAt = SessionState.GetFloat(KeyRetryAt, 0);
                Init();
            }
        }

        private static void MarkHandled(string state)
        {
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

        private static void Warn(Extension extension, string whatToDo)
        {
            var problem = !extension.Skipped
                ? "was published before extensions were tied to one plugin release: the next plugin update would compile it without the plugin and break the build"
                : "is built for "
                    + (extension.PluginVersion == null || extension.PluginVersion == UnityMcpPlugin.Version
                        ? "another build of the plugin"
                        : $"plugin {extension.PluginVersion}, not for the installed plugin {UnityMcpPlugin.Version}")
                    + ", so Unity does not compile it and its tools are missing";
            Debug.LogWarning($"{Tag} Extension '{extension.Name}' {extension.Version} ({extension.PackageId}) {problem}. {whatToDo}");
        }
    }
}
