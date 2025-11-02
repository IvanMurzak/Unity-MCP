/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

/*
 * MCP Tools Editor Window
 * Ready for PR:
 * - Stable UI loading with safe USS application (try/catch).
 * - Dropdown 'Type' + text filter with live filtering.
 * - Tool enabled/disabled state persisted via EditorPrefs (JSON).
 * - Mock tools expanded for testing editor/code workflows.
 * - Safe queries and null-checks to avoid runtime errors.
 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MCPToolsWindow : EditorWindow
{
    private VisualTreeAsset toolItemTemplate;
    private List<ToolData> allTools;

    [MenuItem("Window/MCP Tools")]
    public static void ShowWindow()
    {
        MCPToolsWindow wnd = GetWindow<MCPToolsWindow>();
        wnd.titleContent = new GUIContent("MCP Tools");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        // Try multiple likely locations for the UXML (project refactor may move files).
        string[] uxmlPaths =
        {
            "Assets/root/Editor/UI/uxml/MCPToolsWindow.uxml",
            "Assets/root/Editor/MCPTools/MCPToolsWindow.uxml",
            "Assets/UnityLocalAi/Editor/MCPTools/MCPToolsWindow.uxml",
            "Assets/root/Editor/UI/uxml/MCPToolsWindow.uxml" // keep as last-resort duplicate
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

        // Try multiple likely locations for the USS stylesheet.
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

        // Try multiple likely locations for the ToolItem template.
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

        // Load mock data once and keep it in memory for filtering.
        allTools = GetMockTools();
        // Try to restore saved tool enabled/disabled state from EditorPrefs (if any).
        LoadToolState();

        // Populate simple DropdownField choices from code to avoid UXML <Choice> deserialization issues.
        var typeDropdown = root.Q<DropdownField>("type-dropdown");
        if (typeDropdown != null)
        {
            typeDropdown.choices = new List<string> { "Enabled", "Disabled", "All" };
            typeDropdown.index = 0;
            // Re-populate the list whenever the dropdown selection changes.
            typeDropdown.RegisterValueChangedCallback(evt => PopulateToolList());
        }

        // Hook up filter text field to allow searching by title/id/description.
        var filterField = root.Q<TextField>("filter-textfield");
        if (filterField != null)
        {
            filterField.RegisterValueChangedCallback(evt => PopulateToolList());
        }

        PopulateToolList();
    }

    private void PopulateToolList()
    {
        var scrollView = rootVisualElement.Q<ScrollView>("tool-list-scrollview");
        if (scrollView == null || toolItemTemplate == null)
        {
            Debug.LogWarning("[MCPTools] UI elements missing when populating tool list.");
            return;
        }

        // Clear existing items
        scrollView.Clear();

        // Read current filter values
        var root = rootVisualElement;
        var typeDropdown = root.Q<DropdownField>("type-dropdown");
        var filterField = root.Q<TextField>("filter-textfield");
        string filterText = filterField?.value?.Trim() ?? "";

        // Determine desired filter: Enabled / Disabled / All
        string selectedType = "All";
        if (typeDropdown != null && typeDropdown.index >= 0 && typeDropdown.index < typeDropdown.choices.Count)
            selectedType = typeDropdown.choices[typeDropdown.index];

        // Work from cached allTools list
        var tools = allTools ?? GetMockTools();

        IEnumerable<ToolData> filtered = tools;

        // Apply enabled/disabled filter
        if (selectedType == "Enabled")
            filtered = filtered.Where(t => t.IsEnabled);
        else if (selectedType == "Disabled")
            filtered = filtered.Where(t => !t.IsEnabled);

        // Apply text filter (title, id, description)
        if (!string.IsNullOrEmpty(filterText))
        {
            filtered = filtered.Where(t =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(t.Id) && t.Id.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrEmpty(t.Description) && t.Description.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0)
            );
        }

        foreach (var tool in filtered)
        {
            var toolItem = toolItemTemplate.Instantiate();

            var titleLabel = toolItem.Q<Label>("tool-title");
            if (titleLabel != null) titleLabel.text = tool.Title;
            var idLabel = toolItem.Q<Label>("tool-id");
            if (idLabel != null) idLabel.text = tool.Id;

            var toolToggle = toolItem.Q<Toggle>("tool-toggle");
            if (toolToggle != null)
            {
                toolToggle.value = tool.IsEnabled;
                // Ensure USS can style the toggle using the 'checked' class.
                toolToggle.EnableInClassList("checked", tool.IsEnabled);
            }

            // The UXML uses class="tool-item-container" (not name). Query by class to find it.
            var toolItemContainer = toolItem.Q<VisualElement>(null, "tool-item-container") ?? toolItem;

            UpdateToolItemClasses(toolItemContainer, toolToggle != null && toolToggle.value);

            // Capture the container and toggle reference for the callback to avoid re-querying.
            var capturedContainer = toolItemContainer;
            var capturedToggle = toolToggle;
            var toolId = tool.Id;
            if (toolToggle != null)
            {
                toolToggle.RegisterValueChangedCallback(evt =>
                {
                    if (capturedToggle != null) capturedToggle.EnableInClassList("checked", evt.newValue);
                    UpdateToolItemClasses(capturedContainer, evt.newValue);

                    // Persist change to the master list and save state so it survives editor restarts.
                    if (allTools != null)
                    {
                        var stored = allTools.FirstOrDefault(t => t.Id == toolId);
                        if (stored != null)
                        {
                            stored.IsEnabled = evt.newValue;
                            SaveToolState();
                        }
                    }

                    // Refresh the list to respect current filters (an item might need to disappear).
                    // Use a delayed call to avoid modifying the visual tree while it's being iterated.
                    EditorApplication.delayCall += () => PopulateToolList();
                });
            }

            var descriptionFoldout = toolItem.Q<Foldout>("description-foldout");
            if (descriptionFoldout != null)
            {
                if (!string.IsNullOrEmpty(tool.Description))
                {
                    var descLabel = descriptionFoldout.Q<Label>("description-text");
                    if (descLabel != null) descLabel.text = tool.Description;
                }
                else
                {
                    descriptionFoldout.style.display = DisplayStyle.None;
                }
            }

            var argumentsFoldout = toolItem.Q<Foldout>("arguments-foldout");
            if (argumentsFoldout != null)
            {
                if (tool.Arguments != null && tool.Arguments.Count > 0)
                {
                    argumentsFoldout.text = $"Input arguments ({tool.Arguments.Count})";
                    var argumentsContainer = toolItem.Q("arguments-container");
                    if (argumentsContainer != null)
                    {
                        foreach (var arg in tool.Arguments)
                        {
                            var argItem = new VisualElement();
                            argItem.AddToClassList("argument-item");

                            var nameLabel = new Label(arg.Name);
                            nameLabel.AddToClassList("argument-name");
                            argItem.Add(nameLabel);

                            var descLabel = new Label(arg.Description);
                            descLabel.AddToClassList("argument-description");
                            argItem.Add(descLabel);

                            argumentsContainer.Add(argItem);
                        }
                    }
                }
                else
                {
                    argumentsFoldout.style.display = DisplayStyle.None;
                }
            }

            scrollView.Add(toolItem);
        }
    }

    private void UpdateToolItemClasses(VisualElement toolItemContainer, bool isEnabled)
    {
        if (toolItemContainer == null) return;
        toolItemContainer.EnableInClassList("enabled", isEnabled);
        toolItemContainer.EnableInClassList("disabled", !isEnabled);
    }

    // --- Mock Data ---
    public class ToolData
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
        public string Description { get; set; }
        public List<ArgumentData> Arguments { get; set; }
    }

    public class ArgumentData
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    // Persist tool enabled/disabled state between editor sessions using EditorPrefs.
    private const string TOOL_STATE_KEY = "MCPTools_ToolStates_v1";

    private void SaveToolState()
    {
        try
        {
            if (allTools == null) return;
            string json = JsonSerializer.Serialize(allTools);
            EditorPrefs.SetString(TOOL_STATE_KEY, json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MCPTools] Failed to save tool state: {ex.Message}");
        }
    }

    private void LoadToolState()
    {
        try
        {
            if (!EditorPrefs.HasKey(TOOL_STATE_KEY)) return;
            string json = EditorPrefs.GetString(TOOL_STATE_KEY);
            var saved = JsonSerializer.Deserialize<List<ToolData>>(json);
            if (saved == null || allTools == null) return;
            foreach (var s in saved)
            {
                var existing = allTools.FirstOrDefault(t => t.Id == s.Id);
                if (existing != null)
                {
                    existing.IsEnabled = s.IsEnabled;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MCPTools] Failed to load saved tool state: {ex.Message}");
        }
    }

    private List<ToolData> GetMockTools()
    {
        // Expanded mock tool set to test filtering and different editor-related actions.
        return new List<ToolData>
        {
            new ToolData
            {
                Title = "Open Script in IDE",
                Id = "open-script",
                IsEnabled = true,
                Description = "Opens the selected C# script in the external IDE at the given line.",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "filePath", Description = "Path to the script file" },
                    new ArgumentData { Name = "lineNumber", Description = "Line number to jump to (optional)" }
                }
            },
            new ToolData
            {
                Title = "Format Project Code",
                Id = "format-code",
                IsEnabled = true,
                Description = "Runs the project code formatter / prettifier.",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "style", Description = "Formatting style (default/strict)" }
                }
            },
            new ToolData
            {
                Title = "Create GameObject from Prefab",
                Id = "create-gameobject",
                IsEnabled = true,
                Description = "Instantiates a prefab into the current scene.",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "prefabPath", Description = "Path to the prefab asset" },
                    new ArgumentData { Name = "objectName", Description = "Name for the new GameObject" }
                }
            },
            new ToolData
            {
                Title = "Run Editor Menu Command",
                Id = "run-editor-command",
                IsEnabled = true,
                Description = "Execute an arbitrary editor menu command (useful for automation).",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "menuPath", Description = "Full menu path (e.g. File/Save Project)" }
                }
            },
            new ToolData
            {
                Title = "Rename Asset",
                Id = "rename-asset",
                IsEnabled = false,
                Description = "Renames an asset on disk and refreshes the AssetDatabase.",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "assetPath", Description = "Existing asset path" },
                    new ArgumentData { Name = "newName", Description = "New filename (without folder path)" }
                }
            },
            new ToolData
            {
                Title = "Build Project",
                Id = "build-project",
                IsEnabled = false,
                Description = "Starts a build for the chosen target platform.",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "target", Description = "Build target (e.g. StandaloneWindows64, Android)" }
                }
            },
            new ToolData
            {
                Title = "Run Linter",
                Id = "lint-project",
                IsEnabled = true,
                Description = "Runs code linting across the project and reports issues.",
                Arguments = new List<ArgumentData>
                {
                    new ArgumentData { Name = "ruleset", Description = "Optional ruleset to use" }
                }
            },
            new ToolData
            {
                Title = "Clear Console",
                Id = "clear-console",
                IsEnabled = true,
                Description = "Clears the Unity console messages.",
                Arguments = new List<ArgumentData>()
            }
        };
    }
}
