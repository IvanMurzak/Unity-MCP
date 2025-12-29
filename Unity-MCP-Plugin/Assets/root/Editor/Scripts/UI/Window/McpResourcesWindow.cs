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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class McpResourcesWindow : EditorWindow
    {
        public enum ResourceFilterType
        {
            All,
            Enabled,
            Disabled
        }
        private static readonly string[] WindowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpResourcesWindow.uxml");
        private static readonly string[] ResourceItemUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/ResourceItem.uxml");
        private static readonly string[] WindowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpResourcesWindow.uss");

        private const string FilterStatsFormat = "Filtered: {0}, Total: {1}";
        private const string MissingTemplateMessage =
            "ResourceItem template is missing. Please ensure ResourceItem.uxml exists in the package or the Assets/root folder.";

        private VisualTreeAsset? resourceItemTemplate;
        private List<ResourceViewModel> allResources = new();

        private ListView? resourceListView;
        private Label? emptyListLabel;
        private TextField? filterField;
        private DropdownField? typeDropdown;
        private Label? filterStatsLabel;

        readonly Microsoft.Extensions.Logging.ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(McpResourcesWindow));

        public static McpResourcesWindow ShowWindow()
        {
            var window = GetWindow<McpResourcesWindow>("MCP Resources");
            var icon = EditorAssetLoader.LoadAssetAtPath<Texture>(EditorAssetLoader.PackageLogoIcon);
            if (icon != null)
                window.titleContent = new GUIContent("MCP Resources", icon);

            window.Show();
            window.Focus();

            return window;
        }
        public void CreateGUI()
        {
            rootVisualElement.Clear();

            InitializePlugin();

            var visualTree = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPaths, _logger);
            if (visualTree == null)
                return;

            visualTree.CloneTree(rootVisualElement);
            ApplyStyleSheets(rootVisualElement);

            resourceItemTemplate = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(ResourceItemUxmlPaths, _logger);
            InitializeFilters(rootVisualElement);

            RefreshResources();
            PopulateResourceList();
        }

        private void InitializePlugin()
        {
            UnityMcpPlugin.InitSingletonIfNeeded();
            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
        }

        private void InitializeFilters(VisualElement root)
        {
            filterField = root.Q<TextField>("filter-textfield");
            if (filterField != null)
                filterField.RegisterValueChangedCallback(evt => PopulateResourceList());

            typeDropdown = root.Q<DropdownField>("type-dropdown");
            if (typeDropdown != null)
            {
                typeDropdown.choices = Enum.GetNames(typeof(ResourceFilterType)).ToList();
                typeDropdown.index = (int)ResourceFilterType.All;
                typeDropdown.RegisterValueChangedCallback(evt => PopulateResourceList());
            }

            filterStatsLabel = root.Q<Label>("filter-stats-label");
            resourceListView = root.Q<ListView>("resource-list-view");
            emptyListLabel = root.Q<Label>("empty-list-label");
        }

        private void RefreshResources()
        {
            var resourceManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ResourceManager;
            var refreshed = new List<ResourceViewModel>();

            if (resourceManager != null)
            {
                foreach (var resource in resourceManager.GetAllResources().Where(resource => resource != null))
                {
                    refreshed.Add(BuildResourceViewModel(resourceManager, resource));
                }
            }

            allResources = refreshed;
        }

        private ResourceViewModel BuildResourceViewModel(IResourceManager resourceManager, IRunResource resource)
        {
            return new ResourceViewModel(resourceManager, resource);
        }

        private void ApplyStyleSheets(VisualElement root)
        {
            var sheet = EditorAssetLoader.LoadAssetAtPath<StyleSheet>(WindowUssPaths, _logger);
            if (sheet == null)
            {
                _logger.LogWarning("{method} USS file not found.",
                    nameof(ApplyStyleSheets));
                return;
            }
            try
            {
                root.styleSheets.Add(sheet);
                _logger.LogTrace("{method} Applied USS",
                    nameof(ApplyStyleSheets));
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{method} Failed to add USS: {ex}",
                    nameof(ApplyStyleSheets), ex);
            }
        }

        private void PopulateResourceList()
        {
            if (resourceListView == null)
            {
                _logger.LogWarning("{method} UI list view missing when populating resource list.",
                    nameof(PopulateResourceList));
                return;
            }

            if (resourceItemTemplate == null)
            {
                _logger.LogWarning(MissingTemplateMessage);
                return;
            }

            if (emptyListLabel == null)
            {
                _logger.LogWarning("{method} Empty list label missing when populating resource list.",
                    nameof(PopulateResourceList));
                return;
            }

            var filteredResources = FilterResources().ToList();
            UpdateFilterStats(filteredResources);

            resourceListView.visible = filteredResources.Count > 0;
            resourceListView.style.display = filteredResources.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyListLabel.style.display = filteredResources.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            resourceListView.makeItem = MakeResourceItem;
            resourceListView.bindItem = (element, index) =>
            {
                if (index >= 0 && index < filteredResources.Count)
                {
                    BindResourceItem(element, filteredResources[index]);
                }
            };
            resourceListView.unbindItem = (element, index) =>
            {
                UnbindResourceItem(element);
            };

            resourceListView.itemsSource = filteredResources;
            resourceListView.selectionType = SelectionType.None;
            resourceListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            resourceListView.Rebuild();
        }

        private VisualElement MakeResourceItem()
        {
            var resourceItem = resourceItemTemplate!.Instantiate();
            var resourceToggle = resourceItem.Q<Toggle>("resource-toggle");
            var resourceItemContainer = resourceItem.Q<VisualElement>(null, "resource-item-container") ?? resourceItem;

            if (resourceToggle != null)
            {
                resourceToggle.RegisterValueChangedCallback(evt =>
                {
                    var resource = resourceItem.userData as ResourceViewModel;
                    if (resource == null) return;

                    resourceToggle.EnableInClassList("checked", evt.newValue);
                    UpdateResourceItemClasses(resourceItemContainer, evt.newValue);

                    var resourceManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ResourceManager;
                    if (resourceManager == null)
                    {
                        _logger.LogError("{method} ResourceManager is not available.", nameof(MakeResourceItem));
                        return;
                    }

                    resource.IsEnabled = evt.newValue;
                    if (!string.IsNullOrWhiteSpace(resource.Name))
                    {
                        _logger.LogTrace("{method} Setting resource '{resourceName}' enabled state to {enabled}.",
                            nameof(MakeResourceItem), resource.Name, evt.newValue);
                        resourceManager.SetResourceEnabled(resource.Name, evt.newValue);
                        UnityMcpPlugin.Instance.Save();
                    }

                    if (typeDropdown?.index != (int)ResourceFilterType.All)
                    {
                        EditorApplication.delayCall += PopulateResourceList;
                    }
                });
            }
            else
            {
                _logger.LogWarning("{method} Resource toggle missing in resource item template.",
                    nameof(MakeResourceItem));
            }

            resourceItem.Query<Foldout>().ForEach(foldout =>
            {
                foldout.RegisterValueChangedCallback(evt =>
                {
                    UpdateFoldoutState(foldout, evt.newValue);
                    if (resourceItem.userData is ResourceViewModel resource)
                    {
                        if (foldout.name == "description-foldout") resource.descriptionExpanded.Value = evt.newValue;
                    }
                });
                UpdateFoldoutState(foldout, foldout.value);
            });

            return resourceItem;
        }

        private void UpdateFoldoutState(Foldout foldout, bool expanded)
        {
            foldout.EnableInClassList("expanded", expanded);
            foldout.EnableInClassList("collapsed", !expanded);
        }

        private void BindResourceItem(VisualElement resourceItem, ResourceViewModel resource)
        {
            resourceItem.userData = resource;

            var titleLabel = resourceItem.Q<Label>("resource-title");
            if (titleLabel != null)
                titleLabel.text = resource.Title ?? resource.Name;

            var idLabel = resourceItem.Q<Label>("resource-id");
            if (idLabel != null)
                idLabel.text = resource.Name;

            var uriLabel = resourceItem.Q<Label>("resource-uri");
            if (uriLabel != null)
            {
                uriLabel.text = resource.Uri ?? string.Empty;
                uriLabel.style.display = string.IsNullOrEmpty(resource.Uri) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            var mimeTypeLabel = resourceItem.Q<Label>("resource-mimetype");
            if (mimeTypeLabel != null)
            {
                mimeTypeLabel.text = string.IsNullOrEmpty(resource.MimeType) ? string.Empty : $"MimeType: {resource.MimeType}";
                mimeTypeLabel.style.display = string.IsNullOrEmpty(resource.MimeType) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            var resourceToggle = resourceItem.Q<Toggle>("resource-toggle");
            if (resourceToggle != null)
            {
                resourceToggle.SetValueWithoutNotify(resource.IsEnabled);
                resourceToggle.EnableInClassList("checked", resource.IsEnabled);
            }

            var resourceItemContainer = resourceItem.Q<VisualElement>(null, "resource-item-container") ?? resourceItem;
            UpdateResourceItemClasses(resourceItemContainer, resource.IsEnabled);

            var descriptionFoldout = resourceItem.Q<Foldout>("description-foldout");
            if (descriptionFoldout != null)
            {
                var descLabel = descriptionFoldout.Q<Label>("description-text");
                if (descLabel != null)
                    descLabel.text = resource.Description ?? string.Empty;

                var hasDescription = !string.IsNullOrEmpty(resource.Description);
                descriptionFoldout.style.display = hasDescription ? DisplayStyle.Flex : DisplayStyle.None;

                descriptionFoldout.SetValueWithoutNotify(resource.descriptionExpanded.Value);
                UpdateFoldoutState(descriptionFoldout, resource.descriptionExpanded.Value);
            }
            else
            {
                _logger.LogWarning("{method} Description foldout missing for resource: {resourceName}",
                    nameof(BindResourceItem), resource.Name);
            }
        }

        private void UnbindResourceItem(VisualElement resourceItem)
        {
            resourceItem.userData = null;
        }

        private IEnumerable<ResourceViewModel> FilterResources()
        {
            var filtered = allResources.AsEnumerable();

            var selectedType = ResourceFilterType.All;
            if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            {
                if (Enum.TryParse<ResourceFilterType>(typeDropdown.choices[typeDropdown.index], out var parsedType))
                    selectedType = parsedType;
            }

            filtered = selectedType switch
            {
                ResourceFilterType.Enabled => filtered.Where(t => t.IsEnabled),
                ResourceFilterType.Disabled => filtered.Where(t => !t.IsEnabled),
                _ => filtered
            };

            var filterText = filterField?.value?.Trim();
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(t =>
                    t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    (t.Title?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (t.Description?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (t.Uri?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true));
            }

            return filtered;
        }

        private void UpdateFilterStats(IEnumerable<ResourceViewModel> filteredResources)
        {
            if (filterStatsLabel == null)
                return;

            var filteredList = filteredResources.ToList();
            filterStatsLabel.text = string.Format(FilterStatsFormat, filteredList.Count, allResources.Count);
        }

        private void UpdateResourceItemClasses(VisualElement resourceItemContainer, bool isEnabled)
        {
            if (resourceItemContainer == null)
                return;

            resourceItemContainer.EnableInClassList("enabled", isEnabled);
            resourceItemContainer.EnableInClassList("disabled", !isEnabled);
        }

        private class ResourceViewModel
        {
            public string Name { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Uri { get; set; }
            public string? MimeType { get; set; }
            public bool IsEnabled { get; set; }
            public PlayerPrefsBool descriptionExpanded;

            public ResourceViewModel(IResourceManager resourceManager, IRunResource resource)
            {
                Name = resource.Name;
                Title = resource.Name; // TODO: add name
                Description = resource.Description;
                Uri = resource.Route;
                MimeType = resource.MimeType;
                IsEnabled = resourceManager?.IsResourceEnabled(resource.Name) == true;
                descriptionExpanded = new PlayerPrefsBool(GetFoldoutKey(resource.Name, "description-foldout"));
            }

            private string GetFoldoutKey(string resourceName, string foldoutName)
            {
                var sanitizedName = resourceName.Replace(" ", "_").Replace(".", "_");
                return $"Unity_MCP_ResourcesWindow_{sanitizedName}_{foldoutName}_Expanded";
            }
        }
    }
}
