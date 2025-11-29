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
using com.IvanMurzak.Unity.MCP;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
    private TextField? filterField;
    private DropdownField? typeDropdown;
    private Label? filterStatsLabel;

    readonly Microsoft.Extensions.Logging.ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(McpToolsWindow));

    public static void ShowWindow()
    {
        var wnd = GetWindow<McpToolsWindow>();
        wnd.titleContent = new GUIContent("MCP Tools");
    }
    public void CreateGUI()
    {
        rootVisualElement.Clear();

        InitializePlugin();

        var visualTree = LoadVisualTreeAsset(WindowUxmlPaths, "MCPToolsWindow");
        if (visualTree == null)
            return;

        visualTree.CloneTree(rootVisualElement);
        ApplyStyleSheets(rootVisualElement);

        toolItemTemplate = LoadVisualTreeAsset(ToolItemUxmlPaths, "ToolItem");
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
    }

    private void RefreshTools()
    {
        var toolManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ToolManager;
        var refreshed = new List<ToolViewModel>();

        if (toolManager != null)
        {
            foreach (var tool in toolManager.GetAllTools())
            {
                if (tool == null)
                    continue;

                refreshed.Add(BuildToolViewModel(toolManager, tool));
            }
        }

        allTools = refreshed;
    }

    private ToolViewModel BuildToolViewModel(IToolManager toolManager, IRunTool tool)
    {
        return new ToolViewModel
        {
            Name = tool.Name,
            Title = tool.Title,
            Description = tool.Description,
            IsEnabled = toolManager?.IsToolEnabled(tool.Name) == true,
            Inputs = ParseSchemaArguments(tool.InputSchema),
            Outputs = ParseSchemaArguments(tool.OutputSchema)
        };
    }

    private VisualTreeAsset? LoadVisualTreeAsset(IEnumerable<string> paths, string description)
    {
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (asset != null)
            {
                _logger.LogInformation("{method} Loaded {description} template from: {path}",
                    nameof(LoadVisualTreeAsset), description, path);
                return asset;
            }
        }

        _logger.LogWarning("{method} {description} template not found. Checked: {paths}",
            nameof(LoadVisualTreeAsset), description, string.Join(", ", paths));
        return null;
    }

    private void ApplyStyleSheets(VisualElement root)
    {
        foreach (var path in WindowUssPaths)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null)
                continue;

            try
            {
                root.styleSheets.Add(sheet);
                _logger.LogInformation("{method} Applied USS from: {path}",
                    nameof(ApplyStyleSheets), path);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{method} Failed to add USS '{path}': {message}",
                    nameof(ApplyStyleSheets), path, ex.Message);
                return;
            }
        }

        _logger.LogWarning("{method} USS not found; checked: {paths}",
            nameof(ApplyStyleSheets), string.Join(", ", WindowUssPaths));
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

        var filteredTools = FilterTools().ToList();
        UpdateFilterStats(filteredTools);

        toolListView.makeItem = MakeToolItem;
        toolListView.bindItem = (element, index) =>
        {
            if (index >= 0 && index < filteredTools.Count)
            {
                BindToolItem(element, filteredTools[index]);
            }
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
                }

                if (typeDropdown?.index != (int)ToolFilterType.All)
                {
                    EditorApplication.delayCall += PopulateToolList;
                }
            });
        }
        return toolItem;
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
            if (!string.IsNullOrEmpty(tool.Description))
            {
                descriptionFoldout.style.display = DisplayStyle.Flex;
                var descLabel = descriptionFoldout.Q<Label>("description-text");
                if (descLabel != null)
                    descLabel.text = tool.Description;
            }
            else
            {
                descriptionFoldout.style.display = DisplayStyle.None;
            }
        }

        PopulateArgumentFoldout(toolItem, "arguments-foldout", "arguments-container", "Input arguments", tool.Inputs);
        PopulateArgumentFoldout(toolItem, "outputs-foldout", "outputs-container", "Outputs", tool.Outputs);
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
        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public IReadOnlyList<ArgumentData> Inputs { get; set; } = Array.Empty<ArgumentData>();
        public IReadOnlyList<ArgumentData> Outputs { get; set; } = Array.Empty<ArgumentData>();
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
