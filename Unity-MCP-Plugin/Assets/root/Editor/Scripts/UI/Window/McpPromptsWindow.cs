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
using com.IvanMurzak.Unity.MCP.Utils;
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class McpPromptsWindow : EditorWindow
    {
        public enum PromptFilterType
        {
            All,
            Enabled,
            Disabled
        }
        private static readonly string[] WindowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpPromptsWindow.uxml");
        private static readonly string[] PromptItemUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/PromptItem.uxml");
        private static readonly string[] WindowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpPromptsWindow.uss");

        private const string FilterStatsFormat = "Filtered: {0}, Total: {1}";
        private const string MissingTemplateMessage =
            "PromptItem template is missing. Please ensure PromptItem.uxml exists in the package or the Assets/root folder.";

        private VisualTreeAsset? promptItemTemplate;
        private List<PromptViewModel> allPrompts = new();

        private ListView? promptListView;
        private Label? emptyListLabel;
        private TextField? filterField;
        private DropdownField? typeDropdown;
        private Label? filterStatsLabel;

        readonly Microsoft.Extensions.Logging.ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(McpPromptsWindow));

        public static McpPromptsWindow ShowWindow()
        {
            var window = GetWindow<McpPromptsWindow>("MCP Prompts");
            var icon = EditorAssetLoader.LoadAssetAtPath<Texture>(EditorAssetLoader.PackageLogoIcon);
            if (icon != null)
                window.titleContent = new GUIContent("MCP Prompts", icon);

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

            promptItemTemplate = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(PromptItemUxmlPaths, _logger);
            InitializeFilters(rootVisualElement);

            RefreshPrompts();
            PopulatePromptList();
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
                filterField.RegisterValueChangedCallback(evt => PopulatePromptList());

            typeDropdown = root.Q<DropdownField>("type-dropdown");
            if (typeDropdown != null)
            {
                typeDropdown.choices = Enum.GetNames(typeof(PromptFilterType)).ToList();
                typeDropdown.index = (int)PromptFilterType.All;
                typeDropdown.RegisterValueChangedCallback(evt => PopulatePromptList());
            }

            filterStatsLabel = root.Q<Label>("filter-stats-label");
            promptListView = root.Q<ListView>("prompt-list-view");
            emptyListLabel = root.Q<Label>("empty-list-label");
        }

        private void RefreshPrompts()
        {
            var promptManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.PromptManager;
            var refreshed = new List<PromptViewModel>();

            if (promptManager != null)
            {
                foreach (var prompt in promptManager.GetAllPrompts().Where(prompt => prompt != null))
                {
                    refreshed.Add(BuildPromptViewModel(promptManager, prompt));
                }
            }

            allPrompts = refreshed;
        }

        private PromptViewModel BuildPromptViewModel(IPromptManager promptManager, IRunPrompt prompt)
        {
            return new PromptViewModel(promptManager, prompt);
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

        private void PopulatePromptList()
        {
            if (promptListView == null)
            {
                _logger.LogWarning("{method} UI list view missing when populating prompt list.",
                    nameof(PopulatePromptList));
                return;
            }

            if (promptItemTemplate == null)
            {
                _logger.LogWarning(MissingTemplateMessage);
                return;
            }

            if (emptyListLabel == null)
            {
                _logger.LogWarning("{method} Empty list label missing when populating prompt list.",
                    nameof(PopulatePromptList));
                return;
            }

            var filteredPrompts = FilterPrompts().ToList();
            UpdateFilterStats(filteredPrompts);

            promptListView.visible = filteredPrompts.Count > 0;
            promptListView.style.display = filteredPrompts.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyListLabel.style.display = filteredPrompts.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            promptListView.makeItem = MakePromptItem;
            promptListView.bindItem = (element, index) =>
            {
                if (index >= 0 && index < filteredPrompts.Count)
                {
                    BindPromptItem(element, filteredPrompts[index]);
                }
            };
            promptListView.unbindItem = (element, index) =>
            {
                UnbindPromptItem(element);
            };

            promptListView.itemsSource = filteredPrompts;
            promptListView.selectionType = SelectionType.None;
            promptListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            promptListView.Rebuild();
        }

        private VisualElement MakePromptItem()
        {
            var promptItem = promptItemTemplate!.Instantiate();
            var promptToggle = promptItem.Q<Toggle>("prompt-toggle");
            var promptItemContainer = promptItem.Q<VisualElement>(null, "prompt-item-container") ?? promptItem;

            if (promptToggle != null)
            {
                promptToggle.RegisterValueChangedCallback(evt =>
                {
                    var prompt = promptItem.userData as PromptViewModel;
                    if (prompt == null) return;

                    promptToggle.EnableInClassList("checked", evt.newValue);
                    UpdatePromptItemClasses(promptItemContainer, evt.newValue);

                    var promptManager = UnityMcpPlugin.Instance.McpPluginInstance?.McpManager.PromptManager;
                    if (promptManager == null)
                    {
                        _logger.LogError("{method} PromptManager is not available.", nameof(MakePromptItem));
                        return;
                    }

                    prompt.IsEnabled = evt.newValue;
                    if (!string.IsNullOrWhiteSpace(prompt.Name))
                    {
                        _logger.LogTrace("{method} Setting prompt '{promptName}' enabled state to {enabled}.",
                            nameof(MakePromptItem), prompt.Name, evt.newValue);
                        promptManager.SetPromptEnabled(prompt.Name, evt.newValue);
                        UnityMcpPlugin.Instance.Save();
                    }

                    if (typeDropdown?.index != (int)PromptFilterType.All)
                    {
                        EditorApplication.delayCall += PopulatePromptList;
                    }
                });
            }
            else
            {
                _logger.LogWarning("{method} Prompt toggle missing in prompt item template.",
                    nameof(MakePromptItem));
            }

            promptItem.Query<Foldout>().ForEach(foldout =>
            {
                foldout.RegisterValueChangedCallback(evt =>
                {
                    UpdateFoldoutState(foldout, evt.newValue);
                    if (promptItem.userData is PromptViewModel prompt)
                    {
                        if (foldout.name == "description-foldout") prompt.descriptionExpanded.Value = evt.newValue;
                        else if (foldout.name == "arguments-foldout") prompt.argumentsExpanded.Value = evt.newValue;
                    }
                });
                UpdateFoldoutState(foldout, foldout.value);
            });

            return promptItem;
        }

        private void UpdateFoldoutState(Foldout foldout, bool expanded)
        {
            foldout.EnableInClassList("expanded", expanded);
            foldout.EnableInClassList("collapsed", !expanded);
        }

        private void BindPromptItem(VisualElement promptItem, PromptViewModel prompt)
        {
            promptItem.userData = prompt;

            var titleLabel = promptItem.Q<Label>("prompt-title");
            if (titleLabel != null)
                titleLabel.text = prompt.Title ?? prompt.Name;

            var idLabel = promptItem.Q<Label>("prompt-id");
            if (idLabel != null)
                idLabel.text = prompt.Name;

            var roleLabel = promptItem.Q<Label>("prompt-role");
            if (roleLabel != null)
                roleLabel.text = $"Role: {prompt.Role}";

            var promptToggle = promptItem.Q<Toggle>("prompt-toggle");
            if (promptToggle != null)
            {
                promptToggle.SetValueWithoutNotify(prompt.IsEnabled);
                promptToggle.EnableInClassList("checked", prompt.IsEnabled);
            }

            var promptItemContainer = promptItem.Q<VisualElement>(null, "prompt-item-container") ?? promptItem;
            UpdatePromptItemClasses(promptItemContainer, prompt.IsEnabled);

            var descriptionFoldout = promptItem.Q<Foldout>("description-foldout");
            if (descriptionFoldout != null)
            {
                var descLabel = descriptionFoldout.Q<Label>("description-text");
                if (descLabel != null)
                    descLabel.text = prompt.Description ?? string.Empty;

                var hasDescription = !string.IsNullOrEmpty(prompt.Description);
                descriptionFoldout.style.display = hasDescription ? DisplayStyle.Flex : DisplayStyle.None;

                descriptionFoldout.SetValueWithoutNotify(prompt.descriptionExpanded.Value);
                UpdateFoldoutState(descriptionFoldout, prompt.descriptionExpanded.Value);
            }
            else
            {
                _logger.LogWarning("{method} Description foldout missing for prompt: {promptName}",
                    nameof(BindPromptItem), prompt.Name);
            }

            var argumentsFoldout = promptItem.Q<Foldout>("arguments-foldout");
            if (argumentsFoldout != null)
            {
                argumentsFoldout.SetValueWithoutNotify(prompt.argumentsExpanded.Value);
                UpdateFoldoutState(argumentsFoldout, prompt.argumentsExpanded.Value);
            }
            else
            {
                _logger.LogWarning("{method} Arguments foldout missing for prompt: {promptName}",
                    nameof(BindPromptItem), prompt.Name);
            }

            PopulateArgumentFoldout(promptItem, "arguments-foldout", "arguments-container", "Arguments", prompt.Arguments);
        }

        private void UnbindPromptItem(VisualElement promptItem)
        {
            promptItem.userData = null;
        }

        private IEnumerable<PromptViewModel> FilterPrompts()
        {
            var filtered = allPrompts.AsEnumerable();

            var selectedType = PromptFilterType.All;
            if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            {
                if (Enum.TryParse<PromptFilterType>(typeDropdown.choices[typeDropdown.index], out var parsedType))
                    selectedType = parsedType;
            }

            filtered = selectedType switch
            {
                PromptFilterType.Enabled => filtered.Where(t => t.IsEnabled),
                PromptFilterType.Disabled => filtered.Where(t => !t.IsEnabled),
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

        private void UpdateFilterStats(IEnumerable<PromptViewModel> filteredPrompts)
        {
            if (filterStatsLabel == null)
                return;

            var filteredList = filteredPrompts.ToList();
            filterStatsLabel.text = string.Format(FilterStatsFormat, filteredList.Count, allPrompts.Count);
        }

        private void PopulateArgumentFoldout(VisualElement promptItem, string foldoutName, string containerName, string titlePrefix, IReadOnlyList<ArgumentData> arguments)
        {
            var foldout = promptItem.Q<Foldout>(foldoutName);
            if (foldout == null)
                return;

            var container = promptItem.Q(containerName);
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

        private void UpdatePromptItemClasses(VisualElement promptItemContainer, bool isEnabled)
        {
            if (promptItemContainer == null)
                return;

            promptItemContainer.EnableInClassList("enabled", isEnabled);
            promptItemContainer.EnableInClassList("disabled", !isEnabled);
        }

        private class PromptViewModel
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
