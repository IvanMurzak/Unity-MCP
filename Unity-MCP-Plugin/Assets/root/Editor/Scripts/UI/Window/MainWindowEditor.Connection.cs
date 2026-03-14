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
using com.IvanMurzak.Unity.MCP.Editor.Services;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private void SetupConnectionSection(VisualElement root)
        {
            var inputFieldHost = root.Q<TextField>("InputServerURL");
            var btnConnect = root.Q<Button>("btnConnectOrDisconnect");
            var statusCircle = root.Q<VisualElement>("connectionStatusCircle");
            var statusText = root.Q<Label>("connectionStatusText");

            _btnConnect = btnConnect;
            _timelinePointUnity = root.Q<VisualElement>("TimelinePointUnity");

            _aiAgentLabelsContainer = root.Q<VisualElement>("aiAgentLabelsContainer");
            _aiAgentStatusCircle = root.Q<VisualElement>("aiAgentStatusCircle");

            inputFieldHost.value = UnityMcpPluginEditor.LocalHost;
            inputFieldHost.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = inputFieldHost.value;
                if (UnityMcpPluginEditor.LocalHost == newValue)
                    return;

                UnityMcpPluginEditor.LocalHost = newValue;
                SaveChanges($"[{nameof(MainWindowEditor)}] Host Changed: {newValue}");
                Invalidate();

                UnityMcpPluginEditor.Instance.DisposeMcpPluginInstance();
                UnityBuildAndConnect();
            });

            SubscribeToConnectionState((state, keepConnected) =>
            {
                UpdateHostFieldState(inputFieldHost, UnityMcpPluginEditor.PluginProperty.CurrentValue!.KeepConnected.CurrentValue, state);
                statusText.text = "Unity: " + GetConnectionStatusText(state, keepConnected);
                btnConnect.text = GetButtonText(state, keepConnected);
                var isConnect = btnConnect.text == ServerButtonText_Connect;
                btnConnect.EnableInClassList("btn-primary", isConnect);
                btnConnect.EnableInClassList("btn-secondary", !isConnect);
                SetStatusIndicator(statusCircle, GetConnectionStatusClass(state, keepConnected));

                if (!(state == HubConnectionState.Connected && keepConnected))
                    SetAiAgentStatus(false);

                UpdateCloudAuthState();
            });

            btnConnect.RegisterCallback<ClickEvent>(evt => HandleConnectButton(btnConnect.text));
        }

        internal static bool IsHostFieldReadOnly(bool keepConnected, HubConnectionState state) =>
            keepConnected || state != HubConnectionState.Disconnected;

        private static void UpdateHostFieldState(TextField field, bool keepConnected, HubConnectionState state)
        {
            var isReadOnly = IsHostFieldReadOnly(keepConnected, state);
            field.isReadOnly = isReadOnly;
            var defaultUrl = $"http://localhost:{UnityMcpPlugin.GeneratePortFromDirectory()}";
            field.tooltip = keepConnected
                ? "Editable only when Unity disconnected from the MCP Server."
                : $"Usually the server is hosted locally at {defaultUrl}. Feel free to connect to a remote MCP server if needed. The connection is established using SignalR.";

            field.EnableInClassList("disabled-text-field", isReadOnly);
            field.EnableInClassList("enabled-text-field", !isReadOnly);
        }

        private static void HandleConnectButton(string buttonText)
        {
            if (buttonText.Equals(ServerButtonText_Connect, StringComparison.OrdinalIgnoreCase))
            {
                UnityMcpPluginEditor.KeepConnected = true;
                UnityMcpPluginEditor.Instance.Save();
                UnityBuildAndConnect();
            }
            else
            {
                UnityMcpPluginEditor.KeepConnected = false;
                UnityMcpPluginEditor.Instance.Save();
                if (UnityMcpPluginEditor.Instance.HasMcpPluginInstance)
                    _ = UnityMcpPluginEditor.Instance.Disconnect();
            }
        }

        private void SetupConnectionModeToggle(VisualElement root)
        {
            var toggleCustom = root.Q<Toggle>("toggleModeCustom");
            var toggleCloud = root.Q<Toggle>("toggleModeCloud");
            if (toggleCustom == null || toggleCloud == null) return;

            var inputServerUrl = root.Q<TextField>("InputServerURL");
            var mcpServerPoint = root.Q<VisualElement>("TimelinePointMcpServer");
            var cloudAuthSection = root.Q<VisualElement>("cloudAuthSection");

            void UpdateModeVisibility(ConnectionMode mode)
            {
                var isCustom = mode == ConnectionMode.Custom;
                if (inputServerUrl != null) inputServerUrl.style.display = isCustom ? DisplayStyle.Flex : DisplayStyle.None;
                if (mcpServerPoint != null) mcpServerPoint.style.display = isCustom ? DisplayStyle.Flex : DisplayStyle.None;
                if (cloudAuthSection != null) cloudAuthSection.style.display = isCustom ? DisplayStyle.None : DisplayStyle.Flex;
            }

            var currentMode = UnityMcpPluginEditor.ConnectionMode;
            toggleCustom.SetValueWithoutNotify(currentMode == ConnectionMode.Custom);
            toggleCloud.SetValueWithoutNotify(currentMode == ConnectionMode.Cloud);
            UpdateModeVisibility(currentMode);

            SetupMutuallyExclusiveToggles(toggleCustom, toggleCloud,
                onASelected: () =>
                {
                    UnityMcpPluginEditor.ConnectionMode = ConnectionMode.Custom;
                    UnityMcpPluginEditor.Instance.Save();
                    UpdateModeVisibility(ConnectionMode.Custom);
                    UpdateCloudAuthState();

                    // Invalidate cached AI agent configs so they pick up the new Host/Token
                    InvalidateAndReloadAgentUI();

                    // Start local server if configured and reconnect to it
                    McpServerManager.StartServerIfNeeded();
                    ReconnectAfterModeSwitch();
                },
                onBSelected: () =>
                {
                    UnityMcpPluginEditor.ConnectionMode = ConnectionMode.Cloud;
                    UnityMcpPluginEditor.Instance.Save();
                    UpdateModeVisibility(ConnectionMode.Cloud);
                    UpdateCloudAuthState();

                    // Invalidate cached AI agent configs so they pick up the new Host/Token
                    InvalidateAndReloadAgentUI();

                    // Stop local server — not needed in Cloud mode
                    if (McpServerManager.IsRunning)
                        McpServerManager.StopServer();

                    // Reconnect to cloud server (only if authorized)
                    if (!string.IsNullOrEmpty(UnityMcpPluginEditor.CloudToken))
                        ReconnectAfterModeSwitch();
                });
        }

        internal static bool IsAuthFlowRunning(DeviceAuthFlowState state) =>
            state == DeviceAuthFlowState.Initiating
            || state == DeviceAuthFlowState.WaitingForUser
            || state == DeviceAuthFlowState.Polling;

        internal static string GetAuthFlowStatusMessage(DeviceAuthFlowState state, string? userCode, string? errorMessage) => state switch
        {
            DeviceAuthFlowState.Initiating => "Initiating...",
            DeviceAuthFlowState.WaitingForUser => $"Code: {userCode} — Authorize in browser",
            DeviceAuthFlowState.Polling => $"Code: {userCode} — Waiting for authorization...",
            DeviceAuthFlowState.Authorized => "Authorized!",
            DeviceAuthFlowState.Failed => $"Failed: {errorMessage}",
            DeviceAuthFlowState.Expired => "Expired — try again",
            DeviceAuthFlowState.Cancelled => "Cancelled",
            _ => ""
        };

        private void SetupCloudAuthSection(VisualElement root)
        {
            var inputCloudUrl = root.Q<TextField>("inputCloudServerUrl");
            var inputCloudToken = root.Q<TextField>("inputCloudToken");
            var btnRevoke = root.Q<Button>("btnCloudRevoke");
            var btnAuthorize = root.Q<Button>("btnCloudAuthorize");
            var statusLabel = root.Q<Label>("labelCloudAuthStatus");
            if (inputCloudUrl == null || inputCloudToken == null || btnAuthorize == null) return;

            _btnAuthorize = btnAuthorize;

            const string tokenPlaceholder = "Token — press Authorize";
            void SetTokenValue(string? token)
            {
                var isEmpty = string.IsNullOrEmpty(token);
                inputCloudToken.value = isEmpty ? tokenPlaceholder : token!;
                inputCloudToken.EnableInClassList("token-placeholder", isEmpty);
            }

            inputCloudUrl.value = UnityMcpPluginEditor.CloudServerUrl;
            SetTokenValue(UnityMcpPluginEditor.CloudToken);
            UpdateCloudAuthState();

            SubscribeToConnectionState((state, keepConnected) =>
            {
                var isReadOnly = keepConnected || state != HubConnectionState.Disconnected;
                inputCloudUrl.isReadOnly = isReadOnly;
                inputCloudUrl.EnableInClassList("disabled-text-field", isReadOnly);
                inputCloudUrl.EnableInClassList("enabled-text-field", !isReadOnly);
                inputCloudUrl.tooltip = isReadOnly
                    ? "Editable only when Unity disconnected from the MCP Server."
                    : "The cloud server URL to connect to.";
            });

            void UpdateRevokeButtonVisibility()
            {
                if (btnRevoke != null)
                    btnRevoke.style.display = string.IsNullOrEmpty(UnityMcpPluginEditor.CloudToken)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
            }
            UpdateRevokeButtonVisibility();

            btnRevoke?.RegisterCallback<ClickEvent>(evt =>
            {
                UnityMcpPluginEditor.CloudToken = null;
                UnityMcpPluginEditor.Instance.Save();
                SetTokenValue(null);
                UpdateRevokeButtonVisibility();

                if (statusLabel != null)
                {
                    statusLabel.text = "Token revoked.";
                    statusLabel.style.display = DisplayStyle.Flex;
                }

                // Invalidate cached AI agent configs
                InvalidateAndReloadAgentUI();

                UpdateCloudAuthState();

                // Disconnect if currently in Cloud mode
                if (UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud
                    && UnityMcpPluginEditor.Instance.HasMcpPluginInstance)
                    _ = UnityMcpPluginEditor.Instance.Disconnect();
            });

            inputCloudUrl.RegisterCallback<FocusOutEvent>(_ =>
            {
                var newValue = inputCloudUrl.value;
                if (UnityMcpPluginEditor.CloudServerUrl == newValue) return;
                UnityMcpPluginEditor.CloudServerUrl = newValue;
                SaveChanges($"[AI Game Developer] Cloud URL Changed: {newValue}");

                // Invalidate cached AI agent configs so they pick up the new cloud URL
                InvalidateAndReloadAgentUI();

                // Reconnect to the new cloud URL if currently in Cloud mode (only if authorized)
                if (UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud
                    && !string.IsNullOrEmpty(UnityMcpPluginEditor.CloudToken))
                    ReconnectAfterModeSwitch();
            });

            btnAuthorize.RegisterCallback<ClickEvent>(async _ =>
            {
                // If currently running, cancel
                if (_deviceAuthFlow != null && IsAuthFlowRunning(_deviceAuthFlow.State))
                {
                    _deviceAuthFlow.Cancel();
                    return;
                }

                _deviceAuthFlow?.Cancel();
                _deviceAuthFlow = new DeviceAuthFlow();

                _deviceAuthFlow.OnStateChanged += state =>
                {
                    // Must dispatch to main thread for UI updates
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (statusLabel != null)
                        {
                            statusLabel.text = GetAuthFlowStatusMessage(state, _deviceAuthFlow.UserCode, _deviceAuthFlow.ErrorMessage);
                            statusLabel.style.display = string.IsNullOrEmpty(statusLabel.text)
                                ? DisplayStyle.None
                                : DisplayStyle.Flex;
                        }
                        if (state == DeviceAuthFlowState.Authorized && inputCloudToken != null)
                        {
                            SetTokenValue(UnityMcpPluginEditor.CloudToken);
                            UpdateRevokeButtonVisibility();
                            UpdateCloudAuthState();
                        }
                        if (state == DeviceAuthFlowState.Authorized)
                        {
                            // Invalidate cached AI agent configs so they pick up the new cloud token
                            InvalidateAndReloadAgentUI();

                            // Reconnect to cloud server with the new token
                            ReconnectAfterModeSwitch();
                        }
                        if (btnAuthorize != null)
                        {
                            btnAuthorize.text = IsAuthFlowRunning(state) ? "Cancel" : "Authorize";
                        }
                    };
                };

                await _deviceAuthFlow.StartAsync(UnityMcpPluginEditor.CloudServerUrl, "Unity Editor");
            });
        }
    }
}
