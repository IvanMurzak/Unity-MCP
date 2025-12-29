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
using System.Linq;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public partial class MainWindowEditor : EditorWindow
    {
        // Template paths for both local development and UPM package environments

        public static readonly string[] WindowUxmlPath = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/AiConnectorWindow.uxml");

        const string USS_IndicatorClass_Connected = "status-indicator-circle-online";
        const string USS_IndicatorClass_Connecting = "status-indicator-circle-connecting";
        const string USS_IndicatorClass_Disconnected = "status-indicator-circle-disconnected";

        const string ServerButtonText_Connect = "Connect";
        const string ServerButtonText_Disconnect = "Disconnect";
        const string ServerButtonText_Stop = "Stop";

        public void CreateGUI()
        {
            _disposables.Clear();
            rootVisualElement.Clear();

            // Try to load the template from both possible paths (UPM package or local development)
            var templateControlPanel = EditorAssetLoader.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
            if (templateControlPanel == null)
            {
                Debug.LogError("AiConnectorWindow template not found in specified paths. Please ensure the template file exists.");
                return;
            }

            var root = templateControlPanel.Instantiate();
            rootVisualElement.Add(root);

            // Settings
            // -----------------------------------------------------------------

            var dropdownLogLevel = root.Query<EnumField>("dropdownLogLevel").First();
            dropdownLogLevel.value = UnityMcpPlugin.LogLevel;
            dropdownLogLevel.tooltip = "The minimum level of messages to log. Debug includes all messages, while Critical includes only the most severe.";
            dropdownLogLevel.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPlugin.LogLevel = evt.newValue as LogLevel? ?? LogLevel.Warning;
                SaveChanges($"[AI Game Developer] LogLevel Changed: {evt.newValue}");
            });

            var inputTimeoutMs = root.Query<IntegerField>("inputTimeoutMs").First();
            inputTimeoutMs.value = UnityMcpPlugin.TimeoutMs;
            inputTimeoutMs.tooltip = $"Timeout for MCP tool execution in milliseconds.\n\nMost tools only need a few seconds.\n\nSet this higher than your longest test execution time.\n\nImportant: Also update the '{Consts.MCP.Server.Args.PluginTimeout}' argument in your MCP client configuration to match this value so your MCP client doesn't timeout before the tool completes.";
            inputTimeoutMs.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = Mathf.Max(1000, inputTimeoutMs.value);
                if (newValue == UnityMcpPlugin.TimeoutMs)
                    return;

                if (newValue != inputTimeoutMs.value)
                    inputTimeoutMs.SetValueWithoutNotify(newValue);

                UnityMcpPlugin.TimeoutMs = newValue;

                // Update the raw JSON configuration display
                var rawJsonField = root.Query<TextField>("rawJsonConfiguration").First();
                rawJsonField.value = Startup.Server.RawJsonConfiguration(UnityMcpPlugin.Port, "mcpServers", UnityMcpPlugin.TimeoutMs).ToString();

                SaveChanges($"[AI Game Developer] Timeout Changed: {newValue} ms");
                UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                UnityMcpPlugin.ConnectIfNeeded();
            });

            var currentVersion = root.Query<TextField>("currentVersion").First();
            currentVersion.value = UnityMcpPlugin.Version;

            // Connection status
            // -----------------------------------------------------------------

            var inputFieldHost = root.Query<TextField>("InputServerURL").First();
            inputFieldHost.value = UnityMcpPlugin.Host;
            inputFieldHost.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = inputFieldHost.value;
                if (UnityMcpPlugin.Host == newValue)
                    return;

                UnityMcpPlugin.Host = newValue;
                SaveChanges($"[{nameof(MainWindowEditor)}] Host Changed: {newValue}");
                Invalidate();

                UnityMcpPlugin.Instance.DisposeMcpPluginInstance();
                UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
            });

            var btnConnectOrDisconnect = root.Query<Button>("btnConnectOrDisconnect").First();
            var connectionStatusCircle = root
                .Query<VisualElement>("ServerConnectionInfo").First()
                .Query<VisualElement>("connectionStatusCircle").First();
            var connectionStatusText = root
                .Query<VisualElement>("ServerConnectionInfo").First()
                .Query<Label>("connectionStatusText").First();

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                Observable.CombineLatest(
                    UnityMcpPlugin.ConnectionState, plugin.KeepConnected,
                    (connectionState, keepConnected) => (connectionState, keepConnected)
                )
                .ThrottleLast(TimeSpan.FromMilliseconds(10))
                .ObserveOnCurrentSynchronizationContext()
                .SubscribeOnCurrentSynchronizationContext()
                .Subscribe(tuple =>
                {
                    var (connectionState, keepConnected) = tuple;

                    inputFieldHost.isReadOnly = keepConnected || connectionState switch
                    {
                        HubConnectionState.Connected => true,
                        HubConnectionState.Disconnected => false,
                        HubConnectionState.Reconnecting => true,
                        HubConnectionState.Connecting => true,
                        _ => false
                    };
                    inputFieldHost.tooltip = plugin.KeepConnected.CurrentValue
                        ? "Editable only when disconnected from the MCP Server."
                        : $"The server URL. http://localhost:{UnityMcpPlugin.GeneratePortFromDirectory()}";

                    // Update the style class
                    if (inputFieldHost.isReadOnly)
                    {
                        inputFieldHost.AddToClassList("disabled-text-field");
                        inputFieldHost.RemoveFromClassList("enabled-text-field");
                    }
                    else
                    {
                        inputFieldHost.AddToClassList("enabled-text-field");
                        inputFieldHost.RemoveFromClassList("disabled-text-field");
                    }

                    connectionStatusText.text = connectionState switch
                    {
                        HubConnectionState.Connected => keepConnected
                            ? "Connected"
                            : "Disconnected",
                        HubConnectionState.Disconnected => keepConnected
                            ? "Connecting..."
                            : "Disconnected",
                        HubConnectionState.Reconnecting => keepConnected
                            ? "Connecting..."
                            : "Disconnected",
                        HubConnectionState.Connecting => keepConnected
                            ? "Connecting..."
                            : "Disconnected",
                        _ => UnityMcpPlugin.IsConnected.CurrentValue.ToString() ?? "Unknown"
                    };

                    btnConnectOrDisconnect.text = connectionState switch
                    {
                        HubConnectionState.Connected => keepConnected
                            ? ServerButtonText_Disconnect
                            : ServerButtonText_Connect,
                        HubConnectionState.Disconnected => keepConnected
                            ? ServerButtonText_Stop
                            : ServerButtonText_Connect,
                        HubConnectionState.Reconnecting => keepConnected
                            ? ServerButtonText_Stop
                            : ServerButtonText_Connect,
                        HubConnectionState.Connecting => keepConnected
                            ? ServerButtonText_Stop
                            : ServerButtonText_Connect,
                        _ => UnityMcpPlugin.IsConnected.CurrentValue.ToString() ?? "Unknown"
                    };

                    connectionStatusCircle.RemoveFromClassList(USS_IndicatorClass_Connected);
                    connectionStatusCircle.RemoveFromClassList(USS_IndicatorClass_Connecting);
                    connectionStatusCircle.RemoveFromClassList(USS_IndicatorClass_Disconnected);

                    connectionStatusCircle.AddToClassList(connectionState switch
                    {
                        HubConnectionState.Connected => keepConnected
                            ? USS_IndicatorClass_Connected
                            : USS_IndicatorClass_Disconnected,
                        HubConnectionState.Disconnected => keepConnected
                            ? USS_IndicatorClass_Connecting
                            : USS_IndicatorClass_Disconnected,
                        HubConnectionState.Reconnecting => keepConnected
                            ? USS_IndicatorClass_Connecting
                            : USS_IndicatorClass_Disconnected,
                        HubConnectionState.Connecting => keepConnected
                            ? USS_IndicatorClass_Connecting
                            : USS_IndicatorClass_Disconnected,
                        _ => throw new ArgumentOutOfRangeException(nameof(connectionState), connectionState, null)
                    });
                })
                .AddTo(_disposables);
            }).AddTo(_disposables);

            btnConnectOrDisconnect.RegisterCallback<ClickEvent>((EventCallback<ClickEvent>)(evt =>
            {
                if (btnConnectOrDisconnect.text.Equals(ServerButtonText_Connect, StringComparison.OrdinalIgnoreCase))
                {
                    UnityMcpPlugin.KeepConnected = true;
                    UnityMcpPlugin.Instance.Save();
                    UnityMcpPlugin.Instance.BuildMcpPluginIfNeeded();
                    UnityMcpPlugin.Instance.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage());
                    UnityMcpPlugin.ConnectIfNeeded();
                }
                else if (btnConnectOrDisconnect.text.Equals(ServerButtonText_Disconnect, StringComparison.OrdinalIgnoreCase))
                {
                    UnityMcpPlugin.KeepConnected = false;
                    UnityMcpPlugin.Instance.Save();
                    if (UnityMcpPlugin.Instance.HasMcpPluginInstance)
                    {
                        _ = UnityMcpPlugin.Instance.Disconnect();
                    }
                }
                else if (btnConnectOrDisconnect.text.Equals(ServerButtonText_Stop, StringComparison.OrdinalIgnoreCase))
                {
                    UnityMcpPlugin.KeepConnected = false;
                    UnityMcpPlugin.Instance.Save();
                    if (UnityMcpPlugin.Instance.HasMcpPluginInstance)
                    {
                        _ = UnityMcpPlugin.Instance.Disconnect();
                    }
                }
                else
                {
                    throw new Exception("Unknown button state: " + btnConnectOrDisconnect.text);
                }
            }));

            // Tools Configuration
            // -----------------------------------------------------------------
            var btnOpenTools = root.Query<Button>("btnOpenTools").First();
            btnOpenTools.RegisterCallback<ClickEvent>(evt =>
            {
                McpToolsWindow.ShowWindow();
            });

            var toolsCountLabel = root.Query<Label>("toolsCountLabel").First();

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var toolManager = plugin.McpManager.ToolManager;
                if (toolManager == null)
                {
                    toolsCountLabel.text = "0 / 0 tools";
                    return;
                }

                void UpdateStats()
                {
                    var allTools = toolManager.GetAllTools();
                    var total = allTools.Count();
                    var active = allTools.Count(t => toolManager.IsToolEnabled(t.Name));
                    toolsCountLabel.text = $"{active} / {total} tools";
                }

                UpdateStats();

                toolManager.OnToolsUpdated
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ => UpdateStats())
                    .AddTo(_disposables);
            }).AddTo(_disposables);

            // Prompts Configuration
            // -----------------------------------------------------------------
            var btnOpenPrompts = root.Query<Button>("btnOpenPrompts").First();
            btnOpenPrompts.RegisterCallback<ClickEvent>(evt =>
            {
                McpPromptsWindow.ShowWindow();
            });

            var promptsCountLabel = root.Query<Label>("promptsCountLabel").First();

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var promptManager = plugin.McpManager.PromptManager;
                if (promptManager == null)
                {
                    promptsCountLabel.text = "0 / 0 prompts";
                    return;
                }

                void UpdateStats()
                {
                    var allPrompts = promptManager.GetAllPrompts();
                    var total = allPrompts.Count();
                    var active = allPrompts.Count(p => promptManager.IsPromptEnabled(p.Name));
                    promptsCountLabel.text = $"{active} / {total} prompts";
                }

                UpdateStats();

                promptManager.OnPromptsUpdated
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ => UpdateStats())
                    .AddTo(_disposables);
            }).AddTo(_disposables);

            // Resources Configuration
            // -----------------------------------------------------------------
            var btnOpenResources = root.Query<Button>("btnOpenResources").First();
            btnOpenResources.RegisterCallback<ClickEvent>(evt =>
            {
                McpResourcesWindow.ShowWindow();
            });

            var resourcesCountLabel = root.Query<Label>("resourcesCountLabel").First();

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var resourceManager = plugin.McpManager.ResourceManager;
                if (resourceManager == null)
                {
                    resourcesCountLabel.text = "0 / 0 resources";
                    return;
                }

                void UpdateStats()
                {
                    var allResources = resourceManager.GetAllResources();
                    var total = allResources.Count();
                    var active = allResources.Count(r => resourceManager.IsResourceEnabled(r.Name));
                    resourcesCountLabel.text = $"{active} / {total} resources";
                }

                UpdateStats();

                resourceManager.OnResourcesUpdated
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ => UpdateStats())
                    .AddTo(_disposables);
            }).AddTo(_disposables);

            // Configure MCP Client
            // -----------------------------------------------------------------

#if UNITY_EDITOR_WIN
            ConfigureClientsWindows(root);
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
            ConfigureClientsMacAndLinux(root);
#endif

            // Provide raw json configuration
            // -----------------------------------------------------------------

            var rawJsonField = root.Query<TextField>("rawJsonConfiguration").First();
            rawJsonField.value = Startup.Server.RawJsonConfiguration(UnityMcpPlugin.Port, "mcpServers", UnityMcpPlugin.TimeoutMs).ToString();
        }
    }
}
