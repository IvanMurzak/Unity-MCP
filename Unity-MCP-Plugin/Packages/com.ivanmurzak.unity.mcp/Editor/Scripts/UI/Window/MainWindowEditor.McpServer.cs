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
using System.Threading;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.UI.Controls;
using Microsoft.Extensions.Logging;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
using TransportMethod = com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server.TransportMethod;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private void SetupMcpServerSection(VisualElement root)
        {
            var btnStartStop = root.Q<Button>("btnStartStopServer") ?? throw new InvalidOperationException("MCP Server start/stop button not found.");
            var statusCircle = root.Q<VisualElement>("mcpServerStatusCircle") ?? throw new InvalidOperationException("MCP Server status circle not found.");
            var statusLabel = root.Q<Label>("mcpServerLabel") ?? throw new InvalidOperationException("MCP Server status label not found.");

            var timelinePointMcpServer = root.Q<VisualElement>("TimelinePointMcpServer");
            if (timelinePointMcpServer != null)
                timelinePointMcpServer.tooltip = Tooltip_McpServerTimelineLabel;
            statusCircle.tooltip = Tooltip_McpServerTimelineLabel;
            statusLabel.tooltip = Tooltip_McpServerTimelineLabel;

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
            var labelAuthorizationToken = root.Q<Label>("labelAuthorizationToken");
            var segmentAuthorization = root.Q<VisualElement>("segmentAuthorization");
            var inputAuthorizationToken = root.Q<TextField>("inputAuthorizationToken");
            var tokenSection = root.Q<VisualElement>("tokenSection");
            var btnGenerateToken = root.Q<Button>("btnGenerateToken");

            if (segmentAuthorization == null || inputAuthorizationToken == null
                || tokenSection == null || btnGenerateToken == null)
            {
                Debug.LogError("One or more authorization UI elements not found in UXML: " +
                    $"segmentAuthorization={segmentAuthorization != null}, " +
                    $"inputAuthorizationToken={inputAuthorizationToken != null}, " +
                    $"tokenSection={tokenSection != null}, " +
                    $"btnGenerateToken={btnGenerateToken != null}");
                return;
            }

            var authControl = new SegmentedControl("none", "required");
            authControl.SetTooltips(Tooltip_ToggleAuthNone, Tooltip_ToggleAuthRequired);
            segmentAuthorization.Add(authControl);

            inputAuthorizationToken.isPasswordField = true;
            inputAuthorizationToken.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.C && (evt.ctrlKey || evt.commandKey))
                {
                    GUIUtility.systemCopyBuffer = inputAuthorizationToken.value;
                    evt.StopPropagation();
                }
            });

            if (labelAuthorizationToken != null) labelAuthorizationToken.tooltip = Tooltip_LabelAuthorizationToken;
            btnGenerateToken.tooltip = Tooltip_BtnGenerateToken;

            var authOption = UnityMcpPluginEditor.AuthOption;
            authControl.SetValueWithoutNotify(authOption == AuthOption.none ? 0 : 1);
            inputAuthorizationToken.SetValueWithoutNotify(UnityMcpPluginEditor.Token ?? string.Empty);
            SetTokenFieldsVisible(inputAuthorizationToken, tokenSection, authOption == AuthOption.required);

            authControl.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                if (evt.newValue == 0)
                {
                    ApplyServerSettingAndRestart(() =>
                    {
                        UnityMcpPluginEditor.AuthOption = AuthOption.none;
                    });
                    SetTokenFieldsVisible(inputAuthorizationToken, tokenSection, false);
                    InvalidateAndReloadAgentUI();
                }
                else
                {
                    ApplyServerSettingAndRestart(() =>
                    {
                        UnityMcpPluginEditor.AuthOption = AuthOption.required;
                    });
                    SetTokenFieldsVisible(inputAuthorizationToken, tokenSection, true);
                    InvalidateAndReloadAgentUI();
                }
            });

            inputAuthorizationToken.RegisterCallback<FocusOutEvent>(_ =>
            {
                var newToken = inputAuthorizationToken.value;
                if (newToken == UnityMcpPluginEditor.Token)
                    return;

                ApplyServerSettingAndRestart(() =>
                {
                    UnityMcpPluginEditor.Token = newToken;
                });
                InvalidateAndReloadAgentUI();
            });

            btnGenerateToken.RegisterCallback<ClickEvent>(_ =>
            {
                var newToken = UnityMcpPlugin.GenerateToken();
                inputAuthorizationToken.SetValueWithoutNotify(newToken);

                ApplyServerSettingAndRestart(() =>
                {
                    UnityMcpPluginEditor.Token = newToken;
                });
                InvalidateAndReloadAgentUI();
            });
        }

        private void ApplyServerSettingAndRestart(Action applySetting)
        {
            var wasRunning = McpServerManager.IsRunning && UnityMcpPluginEditor.TransportMethod != TransportMethod.stdio;
            applySetting();
            UnityMcpPluginEditor.Instance.Save();
            RestartServerIfWasRunning(wasRunning);
        }

        internal static McpServerStatus CombineMcpServerStatus(McpServerStatus status, bool isConnected)
        {
            if (isConnected && status != McpServerStatus.Running)
                return McpServerStatus.External;

            return status;
        }

        internal static string GetServerButtonText(McpServerStatus status) => status switch
        {
            McpServerStatus.Running => "Stop",
            McpServerStatus.Starting => "Starting...",
            McpServerStatus.Stopping => "Stopping...",
            McpServerStatus.External => "External",
            _ => "Start"
        };

        internal static string GetServerLabelText(McpServerStatus status, TransportMethod? serverTransport) => status switch
        {
            McpServerStatus.Running => "MCP server: Running (http)",
            McpServerStatus.Starting => "MCP server: Starting... (http)",
            McpServerStatus.Stopping => "MCP server: Stopping... (http)",
            McpServerStatus.External => "MCP server: External" + serverTransport switch
            {
                TransportMethod.stdio => " (stdio)",
                TransportMethod.streamableHttp => " (http)",
                _ => string.Empty
            },
            _ => "MCP server"
        };

        internal static string GetServerStatusClass(McpServerStatus status) => status switch
        {
            McpServerStatus.Running => USS_Connected,
            McpServerStatus.Starting or McpServerStatus.Stopping => USS_Connecting,
            McpServerStatus.External => USS_External,
            _ => USS_Disconnected
        };

        internal static bool IsServerButtonEnabled(McpServerStatus status) =>
            status == McpServerStatus.Running || status == McpServerStatus.Stopped;

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

        private long SetMcpServerData(McpServerData? data, McpServerStatus status, Button btnStartStop, VisualElement statusCircle, Label statusLabel)
        {
            var version = Interlocked.Increment(ref _mcpServerDataVersion);
            if (Logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                Logger.LogTrace("Setting MCP server data: {status}, Data: {data}", status, data?.ToPrettyJson() ?? "null");

            btnStartStop.text = GetServerButtonText(status);
            var isStart = status == McpServerStatus.Stopped;
            btnStartStop.EnableInClassList("btn-primary", isStart);
            btnStartStop.EnableInClassList("btn-secondary", !isStart);
            btnStartStop.SetEnabled(IsServerButtonEnabled(status));
            statusLabel.text = GetServerLabelText(status, data?.ServerTransport);
            SetStatusIndicator(statusCircle, GetServerStatusClass(status));
            return version;
        }

        private void FetchMcpServerData(McpServerStatus status, Button btnStartStop, VisualElement statusCircle, Label statusLabel)
        {
            // Update UI immediately with current status; capture the version atomically so that
            // the async result can detect if a newer update has superseded it.
            var fetchVersion = SetMcpServerData(null, status, btnStartStop, statusCircle, statusLabel);

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

            var task = mcpManagerHub.GetMcpServerData();
            if (task == null)
            {
                Logger.LogDebug("Cannot fetch MCP server data: GetMcpServerData returned null");
                return;
            }

            task.ContinueWith(t =>
            {
                if (Interlocked.Read(ref _mcpServerDataVersion) != fetchVersion)
                {
                    Logger.LogTrace("Skipping MCP server data update because a newer update was applied at {time}",
                        DateTime.UtcNow);
                    return;
                }
                MainThread.Instance.Run(() =>
                {
                    // Second check: close the TOCTOU window between the thread-pool check above
                    // and the main-thread callback execution.
                    if (Interlocked.Read(ref _mcpServerDataVersion) != fetchVersion)
                        return;
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
    }
}
