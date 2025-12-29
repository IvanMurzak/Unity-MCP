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
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class McpResourcesWindow : McpListWindowBase<McpResourcesWindow.ResourceViewModel>
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpResourcesWindow.uxml");
        private static readonly string[] _itemUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/ResourceItem.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpResourcesWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] ItemUxmlPaths => _itemUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => "MCP Resources";
        protected override string MissingTemplateMessage =>
            "ResourceItem template is missing. Please ensure ResourceItem.uxml exists in the package or the Assets/root folder.";

        public static McpResourcesWindow ShowWindow()
        {
            var window = GetWindow<McpResourcesWindow>("MCP Resources");
            window.SetupWindowWithIcon();
            window.Show();
            window.Focus();
            return window;
        }

        protected override void RefreshItems()
        {
            var resourceManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ResourceManager;
            var refreshed = new List<ResourceViewModel>();

            if (resourceManager != null)
            {
                foreach (var resource in resourceManager.GetAllResources().Where(resource => resource != null))
                {
                    refreshed.Add(new ResourceViewModel(resourceManager, resource));
                }
            }

            allItems = refreshed;
        }

        protected override void OnItemToggleChanged(ResourceViewModel viewModel, bool isEnabled)
        {
            var resourceManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ResourceManager;
            if (resourceManager == null)
            {
                Logger.LogError("{method} ResourceManager is not available.", nameof(OnItemToggleChanged));
                return;
            }

            viewModel.IsEnabled = isEnabled;
            if (!string.IsNullOrWhiteSpace(viewModel.Name))
            {
                Logger.LogTrace("{method} Setting resource '{name}' enabled state to {enabled}.",
                    nameof(OnItemToggleChanged), viewModel.Name, isEnabled);
                resourceManager.SetResourceEnabled(viewModel.Name, isEnabled);
                UnityMcpPlugin.Instance.Save();
            }
        }

        protected override void OnFoldoutChanged(ResourceViewModel viewModel, string foldoutName, bool isExpanded)
        {
            if (foldoutName == "description-foldout")
            {
                viewModel.descriptionExpanded.Value = isExpanded;
            }
        }

        protected override void BindItem(VisualElement element, ResourceViewModel viewModel)
        {
            BindCommonItemFields(element, viewModel);
            BindDescriptionFoldout(element, viewModel, viewModel.descriptionExpanded.Value);

            var uriLabel = element.Q<Label>("item-uri");
            if (uriLabel != null)
            {
                uriLabel.text = viewModel.Uri ?? string.Empty;
                uriLabel.style.display = string.IsNullOrEmpty(viewModel.Uri) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            var mimeTypeLabel = element.Q<Label>("item-mimetype");
            if (mimeTypeLabel != null)
            {
                mimeTypeLabel.text = string.IsNullOrEmpty(viewModel.MimeType) ? string.Empty : $"MimeType: {viewModel.MimeType}";
                mimeTypeLabel.style.display = string.IsNullOrEmpty(viewModel.MimeType) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        protected override IEnumerable<ResourceViewModel> FilterByText(IEnumerable<ResourceViewModel> items, string filterText)
        {
            return items.Where(t =>
                t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                (t.Title?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.Description?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.Uri?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true));
        }

        public class ResourceViewModel : IMcpItemViewModel
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
