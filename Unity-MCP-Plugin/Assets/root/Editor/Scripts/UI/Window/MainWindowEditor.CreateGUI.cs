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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.McpPlugin.Skills;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
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

        // ── Shared tooltip building blocks ──────────────────────────────────────────
        //
        // These blocks are embedded verbatim inside the per-element tooltips below so
        // that each tooltip is self-contained yet the authoritative text lives in one place.

        private const string Tooltip_TransportMethods =
            "• stdio  —  The AI agent launches the MCP server as its own child process and " +
            "exchanges messages over stdin/stdout. Only one agent at a time; not recommended " +
            "unless the AI client has no HTTP support.\n\n" +
            "• http  —  The AI agent connects over HTTP to a running MCP server. Supports " +
            "multiple simultaneous agents and remote deployments. Recommended.";

        private const string Tooltip_AuthorizationTokenConcept =
            "The authorization token is a shared secret key. When required, every AI agent " +
            "must include this token in its MCP server configuration. The server rejects any " +
            "connection that does not supply the correct token.\n\n" +
            "Treat this token like a password — do not share it publicly or commit it to version control.";

        // ── Per-element tooltips ─────────────────────────────────────────────────────

        private const string Tooltip_LabelTransport =
            "Transport method defines the communication channel between the AI agent and the " +
            "MCP server. It determines how the agent discovers, launches, and sends messages " +
            "to the server.\n\n" +
            "Available methods:\n" +
            Tooltip_TransportMethods;

        private const string Tooltip_ToggleStdio =
            "Use STDIO transport.\n\n" +
            "The AI agent launches the MCP server as its own subprocess and exchanges messages " +
            "via standard input/output (stdin/stdout) streams.\n\n" +
            "Limitations:\n" +
            "  • Only one AI agent instance can connect at a time.\n" +
            "  • The local MCP server Start / Stop controls are disabled — the AI agent manages " +
            "the server lifecycle itself.\n" +
            "  • Some features requiring a persistent long-running server may not function.\n\n" +
            "Prefer HTTP unless your AI client has no HTTP support.\n\n" +
            "Transport method overview:\n" +
            Tooltip_TransportMethods;

        private const string Tooltip_ToggleHttp =
            "Use HTTP transport (recommended).\n\n" +
            "The AI agent connects over HTTP to the MCP server already running on this machine " +
            "(or a remote host if configured).\n\n" +
            "Advantages:\n" +
            "  • Multiple AI agents can connect to the same server simultaneously.\n" +
            "  • Supports remote deployments — the server can run on a different machine.\n" +
            "  • Full lifecycle control via the Start / Stop button.\n\n" +
            "Ensure the local MCP server is running before the AI agent attempts to connect.\n\n" +
            "Transport method overview:\n" +
            Tooltip_TransportMethods;

        private const string Tooltip_LabelAuthorizationToken =
            "Controls whether the MCP server requires a secret token to accept connections " +
            "from AI agents.\n\n" +
            Tooltip_AuthorizationTokenConcept;

        private const string Tooltip_ToggleAuthNone =
            "Local deployment — no authorization token required.\n\n" +
            "The MCP server accepts any connection without checking a token. This is safe when " +
            "both Unity and the AI agent run on the same machine and the server port is not " +
            "reachable from the network.\n\n" +
            "Use this when:\n" +
            "  • Unity, the MCP server, and the AI agent are all on the same computer.\n" +
            "  • No other machines need to reach the server.\n\n" +
            "⚠ Do not use this if the server port is exposed to other machines or the internet.\n\n" +
            "About authorization tokens:\n" +
            Tooltip_AuthorizationTokenConcept;

        private const string Tooltip_ToggleAuthRequired =
            "Remote deployment — authorization token required.\n\n" +
            "Every AI agent must supply the correct token in its MCP server configuration. " +
            "The server will reject any connection that does not include a valid token.\n\n" +
            "Use this when:\n" +
            "  • The MCP server runs on a different machine from the AI agent.\n" +
            "  • The server endpoint is reachable over a network.\n\n" +
            "After enabling, generate a secure token with the 'New' button, then copy it into " +
            "your AI agent's MCP server configuration.\n\n" +
            "About authorization tokens:\n" +
            Tooltip_AuthorizationTokenConcept;

        private const string Tooltip_ToolsCountLabel =
            "In MCP, a Tool is an executable function the AI agent can invoke to perform an action — " +
            "creating GameObjects, modifying assets, running scripts, reading scene data, and more. " +
            "Tools are the primary instrument through which the AI interacts with your Unity project.\n\n" +
            "Every active tool contributes tokens to the AI's context window. A smaller context means " +
            "the AI has more room to reason, producing better and more accurate results. " +
            "Disable tools you don't use to keep context consumption low.";

        private const string Tooltip_PromptsCountLabel =
            "In MCP, a Prompt is a slash-command the user can invoke directly from the AI client " +
            "(e.g. /setup-basic-scene). Each prompt can accept arguments, and when triggered it " +
            "injects a pre-written instruction into the AI context, guiding the AI on what to do next.\n\n" +
            "Prompts do not passively consume context tokens — they only use context at the moment " +
            "a user explicitly invokes them.";

        private const string Tooltip_ResourcesCountLabel =
            "In MCP, a Resource is a named, read-only data provider the AI agent can query to " +
            "understand your Unity project. Resources expose structured information such as scene " +
            "hierarchy, asset metadata, and project settings without performing any actions or " +
            "modifications.\n\n" +
            "Resources are fetched on-demand when the AI needs specific project information. " +
            "Unlike tools, they never modify any state.";

        private const string Tooltip_BtnGenerateToken =
            "Generate a new cryptographically secure random token.\n\n" +
            "Uses a cryptographic RNG to produce 32 bytes (256 bits) of randomness encoded as " +
            "URL-safe Base64 — suitable for production-level authentication.\n\n" +
            "Steps after generating:\n" +
            "  1. The new token is saved automatically to your project configuration.\n" +
            "  2. The MCP server is restarted to apply the new token.\n" +
            "  3. Copy the token from the input field next to this button.\n" +
            "  4. Open your AI agent's MCP server configuration and paste the token into the " +
            "authorization field.\n\n" +
            "⚠ Generating a new token immediately invalidates the previous one. Every AI agent " +
            "must be updated with the new token before it can connect again.";

        private VisualElement? _aiAgentLabelsContainer;
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
            SetupSkillsSection(root);
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

        private void SetAiAgentStatus(bool isConnected, IEnumerable<string>? labels = null)
        {
            _setAiAgentDataTime = DateTime.UtcNow;

            if (_aiAgentStatusCircle == null)
            {
                Logger.LogError("{field} is not initialized, cannot update AI agent status", nameof(_aiAgentStatusCircle));
                return;
            }
            if (_aiAgentLabelsContainer == null)
            {
                Logger.LogError("{field} is not initialized, cannot update AI agent status", nameof(_aiAgentLabelsContainer));
                return;
            }

            SetStatusIndicator(_aiAgentStatusCircle, isConnected ? USS_Connected : USS_Disconnected);

            _aiAgentLabelsContainer.Clear();
            var labelList = labels?.ToList();
            if (labelList == null || labelList.Count == 0)
            {
                var lbl = new Label("AI agent");
                lbl.AddToClassList("timeline-label");
                _aiAgentLabelsContainer.Add(lbl);
            }
            else
            {
                foreach (var text in labelList)
                {
                    var lbl = new Label(text);
                    lbl.AddToClassList("timeline-label");
                    _aiAgentLabelsContainer.Add(lbl);
                }
            }
        }

        #endregion

        #region Header

        private void SetupSettingsSection(VisualElement root)
        {
            var dropdownLogLevel = root.Q<EnumField>("dropdownLogLevel");
            dropdownLogLevel.value = UnityMcpPluginEditor.LogLevel;
            dropdownLogLevel.tooltip = "The minimum level of messages to log. Debug includes all messages, while Critical includes only the most severe.";
            dropdownLogLevel.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.LogLevel = evt.newValue as LogLevel? ?? LogLevel.Warning;
                SaveChanges($"[AI Game Developer] LogLevel Changed: {evt.newValue}");
            });

            var inputTimeoutMs = root.Q<IntegerField>("inputTimeoutMs");
            inputTimeoutMs.value = UnityMcpPluginEditor.TimeoutMs;
            inputTimeoutMs.tooltip = $"Timeout for MCP tool execution in milliseconds.\n\nMost tools only need a few seconds.\n\nSet this higher than your longest test execution time.\n\nImportant: Also update the '{McpPlugin.Common.Consts.MCP.Server.Args.PluginTimeout}' argument in your AI agent configuration to match this value so your AI agent doesn't timeout before the tool completes.";
            inputTimeoutMs.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = Mathf.Max(1000, inputTimeoutMs.value);
                if (newValue == UnityMcpPluginEditor.TimeoutMs)
                    return;

                if (newValue != inputTimeoutMs.value)
                    inputTimeoutMs.SetValueWithoutNotify(newValue);

                UnityMcpPluginEditor.TimeoutMs = newValue;

                var rawJsonField = root.Q<TextField>("rawJsonConfigurationStdio");
                rawJsonField.value = McpServerManager.RawJsonConfigurationStdio(UnityMcpPluginEditor.Port, "mcpServers", UnityMcpPluginEditor.TimeoutMs).ToString();

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

            _aiAgentLabelsContainer = root.Q<VisualElement>("aiAgentLabelsContainer");
            _aiAgentStatusCircle = root.Q<VisualElement>("aiAgentStatusCircle");

            inputFieldHost.value = UnityMcpPluginEditor.Host;
            inputFieldHost.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = inputFieldHost.value;
                if (UnityMcpPluginEditor.Host == newValue)
                    return;

                UnityMcpPluginEditor.Host = newValue;
                SaveChanges($"[{nameof(MainWindowEditor)}] Host Changed: {newValue}");
                Invalidate();

                UnityMcpPluginEditor.Instance.DisposeMcpPluginInstance();
                UnityBuildAndConnect();
            });

            UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
            {
                Observable.CombineLatest(
                    UnityMcpPluginEditor.ConnectionState, plugin.KeepConnected,
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

        #endregion

        #region MCP Server

        private void SetupMcpServerSection(VisualElement root)
        {
            var btnStartStop = root.Q<Button>("btnStartStopServer") ?? throw new InvalidOperationException("MCP Server start/stop button not found.");
            var statusCircle = root.Q<VisualElement>("mcpServerStatusCircle") ?? throw new InvalidOperationException("MCP Server status circle not found.");
            var statusLabel = root.Q<Label>("mcpServerLabel") ?? throw new InvalidOperationException("MCP Server status label not found.");

            Observable.CombineLatest(
                    source1: McpServerManager.ServerStatus,
                    source2: UnityMcpPluginEditor.IsConnected,
                    resultSelector: CombineMcpServerStatus)
                .ThrottleLast(TimeSpan.FromMilliseconds(50))
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(status => FetchMcpServerData(status, btnStartStop, statusCircle, statusLabel))
                .AddTo(_disposables);

            btnStartStop.RegisterCallback<ClickEvent>(evt => HandleServerButton(btnStartStop, statusLabel));

            // MCP server authorization configuration UI elements
            var labelAuthorizationToken = root.Query<Label>("labelAuthorizationToken").First();
            var toggleAuthorizationNone = root.Query<Toggle>("toggleAuthorizationNone").First();
            var toggleAuthorizationRequired = root.Query<Toggle>("toggleAuthorizationRequired").First();
            var inputAuthorizationToken = root.Query<TextField>("inputAuthorizationToken").First();
            var tokenSection = root.Query<VisualElement>("tokenSection").First();
            var btnGenerateToken = root.Query<Button>("btnGenerateToken").First();

            if (toggleAuthorizationNone == null)
            {
                Debug.LogError("toggleAuthorizationNone not found in UXML.");
                return;
            }
            if (toggleAuthorizationRequired == null)
            {
                Debug.LogError("toggleAuthorizationRequired not found in UXML.");
                return;
            }
            if (inputAuthorizationToken == null)
            {
                Debug.LogError("inputAuthorizationToken not found in UXML.");
                return;
            }
            if (tokenSection == null)
            {
                Debug.LogError("tokenSection not found in UXML.");
                return;
            }
            if (btnGenerateToken == null)
            {
                Debug.LogError("btnGenerateToken not found in UXML.");
                return;
            }

            if (labelAuthorizationToken != null) labelAuthorizationToken.tooltip = Tooltip_LabelAuthorizationToken;
            toggleAuthorizationNone.tooltip = Tooltip_ToggleAuthNone;
            toggleAuthorizationRequired.tooltip = Tooltip_ToggleAuthRequired;
            btnGenerateToken.tooltip = Tooltip_BtnGenerateToken;

            var authOption = UnityMcpPluginEditor.AuthOption;
            toggleAuthorizationNone.SetValueWithoutNotify(authOption == AuthOption.none);
            toggleAuthorizationRequired.SetValueWithoutNotify(authOption == AuthOption.required);
            inputAuthorizationToken.SetValueWithoutNotify(UnityMcpPluginEditor.Token ?? string.Empty);
            SetTokenFieldsVisible(inputAuthorizationToken, tokenSection, authOption == AuthOption.required);



            toggleAuthorizationNone.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var wasRunning = McpServerManager.IsRunning && UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio;
                    UnityMcpPluginEditor.AuthOption = AuthOption.none;
                    UnityMcpPluginEditor.Instance.Save();
                    toggleAuthorizationRequired.SetValueWithoutNotify(false);
                    SetTokenFieldsVisible(inputAuthorizationToken, tokenSection, false);
                    InvalidateAndReloadAgentUI();
                    RestartServerIfWasRunning(wasRunning);
                }
                else if (!toggleAuthorizationRequired.value)
                {
                    toggleAuthorizationNone.SetValueWithoutNotify(true);
                }
            });

            toggleAuthorizationRequired.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    var wasRunning = McpServerManager.IsRunning && UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio;
                    UnityMcpPluginEditor.AuthOption = AuthOption.required;
                    UnityMcpPluginEditor.Instance.Save();
                    toggleAuthorizationNone.SetValueWithoutNotify(false);
                    SetTokenFieldsVisible(inputAuthorizationToken, tokenSection, true);
                    InvalidateAndReloadAgentUI();
                    RestartServerIfWasRunning(wasRunning);
                }
                else if (!toggleAuthorizationNone.value)
                {
                    toggleAuthorizationRequired.SetValueWithoutNotify(true);
                }
            });

            inputAuthorizationToken.RegisterCallback<FocusOutEvent>(_ =>
            {
                var newToken = inputAuthorizationToken.value;
                if (newToken == UnityMcpPluginEditor.Token)
                    return;

                var wasRunning = McpServerManager.IsRunning && UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio;
                UnityMcpPluginEditor.Token = newToken;
                UnityMcpPluginEditor.Instance.Save();

                InvalidateAndReloadAgentUI();
                RestartServerIfWasRunning(wasRunning);
            });

            btnGenerateToken.RegisterCallback<ClickEvent>(_ =>
            {
                var newToken = UnityMcpPlugin.GenerateToken();
                inputAuthorizationToken.SetValueWithoutNotify(newToken);

                var wasRunning = McpServerManager.IsRunning && UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio;
                UnityMcpPluginEditor.Token = newToken;
                UnityMcpPluginEditor.Instance.Save();

                InvalidateAndReloadAgentUI();
                RestartServerIfWasRunning(wasRunning);
            });
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
                    UnityMcpPluginEditor.KeepServerRunning = false;
                    UnityMcpPluginEditor.Instance.Save();
                    statusLabel.text = "MCP server: Stopping...";
                    McpServerManager.StopServer();
                }
                else
                {
                    // User is starting the server - remember to auto-start
                    UnityMcpPluginEditor.KeepServerRunning = true;
                    UnityMcpPluginEditor.Instance.Save();
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
            var mcpPluginInstance = UnityMcpPluginEditor.Instance.McpPluginInstance;
            if (mcpPluginInstance == null)
            {
                Logger.LogDebug("Cannot fetch MCP server data: McpPluginInstance is null");
                return;
            }

            var mcpManagerHub = mcpPluginInstance.McpManagerHub;
            if (mcpManagerHub == null)
            {
                Logger.LogDebug("Cannot fetch MCP server data: McpManagerHub is null");
                return;
            }

            var fetchTime = DateTime.UtcNow;
            var task = mcpManagerHub.GetMcpServerData();
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
            UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
            {
                plugin.McpManager.OnClientConnected
                    .Subscribe(data =>
                    {
                        Logger.LogInformation("On AI agent connected: {clientName} ({clientVersion})",
                            data.ClientName, data.ClientVersion);

                        if (Logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                            Logger.LogTrace("AI Agent Data: {data}", data.ToPrettyJson());
                    })
                    .AddTo(_disposables);

                plugin.McpManager.OnClientDisconnected
                    .Subscribe(mcpClientData =>
                    {
                        Logger.LogInformation("On AI agent disconnected: {clientName} ({clientVersion})",
                            mcpClientData.ClientName, mcpClientData.ClientVersion);
                    })
                    .AddTo(_disposables);

                plugin.McpManager.OnClientsChanged
                    .ObserveOnCurrentSynchronizationContext()
                    .Subscribe(mcpClients =>
                    {
                        Logger.LogDebug("On AI agents changed: {count} clients", mcpClients.Count);

                        var connectedAgents = mcpClients.Where(c => c.IsConnected).ToList();
                        if (connectedAgents.Count == 0)
                        {
                            Logger.LogDebug("No connected AI agents found in clients list.");
                            SetAiAgentStatus(false);
                            return;
                        }

                        SetAiAgentStatus(true, connectedAgents.Select(a => $"AI agent: {a.ClientName} ({a.ClientVersion})"));
                    })
                    .AddTo(_disposables);

                FetchAiAgentData();
            }).AddTo(_disposables);

            UnityMcpPluginEditor.IsConnected
                .Where(isConnected => isConnected)
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(_ => FetchAiAgentData())
                .AddTo(_disposables);

            var containerMcpServer = root.Q<VisualElement>("mcpServerStatusControl") ?? throw new InvalidOperationException("mcpServerStatusControl element not found.");
            var btnStartStopMcpServer = root.Q<Button>("btnStartStopServer") ?? throw new InvalidOperationException("MCP Server start/stop button not found.");

            var toggleOptionHttp = root.Q<Toggle>("toggleOptionHttp") ?? throw new NullReferenceException("Toggle 'toggleOptionHttp' not found in UI.");
            var toggleOptionStdio = root.Q<Toggle>("toggleOptionStdio") ?? throw new NullReferenceException("Toggle 'toggleOptionStdio' not found in UI.");

            var labelTransport = root.Q<Label>("labelTransport");
            if (labelTransport != null) labelTransport.tooltip = Tooltip_LabelTransport;
            toggleOptionStdio.tooltip = Tooltip_ToggleStdio;
            toggleOptionHttp.tooltip = Tooltip_ToggleHttp;

            // Initialize with HTTP selected by default
            toggleOptionStdio.value = UnityMcpPluginEditor.TransportMethod == TransportMethod.stdio;
            toggleOptionHttp.value = UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio;
            currentAiAgentConfigurator?.SetTransportMethod(UnityMcpPluginEditor.TransportMethod);

            void UpdateMcpServerState()
            {
                containerMcpServer.SetEnabled(UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio);
                btnStartStopMcpServer.tooltip = UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio
                    ? "Start or stop the local MCP server."
                    : "Local MCP server is disabled in STDIO mode. AI agent will launch its own MCP server instance.";
            }
            UpdateMcpServerState();

            toggleOptionStdio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    UnityMcpPluginEditor.TransportMethod = TransportMethod.stdio;
                    UnityMcpPluginEditor.Instance.Save();
                    toggleOptionHttp.SetValueWithoutNotify(false);
                    currentAiAgentConfigurator?.SetTransportMethod(TransportMethod.stdio);

                    // Stop MCP server if running to switch to stdio mode
                    if (McpServerManager.IsRunning)
                    {
                        UnityMcpPluginEditor.KeepServerRunning = false;
                        UnityMcpPluginEditor.Instance.Save();
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
                    UnityMcpPluginEditor.TransportMethod = TransportMethod.streamableHttp;
                    UnityMcpPluginEditor.Instance.Save();
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

        private void FetchAiAgentData(int retryCount = 3, int retryDelayMs = 3000)
        {
            var mcpPluginInstance = UnityMcpPluginEditor.Instance.McpPluginInstance;
            if (mcpPluginInstance == null)
            {
                Logger.LogDebug("Cannot fetch AI agent data: McpPluginInstance is null");
                return;
            }

            var mcpManagerHub = mcpPluginInstance.McpManagerHub;
            if (mcpManagerHub == null)
            {
                Logger.LogDebug("Cannot fetch AI agent data: McpManagerHub is null");
                return;
            }

            var fetchTime = DateTime.UtcNow;
            var task = mcpManagerHub.GetMcpClientData();
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
                        var clients = t.Result;
                        var connectedAgents = clients.Where(c => c.IsConnected).ToList();
                        var isConnected = connectedAgents.Count > 0;
                        SetAiAgentStatus(isConnected, isConnected
                            ? connectedAgents.Select(a => $"AI agent: {a.ClientName} ({a.ClientVersion})")
                            : null);

                        // If AI agent is not connected but Unity is, retry after delay.
                        // The AI agent may need time to re-establish its session after Unity reconnects.
                        if (!isConnected && retryCount > 0 && UnityMcpPluginEditor.IsConnected.CurrentValue)
                        {
                            Logger.LogDebug("AI agent not connected yet, scheduling retry ({retriesLeft} left)", retryCount);
                            Observable.Timer(TimeSpan.FromMilliseconds(retryDelayMs))
                                .ObserveOnCurrentSynchronizationContext()
                                .Subscribe(_ => FetchAiAgentData(retryCount - 1, retryDelayMs))
                                .AddTo(_disposables);
                        }
                    }
                    else if (t.IsFaulted)
                    {
                        Logger.LogDebug("Failed to fetch AI agent data: {error}", t.Exception?.Message ?? "Unknown error");
                        SetAiAgentStatus(false);
                    }
                    else
                    {
                        SetAiAgentStatus(false, new[] { "AI agent: Not found" });
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

            UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
            {
                var manager = plugin.McpManager.ToolManager;
                if (manager == null)
                {
                    label.text = "0 / 0 tools";
                    label.tooltip = Tooltip_ToolsCountLabel + "\n\n0 enabled / 0 disabled / 0 total";
                    if (tokenLabel != null) tokenLabel.text = "~0 tokens total";
                    return;
                }

                void UpdateStats()
                {
                    var all = manager.GetAllTools();
                    var totalCount = all.Count();
                    var enabledCount = all.Count(t => manager.IsToolEnabled(t.Name));
                    var disabledCount = totalCount - enabledCount;
                    label.text = $"{enabledCount} / {totalCount} tools";
                    label.tooltip = $"{Tooltip_ToolsCountLabel}\n\n{enabledCount} enabled / {disabledCount} disabled / {totalCount} total";

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

        private void SetupSkillsSection(VisualElement root)
        {
            var labelPath = root.Q<Label>("labelSkillsOutputPath");
            var inputPath = root.Q<TextField>("inputSkillsRootFolder");
            var toggleAutoGenerate = root.Q<Toggle>("toggleAutoGenerateSkills");
            var btnGenerate = root.Q<Button>("btnGenerateSkills");

            if (inputPath == null || toggleAutoGenerate == null || btnGenerate == null)
                return;

            const string skillsOutputPathTooltip =
                "Root folder path where skill markdown files will be generated. " +
                "The recommended default location is \"SKILLS\". " +
                "AI Game Developer will also create a nested folder named \"unity-editor\" inside it.";

            if (labelPath != null) labelPath.tooltip = skillsOutputPathTooltip;
            inputPath.tooltip = skillsOutputPathTooltip;

            inputPath.SetValueWithoutNotify(UnityMcpPluginEditor.SkillsRootFolder);
            toggleAutoGenerate.SetValueWithoutNotify(UnityMcpPluginEditor.GenerateSkillFiles);

            inputPath.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SkillsRootFolder = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();
            });

            toggleAutoGenerate.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.GenerateSkillFiles = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();
            });

            btnGenerate.RegisterCallback<ClickEvent>(evt =>
            {
                var tools = UnityMcpPluginEditor.Instance.Tools;
                if (tools == null)
                {
                    Logger.LogWarning("Cannot generate skill files: Tools manager is not available.");
                    return;
                }
                new SkillFileGenerator(UnityMcpPluginEditor.Instance.Logger).Generate(
                    tools: tools.GetAllTools(),
                    rootFolder: "unity-editor",
                    basePath: UnityMcpPluginEditor.SkillsRootFolder
                );
            });
        }

        private void SetupPromptsSection(VisualElement root)
        {
            var btn = root.Q<Button>("btnOpenPrompts");
            var label = root.Q<Label>("promptsCountLabel");

            btn.RegisterCallback<ClickEvent>(evt => McpPromptsWindow.ShowWindow());

            UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
            {
                var manager = plugin.McpManager.PromptManager;
                if (manager == null)
                {
                    label.text = "0 / 0 prompts";
                    label.tooltip = Tooltip_PromptsCountLabel + "\n\n0 enabled / 0 disabled / 0 total";
                    return;
                }

                void UpdateStats()
                {
                    var all = manager.GetAllPrompts();
                    var totalCount = all.Count();
                    var enabledCount = all.Count(p => manager.IsPromptEnabled(p.Name));
                    var disabledCount = totalCount - enabledCount;
                    label.text = $"{enabledCount} / {totalCount} prompts";
                    label.tooltip = $"{Tooltip_PromptsCountLabel}\n\n{enabledCount} enabled / {disabledCount} disabled / {totalCount} total";
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

            UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
            {
                var manager = plugin.McpManager.ResourceManager;
                if (manager == null)
                {
                    label.text = "0 / 0 resources";
                    label.tooltip = Tooltip_ResourcesCountLabel + "\n\n0 enabled / 0 disabled / 0 total";
                    return;
                }

                void UpdateStats()
                {
                    var all = manager.GetAllResources();
                    var totalCount = all.Count();
                    var enabledCount = all.Count(r => manager.IsResourceEnabled(r.Name));
                    var disabledCount = totalCount - enabledCount;
                    label.text = $"{enabledCount} / {totalCount} resources";
                    label.tooltip = $"{Tooltip_ResourcesCountLabel}\n\n{enabledCount} enabled / {disabledCount} disabled / {totalCount} total";
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
