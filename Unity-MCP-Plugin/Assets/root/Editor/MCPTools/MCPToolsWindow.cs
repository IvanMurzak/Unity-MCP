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
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.Unity.MCP;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MCPToolsWindow : EditorWindow
{
    private VisualTreeAsset toolItemTemplate;
    private List<ToolViewModel> allTools = new();
    private object? toolManager;
    private MethodInfo? getAllToolsMethod;
    private MethodInfo? isToolEnabledMethod;
    private MethodInfo? setToolEnabledMethod;
    private const string FilterStatsFormat = "Filtered: {0}, Total: {1}";

    [MenuItem("Window/MCP Tools")]
    public static void ShowWindow()
    {
        MCPToolsWindow wnd = GetWindow<MCPToolsWindow>();
        wnd.titleContent = new GUIContent("MCP Tools");
    }

    public void CreateGUI()
    {
        UnityMcpPlugin.InitSingletonIfNeeded();
        UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();

        VisualElement root = rootVisualElement;

        string[] uxmlPaths =
        {
            "Assets/root/Editor/UI/uxml/MCPToolsWindow.uxml",
            "Assets/root/Editor/MCPTools/MCPToolsWindow.uxml",
            "Assets/UnityLocalAi/Editor/MCPTools/MCPToolsWindow.uxml",
            "Assets/root/Editor/UI/uxml/MCPToolsWindow.uxml"
        };

        VisualTreeAsset visualTree = null;
        foreach (var p in uxmlPaths)
        {
            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(p);
            if (visualTree != null)
            {
                Debug.Log($"[MCPTools] Loaded UXML from: {p}");
                break;
            }
        }

        if (visualTree == null)
        {
            Debug.LogWarning("[MCPTools] Failed to load MCPToolsWindow UXML. Checked: " + string.Join(", ", uxmlPaths));
            return;
        }

        visualTree.CloneTree(root);

        string[] ussPaths =
        {
            "Assets/root/Editor/UI/uss/MCPToolsWindow.uss",
            "Assets/root/Editor/MCPTools/MCPToolsWindow.uss",
            "Assets/UnityLocalAi/Editor/MCPTools/MCPToolsWindow.uss"
        };

        StyleSheet styleSheet = null;
        foreach (var p in ussPaths)
        {
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(p);
            if (styleSheet != null)
            {
                try
                {
                    root.styleSheets.Add(styleSheet);
                    Debug.Log($"[MCPTools] Applied USS from: {p}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MCPTools] Skipped adding USS due to error: {ex.Message}");
                }
                break;
            }
        }

        if (styleSheet == null)
        {
            Debug.LogWarning("[MCPTools] USS not found; continuing without stylesheet. Checked: " + string.Join(", ", ussPaths));
        }

        string[] toolItemPaths =
        {
            "Assets/root/Editor/UI/uxml/ToolItem.uxml",
            "Assets/root/Editor/MCPTools/ToolItem.uxml",
            "Assets/UnityLocalAi/Editor/MCPTools/ToolItem.uxml"
        };

        toolItemTemplate = null;
        foreach (var p in toolItemPaths)
        {
            toolItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(p);
            if (toolItemTemplate != null)
            {
                Debug.Log($"[MCPTools] Loaded ToolItem template from: {p}");
                break;
            }
        }

        if (toolItemTemplate == null)
        {
            Debug.LogWarning("[MCPTools] ToolItem UXML not found. Checked: " + string.Join(", ", toolItemPaths));
        }

        RefreshTools();

        var typeDropdown = root.Q<DropdownField>("type-dropdown");
        if (typeDropdown != null)
        {
            typeDropdown.choices = new List<string> { "Enabled", "Disabled", "All" };
            typeDropdown.index = 0;
            typeDropdown.RegisterValueChangedCallback(evt => PopulateToolList());
        }

        var filterField = root.Q<TextField>("filter-textfield");
        if (filterField != null)
        {
            filterField.RegisterValueChangedCallback(evt => PopulateToolList());
        }

        PopulateToolList();
    }

    private void RefreshTools()
    {
        toolManager = UnityMcpPlugin.HasInstance ? UnityMcpPlugin.Instance.Tools : null;
        if (toolManager != null)
        {
            getAllToolsMethod = toolManager.GetType().GetMethod("GetAllTools");
            isToolEnabledMethod = toolManager.GetType().GetMethod("IsToolEnabled", new[] { typeof(string) });
            setToolEnabledMethod = toolManager.GetType().GetMethod("SetToolEnabled", new[] { typeof(string), typeof(bool) });
        }

        var refreshed = new List<ToolViewModel>();
        if (toolManager != null && getAllToolsMethod != null)
        {
            var raw = getAllToolsMethod.Invoke(toolManager, null) as IEnumerable;
            if (raw != null)
            {
                foreach (var tool in raw)
                {
                    var toolName = GetStringProperty(tool, "Name");
                    var titleCandidate = GetStringProperty(tool, "Title");
                    var description = GetStringProperty(tool, "Description");
                    var title = !string.IsNullOrWhiteSpace(titleCandidate) ? titleCandidate : toolName;

                    var isEnabled = !string.IsNullOrWhiteSpace(toolName) && InvokeBoolMethod(isToolEnabledMethod, toolManager, toolName);

                    refreshed.Add(new ToolViewModel
                    {
                        Title = title,
                        Name = toolName,
                        Description = description,
                        IsEnabled = isEnabled,
                        Inputs = ParseSchemaArguments(GetSchemaProperty(tool, "InputSchema")),
                        Outputs = ParseSchemaArguments(GetSchemaProperty(tool, "OutputSchema"))
                    });
                }
            }
        }

        allTools = refreshed;
    }

    private static IReadOnlyList<ArgumentData> ParseSchemaArguments(JsonNode? schema)
    {
        var list = new List<ArgumentData>();
        if (schema?.AsObject()?.TryGetPropertyValue(SchemaPropertyNames.Properties, out var properties) != true)
            return list;

        foreach (var item in properties!.AsObject())
        {
            var inputName = item.Key;
            var description = string.Empty;
            if (item.Value?.AsObject().TryGetPropertyValue(SchemaPropertyNames.Description, out var desc) == true)
            {
                description = desc?.ToString() ?? string.Empty;
            }

            list.Add(new ArgumentData(inputName, description));
        }

        return list;
    }

    private static string GetStringProperty(object? obj, string propertyName)
    {
        if (obj == null)
            return string.Empty;

        var prop = obj.GetType().GetProperty(propertyName);
        return prop?.GetValue(obj)?.ToString() ?? string.Empty;
    }

    private static JsonNode? GetSchemaProperty(object? obj, string propertyName)
    {
        if (obj == null)
            return null;

        var prop = obj.GetType().GetProperty(propertyName);
        return prop?.GetValue(obj) as JsonNode;
    }

    private static bool InvokeBoolMethod(MethodInfo? method, object instance, params object[] args)
    {
        if (method == null)
            return false;

        var result = method.Invoke(instance, args);
        return result is bool b && b;
    }

    private static void InvokeVoidMethod(MethodInfo? method, object? instance, params object[] args)
    {
        if (method == null || instance == null)
            return;

        method.Invoke(instance, args);
    }

    private void PopulateToolList()
    {
        var scrollView = rootVisualElement.Q<ScrollView>("tool-list-scrollview");
        if (scrollView == null || toolItemTemplate == null)
        {
            Debug.LogWarning("[MCPTools] UI elements missing when populating tool list.");
            return;
        }

        scrollView.Clear();

        var root = rootVisualElement;
        var filterField = root.Q<TextField>("filter-textfield");
        string filterText = filterField?.value?.Trim() ?? string.Empty;

        var typeDropdown = root.Q<DropdownField>("type-dropdown");
        string selectedType = "All";
        if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            selectedType = typeDropdown.choices[typeDropdown.index];

        IEnumerable<ToolViewModel> filtered = allTools;
        if (selectedType == "Enabled")
            filtered = filtered.Where(t => t.IsEnabled);
        else if (selectedType == "Disabled")
            filtered = filtered.Where(t => !t.IsEnabled);

        if (!string.IsNullOrEmpty(filterText))
        {
            filtered = filtered.Where(t =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(t.Name) && t.Name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(t.Description) && t.Description.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        var filteredList = filtered.ToList();
        var statsLabel = root.Q<Label>("filter-stats-label");
        if (statsLabel != null)
            statsLabel.text = string.Format(FilterStatsFormat, filteredList.Count, allTools.Count);

        foreach (var tool in filteredList)
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
            UpdateToolItemClasses(toolItemContainer, toolToggle != null && toolToggle.value);

            var capturedTool = tool;
            var capturedContainer = toolItemContainer;
            var capturedToggle = toolToggle;
            var toolId = tool.Name;

            if (toolToggle != null)
            {
                toolToggle.RegisterValueChangedCallback(evt =>
                {
                    if (capturedToggle != null)
                        capturedToggle.EnableInClassList("checked", evt.newValue);

                    UpdateToolItemClasses(capturedContainer, evt.newValue);

                    capturedTool.IsEnabled = evt.newValue;
                    if (!string.IsNullOrWhiteSpace(toolId))
                    {
                        InvokeVoidMethod(setToolEnabledMethod, toolManager, toolId, evt.newValue);
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
            scrollView.Add(toolItem);
        }
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

    private static class SchemaPropertyNames
    {
        public const string Properties = "properties";
        public const string Description = "description";
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
