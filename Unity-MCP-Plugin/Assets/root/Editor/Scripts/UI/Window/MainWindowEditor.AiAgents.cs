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
using Microsoft.Extensions.Logging;
using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private static PlayerPrefsString selectedAiAgentId = new("Unity_MCP_SelectedAiAgent");

        private AiAgentConfigurator? currentAiAgentConfigurator;

        private DropdownField? aiAgentDropdown;
        private VisualElement? aiAgentContainer;

        void ConfigureAgents(VisualElement root)
        {
            // Get the dropdown element
            aiAgentDropdown = root.Query<DropdownField>("aiAgentDropdown").First();
            if (aiAgentDropdown == null)
            {
                Debug.LogError("aiAgentDropdown not found in UXML. Please ensure the dropdown element exists.");
                return;
            }

            // Get the container where agent panels will be added
            aiAgentContainer = root.Query<VisualElement>("ConfigureAgentsContainer").First();
            if (aiAgentContainer == null)
            {
                Debug.LogError("ConfigureAgentsContainer not found in UXML. Please ensure the container element exists.");
                return;
            }

            // Get agent names from registry
            var agentNames = AiAgentConfiguratorRegistry.GetAgentNames();
            aiAgentDropdown.choices = agentNames;

            // Load saved selection from PlayerPrefs
            var savedAiAgentId = selectedAiAgentId.Value;
            var selectedIndex = 0;

            if (!string.IsNullOrEmpty(savedAiAgentId))
            {
                selectedIndex = AiAgentConfiguratorRegistry.GetIndexByAgentId(savedAiAgentId);
                if (selectedIndex < 0) selectedIndex = 0;
            }

            // Set initial dropdown value without triggering callback
            if (agentNames.Count > 0)
            {
                aiAgentDropdown.SetValueWithoutNotify(agentNames[selectedIndex]);
            }

            // Load initial UI for selected agent
            LoadAgentUI(aiAgentContainer, selectedIndex);

            // Register callback for dropdown changes
            aiAgentDropdown.RegisterValueChangedCallback(evt =>
            {
                var newIndex = agentNames.IndexOf(evt.newValue);
                if (newIndex < 0) return;

                // Save selection to PlayerPrefs
                var configurator = AiAgentConfiguratorRegistry.All[newIndex];
                selectedAiAgentId.Value = configurator.AgentId;

                // Load UI for the newly selected agent
                LoadAgentUI(aiAgentContainer, newIndex);
            });
        }

        private void InvalidateAndReloadAgentUI()
        {
            currentAiAgentConfigurator?.Invalidate();
            if (aiAgentContainer == null || aiAgentDropdown == null)
            {
                Logger.LogError($"Cannot reload agent UI: {nameof(aiAgentContainer)} or {nameof(aiAgentDropdown)} is null.");
                return;
            }
            var agentNames = AiAgentConfiguratorRegistry.GetAgentNames();
            var index = agentNames.IndexOf(aiAgentDropdown.value);
            if (index < 0 && agentNames.Count > 0)
            {
                index = 0;
                aiAgentDropdown.SetValueWithoutNotify(agentNames[index]);
            }
            LoadAgentUI(aiAgentContainer, index);
        }

        private static void SetTokenFieldsVisible(TextField tokenField, VisualElement actionsRow, bool visible)
        {
            tokenField.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            actionsRow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RestartServerIfWasRunning(bool wasRunning)
        {
            if (!wasRunning)
                return;

            McpServerManager.StopServer();

            McpServerManager.ServerStatus
                .Where(status => status == McpServerStatus.Stopped)
                .Take(1)
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(_ => McpServerManager.StartServer())
                .AddTo(_disposables);
        }

        void LoadAgentUI(VisualElement container, int selectedIndex)
        {
            // Clear any existing content
            container.Clear();

            if (selectedIndex < 0 || selectedIndex >= AiAgentConfiguratorRegistry.All.Count)
                return;

            var configurator = AiAgentConfiguratorRegistry.All[selectedIndex];
            currentAiAgentConfigurator = configurator;

            // Load agent-specific configuration UI from the configurator
            // The configurator now contains its own AiAgentConfig via the AiAgentConfig property
            var agentSpecificUI = configurator.CreateUI(container);
            if (agentSpecificUI == null)
                return;

            container.Add(agentSpecificUI);
        }
    }
}
