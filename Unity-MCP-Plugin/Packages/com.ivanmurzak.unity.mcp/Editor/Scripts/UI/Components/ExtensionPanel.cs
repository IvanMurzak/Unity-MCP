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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Reusable extension item component backed by the ExtensionItem.uxml template.
    /// Displays an extension name, description, and install/update button with progress tracking.
    /// Installs packages via OpenUPM scoped registry in manifest.json.
    /// </summary>
    public class ExtensionPanel
    {
        private static readonly string[] TemplatePaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/ExtensionItem.uxml");

        private const string RegistryName = "package.openupm.com";
        private const string RegistryUrl = "https://package.openupm.com";
        internal static string ManifestPath => Path.Combine(Application.dataPath, "../Packages/manifest.json");
        private static readonly string[] RequiredScopes =
        {
            "com.ivanmurzak",
            "extensions.unity"
        };

        private readonly Label _name;
        private readonly Label _description;
        private readonly Button _actionBtn;
        private readonly ExtensionData _data;
        private PanelState _state;
        private bool _busyWithUpdate;
        private bool _isFromRegistry;

        /// <summary>Raised whenever the extension moves to a different install/update state.</summary>
        internal event Action? StateChanged;

        /// <summary>The extension is not installed and can be installed right now.</summary>
        internal bool CanInstall => _state == PanelState.Install;

        /// <summary>A newer version is available and the extension can be updated right now.</summary>
        internal bool CanUpdate => _state == PanelState.Update;

        /// <summary>
        /// Update All leaves a <c>file:</c> or git install alone: only its own Update button, a deliberate
        /// per-extension choice, replaces such a checkout with a registry version.
        /// </summary>
        internal bool CanBulkUpdate => CanUpdate && _isFromRegistry;

        /// <summary>
        /// The root visual element of the extension panel. Add this to a parent container.
        /// </summary>
        public VisualElement Root { get; }

        /// <summary>
        /// Creates a new extension panel from the UXML template.
        /// Button is hidden until <see cref="RefreshStatus"/> determines the correct state.
        /// </summary>
        /// <param name="data">The extension data including name, description, package ID, and tools list.</param>
        public ExtensionPanel(ExtensionData data)
        {
            _data = data;

            var template = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(TemplatePaths)
                ?? throw new NullReferenceException($"Failed to load ExtensionItem.uxml. Checked: {string.Join(", ", TemplatePaths)}");

            var tree = template.CloneTree();
            Root = tree.Q<VisualElement>("extension-item")
                ?? throw new NullReferenceException("VisualElement 'extension-item' not found in ExtensionItem.uxml.");

            _name = Root.Q<Label>("extension-name")
                ?? throw new NullReferenceException("Label 'extension-name' not found in ExtensionItem.uxml.");
            _description = Root.Q<Label>("extension-desc")
                ?? throw new NullReferenceException("Label 'extension-desc' not found in ExtensionItem.uxml.");
            _actionBtn = Root.Q<Button>("extension-install-btn")
                ?? throw new NullReferenceException("Button 'extension-install-btn' not found in ExtensionItem.uxml.");

            _name.text = data.Name;
            _description.text = data.Description;

            Root.tooltip = BuildTooltip(data);

            ShowAsChecking();
        }

        /// <summary>
        /// Determines the button state based on whether the package is installed
        /// and whether a newer version is available on OpenUPM.
        /// </summary>
        /// <param name="installedVersion">
        /// The currently installed version of this package, or null if not installed.
        /// </param>
        /// <param name="isFromRegistry">Whether that version came from a package registry, not <c>file:</c> or git.</param>
        public void RefreshStatus(string? installedVersion, bool isFromRegistry = false)
        {
            _isFromRegistry = isFromRegistry;
            _actionBtn.UnregisterCallback<ClickEvent>(OnActionClicked);

            if (installedVersion == null)
            {
                ShowAsInstall();
                return;
            }

            ShowAsChecking();
            _ = CheckForUpdateAsync(installedVersion);
        }

        private void SetState(PanelState state)
        {
            _state = state;
            StateChanged?.Invoke();
        }

        private void ShowAsChecking()
        {
            _actionBtn.text = "Checking...";
            _actionBtn.tooltip = $"Checking status of {_data.Name} extension...";
            _actionBtn.RemoveFromClassList("btn-primary");
            _actionBtn.AddToClassList("btn-secondary");
            _actionBtn.style.display = DisplayStyle.Flex;
            _actionBtn.SetEnabled(false);
            SetState(PanelState.Checking);
        }

        private void ShowAsInstall()
        {
            _actionBtn.text = "Install";
            _actionBtn.tooltip = $"Install {_data.Name} extension via OpenUPM.\nPackage: {_data.PackageId}";
            _actionBtn.RemoveFromClassList("btn-primary");
            _actionBtn.AddToClassList("btn-secondary");
            _actionBtn.style.display = DisplayStyle.Flex;
            _actionBtn.SetEnabled(true);

            _actionBtn.RegisterCallback<ClickEvent>(OnActionClicked);
            SetState(PanelState.Install);
        }

        private void ShowAsUpdate(string installedVersion, string latestVersion)
        {
            _actionBtn.text = "Update";
            _actionBtn.tooltip = $"Update {_data.Name} extension from {installedVersion} to {latestVersion}.\nPackage: {_data.PackageId}";
            _actionBtn.RemoveFromClassList("btn-secondary");
            _actionBtn.AddToClassList("btn-primary");
            _actionBtn.style.display = DisplayStyle.Flex;
            _actionBtn.SetEnabled(true);

            _actionBtn.RegisterCallback<ClickEvent>(OnActionClicked);
            SetState(PanelState.Update);
        }

        private async void OnActionClicked(ClickEvent evt)
        {
            await InstallOrUpdateAsync(new[] { this });
        }

        private async Task CheckForUpdateAsync(string installedVersion)
        {
            var latestVersion = await UpdateChecker.FetchLatestVersionAsync(_data.PackageId);
            if (latestVersion != null && UpdateChecker.IsNewerVersion(latestVersion, installedVersion))
            {
                EditorApplication.delayCall += () => ShowAsUpdate(installedVersion, latestVersion);
                return;
            }

            EditorApplication.delayCall += () => ShowAsInstalled();
        }

        private void ShowAsInstalled()
        {
            _actionBtn.text = "Installed";
            _actionBtn.tooltip = $"{_data.Name} extension is installed and up to date.";
            _actionBtn.RemoveFromClassList("btn-primary");
            _actionBtn.AddToClassList("btn-secondary");
            _actionBtn.style.display = DisplayStyle.Flex;
            _actionBtn.SetEnabled(false);
            SetState(PanelState.Installed);
        }

        /// <summary>
        /// Installs or updates the given extensions to their latest OpenUPM versions in one package request, so a
        /// bulk action costs one resolve and one recompile. If a package cannot resolve, the manifest goes back to
        /// how it was and every button comes back, so each extension can still be tried on its own.
        /// </summary>
        internal static async Task InstallOrUpdateAsync(IReadOnlyList<ExtensionPanel> panels)
        {
            foreach (var panel in panels)
                panel.ShowAsBusy();

            var versions = await Task.WhenAll(panels.Select(panel => UpdateChecker.FetchLatestVersionAsync(panel._data.PackageId)));

            var known = new List<(ExtensionPanel Panel, string Version)>(panels.Count);
            for (var i = 0; i < panels.Count; i++)
            {
                if (versions[i] == null)
                {
                    panels[i].FailBusy("its latest version on OpenUPM is unknown");
                    continue;
                }
                known.Add((panels[i], versions[i]!));
            }
            if (known.Count == 0)
                return;

            string? manifestBefore = null;
            try
            {
                manifestBefore = File.ReadAllText(ManifestPath);
                foreach (var (panel, version) in known)
                {
                    if (!AddToManifest(ManifestPath, panel._data.PackageId, version))
                        throw new InvalidOperationException("manifest.json could not be updated");
                }

                await PackageUtils.AddPackagesAsync(known.Select(item => $"{item.Panel._data.PackageId}@{item.Version}").ToArray());
            }
            catch (Exception ex)
            {
                if (manifestBefore != null)
                {
                    File.WriteAllText(ManifestPath, manifestBefore);
                    Client.Resolve();
                }
                foreach (var (panel, _) in known)
                    panel.FailBusy(ex.Message);
            }
        }

        private void ShowAsBusy()
        {
            _busyWithUpdate = _state == PanelState.Update;
            _actionBtn.UnregisterCallback<ClickEvent>(OnActionClicked);
            _actionBtn.SetEnabled(false);
            _actionBtn.text = _busyWithUpdate ? "Updating..." : "Installing...";
            SetState(PanelState.Busy);
        }

        private void FailBusy(string reason)
        {
            Debug.LogError($"[Unity-MCP] Failed to {(_busyWithUpdate ? "update" : "install")} extension '{_data.Name}': {reason}");
            ResetButton();
        }

        /// <summary>The Unity package floors of <paramref name="packageId"/> that the manifest does not satisfy yet.</summary>
        internal static IEnumerable<KeyValuePair<string, string>> MissingCompatibilityDependencies(string packageId, JsonObject? dependencies, string unityVersion)
        {
            foreach (var required in UnityCompatibilityDependencies(packageId, unityVersion))
            {
                var current = dependencies?[required.Key]?.GetValue<string>();
                if (string.IsNullOrEmpty(current) || (System.Version.TryParse(current, out var prior)
                    && System.Version.Parse(required.Value) > prior))
                    yield return required;
            }
        }

        private void ResetButton()
        {
            _actionBtn.SetEnabled(true);
            _actionBtn.text = _busyWithUpdate ? "Update" : "Install";
            _actionBtn.RegisterCallback<ClickEvent>(OnActionClicked);
            SetState(_busyWithUpdate ? PanelState.Update : PanelState.Install);
        }

        /// <summary>
        /// Adds the OpenUPM scoped registry (if needed) and the package dependency to manifest.json.
        /// </summary>
        internal static bool AddToManifest(string manifestPath, string packageId, string version, string? unityVersion = null)
        {
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[Unity-MCP] {manifestPath} not found.");
                return false;
            }

            var jsonText = File.ReadAllText(manifestPath);
            var manifest = JsonNode.Parse(jsonText)?.AsObject();
            if (manifest == null)
            {
                Debug.LogError($"[Unity-MCP] Failed to parse {manifestPath} as JSON.");
                return false;
            }

            EnsureScopedRegistry(manifest);

            var dependencies = manifest["dependencies"]?.AsObject();
            if (dependencies == null)
            {
                dependencies = new JsonObject();
                manifest["dependencies"] = dependencies;
            }
            dependencies[packageId] = version;
            foreach (var required in MissingCompatibilityDependencies(packageId, dependencies, unityVersion ?? Application.unityVersion))
                dependencies[required.Key] = required.Value;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            File.WriteAllText(manifestPath, manifest.ToJsonString(options));
            return true;
        }

        // Mirror of extensions.compatibility.json; original package floors serve older editors.
        internal static Dictionary<string, string> UnityCompatibilityDependencies(string packageId, string unityVersion)
        {
            var match = System.Text.RegularExpressions.Regex.Match(unityVersion, @"^(\d+)\.(\d+)");
            if (!match.Success || int.Parse(match.Groups[1].Value) < 6000
                || (int.Parse(match.Groups[1].Value) == 6000 && int.Parse(match.Groups[2].Value) < 6))
                return new Dictionary<string, string>();
            switch (packageId)
            {
                case "com.ivanmurzak.unity.mcp.inputsystem":
                    return new Dictionary<string, string> { ["com.unity.inputsystem"] = "1.19.0" };
                case "com.ivanmurzak.unity.mcp.navigation":
                    return new Dictionary<string, string> { ["com.unity.ai.navigation"] = "2.0.10" };
                case "com.ivanmurzak.unity.mcp.probuilder":
                    return new Dictionary<string, string> { ["com.unity.probuilder"] = "6.1.3" };
                case "com.ivanmurzak.unity.mcp.tilemap":
                    return new Dictionary<string, string> { ["com.unity.2d.tilemap.extras"] = "7.0.1" };
                default:
                    return new Dictionary<string, string>();
            }
        }

        private static void EnsureScopedRegistry(JsonObject manifest)
        {
            var registries = manifest["scopedRegistries"]?.AsArray();
            if (registries == null)
            {
                registries = new JsonArray();
                manifest["scopedRegistries"] = registries;
            }

            JsonObject? openUpmEntry = null;
            foreach (var entry in registries)
            {
                if (entry?.AsObject() is JsonObject obj
                    && obj["name"]?.GetValue<string>() == RegistryName)
                {
                    openUpmEntry = obj;
                    break;
                }
            }

            if (openUpmEntry == null)
            {
                openUpmEntry = new JsonObject
                {
                    ["name"] = RegistryName,
                    ["url"] = RegistryUrl,
                    ["scopes"] = new JsonArray()
                };
                registries.Add(openUpmEntry);
            }

            var scopes = openUpmEntry["scopes"]?.AsArray();
            if (scopes == null)
            {
                scopes = new JsonArray();
                openUpmEntry["scopes"] = scopes;
            }

            var existingScopes = scopes
                .Select(s => s?.GetValue<string>())
                .Where(s => s != null)
                .ToHashSet();

            foreach (var scope in RequiredScopes)
            {
                if (!existingScopes.Contains(scope))
                    scopes.Add(scope);
            }
        }

        private static string BuildTooltip(ExtensionData data)
        {
            var sb = new StringBuilder();
            sb.Append(data.Description);
            sb.AppendLine();

            if (data.Tools.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("MCP Tools:");

                var maxNameLen = 0;
                foreach (var (toolName, _) in data.Tools)
                {
                    if (toolName.Length > maxNameLen)
                        maxNameLen = toolName.Length;
                }

                foreach (var (toolName, toolDesc) in data.Tools)
                    sb.AppendLine($"  {toolName.PadRight(maxNameLen)}  {toolDesc}");
            }

            sb.AppendLine();
            sb.Append($"Package: {data.PackageId}");

            return sb.ToString();
        }

        private enum PanelState
        {
            Checking,
            Install,
            Update,
            Installed,
            Busy
        }

        /// <summary>
        /// Data describing an extension: display info, package ID, and the MCP tools it provides.
        /// </summary>
        public readonly struct ExtensionData
        {
            public string Name { get; }
            public string Description { get; }
            public string PackageId { get; }
            public string GitUrl { get; }
            public (string name, string description)[] Tools { get; }

            public ExtensionData(string name, string description, string packageId, string gitUrl, (string name, string description)[] tools)
            {
                Name = name;
                Description = description;
                PackageId = packageId;
                GitUrl = gitUrl;
                Tools = tools;
            }
        }
    }
}
