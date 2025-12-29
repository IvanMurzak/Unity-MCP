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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Filter type for MCP list windows (Tools, Prompts, Resources).
    /// </summary>
    public enum McpFilterType
    {
        All,
        Enabled,
        Disabled
    }

    /// <summary>
    /// Base class for MCP list windows providing common ListView functionality.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type for list items.</typeparam>
    public abstract class McpListWindowBase<TViewModel> : McpWindowBase
        where TViewModel : class, IMcpItemViewModel
    {
        private const string FilterStatsFormat = "Filtered: {0}, Total: {1}";

        protected abstract string[] ItemUxmlPaths { get; }
        protected abstract string MissingTemplateMessage { get; }

        protected VisualTreeAsset? itemTemplate;
        protected List<TViewModel> allItems = new();

        protected ListView? listView;
        protected Label? emptyListLabel;
        protected TextField? filterField;
        protected DropdownField? typeDropdown;
        protected Label? filterStatsLabel;

        public override void CreateGUI()
        {
            rootVisualElement.Clear();

            InitializePlugin();

            var visualTree = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPaths, Logger);
            if (visualTree == null)
                return;

            visualTree.CloneTree(rootVisualElement);
            ApplyStyleSheets(rootVisualElement);

            itemTemplate = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(ItemUxmlPaths, Logger);
            InitializeFilters(rootVisualElement);

            RefreshItems();
            PopulateList();
        }

        protected void InitializePlugin()
        {
            UnityMcpPlugin.InitSingletonIfNeeded();
            UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
            UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
        }

        protected void InitializeFilters(VisualElement root)
        {
            filterField = root.Q<TextField>("filter-textfield");
            if (filterField != null)
                filterField.RegisterValueChangedCallback(evt => PopulateList());

            typeDropdown = root.Q<DropdownField>("type-dropdown");
            if (typeDropdown != null)
            {
                typeDropdown.choices = Enum.GetNames(typeof(McpFilterType)).ToList();
                typeDropdown.index = (int)McpFilterType.All;
                typeDropdown.RegisterValueChangedCallback(evt => PopulateList());
            }

            filterStatsLabel = root.Q<Label>("filter-stats-label");
            listView = root.Q<ListView>("mcp-list-view");
            emptyListLabel = root.Q<Label>("empty-list-label");
        }

        protected abstract void RefreshItems();

        protected void PopulateList()
        {
            if (listView == null)
            {
                Logger.LogWarning("{method} UI list view missing.", nameof(PopulateList));
                return;
            }

            if (itemTemplate == null)
            {
                Logger.LogWarning(MissingTemplateMessage);
                return;
            }

            if (emptyListLabel == null)
            {
                Logger.LogWarning("{method} Empty list label missing.", nameof(PopulateList));
                return;
            }

            var filteredItems = FilterItems().ToList();
            UpdateFilterStats(filteredItems);

            listView.visible = filteredItems.Count > 0;
            listView.style.display = filteredItems.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyListLabel.style.display = filteredItems.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            listView.makeItem = MakeItem;
            listView.bindItem = (element, index) =>
            {
                if (index >= 0 && index < filteredItems.Count)
                {
                    BindItem(element, filteredItems[index]);
                }
            };
            listView.unbindItem = (element, index) =>
            {
                UnbindItem(element);
            };

            listView.itemsSource = filteredItems;
            listView.selectionType = SelectionType.None;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.Rebuild();
        }

        protected virtual VisualElement MakeItem()
        {
            var item = itemTemplate!.Instantiate();
            var toggle = item.Q<Toggle>("item-toggle");
            var itemContainer = item.Q<VisualElement>(null, "item-container") ?? item;

            if (toggle != null)
            {
                toggle.RegisterValueChangedCallback(evt =>
                {
                    var viewModel = item.userData as TViewModel;
                    if (viewModel == null) return;

                    toggle.EnableInClassList("checked", evt.newValue);
                    UpdateItemClasses(itemContainer, evt.newValue);

                    OnItemToggleChanged(viewModel, evt.newValue);

                    if (typeDropdown?.index != (int)McpFilterType.All)
                    {
                        EditorApplication.delayCall += PopulateList;
                    }
                });
            }
            else
            {
                Logger.LogWarning("{method} Toggle missing in item template.", nameof(MakeItem));
            }

            item.Query<Foldout>().ForEach(foldout =>
            {
                foldout.RegisterValueChangedCallback(evt =>
                {
                    UpdateFoldoutState(foldout, evt.newValue);
                    if (item.userData is TViewModel viewModel)
                    {
                        OnFoldoutChanged(viewModel, foldout.name, evt.newValue);
                    }
                });
                UpdateFoldoutState(foldout, foldout.value);
            });

            return item;
        }

        protected abstract void OnItemToggleChanged(TViewModel viewModel, bool isEnabled);

        protected virtual void OnFoldoutChanged(TViewModel viewModel, string foldoutName, bool isExpanded)
        {
        }

        protected abstract void BindItem(VisualElement element, TViewModel viewModel);

        protected void UnbindItem(VisualElement element)
        {
            element.userData = null;
        }

        protected virtual IEnumerable<TViewModel> FilterItems()
        {
            var filtered = allItems.AsEnumerable();

            var selectedType = McpFilterType.All;
            if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            {
                if (Enum.TryParse<McpFilterType>(typeDropdown.choices[typeDropdown.index], out var parsedType))
                    selectedType = parsedType;
            }

            filtered = selectedType switch
            {
                McpFilterType.Enabled => filtered.Where(t => t.IsEnabled),
                McpFilterType.Disabled => filtered.Where(t => !t.IsEnabled),
                _ => filtered
            };

            var filterText = filterField?.value?.Trim();
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = FilterByText(filtered, filterText!);
            }

            return filtered;
        }

        protected abstract IEnumerable<TViewModel> FilterByText(IEnumerable<TViewModel> items, string filterText);

        protected void UpdateFilterStats(IEnumerable<TViewModel> filteredItems)
        {
            if (filterStatsLabel == null)
                return;

            var filteredList = filteredItems.ToList();
            filterStatsLabel.text = string.Format(FilterStatsFormat, filteredList.Count, allItems.Count);
        }

        protected void BindCommonItemFields(VisualElement element, TViewModel viewModel)
        {
            element.userData = viewModel;

            var titleLabel = element.Q<Label>("item-title");
            if (titleLabel != null)
                titleLabel.text = viewModel.Title ?? viewModel.Name;

            var idLabel = element.Q<Label>("item-id");
            if (idLabel != null)
                idLabel.text = viewModel.Name;

            var toggle = element.Q<Toggle>("item-toggle");
            if (toggle != null)
            {
                toggle.SetValueWithoutNotify(viewModel.IsEnabled);
                toggle.EnableInClassList("checked", viewModel.IsEnabled);
            }

            var itemContainer = element.Q<VisualElement>(null, "item-container") ?? element;
            UpdateItemClasses(itemContainer, viewModel.IsEnabled);
        }

        protected void BindDescriptionFoldout(VisualElement element, TViewModel viewModel, bool isExpanded)
        {
            var descriptionFoldout = element.Q<Foldout>("description-foldout");
            if (descriptionFoldout != null)
            {
                var descLabel = descriptionFoldout.Q<Label>("description-text");
                if (descLabel != null)
                    descLabel.text = viewModel.Description ?? string.Empty;

                var hasDescription = !string.IsNullOrEmpty(viewModel.Description);
                descriptionFoldout.style.display = hasDescription ? DisplayStyle.Flex : DisplayStyle.None;

                descriptionFoldout.SetValueWithoutNotify(isExpanded);
                UpdateFoldoutState(descriptionFoldout, isExpanded);
            }
            else
            {
                Logger.LogWarning("{method} Description foldout missing for: {name}",
                    nameof(BindDescriptionFoldout), viewModel.Name);
            }
        }

        protected void PopulateArgumentFoldout(VisualElement element, string foldoutName, string containerName, string titlePrefix, IReadOnlyList<ArgumentData> arguments)
        {
            var foldout = element.Q<Foldout>(foldoutName);
            if (foldout == null)
                return;

            var container = element.Q(containerName);
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
    }

    /// <summary>
    /// Interface for MCP item view models.
    /// </summary>
    public interface IMcpItemViewModel
    {
        string Name { get; }
        string? Title { get; }
        string? Description { get; }
        bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Data class for tool/prompt arguments.
    /// </summary>
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
