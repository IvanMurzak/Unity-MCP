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
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class McpToolsWindow : EditorWindow
    {
        public enum ToolFilterType
        {
            All,
            Enabled,
            Disabled
        }
        private static readonly string[] WindowUxmlPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uxml/McpToolsWindow.uxml",
            "Assets/root/Editor/UI/uxml/McpToolsWindow.uxml"
        };

        private static readonly string[] ToolItemUxmlPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uxml/ToolItem.uxml",
            "Assets/root/Editor/UI/uxml/ToolItem.uxml"
        };

        private static readonly string[] WindowUssPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uss/McpToolsWindow.uss",
            "Assets/root/Editor/UI/uss/McpToolsWindow.uss"
        };

        private const string FilterStatsFormat = "Filtered: {0}, Total: {1}";
        private const string MissingTemplateMessage =
            "ToolItem template is missing. Please ensure ToolItem.uxml exists in the package or the Assets/root folder.";

        private VisualTreeAsset? toolItemTemplate;
        private List<ToolViewModel> allTools = new();

        private ListView? toolListView;
        private Label? emptyListLabel;
        private TextField? filterField;
        private DropdownField? typeDropdown;
        private Label? filterStatsLabel;

        readonly Microsoft.Extensions.Logging.ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(McpToolsWindow));

        public static McpToolsWindow ShowWindow()
        {
            var window = GetWindow<McpToolsWindow>("MCP Tools");
            var icon = EditorAssetLoader.LoadAssetAtPath<Texture>(EditorAssetLoader.PackageLogoIcon);
            if (icon != null)
                window.titleContent = new GUIContent("MCP Tools", icon);

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

            toolItemTemplate = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(ToolItemUxmlPaths, _logger);
            InitializeFilters(rootVisualElement);

            RefreshTools();
            PopulateToolList();
        }

        private void InitializePlugin()
        {
            UnityMcpPlugin.InitSingletonIfNeeded();
            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
        }

        private void InitializeFilters(VisualElement root)
        {
            filterField = root.Q<TextField>("filter-textfield");
            if (filterField != null)
                filterField.RegisterValueChangedCallback(evt => PopulateToolList());

            typeDropdown = root.Q<DropdownField>("type-dropdown");
            if (typeDropdown != null)
            {
                typeDropdown.choices = Enum.GetNames(typeof(ToolFilterType)).ToList();
                typeDropdown.index = (int)ToolFilterType.All;
                typeDropdown.RegisterValueChangedCallback(evt => PopulateToolList());
            }

            filterStatsLabel = root.Q<Label>("filter-stats-label");
            toolListView = root.Q<ListView>("tool-list-view");
            emptyListLabel = root.Q<Label>("empty-list-label");
        }

        private void RefreshTools()
        {
            var toolManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ToolManager;
            var refreshed = new List<ToolViewModel>();

            if (toolManager != null)
            {
                foreach (var tool in toolManager.GetAllTools().Where(tool => tool != null))
                {
                    refreshed.Add(BuildToolViewModel(toolManager, tool));
                }
            }

            allTools = refreshed;
        }

        private ToolViewModel BuildToolViewModel(IToolManager toolManager, IRunTool tool)
        {
            return new ToolViewModel(toolManager, tool);
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

        private void PopulateToolList()
        {
            if (toolListView == null)
            {
                _logger.LogWarning("{method} UI list view missing when populating tool list.",
                    nameof(PopulateToolList));
                return;
            }

            if (toolItemTemplate == null)
            {
                _logger.LogWarning(MissingTemplateMessage);
                return;
            }

            if (emptyListLabel == null)
            {
                _logger.LogWarning("{method} Empty list label missing when populating tool list.",
                    nameof(PopulateToolList));
                return;
            }

            var filteredTools = FilterTools().ToList();
            UpdateFilterStats(filteredTools);

            toolListView.visible = filteredTools.Count > 0;
            toolListView.style.display = filteredTools.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyListLabel.style.display = filteredTools.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            toolListView.makeItem = MakeToolItem;
            toolListView.bindItem = (element, index) =>
            {
                if (index >= 0 && index < filteredTools.Count)
                {
                    BindToolItem(element, filteredTools[index]);
                }
            };
            toolListView.unbindItem = (element, index) =>
            {
                UnbindToolItem(element);
            };

            toolListView.itemsSource = filteredTools;
            toolListView.selectionType = SelectionType.None;
            toolListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            toolListView.Rebuild();
        }

        private VisualElement MakeToolItem()
        {
            var toolItem = toolItemTemplate!.Instantiate();
            var toolToggle = toolItem.Q<Toggle>("tool-toggle");
            var toolItemContainer = toolItem.Q<VisualElement>(null, "tool-item-container") ?? toolItem;

            if (toolToggle != null)
            {
                toolToggle.RegisterValueChangedCallback(evt =>
                {
                    var tool = toolItem.userData as ToolViewModel;
                    if (tool == null) return;

                    toolToggle.EnableInClassList("checked", evt.newValue);
                    UpdateToolItemClasses(toolItemContainer, evt.newValue);

                    var toolManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ToolManager;
                    if (toolManager == null)
                    {
                        _logger.LogError("{method} ToolManager is not available.", nameof(MakeToolItem));
                        return;
                    }

                    tool.IsEnabled = evt.newValue;
                    if (!string.IsNullOrWhiteSpace(tool.Name))
                    {
                        toolManager.SetToolEnabled(tool.Name, evt.newValue);
                        UnityMcpPlugin.Instance.Save();
                    }

                    if (typeDropdown?.index != (int)ToolFilterType.All)
                    {
                        EditorApplication.delayCall += PopulateToolList;
                    }
                });
            }
            else
            {
                _logger.LogWarning("{method} Tool toggle missing in tool item template.",
                    nameof(MakeToolItem));
            }

            toolItem.Query<Foldout>().ForEach(foldout =>
            {
                foldout.RegisterValueChangedCallback(evt =>
                {
                    UpdateFoldoutState(foldout, evt.newValue);
                    if (toolItem.userData is ToolViewModel tool)
                    {
                        if (foldout.name == "description-foldout") tool.descriptionExpanded.Value = evt.newValue;
                        else if (foldout.name == "arguments-foldout") tool.inputsExpanded.Value = evt.newValue;
                        else if (foldout.name == "outputs-foldout") tool.outputsExpanded.Value = evt.newValue;
                    }
                });
                UpdateFoldoutState(foldout, foldout.value);
            });

            return toolItem;
        }

        private void UpdateFoldoutState(Foldout foldout, bool expanded)
        {
            foldout.EnableInClassList("expanded", expanded);
            foldout.EnableInClassList("collapsed", !expanded);
        }

        private void BindToolItem(VisualElement toolItem, ToolViewModel tool)
        {
            toolItem.userData = tool;

            var titleLabel = toolItem.Q<Label>("tool-title");
            if (titleLabel != null)
                titleLabel.text = tool.Title;

            var idLabel = toolItem.Q<Label>("tool-id");
            if (idLabel != null)
                idLabel.text = tool.Name;

            var toolToggle = toolItem.Q<Toggle>("tool-toggle");
            if (toolToggle != null)
            {
                toolToggle.SetValueWithoutNotify(tool.IsEnabled);
                toolToggle.EnableInClassList("checked", tool.IsEnabled);
            }

            var toolItemContainer = toolItem.Q<VisualElement>(null, "tool-item-container") ?? toolItem;
            UpdateToolItemClasses(toolItemContainer, tool.IsEnabled);

            var descriptionFoldout = toolItem.Q<Foldout>("description-foldout");
            if (descriptionFoldout != null)
            {
                var descLabel = descriptionFoldout.Q<Label>("description-text");
                if (descLabel != null)
                    descLabel.text = tool.Description ?? string.Empty;

                var hasDescription = !string.IsNullOrEmpty(tool.Description);
                descriptionFoldout.style.display = hasDescription ? DisplayStyle.Flex : DisplayStyle.None;

                descriptionFoldout.SetValueWithoutNotify(tool.descriptionExpanded.Value);
                UpdateFoldoutState(descriptionFoldout, tool.descriptionExpanded.Value);
            }
            else
            {
                _logger.LogWarning("{method} Description foldout missing for tool: {toolName}",
                    nameof(BindToolItem), tool.Name);
            }

            var inputArgumentsFoldout = toolItem.Q<Foldout>("arguments-foldout");
            if (inputArgumentsFoldout != null)
            {
                inputArgumentsFoldout.SetValueWithoutNotify(tool.inputsExpanded.Value);
                UpdateFoldoutState(inputArgumentsFoldout, tool.inputsExpanded.Value);
            }
            else
            {
                _logger.LogWarning("{method} Input arguments foldout missing for tool: {toolName}",
                    nameof(BindToolItem), tool.Name);
            }

            var outputsFoldout = toolItem.Q<Foldout>("outputs-foldout");
            if (outputsFoldout != null)
            {
                outputsFoldout.SetValueWithoutNotify(tool.outputsExpanded.Value);
                UpdateFoldoutState(outputsFoldout, tool.outputsExpanded.Value);
            }
            else
            {
                _logger.LogWarning("{method} Outputs foldout missing for tool: {toolName}",
                    nameof(BindToolItem), tool.Name);
            }

            PopulateArgumentFoldout(toolItem, "arguments-foldout", "arguments-container", "Input arguments", tool.Inputs);
            PopulateArgumentFoldout(toolItem, "outputs-foldout", "outputs-container", "Outputs", tool.Outputs);
        }

        private void UnbindToolItem(VisualElement toolItem)
        {
            toolItem.userData = null;
        }

        private IEnumerable<ToolViewModel> FilterTools()
        {
            var filtered = allTools.AsEnumerable();

            var selectedType = ToolFilterType.All;
            if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            {
                if (Enum.TryParse<ToolFilterType>(typeDropdown.choices[typeDropdown.index], out var parsedType))
                    selectedType = parsedType;
            }

            filtered = selectedType switch
            {
                ToolFilterType.Enabled => filtered.Where(t => t.IsEnabled),
                ToolFilterType.Disabled => filtered.Where(t => !t.IsEnabled),
                _ => filtered
            };

            var filterText = filterField?.value?.Trim();
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = filtered.Where(t =>
                    t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    (t.Title?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (t.Description?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true));
            }

            return filtered;
        }

        private void UpdateFilterStats(IEnumerable<ToolViewModel> filteredTools)
        {
            if (filterStatsLabel == null)
                return;

            var filteredList = filteredTools.ToList();
            filterStatsLabel.text = string.Format(FilterStatsFormat, filteredList.Count, allTools.Count);
        }

        private void PopulateArgumentFoldout(VisualElement toolItem, string foldoutName, string containerName, string titlePrefix, IReadOnlyList<ArgumentData> arguments)
        {
            var foldout = toolItem.Q<Foldout>(foldoutName);
            if (foldout == null)
                return;

            var container = toolItem.Q(containerName);
            if (container == null)
                return;

            container.Clear();

            if (arguments.Count == 0)
            {
                foldout.style.display = DisplayStyle.None;
                return;
            }

            foldout.style.display = DisplayStyle.Flex;
            foldout.text = $"{titlePrefix} ({arguments.Count})";

            foreach (var arg in arguments)
            {
                var argItem = new VisualElement();
                argItem.AddToClassList("argument-item");

                var nameLabel = new Label(arg.Name);
                nameLabel.AddToClassList("argument-name");
                argItem.Add(nameLabel);

                if (!string.IsNullOrEmpty(arg.Description))
                {
                    var descLabel = new Label(arg.Description);
                    descLabel.AddToClassList("argument-description");
                    argItem.Add(descLabel);
                }

                container.Add(argItem);
            }
        }

        private void UpdateToolItemClasses(VisualElement toolItemContainer, bool isEnabled)
        {
            if (toolItemContainer == null)
                return;

            toolItemContainer.EnableInClassList("enabled", isEnabled);
            toolItemContainer.EnableInClassList("disabled", !isEnabled);
        }

        private class ToolViewModel
        {
            public string Name { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public bool IsEnabled { get; set; }
            public IReadOnlyList<ArgumentData> Inputs { get; set; }
            public IReadOnlyList<ArgumentData> Outputs { get; set; }
            public PlayerPrefsBool descriptionExpanded;
            public PlayerPrefsBool inputsExpanded;
            public PlayerPrefsBool outputsExpanded;

            public ToolViewModel(IToolManager toolManager, IRunTool tool)
            {
                Name = tool.Name;
                Title = tool.Title;
                Description = tool.Description;
                IsEnabled = toolManager?.IsToolEnabled(tool.Name) == true;
                Inputs = ParseSchemaArguments(tool.InputSchema);
                Outputs = ParseSchemaArguments(tool.OutputSchema);
                descriptionExpanded = new PlayerPrefsBool(GetFoldoutKey(tool.Name, "description-foldout"));
                inputsExpanded = new PlayerPrefsBool(GetFoldoutKey(tool.Name, "arguments-foldout"));
                outputsExpanded = new PlayerPrefsBool(GetFoldoutKey(tool.Name, "outputs-foldout"));
            }

            private IReadOnlyList<ArgumentData> ParseSchemaArguments(JsonNode? schema)
            {
                if (schema is not JsonObject schemaObject)
                    return Array.Empty<ArgumentData>();

                if (!schemaObject.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode))
                    return Array.Empty<ArgumentData>();

                if (propertiesNode is not JsonObject propertiesObject)
                    return Array.Empty<ArgumentData>();

                var arguments = new List<ArgumentData>();
                foreach (var (name, element) in propertiesObject)
                {
                    var description = string.Empty;
                    if (element is JsonObject propertyObject &&
                        propertyObject.TryGetPropertyValue(JsonSchema.Description, out var descriptionNode) &&
                        descriptionNode != null)
                    {
                        description = descriptionNode.ToString();
                    }

                    arguments.Add(new ArgumentData(name, description));
                }

                return arguments;
            }

            private string GetFoldoutKey(string toolName, string foldoutName)
            {
                var sanitizedName = toolName.Replace(" ", "_").Replace(".", "_");
                return $"Unity_MCP_ToolsWindow_{sanitizedName}_{foldoutName}_Expanded";
            }
        }

        public sealed class ArgumentData
        {
            public string Name { get; }
            public string Description { get; }

            public ArgumentData(string name, string description)
            {
                Name = name ?? string.Empty;
                Description = description ?? string.Empty;
            }
        }
    }
}
