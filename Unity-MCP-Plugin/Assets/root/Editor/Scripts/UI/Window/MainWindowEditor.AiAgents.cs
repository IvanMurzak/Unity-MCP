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
using System.Security.Cryptography;
using Extensions.Unity.PlayerPrefsEx;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private static PlayerPrefsString selectedAiAgentId = new("Unity_MCP_SelectedAiAgent");

        private AiAgentConfigurator? currentAiAgentConfigurator;

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
                selectedAiAgentId.Value = configurator.AgentId;

                // Load UI for the newly selected agent
                LoadAgentUI(container, newIndex);
            });

            // Deployment mode toggles
            var toggleDeploymentLocal = root.Query<Toggle>("toggleDeploymentLocal").First();
            var toggleDeploymentRemote = root.Query<Toggle>("toggleDeploymentRemote").First();
            var inputRemoteToken = root.Query<TextField>("inputRemoteToken").First();
            var tokenActionsRow = root.Query<VisualElement>("tokenActionsRow").First();
            var btnGenerateToken = root.Query<Button>("btnGenerateToken").First();

            if (toggleDeploymentLocal == null)
            {
                Debug.LogError("toggleDeploymentLocal not found in UXML.");
                return;
            }
            if (toggleDeploymentRemote == null)
            {
                Debug.LogError("toggleDeploymentRemote not found in UXML.");
                return;
            }
            if (inputRemoteToken == null)
            {
                Debug.LogError("inputRemoteToken not found in UXML.");
                return;
            }
            if (tokenActionsRow == null)
            {
                Debug.LogError("tokenActionsRow not found in UXML.");
                return;
            }
            if (btnGenerateToken == null)
            {
                Debug.LogError("btnGenerateToken not found in UXML.");
                return;
            }

            var isRemote = UnityMcpPlugin.DeploymentMode == DeploymentMode.remote;
            toggleDeploymentLocal.SetValueWithoutNotify(!isRemote);
            toggleDeploymentRemote.SetValueWithoutNotify(isRemote);
            inputRemoteToken.SetValueWithoutNotify(UnityMcpPlugin.Token ?? string.Empty);
            SetTokenFieldsVisible(inputRemoteToken, tokenActionsRow, isRemote);

            toggleDeploymentLocal.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    UnityMcpPlugin.DeploymentMode = DeploymentMode.local;
                    UnityMcpPlugin.Instance.Save();
                    toggleDeploymentRemote.SetValueWithoutNotify(false);
                    SetTokenFieldsVisible(inputRemoteToken, tokenActionsRow, false);
                }
                else if (!toggleDeploymentRemote.value)
                {
                    toggleDeploymentLocal.SetValueWithoutNotify(true);
                }
            });

            toggleDeploymentRemote.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    UnityMcpPlugin.DeploymentMode = DeploymentMode.remote;
                    UnityMcpPlugin.Instance.Save();
                    toggleDeploymentLocal.SetValueWithoutNotify(false);
                    SetTokenFieldsVisible(inputRemoteToken, tokenActionsRow, true);
                }
                else if (!toggleDeploymentLocal.value)
                {
                    toggleDeploymentRemote.SetValueWithoutNotify(true);
                }
            });

            inputRemoteToken.RegisterCallback<FocusOutEvent>(_ =>
            {
                var newToken = inputRemoteToken.value;
                if (newToken == UnityMcpPlugin.Token)
                    return;

                var wasRunning = McpServerManager.IsRunning;
                UnityMcpPlugin.Token = newToken;
                UnityMcpPlugin.Instance.Save();
                RestartServerIfWasRunning(wasRunning);
            });

            btnGenerateToken.RegisterCallback<ClickEvent>(_ =>
            {
                var newToken = GenerateToken();
                inputRemoteToken.SetValueWithoutNotify(newToken);

                var wasRunning = McpServerManager.IsRunning;
                UnityMcpPlugin.Token = newToken;
                UnityMcpPlugin.Instance.Save();
                RestartServerIfWasRunning(wasRunning);
            });
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

        private static string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
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
