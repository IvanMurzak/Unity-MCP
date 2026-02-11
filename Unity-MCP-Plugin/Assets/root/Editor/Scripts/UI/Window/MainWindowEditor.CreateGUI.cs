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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using LogLevel = com.IvanMurzak.Unity.MCP.Runtime.Utils.LogLevel;
using TransportMethod = com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.TransportMethod;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/MainWindow.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/MainWindow.uss");

        private static readonly string[] _discordIconPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/Gizmos/discord_icon.png");
        private static readonly string[] _githubIconPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/Gizmos/github_icon.png");
        private static readonly string[] _starIconPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/Gizmos/star_icon.png");

        public const string USS_Connected = "status-indicator-circle-online";
        public const string USS_Connecting = "status-indicator-circle-connecting";
        public const string USS_Disconnected = "status-indicator-circle-disconnected";
        public const string USS_External = "status-indicator-circle-external";

        private static readonly string[] AllStatusClasses =
        {
            USS_Connected,
            USS_Connecting,
            USS_Disconnected,
            USS_External
        };

        private const string ServerButtonText_Connect = "Connect";
        private const string ServerButtonText_Disconnect = "Disconnect";
        private const string ServerButtonText_Stop = "Stop";

        private const string URL_GitHub = "https://github.com/IvanMurzak/Unity-MCP";
        private const string URL_GitHubIssues = "https://github.com/IvanMurzak/Unity-MCP/issues";
        private const string URL_Discord = "https://discord.gg/cfbdMZX99G";

        private Label? _labelAiAgentStatus;
        private VisualElement? _aiAgentStatusCircle;

        private DateTime _setMcpServerDataTime;
        private DateTime _setAiAgentDataTime;

        protected override void OnGUICreated(VisualElement root)
        {
            _disposables.Clear();

            SetupSettingsSection(root);
            SetupConnectionSection(root);
            SetupMcpServerSection(root);
            SetupAiAgentSection(root);
            SetupToolsSection(root);
            SetupPromptsSection(root);
            SetupResourcesSection(root);
            ConfigureAgents(root);
            SetupSocialButtons(root);
            SetupDebugButtons(root);
            EnableSmoothFoldoutTransitions(root);
        }

        #region Status Indicator Helpers

        private static void SetStatusIndicator(VisualElement element, string statusClass)
        {
            foreach (var cls in AllStatusClasses)
                element.RemoveFromClassList(cls);
            element.AddToClassList(statusClass);
        }

        private static string GetConnectionStatusClass(HubConnectionState state, bool keepConnected) => state switch
        {
            HubConnectionState.Connected when keepConnected => USS_Connected,
            _ when keepConnected => USS_Connecting,
            _ => USS_Disconnected
        };

        private static string GetConnectionStatusText(HubConnectionState state, bool keepConnected) => state switch
        {
            HubConnectionState.Connected when keepConnected => "Connected",
            _ when keepConnected => "Connecting...",
            _ => "Disconnected"
        };

        private static string GetButtonText(HubConnectionState state, bool keepConnected) => state switch
        {
            HubConnectionState.Connected when keepConnected => ServerButtonText_Disconnect,
            _ when keepConnected => ServerButtonText_Stop,
            _ => ServerButtonText_Connect
        };

        private void SetAiAgentStatus(bool isConnected, string? label = null)
        {
            _setAiAgentDataTime = DateTime.UtcNow;

            if (_aiAgentStatusCircle == null)
            {
                Logger.LogError("{field} is not initialized, cannot update AI agent status", nameof(_aiAgentStatusCircle));
                return;
            }
            if (_labelAiAgentStatus == null)
            {
                Logger.LogError("{field} is not initialized, cannot update AI agent status", nameof(_labelAiAgentStatus));
                return;
            }

            SetStatusIndicator(_aiAgentStatusCircle, isConnected ? USS_Connected : USS_Disconnected);
            _labelAiAgentStatus.text = label ?? "AI agent";
        }

        #endregion

        #region Header

        private void SetupSettingsSection(VisualElement root)
        {
            var dropdownLogLevel = root.Q<EnumField>("dropdownLogLevel");
            dropdownLogLevel.value = UnityMcpPlugin.LogLevel;
            dropdownLogLevel.tooltip = "The minimum level of messages to log. Debug includes all messages, while Critical includes only the most severe.";
            dropdownLogLevel.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPlugin.LogLevel = evt.newValue as LogLevel? ?? LogLevel.Warning;
                SaveChanges($"[AI Game Developer] LogLevel Changed: {evt.newValue}");
            });

            var inputTimeoutMs = root.Q<IntegerField>("inputTimeoutMs");
            inputTimeoutMs.value = UnityMcpPlugin.TimeoutMs;
            inputTimeoutMs.tooltip = $"Timeout for MCP tool execution in milliseconds.\n\nMost tools only need a few seconds.\n\nSet this higher than your longest test execution time.\n\nImportant: Also update the '{McpPlugin.Common.Consts.MCP.Server.Args.PluginTimeout}' argument in your AI agent configuration to match this value so your AI agent doesn't timeout before the tool completes.";
            inputTimeoutMs.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = Mathf.Max(1000, inputTimeoutMs.value);
                if (newValue == UnityMcpPlugin.TimeoutMs)
                    return;

                if (newValue != inputTimeoutMs.value)
                    inputTimeoutMs.SetValueWithoutNotify(newValue);

                UnityMcpPlugin.TimeoutMs = newValue;

                var rawJsonField = root.Q<TextField>("rawJsonConfigurationStdio");
                rawJsonField.value = McpServerManager.RawJsonConfigurationStdio(UnityMcpPlugin.Port, "mcpServers", UnityMcpPlugin.TimeoutMs).ToString();

                SaveChanges($"[AI Game Developer] Timeout Changed: {newValue} ms");
                UnityBuildAndConnect();
            });

            root.Q<TextField>("currentVersion").value = UnityMcpPlugin.Version;
        }

        #endregion

        #region Connection

        private void SetupConnectionSection(VisualElement root)
        {
            var inputFieldHost = root.Q<TextField>("InputServerURL");
            var btnConnect = root.Q<Button>("btnConnectOrDisconnect");
            var statusCircle = root.Q<VisualElement>("connectionStatusCircle");
            var statusText = root.Q<Label>("connectionStatusText");

            _labelAiAgentStatus = root.Q<Label>("aiAgentLabel");
            _aiAgentStatusCircle = root.Q<VisualElement>("aiAgentStatusCircle");

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
                UnityBuildAndConnect();
            });

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                Observable.CombineLatest(
                    UnityMcpPlugin.ConnectionState, plugin.KeepConnected,
                    (state, keepConnected) => (state, keepConnected))
                .ThrottleLast(TimeSpan.FromMilliseconds(10))
                .ObserveOnCurrentSynchronizationContext()
                .SubscribeOnCurrentSynchronizationContext()
                .Subscribe(tuple =>
                {
                    var (state, keepConnected) = tuple;
                    UpdateHostFieldState(inputFieldHost, plugin.KeepConnected.CurrentValue, state);
                    statusText.text = "Unity: " + GetConnectionStatusText(state, keepConnected);
                    btnConnect.text = GetButtonText(state, keepConnected);
                    var isConnect = btnConnect.text == ServerButtonText_Connect;
                    btnConnect.EnableInClassList("btn-primary", isConnect);
                    btnConnect.EnableInClassList("btn-secondary", !isConnect);
                    SetStatusIndicator(statusCircle, GetConnectionStatusClass(state, keepConnected));

                    if (!(state == HubConnectionState.Connected && keepConnected))
                        SetAiAgentStatus(false);
                })
                .AddTo(_disposables);
            }).AddTo(_disposables);

            btnConnect.RegisterCallback<ClickEvent>(evt => HandleConnectButton(btnConnect.text));
        }

        private static void UpdateHostFieldState(TextField field, bool keepConnected, HubConnectionState state)
        {
            var isReadOnly = keepConnected || state != HubConnectionState.Disconnected;
            field.isReadOnly = isReadOnly;
            field.tooltip = keepConnected
                ? "Editable only when Unity disconnected from the MCP Server."
                : $"The server URL. http://localhost:{UnityMcpPlugin.GeneratePortFromDirectory()}";

            field.EnableInClassList("disabled-text-field", isReadOnly);
            field.EnableInClassList("enabled-text-field", !isReadOnly);
        }

        private static void HandleConnectButton(string buttonText)
        {
            if (buttonText.Equals(ServerButtonText_Connect, StringComparison.OrdinalIgnoreCase))
            {
                UnityMcpPlugin.KeepConnected = true;
                UnityMcpPlugin.Instance.Save();
                UnityBuildAndConnect();
            }
            else
            {
                UnityMcpPlugin.KeepConnected = false;
                UnityMcpPlugin.Instance.Save();
                if (UnityMcpPlugin.Instance.HasMcpPluginInstance)
                    _ = UnityMcpPlugin.Instance.Disconnect();
            }
        }

        #endregion

        #region MCP Server

        private void SetupMcpServerSection(VisualElement root)
        {
            var btnStartStop = root.Q<Button>("btnStartStopServer") ?? throw new InvalidOperationException("MCP Server start/stop button not found.");
            var statusCircle = root.Q<VisualElement>("mcpServerStatusCircle") ?? throw new InvalidOperationException("MCP Server status circle not found.");
            var statusLabel = root.Q<Label>("mcpServerLabel") ?? throw new InvalidOperationException("MCP Server status label not found.");

            Observable.CombineLatest(
                    source1: McpServerManager.ServerStatus,
                    source2: UnityMcpPlugin.IsConnected,
                    resultSelector: CombineMcpServerStatus)
                .ThrottleLast(TimeSpan.FromMilliseconds(50))
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(status => FetchMcpServerData(status, btnStartStop, statusCircle, statusLabel))
                .AddTo(_disposables);

            btnStartStop.RegisterCallback<ClickEvent>(evt => HandleServerButton(btnStartStop, statusLabel));
        }

        private McpServerStatus CombineMcpServerStatus(McpServerStatus status, bool isConnected)
        {
            if (isConnected && status != McpServerStatus.Running)
                return McpServerStatus.External;

            return status;
        }

        private static string GetServerButtonText(McpServerStatus status) => status switch
        {
            McpServerStatus.Running => "Stop",
            McpServerStatus.Starting => "Starting...",
            McpServerStatus.Stopping => "Stopping...",
            McpServerStatus.External => "External",
            _ => "Start"
        };

        private static string GetServerLabelText(McpServerStatus status, McpServerData? data) => status switch
        {
            McpServerStatus.Running => "MCP server: Running (http)",
            McpServerStatus.Starting => "MCP server: Starting... (http)",
            McpServerStatus.Stopping => "MCP server: Stopping... (http)",
            McpServerStatus.External => "MCP server: External" + data?.ServerTransport switch
            {
                TransportMethod.stdio => " (stdio)",
                TransportMethod.streamableHttp => " (http)",
                _ => string.Empty
            },
            _ => "MCP server"
        };

        private static string GetServerStatusClass(McpServerStatus status) => status switch
        {
            McpServerStatus.Running => USS_Connected,
            McpServerStatus.Starting or McpServerStatus.Stopping => USS_Connecting,
            McpServerStatus.External => USS_External,
            _ => USS_Disconnected
        };

        private static void HandleServerButton(Button btnStartStop, Label statusLabel)
        {
            // Disable button immediately to prevent double-clicks
            btnStartStop.SetEnabled(false);

            try
            {
                if (McpServerManager.IsRunning)
                {
                    // User is stopping the server - remember not to auto-start
                    UnityMcpPlugin.KeepServerRunning = false;
                    UnityMcpPlugin.Instance.Save();
                    statusLabel.text = "MCP server: Stopping...";
                    McpServerManager.StopServer();
                }
                else
                {
                    // User is starting the server - remember to auto-start
                    UnityMcpPlugin.KeepServerRunning = true;
                    UnityMcpPlugin.Instance.Save();
                    statusLabel.text = "MCP server: Starting...";
                    McpServerManager.StartServer();
                }
            }
            catch
            {
                // Re-enable button on exception to avoid infinite lock
                btnStartStop.SetEnabled(true);
                throw;
            }
        }

        private void SetMcpServerData(McpServerData? data, McpServerStatus status, Button btnStartStop, VisualElement statusCircle, Label statusLabel)
        {
            _setMcpServerDataTime = DateTime.UtcNow;
            if (Logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                Logger.LogTrace("Setting MCP server data: {status}, Data: {data}", status, data?.ToPrettyJson() ?? "null");

            btnStartStop.text = GetServerButtonText(status);
            var isStart = status == McpServerStatus.Stopped;
            btnStartStop.EnableInClassList("btn-primary", isStart);
            btnStartStop.EnableInClassList("btn-secondary", !isStart);
            btnStartStop.SetEnabled(status == McpServerStatus.Running || status == McpServerStatus.Stopped);
            statusLabel.text = GetServerLabelText(status, data);
            SetStatusIndicator(statusCircle, GetServerStatusClass(status));
        }

        private void FetchMcpServerData(McpServerStatus status, Button btnStartStop, VisualElement statusCircle, Label statusLabel)
        {
            // Update UI immediately with current status
            SetMcpServerData(null, status, btnStartStop, statusCircle, statusLabel);

            // Then try to fetch additional data asynchronously
            var mcpPluginInstance = UnityMcpPlugin.Instance.McpPluginInstance;
            if (mcpPluginInstance == null)
            {
                Logger.LogDebug("Cannot fetch MCP server data: McpPluginInstance is null");
                return;
            }

            var remoteMcpManagerHub = mcpPluginInstance.RemoteMcpManagerHub;
            if (remoteMcpManagerHub == null)
            {
                Logger.LogDebug("Cannot fetch MCP server data: RemoteMcpManagerHub is null");
                return;
            }

            var fetchTime = DateTime.UtcNow;
            var task = remoteMcpManagerHub.GetMcpServerData();
            if (task == null)
            {
                Logger.LogDebug("Cannot fetch MCP server data: GetMcpServerData returned null");
                return;
            }

            task.ContinueWith(t =>
            {
                if (_setMcpServerDataTime > fetchTime)
                {
                    Logger.LogWarning("Skipping MCP server data update because a newer update was applied at {time}",
                        _setMcpServerDataTime);
                    return;
                }
                MainThread.Instance.Run(() =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        var data = t.Result;
                        SetMcpServerData(data, status, btnStartStop, statusCircle, statusLabel);
                    }
                    else if (t.IsFaulted)
                    {
                        Logger.LogDebug("Failed to fetch MCP server data: {error}", t.Exception?.Message ?? "Unknown error");
                    }
                });
            });
        }

        #endregion

        #region AI Agent

        private void SetupAiAgentSection(VisualElement root)
        {
            var mcpPluginInstance = UnityMcpPlugin.Instance.McpPluginInstance;
            if (mcpPluginInstance != null)
            {
                mcpPluginInstance.McpManager.OnClientConnected
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(data =>
                    {
                        Logger.LogInformation("On AI agent connected: {clientName} {clientVersion}", data.ClientName, data.ClientVersion);
                        if (Logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                            Logger.LogTrace("AI Agent Data: {data}", data.ToPrettyJson());

                        SetAiAgentStatus(data.IsConnected, $"AI agent: {data.ClientName} ({data.ClientVersion})");
                    })
                    .AddTo(_disposables);

                mcpPluginInstance.McpManager.OnClientDisconnected
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ =>
                    {
                        Logger.LogInformation("On AI agent disconnected");
                        SetAiAgentStatus(false);
                    })
                    .AddTo(_disposables);

                FetchAiAgentData();
            }

            UnityMcpPlugin.IsConnected
                .Where(isConnected => isConnected)
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(_ => FetchAiAgentData())
                .AddTo(_disposables);

            var containerMcpServer = root.Q<VisualElement>("TimelinePointMcpServer") ?? throw new InvalidOperationException("TimelinePointMcpServer element not found.");
            var btnStartStopMcpServer = root.Q<Button>("btnStartStopServer") ?? throw new InvalidOperationException("MCP Server start/stop button not found.");

            var toggleOptionHttp = root.Q<Toggle>("toggleOptionHttp") ?? throw new NullReferenceException("Toggle 'toggleOptionHttp' not found in UI.");
            var toggleOptionStdio = root.Q<Toggle>("toggleOptionStdio") ?? throw new NullReferenceException("Toggle 'toggleOptionStdio' not found in UI.");

            // Initialize with HTTP selected by default
            toggleOptionStdio.value = UnityMcpPlugin.TransportMethod == TransportMethod.stdio;
            toggleOptionHttp.value = UnityMcpPlugin.TransportMethod != TransportMethod.stdio;
            currentAiAgentConfigurator?.SetTransportMethod(UnityMcpPlugin.TransportMethod);

            void UpdateMcpServerState()
            {
                containerMcpServer.SetEnabled(UnityMcpPlugin.TransportMethod != TransportMethod.stdio);
                btnStartStopMcpServer.tooltip = UnityMcpPlugin.TransportMethod != TransportMethod.stdio
                    ? "Start or stop the local MCP server."
                    : "Local MCP server is disabled in STDIO mode. AI agent will launch its own MCP server instance.";
            }
            UpdateMcpServerState();

            toggleOptionStdio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    UnityMcpPlugin.TransportMethod = TransportMethod.stdio;
                    UnityMcpPlugin.Instance.Save();
                    toggleOptionHttp.SetValueWithoutNotify(false);
                    currentAiAgentConfigurator?.SetTransportMethod(TransportMethod.stdio);

                    // Stop MCP server if running to switch to stdio mode
                    if (McpServerManager.IsRunning)
                    {
                        UnityMcpPlugin.KeepServerRunning = false;
                        UnityMcpPlugin.Instance.Save();
                        McpServerManager.StopServer();
                    }
                }
                else if (!toggleOptionHttp.value)
                {
                    // Prevent both toggles from being unchecked
                    toggleOptionStdio.SetValueWithoutNotify(true);
                }
                UpdateMcpServerState();
            });

            toggleOptionHttp.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    UnityMcpPlugin.TransportMethod = TransportMethod.streamableHttp;
                    UnityMcpPlugin.Instance.Save();
                    toggleOptionStdio.SetValueWithoutNotify(false);
                    currentAiAgentConfigurator?.SetTransportMethod(TransportMethod.streamableHttp);
                }
                else if (!toggleOptionStdio.value)
                {
                    // Prevent both toggles from being unchecked
                    toggleOptionHttp.SetValueWithoutNotify(true);
                }
                UpdateMcpServerState();
            });
        }

        private void FetchAiAgentData()
        {
            var mcpPluginInstance = UnityMcpPlugin.Instance.McpPluginInstance;
            if (mcpPluginInstance == null)
            {
                Logger.LogDebug("Cannot fetch AI agent data: McpPluginInstance is null");
                return;
            }

            var remoteMcpManagerHub = mcpPluginInstance.RemoteMcpManagerHub;
            if (remoteMcpManagerHub == null)
            {
                Logger.LogDebug("Cannot fetch AI agent data: RemoteMcpManagerHub is null");
                return;
            }

            var fetchTime = DateTime.UtcNow;
            var task = remoteMcpManagerHub.GetMcpClientData();
            if (task == null)
            {
                Logger.LogDebug("Cannot fetch AI agent data: GetMcpClientData returned null");
                return;
            }

            task.ContinueWith(t =>
            {
                if (_setAiAgentDataTime > fetchTime)
                {
                    Logger.LogWarning("Skipping AI agent data update because a newer update was applied at {time}",
                        _setAiAgentDataTime);
                    return;
                }
                MainThread.Instance.Run(() =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        var data = t.Result;
                        SetAiAgentStatus(data.IsConnected, data.IsConnected
                            ? $"AI agent: {data.ClientName} ({data.ClientVersion})"
                            : "AI agent");
                    }
                    else if (t.IsFaulted)
                    {
                        Logger.LogDebug("Failed to fetch AI agent data: {error}", t.Exception?.Message ?? "Unknown error");
                        SetAiAgentStatus(false);
                    }
                    else
                    {
                        SetAiAgentStatus(false, "AI agent: Not found");
                    }
                });
            });
        }

        #endregion

        #region MCP Features

        private void SetupToolsSection(VisualElement root)
        {
            var btn = root.Q<Button>("btnOpenTools");
            var label = root.Q<Label>("toolsCountLabel");
            var tokenLabel = root.Q<Label>("toolsTokenCountLabel");

            btn.RegisterCallback<ClickEvent>(evt => McpToolsWindow.ShowWindow());

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var manager = plugin.McpManager.ToolManager;
                if (manager == null)
                {
                    label.text = "0 / 0 tools";
                    if (tokenLabel != null) tokenLabel.text = "~0 tokens total";
                    return;
                }

                void UpdateStats()
                {
                    var all = manager.GetAllTools();
                    var enabledCount = all.Count(t => manager.IsToolEnabled(t.Name));
                    label.text = $"{enabledCount} / {all.Count()} tools";

                    // Calculate total tokens for enabled tools
                    if (tokenLabel != null)
                    {
                        var totalTokens = all
                            .Where(t => manager.IsToolEnabled(t.Name))
                            .Sum(t => t.TokenCount);
                        tokenLabel.text = $"~{UIMcpUtils.FormatTokenCount(totalTokens)} tokens total";
                    }
                }
                UpdateStats();

                manager.OnToolsUpdated
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ => UpdateStats())
                    .AddTo(_disposables);
            }).AddTo(_disposables);
        }

        private void SetupPromptsSection(VisualElement root)
        {
            var btn = root.Q<Button>("btnOpenPrompts");
            var label = root.Q<Label>("promptsCountLabel");

            btn.RegisterCallback<ClickEvent>(evt => McpPromptsWindow.ShowWindow());

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var manager = plugin.McpManager.PromptManager;
                if (manager == null) { label.text = "0 / 0 prompts"; return; }

                void UpdateStats()
                {
                    var all = manager.GetAllPrompts();
                    label.text = $"{all.Count(p => manager.IsPromptEnabled(p.Name))} / {all.Count()} prompts";
                }
                UpdateStats();

                manager.OnPromptsUpdated
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ => UpdateStats())
                    .AddTo(_disposables);
            }).AddTo(_disposables);
        }

        private void SetupResourcesSection(VisualElement root)
        {
            var btn = root.Q<Button>("btnOpenResources");
            var label = root.Q<Label>("resourcesCountLabel");

            btn.RegisterCallback<ClickEvent>(evt => McpResourcesWindow.ShowWindow());

            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var manager = plugin.McpManager.ResourceManager;
                if (manager == null) { label.text = "0 / 0 resources"; return; }

                void UpdateStats()
                {
                    var all = manager.GetAllResources();
                    label.text = $"{all.Count(r => manager.IsResourceEnabled(r.Name))} / {all.Count()} resources";
                }
                UpdateStats();

                manager.OnResourcesUpdated
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(_ => UpdateStats())
                    .AddTo(_disposables);
            }).AddTo(_disposables);
        }

        #endregion

        #region Social and Debug Buttons

        private void SetupSocialButtons(VisualElement root)
        {
            var discordIcon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(_discordIconPaths);
            var githubIcon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(_githubIconPaths);
            var starIcon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(_starIconPaths);

            SetupSocialButton(root, "btnGitHubStar", "btnGitHubStarIcon", starIcon, URL_GitHub, "Star on GitHub");
            SetupSocialButton(root, "btnGitHubIssue", "btnGitHubIssueIcon", githubIcon, URL_GitHubIssues, "Report an issue on GitHub");
            SetupSocialButton(root, "btnDiscordHelp", "btnDiscordHelpIcon", discordIcon, URL_Discord, "Get help on Discord");
        }

        private static void SetupSocialButton(VisualElement root, string buttonName, string iconName, Texture2D? icon, string url, string tooltip)
        {
            var button = root.Q<Button>(buttonName);
            if (button == null)
                return;

            var iconElement = root.Q<VisualElement>(iconName);
            if (iconElement != null)
            {
                iconElement.style.backgroundImage = icon;
                iconElement.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
            }

            button.tooltip = tooltip;
            button.RegisterCallback<ClickEvent>(evt => Application.OpenURL(url));
        }

        private static void SetupDebugButtons(VisualElement root)
        {
            var btnCheckSerialization = root.Q<Button>("btnCheckSerialization");
            if (btnCheckSerialization != null)
            {
                btnCheckSerialization.tooltip = "Open Serialization Check window";
                btnCheckSerialization.RegisterCallback<ClickEvent>(evt => SerializationCheckWindow.ShowWindow());
            }
        }

        #endregion
    }
}
