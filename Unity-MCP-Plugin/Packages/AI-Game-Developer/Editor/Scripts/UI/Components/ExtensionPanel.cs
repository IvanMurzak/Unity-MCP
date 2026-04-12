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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            "extensions.unity",
            "org.nuget.com.ivanmurzak",
            "org.nuget.microsoft",
            "org.nuget.system",
            "org.nuget.r3"
        };

        private readonly Label _name;
        private readonly Label _description;
        private readonly Button _actionBtn;
        private readonly ExtensionData _data;

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

        private void ShowAsChecking()
        {
            _actionBtn.text = "Checking...";
            _actionBtn.tooltip = $"Checking status of {_data.Name} extension...";
            _actionBtn.RemoveFromClassList("btn-primary");
            _actionBtn.AddToClassList("btn-secondary");
            _actionBtn.style.display = DisplayStyle.Flex;
            _actionBtn.SetEnabled(false);
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
        }

        private async void OnActionClicked(ClickEvent evt)
        {
            _actionBtn.UnregisterCallback<ClickEvent>(OnActionClicked);
            await InstallOrUpdateAsync();
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
        }

        private async Task InstallOrUpdateAsync()
        {
            var isUpdate = _actionBtn.text == "Update";
            _actionBtn.SetEnabled(false);
            _actionBtn.text = isUpdate ? "Updating..." : "Installing...";

            try
            {
                var version = await FetchLatestOpenUpmVersionAsync(_data.PackageId);
                if (version == null)
                {
                    Debug.LogError($"[Unity-MCP] Failed to fetch latest version for '{_data.Name}' from OpenUPM.");
                    ResetButton(isUpdate);
                    return;
                }

                var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
                if (!AddToManifest(manifestPath, _data.PackageId, version))
                {
                    ResetButton(isUpdate);
                    return;
                }

                Debug.Log($"[Unity-MCP] Extension '{_data.Name}' ({_data.PackageId}@{version}) added to manifest.json. Resolving packages...");
                Client.Resolve();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity-MCP] Failed to {(isUpdate ? "update" : "install")} extension '{_data.Name}': {ex.Message}");
                ResetButton(isUpdate);
            }
        }

        private void ResetButton(bool isUpdate)
        {
            _actionBtn.SetEnabled(true);
            _actionBtn.text = isUpdate ? "Update" : "Install";
            _actionBtn.RegisterCallback<ClickEvent>(OnActionClicked);
        }

        /// <summary>
        /// Adds the OpenUPM scoped registry (if needed) and the package dependency to manifest.json.
        /// </summary>
        internal static bool AddToManifest(string manifestPath, string packageId, string version)
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

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(manifestPath, manifest.ToJsonString(options));
            return true;
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
