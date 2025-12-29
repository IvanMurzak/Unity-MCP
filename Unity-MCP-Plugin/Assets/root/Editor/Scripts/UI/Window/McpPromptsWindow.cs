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
    public class McpPromptsWindow : McpListWindowBase<McpPromptsWindow.PromptViewModel>
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpPromptsWindow.uxml");
        private static readonly string[] _itemUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/PromptItem.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpPromptsWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] ItemUxmlPaths => _itemUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => "MCP Prompts";
        protected override string MissingTemplateMessage =>
            "PromptItem template is missing. Please ensure PromptItem.uxml exists in the package or the Assets/root folder.";

        public static McpPromptsWindow ShowWindow()
        {
            var window = GetWindow<McpPromptsWindow>("MCP Prompts");
            window.SetupWindowWithIcon();
            window.Show();
            window.Focus();
            return window;
        }

        protected override void RefreshItems()
        {
            var promptManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.PromptManager;
            var refreshed = new List<PromptViewModel>();

            if (promptManager != null)
            {
                foreach (var prompt in promptManager.GetAllPrompts().Where(prompt => prompt != null))
                {
                    refreshed.Add(new PromptViewModel(promptManager, prompt));
                }
            }

            allItems = refreshed;
        }

        protected override void OnItemToggleChanged(PromptViewModel viewModel, bool isEnabled)
        {
            var promptManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.PromptManager;
            if (promptManager == null)
            {
                Logger.LogError("{method} PromptManager is not available.", nameof(OnItemToggleChanged));
                return;
            }

            viewModel.IsEnabled = isEnabled;
            if (!string.IsNullOrWhiteSpace(viewModel.Name))
            {
                Logger.LogTrace("{method} Setting prompt '{name}' enabled state to {enabled}.",
                    nameof(OnItemToggleChanged), viewModel.Name, isEnabled);
                promptManager.SetPromptEnabled(viewModel.Name, isEnabled);
                UnityMcpPlugin.Instance.Save();
            }
        }

        protected override void OnFoldoutChanged(PromptViewModel viewModel, string foldoutName, bool isExpanded)
        {
            switch (foldoutName)
            {
                case "description-foldout":
                    viewModel.descriptionExpanded.Value = isExpanded;
                    break;
                case "arguments-foldout":
                    viewModel.argumentsExpanded.Value = isExpanded;
                    break;
            }
        }

        protected override void BindItem(VisualElement element, PromptViewModel viewModel)
        {
            BindCommonItemFields(element, viewModel);
            BindDescriptionFoldout(element, viewModel, viewModel.descriptionExpanded.Value);

            var roleLabel = element.Q<Label>("item-role");
            if (roleLabel != null)
                roleLabel.text = $"Role: {viewModel.Role}";

            var argumentsFoldout = element.Q<Foldout>("arguments-foldout");
            if (argumentsFoldout != null)
            {
                argumentsFoldout.SetValueWithoutNotify(viewModel.argumentsExpanded.Value);
                UpdateFoldoutState(argumentsFoldout, viewModel.argumentsExpanded.Value);
            }
            else
            {
                Logger.LogWarning("{method} Arguments foldout missing for prompt: {name}",
                    nameof(BindItem), viewModel.Name);
            }

            PopulateArgumentFoldout(element, "arguments-foldout", "arguments-container", "Arguments", viewModel.Arguments);
        }

        protected override IEnumerable<PromptViewModel> FilterByText(IEnumerable<PromptViewModel> items, string filterText)
        {
            return items.Where(t =>
                t.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                (t.Title?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true) ||
                (t.Description?.Contains(filterText, StringComparison.OrdinalIgnoreCase) == true));
        }

        public class PromptViewModel : IMcpItemViewModel
        {
            public string Name { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string Role { get; set; }
            public bool IsEnabled { get; set; }
            public IReadOnlyList<ArgumentData> Arguments { get; set; }
            public PlayerPrefsBool descriptionExpanded;
            public PlayerPrefsBool argumentsExpanded;

            public PromptViewModel(IPromptManager promptManager, IRunPrompt prompt)
            {
                Name = prompt.Name;
                Title = prompt.Title;
                Description = prompt.Description;
                Role = prompt.Role.ToString();
                IsEnabled = promptManager?.IsPromptEnabled(prompt.Name) == true;
                Arguments = ParseSchemaArguments(prompt.InputSchema);
                descriptionExpanded = new PlayerPrefsBool(GetFoldoutKey(prompt.Name, "description-foldout"));
                argumentsExpanded = new PlayerPrefsBool(GetFoldoutKey(prompt.Name, "arguments-foldout"));
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

            private string GetFoldoutKey(string promptName, string foldoutName)
            {
                var sanitizedName = promptName.Replace(" ", "_").Replace(".", "_");
                return $"Unity_MCP_PromptsWindow_{sanitizedName}_{foldoutName}_Expanded";
            }
        }
    }
}
