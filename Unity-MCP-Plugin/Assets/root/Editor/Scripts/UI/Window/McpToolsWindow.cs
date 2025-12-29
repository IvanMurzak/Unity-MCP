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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class McpToolsWindow : McpListWindowBase<McpToolsWindow.ToolViewModel>
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpToolsWindow.uxml");
        private static readonly string[] _itemUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/ToolItem.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpToolsWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] ItemUxmlPaths => _itemUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => "MCP Tools";
        protected override string MissingTemplateMessage =>
            "ToolItem template is missing. Please ensure ToolItem.uxml exists in the package or the Assets/root folder.";

        public static McpToolsWindow ShowWindow()
        {
            var window = GetWindow<McpToolsWindow>("MCP Tools");
            window.SetupWindowWithIcon();
            window.Show();
            window.Focus();
            return window;
        }

        protected override void RefreshItems()
        {
            var toolManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ToolManager;
            var refreshed = new List<ToolViewModel>();

            if (toolManager != null)
            {
                foreach (var tool in toolManager.GetAllTools().Where(tool => tool != null))
                {
                    refreshed.Add(new ToolViewModel(toolManager, tool));
                }
            }

            allItems = refreshed;
        }

        protected override void OnItemToggleChanged(ToolViewModel viewModel, bool isEnabled)
        {
            var toolManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.ToolManager;
            if (toolManager == null)
            {
                Logger.LogError("{method} ToolManager is not available.", nameof(OnItemToggleChanged));
                return;
            }

            viewModel.IsEnabled = isEnabled;
            if (!string.IsNullOrWhiteSpace(viewModel.Name))
            {
                Logger.LogTrace("{method} Setting tool '{name}' enabled state to {enabled}.",
                    nameof(OnItemToggleChanged), viewModel.Name, isEnabled);
                toolManager.SetToolEnabled(viewModel.Name, isEnabled);
                UnityMcpPlugin.Instance.Save();
            }
        }

        protected override void OnFoldoutChanged(ToolViewModel viewModel, string foldoutName, bool isExpanded)
        {
            switch (foldoutName)
            {
                case "description-foldout":
                    viewModel.descriptionExpanded.Value = isExpanded;
                    break;
                case "arguments-foldout":
                    viewModel.inputsExpanded.Value = isExpanded;
                    break;
                case "outputs-foldout":
                    viewModel.outputsExpanded.Value = isExpanded;
                    break;
            }
        }

        protected override void BindItem(VisualElement element, ToolViewModel viewModel)
        {
            BindCommonItemFields(element, viewModel);
            BindDescriptionFoldout(element, viewModel, viewModel.descriptionExpanded.Value);

            var inputArgumentsFoldout = element.Q<Foldout>("arguments-foldout");
            if (inputArgumentsFoldout != null)
            {
                inputArgumentsFoldout.SetValueWithoutNotify(viewModel.inputsExpanded.Value);
                UpdateFoldoutState(inputArgumentsFoldout, viewModel.inputsExpanded.Value);
            }
            else
            {
                Logger.LogWarning("{method} Input arguments foldout missing for tool: {name}",
                    nameof(BindItem), viewModel.Name);
            }

            var outputsFoldout = element.Q<Foldout>("outputs-foldout");
            if (outputsFoldout != null)
            {
                outputsFoldout.SetValueWithoutNotify(viewModel.outputsExpanded.Value);
                UpdateFoldoutState(outputsFoldout, viewModel.outputsExpanded.Value);
            }
            else
            {
                Logger.LogWarning("{method} Outputs foldout missing for tool: {name}",
                    nameof(BindItem), viewModel.Name);
            }

            PopulateArgumentFoldout(element, "arguments-foldout", "arguments-container", "Input arguments", viewModel.Inputs);
            PopulateArgumentFoldout(element, "outputs-foldout", "outputs-container", "Outputs", viewModel.Outputs);
        }

        protected override IEnumerable<ToolViewModel> FilterByText(IEnumerable<ToolViewModel> items, string filterText)
        {
            return items.Where(t =>
                t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                (t.Title?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.Description?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true));
        }

        public class ToolViewModel : IMcpItemViewModel
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
    }
}
