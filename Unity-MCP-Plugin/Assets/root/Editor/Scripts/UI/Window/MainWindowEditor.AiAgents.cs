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
using Extensions.Unity.PlayerPrefsEx;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public partial class MainWindowEditor
    {
        // PlayerPrefs key for storing selected AI agent
        private const string PlayerPrefsKey_SelectedAiAgent = "Unity_MCP_SelectedAiAgent";
        private static PlayerPrefsString _selectedAiAgentPref = new(PlayerPrefsKey_SelectedAiAgent);

        void ConfigureAgents(VisualElement root)
        {
            // Get the dropdown element
            var dropdown = root.Query<DropdownField>("aiAgentDropdown").First();
            if (dropdown == null)
            {
                Debug.LogError("aiAgentDropdown not found in UXML. Please ensure the dropdown element exists.");
                return;
            }

            // Get the container where agent panels will be added
            var container = root.Query<VisualElement>("ConfigureAgentsContainer").First();
            if (container == null)
            {
                Debug.LogError("ConfigureAgentsContainer not found in UXML. Please ensure the container element exists.");
                return;
            }

            // Get agent names from registry
            var agentNames = AiAgentConfiguratorRegistry.GetAgentNames();
            dropdown.choices = agentNames;

            // Load saved selection from PlayerPrefs
            var savedAiAgentId = _selectedAiAgentPref.Value;
            var selectedIndex = 0;

            if (!string.IsNullOrEmpty(savedAiAgentId))
            {
                selectedIndex = AiAgentConfiguratorRegistry.GetIndexByAgentId(savedAiAgentId);
                if (selectedIndex < 0) selectedIndex = 0;
            }

            // Set initial dropdown value without triggering callback
            if (agentNames.Count > 0)
            {
                dropdown.SetValueWithoutNotify(agentNames[selectedIndex]);
            }

            // Load initial UI for selected agent
            LoadAgentUI(container, selectedIndex);

            // Register callback for dropdown changes
            dropdown.RegisterValueChangedCallback(evt =>
            {
                var newIndex = agentNames.IndexOf(evt.newValue);
                if (newIndex < 0) return;

                // Save selection to PlayerPrefs
                var configurator = AiAgentConfiguratorRegistry.All[newIndex];
                _selectedAiAgentPref.Value = configurator.AgentId;

                // Load UI for the newly selected agent
                LoadAgentUI(container, newIndex);
            });
        }

        void LoadAgentUI(VisualElement container, int selectedIndex)
        {
            // Clear any existing content
            container.Clear();

            if (selectedIndex < 0 || selectedIndex >= AiAgentConfiguratorRegistry.All.Count)
                return;

            var configurator = AiAgentConfiguratorRegistry.All[selectedIndex];

            // Load agent-specific configuration UI from the configurator
            // The configurator now contains its own AiAgentConfig via the AiAgentConfig property
            var agentSpecificUI = configurator.CreateUI(container);
            if (agentSpecificUI == null)
                return;

            container.Add(agentSpecificUI);
        }
    }
}
