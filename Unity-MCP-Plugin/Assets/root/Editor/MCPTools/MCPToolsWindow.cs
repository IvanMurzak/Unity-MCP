/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.Unity.MCP;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MCPToolsWindow : EditorWindow
{
    public McpPlugin.IToolManager? Tools => McpPluginInstance?.McpManager.ToolManager;
    private static readonly string[] WindowUxmlPaths =
    {
        "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uxml/MCPToolsWindow.uxml",
        "Assets/root/Editor/UI/uxml/MCPToolsWindow.uxml"
    };

    private static readonly string[] ToolItemUxmlPaths =
    {
        "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uxml/ToolItem.uxml",
        "Assets/root/Editor/UI/uxml/ToolItem.uxml"
    };

    private static readonly string[] WindowUssPaths =
    {
        "Packages/com.ivanmurzak.unity.mcp/Editor/UI/uss/MCPToolsWindow.uss",
        "Assets/root/Editor/UI/uss/MCPToolsWindow.uss"
    };

    private const string FilterStatsFormat = "Filtered: {0}, Total: {1}";
    private const string MissingTemplateMessage =
        "ToolItem template is missing. Please ensure ToolItem.uxml exists in the package or the Assets/root folder.";

    private VisualTreeAsset? toolItemTemplate;
    private IToolManager? toolManager;
    private List<ToolViewModel> allTools = new();

    private ScrollView? toolListScrollView;
    private TextField? filterField;
    private DropdownField? typeDropdown;
    private Label? filterStatsLabel;

    [MenuItem("Window/MCP Tools")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<MCPToolsWindow>();
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
            typeDropdown.choices = new List<string> { "Enabled", "Disabled", "All" };
            typeDropdown.index = 0;
            typeDropdown.RegisterValueChangedCallback(evt => PopulateToolList());
        }

        filterStatsLabel = root.Q<Label>("filter-stats-label");
        toolListScrollView = root.Q<ScrollView>("tool-list-scrollview");
    }

    private void RefreshTools()
    {
        toolManager = ResolveToolManager();
        var refreshed = new List<ToolViewModel>();

        if (toolManager != null)
        {
            foreach (var tool in toolManager.GetAllTools() ?? Array.Empty<ITool>())
            {
                if (tool == null)
                    continue;

                refreshed.Add(BuildToolViewModel(tool));
            }
        }

        allTools = refreshed;
    }

    private IToolManager? ResolveToolManager()
    {
        return UnityMcpPlugin.Instance?.Tools;
    }

    private ToolViewModel BuildToolViewModel(ITool tool)
    {
        var toolName = tool.Name ?? string.Empty;
        var titleCandidate = tool.Title;
        var title = !string.IsNullOrWhiteSpace(titleCandidate) ? titleCandidate : toolName;
        var description = tool.Description ?? string.Empty;
        var isEnabled = !string.IsNullOrWhiteSpace(toolName) && toolManager?.IsToolEnabled(toolName) == true;

        return new ToolViewModel
        {
            Title = title,
            Name = toolName,
            Description = description,
            IsEnabled = isEnabled,
            Inputs = ParseSchemaArguments(tool.InputSchema),
            Outputs = ParseSchemaArguments(tool.OutputSchema)
        };
    }

    private static VisualTreeAsset? LoadVisualTreeAsset(IEnumerable<string> paths, string description)
    {
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (asset != null)
            {
                Debug.Log($"[MCPTools] Loaded {description} template from: {path}");
                return asset;
            }
        }

        Debug.LogWarning($"[MCPTools] {description} template not found. Checked: {string.Join(", ", paths)}");
        return null;
    }

    private static void ApplyStyleSheets(VisualElement root)
    {
        foreach (var path in WindowUssPaths)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null)
                continue;

            try
            {
                root.styleSheets.Add(sheet);
                Debug.Log($"[MCPTools] Applied USS from: {path}");
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCPTools] Failed to add USS '{path}': {ex.Message}");
                return;
            }
        }

        Debug.LogWarning($"[MCPTools] USS not found; checked: {string.Join(", ", WindowUssPaths)}");
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
        if (toolListScrollView == null)
        {
            Debug.LogWarning("[MCPTools] UI scroll view missing when populating tool list.");
            return;
        }

        toolListScrollView.Clear();

        if (toolItemTemplate == null)
        {
            toolListScrollView.Add(new Label(MissingTemplateMessage));
            return;
        }

        var filteredTools = FilterTools().ToList();
        UpdateFilterStats(filteredTools);

        foreach (var tool in filteredTools)
        {
            var toolItem = toolItemTemplate.Instantiate();

            var titleLabel = toolItem.Q<Label>("tool-title");
            if (titleLabel != null)
                titleLabel.text = tool.Title;

            var idLabel = toolItem.Q<Label>("tool-id");
            if (idLabel != null)
                idLabel.text = tool.Name;

            var toolToggle = toolItem.Q<Toggle>("tool-toggle");
            if (toolToggle != null)
            {
                toolToggle.value = tool.IsEnabled;
                toolToggle.EnableInClassList("checked", tool.IsEnabled);
            }

            var toolItemContainer = toolItem.Q<VisualElement>(null, "tool-item-container") ?? toolItem;
            UpdateToolItemClasses(toolItemContainer, toolToggle?.value ?? false);

            var toolId = tool.Name;

            if (toolToggle != null)
            {
                toolToggle.RegisterValueChangedCallback(evt =>
                {
                    toolToggle.EnableInClassList("checked", evt.newValue);
                    UpdateToolItemClasses(toolItemContainer, evt.newValue);

                    tool.IsEnabled = evt.newValue;
                    if (!string.IsNullOrWhiteSpace(toolId) && toolManager != null)
                    {
                        toolManager.SetToolEnabled(toolId, evt.newValue);
                    }

                    EditorApplication.delayCall += PopulateToolList;
                });
            }

            var descriptionFoldout = toolItem.Q<Foldout>("description-foldout");
            if (descriptionFoldout != null)
            {
                if (!string.IsNullOrEmpty(tool.Description))
                {
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
            toolListScrollView.Add(toolItem);
        }
    }

    private IEnumerable<ToolViewModel> FilterTools()
    {
        var filtered = allTools.AsEnumerable();

        var selectedType = "All";
        if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            selectedType = typeDropdown.choices[typeDropdown.index];

        if (selectedType == "Enabled")
            filtered = filtered.Where(t => t.IsEnabled);
        else if (selectedType == "Disabled")
            filtered = filtered.Where(t => !t.IsEnabled);

        var filterText = filterField?.value?.Trim();
        if (!string.IsNullOrEmpty(filterText))
        {
            filtered = filtered.Where(t =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.Name) && t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.Description) && t.Description.Contains(filterText, StringComparison.OrdinalIgnoreCase)));
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

        if (arguments.Count == 0)
        {
            foldout.style.display = DisplayStyle.None;
            return;
        }

        foldout.text = $"{titlePrefix} ({arguments.Count})";
        var container = toolItem.Q(containerName);
        if (container == null)
            return;

        foreach (var arg in arguments)
        {
            var argItem = new VisualElement();
            argItem.AddToClassList("argument-item");

            var nameLabel = new Label(arg.Name);
            nameLabel.AddToClassList("argument-name");
            argItem.Add(nameLabel);

            var descLabel = new Label(arg.Description);
            descLabel.AddToClassList("argument-description");
            argItem.Add(descLabel);

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
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
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
