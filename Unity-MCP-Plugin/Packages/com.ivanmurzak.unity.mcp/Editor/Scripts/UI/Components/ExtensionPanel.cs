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
using System.Net.Http;
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

        /// <summary>Raised whenever the extension moves to a different install/update state.</summary>
        internal event Action? StateChanged;

        /// <summary>The extension is not installed and can be installed right now.</summary>
        internal bool CanInstall => _state == PanelState.Install;

        /// <summary>A newer version is available and the extension can be updated right now.</summary>
        internal bool CanUpdate => _state == PanelState.Update;

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
        public void RefreshStatus(string? installedVersion)
        {
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
            try
            {
                var latestVersion = await FetchLatestOpenUpmVersionAsync(_data.PackageId);
                if (latestVersion != null && UpdateChecker.IsNewerVersion(latestVersion, installedVersion))
                {
                    EditorApplication.delayCall += () => ShowAsUpdate(installedVersion, latestVersion);
                    return;
                }
            }
            catch
            {
                // Network failure is non-critical
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
        /// Installs or updates the given extensions to their latest OpenUPM versions: every package is written
        /// to manifest.json first, then a single <see cref="Client.Resolve"/> picks them all up, so a bulk action
        /// costs one package resolve and one recompile. An extension that fails keeps its button usable and does
        /// not stop the others.
        /// </summary>
        internal static async Task InstallOrUpdateAsync(IReadOnlyList<ExtensionPanel> panels)
        {
            var wasUpdate = panels.Select(panel => panel.ShowAsBusy()).ToArray();
            var versions = await Task.WhenAll(panels.Select(panel => TryFetchLatestVersionAsync(panel._data)));

            var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
            var added = 0;
            for (var i = 0; i < panels.Count; i++)
            {
                if (TryAddToManifest(manifestPath, panels[i]._data, versions[i], wasUpdate[i]))
                    added++;
                else
                    panels[i].ResetButton(wasUpdate[i]);
            }

            if (added > 0)
            {
                Debug.Log($"[Unity-MCP] {added} extension(s) added to manifest.json. Resolving packages...");
                Client.Resolve();
            }
        }

        /// <summary>Switches the button to its in-progress look. Returns whether this is an update, not an install.</summary>
        private bool ShowAsBusy()
        {
            var isUpdate = _state == PanelState.Update;
            _actionBtn.UnregisterCallback<ClickEvent>(OnActionClicked);
            _actionBtn.SetEnabled(false);
            _actionBtn.text = isUpdate ? "Updating..." : "Installing...";
            SetState(PanelState.Busy);
            return isUpdate;
        }

        private static async Task<string?> TryFetchLatestVersionAsync(ExtensionData data)
        {
            try
            {
                return await FetchLatestOpenUpmVersionAsync(data.PackageId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Unity-MCP] Failed to fetch latest version for '{data.Name}' from OpenUPM: {ex.Message}");
                return null;
            }
        }

        private static bool TryAddToManifest(string manifestPath, ExtensionData data, string? version, bool isUpdate)
        {
            if (version == null)
            {
                Debug.LogError($"[Unity-MCP] Failed to {(isUpdate ? "update" : "install")} extension '{data.Name}': its latest version is unknown.");
                return false;
            }

            try
            {
                return AddToManifest(manifestPath, data.PackageId, version);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity-MCP] Failed to {(isUpdate ? "update" : "install")} extension '{data.Name}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Package specs ("id@version") that bring the installed extensions to their latest versions, for the plugin
        /// update to apply in the same request. Best effort: an extension whose version cannot be fetched is skipped
        /// with a warning, because it must never block the plugin update itself.
        /// </summary>
        internal static async Task<string[]> GetInstalledExtensionUpdateSpecsAsync(IEnumerable<ExtensionData> catalog)
        {
            try
            {
                var installed = await ListInstalledPackagesAsync();
                var extensions = catalog.Where(data => installed.ContainsKey(data.PackageId)).ToArray();
                var latest = await Task.WhenAll(extensions.Select(data => TryFetchLatestVersionAsync(data)));

                var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
                var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject();
                return BuildExtensionUpdateSpecs(
                    extensions.Select((data, i) =>
                    {
                        var package = installed[data.PackageId];
                        return new InstalledExtension(data.PackageId, package.version, latest[i], package.source == PackageSource.Registry);
                    }),
                    manifest?["dependencies"]?.AsObject(),
                    Application.unityVersion);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Unity-MCP] Updating without the installed extensions: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static Task<Dictionary<string, UnityEditor.PackageManager.PackageInfo>> ListInstalledPackagesAsync()
        {
            var completion = new TaskCompletionSource<Dictionary<string, UnityEditor.PackageManager.PackageInfo>>();
            var request = Client.List();
            EditorApplication.update += OnUpdate;
            return completion.Task;

            void OnUpdate()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= OnUpdate;
                if (request.Status == StatusCode.Success)
                    completion.SetResult(request.Result.ToDictionary(package => package.name, package => package));
                else
                    completion.SetException(new InvalidOperationException(request.Error?.message ?? "Failed to list packages."));
            }
        }

        /// <summary>
        /// Picks what the plugin update must bring along: extensions behind their latest version plus the Unity
        /// package floors those need. Only registry installs qualify, so a <c>file:</c> or git checkout an extension
        /// is being developed from is never silently replaced by a registry version.
        /// </summary>
        internal static string[] BuildExtensionUpdateSpecs(IEnumerable<InstalledExtension> extensions, JsonObject? dependencies, string unityVersion)
        {
            var specs = new List<string>();
            foreach (var extension in extensions)
            {
                if (!extension.IsFromRegistry || extension.LatestVersion == null
                    || !UpdateChecker.IsNewerVersion(extension.LatestVersion, extension.InstalledVersion))
                    continue;

                specs.Add($"{extension.PackageId}@{extension.LatestVersion}");
                foreach (var required in MissingCompatibilityDependencies(extension.PackageId, dependencies, unityVersion))
                {
                    var spec = $"{required.Key}@{required.Value}";
                    if (!specs.Contains(spec))
                        specs.Add(spec);
                }
            }
            return specs.ToArray();
        }

        /// <summary>The Unity package floors of <paramref name="packageId"/> that the manifest does not satisfy yet.</summary>
        private static IEnumerable<KeyValuePair<string, string>> MissingCompatibilityDependencies(string packageId, JsonObject? dependencies, string unityVersion)
        {
            foreach (var required in UnityCompatibilityDependencies(packageId, unityVersion))
            {
                var current = dependencies?[required.Key]?.GetValue<string>();
                if (string.IsNullOrEmpty(current) || (System.Version.TryParse(current, out var prior)
                    && System.Version.Parse(required.Value) > prior))
                    yield return required;
            }
        }

        private void ResetButton(bool isUpdate)
        {
            _actionBtn.SetEnabled(true);
            _actionBtn.text = isUpdate ? "Update" : "Install";
            _actionBtn.RegisterCallback<ClickEvent>(OnActionClicked);
            SetState(isUpdate ? PanelState.Update : PanelState.Install);
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

        /// <summary>
        /// Fetches the latest version of a package from the OpenUPM registry.
        /// </summary>
        private static async Task<string?> FetchLatestOpenUpmVersionAsync(string packageId)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Unity-MCP-ExtensionPanel");
            client.Timeout = TimeSpan.FromSeconds(10);

            var json = await client.GetStringAsync($"{RegistryUrl}/{packageId}");
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("dist-tags", out var distTags))
                return null;

            if (!distTags.TryGetProperty("latest", out var latest))
                return null;

            return latest.GetString();
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

        /// <summary>An installed extension as the plugin update sees it.</summary>
        internal readonly struct InstalledExtension
        {
            public string PackageId { get; }
            public string InstalledVersion { get; }
            public string? LatestVersion { get; }
            public bool IsFromRegistry { get; }

            public InstalledExtension(string packageId, string installedVersion, string? latestVersion, bool isFromRegistry)
            {
                PackageId = packageId;
                InstalledVersion = installedVersion;
                LatestVersion = latestVersion;
                IsFromRegistry = isFromRegistry;
            }
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
