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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using com.IvanMurzak.Unity.MCP.Editor.UI.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public partial class MainWindowEditor
    {
        private TextField? _inputFieldHost;
        private VisualElement? _connectionStatusCircle;
        private Label? _connectionStatusText;
        private readonly SerialDisposable _authRejectedSubscription = new();

        private void SetupConnectionSection(VisualElement root)
        {
            _inputFieldHost = root.Q<TextField>("InputServerURL");
            var btnConnect = root.Q<Button>("btnConnectOrDisconnect");
            _connectionStatusCircle = root.Q<VisualElement>("connectionStatusCircle");
            _connectionStatusText = root.Q<Label>("connectionStatusText");

            _btnConnect = btnConnect;
            _timelinePointUnity = root.Q<VisualElement>("TimelinePointUnity");
            if (_timelinePointUnity != null)
                _timelinePointUnity.tooltip = Tooltip_UnityTimelineLabel;
            if (_connectionStatusCircle != null)
                _connectionStatusCircle.tooltip = Tooltip_UnityTimelineLabel;
            if (_connectionStatusText != null)
                _connectionStatusText.tooltip = Tooltip_UnityTimelineLabel;

            _aiAgentLabelsContainer = root.Q<VisualElement>("aiAgentLabelsContainer");
            _aiAgentStatusCircle = root.Q<VisualElement>("aiAgentStatusCircle");

            var timelinePointAiAgent = root.Q<VisualElement>("TimelinePointAiAgent");
            if (timelinePointAiAgent != null)
                timelinePointAiAgent.tooltip = Tooltip_AiAgentTimelineLabel;

            _inputFieldHost.value = UnityMcpPluginEditor.LocalHost;
            _inputFieldHost.RegisterCallback<FocusOutEvent>(evt =>
            {
                var newValue = _inputFieldHost.value;
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
                UpdateConnectionUI(state, keepConnected);
            });

            UnityMcpPluginEditor.PluginProperty
                .WhereNotNull()
                .Subscribe(plugin =>
                {
                    _authRejectedSubscription.Disposable = plugin.OnAuthorizationRejected
                        .ObserveOnCurrentSynchronizationContext()
                        .Subscribe(_ => OnAuthorizationRejected());
                })
                .AddTo(_disposables);

            btnConnect.RegisterCallback<ClickEvent>(evt => HandleConnectButton(btnConnect.text));
        }

        private void OnAuthorizationRejected()
        {
            if (!ShouldPromptOnAuthorizationRejected(UnityMcpPluginEditor.ConnectionMode, AccountCredentialService.IsSignedIn))
                return;

            // The machine store is the only Cloud credential source (T9 — the cloudToken
            // UserSettings mirror was removed). A rejection here means the presented credential is
            // not usable right now — surface the prompt path instead of staying silently red
            // (oauth-client-error-hygiene 02 §C4). On a dead-family verdict the
            // AssistedReauthService additionally auto-opens the browser (D4), once-gated.
            Debug.LogWarning("[AI Game Developer] The server rejected the authorization. " +
                "Please click 'Authorize' to sign in again.");

            UpdateCloudAuthState();
            RefreshConnectionUI();
        }

        /// <summary>
        /// Whether a server authorization rejection surfaces the sign-in prompt path. Cloud mode
        /// ALWAYS prompts (oauth-client-error-hygiene 02 §C4): the old signed-in early-return
        /// assumed the coordinator's silent refresh+reconnect would recover, but a DEAD credential
        /// family can never be refreshed — the early-return left a signed-in editor silently red
        /// while the authorization server was hit every 60 s. <paramref name="isSignedIn"/> stays an
        /// explicit input so tests pin the removal (restoring the early-return reddens them).
        /// </summary>
        internal static bool ShouldPromptOnAuthorizationRejected(ConnectionMode mode, bool isSignedIn)
            => mode == ConnectionMode.Cloud;

        private void UpdateConnectionUI(HubConnectionState state, bool keepConnected)
        {
            if (_inputFieldHost == null || _connectionStatusText == null
                || _btnConnect == null || _connectionStatusCircle == null)
                return;

            // DEV-ONLY: a dev-injected connection status pins the row — skip the live re-sync so the
            // injection sticks on screen (see _devConnectionStatusOverride). Never set in a shipped plugin.
            if (_devConnectionStatusOverride != null)
                return;

            UpdateHostFieldState(_inputFieldHost, keepConnected, state);
            _connectionStatusText.text = "Unity: " + GetConnectionStatusText(state, keepConnected);
            _btnConnect.text = GetButtonText(state, keepConnected);
            var isConnect = _btnConnect.text == ServerButtonText_Connect;
            _btnConnect.EnableInClassList("btn-primary", isConnect);
            _btnConnect.EnableInClassList("btn-secondary", !isConnect);
            SetStatusIndicator(_connectionStatusCircle, GetConnectionStatusClass(state, keepConnected));

            if (!(state == HubConnectionState.Connected && keepConnected))
                SetAiAgentStatus(false);

            UpdateCloudAuthState();
        }

        /// <summary>
        /// Reads the current connection state and refreshes the Unity connection row UI.
        /// Call this whenever the UI might be stale (e.g. after mode switch).
        /// </summary>
        private void RefreshConnectionUI()
        {
            var state = UnityMcpPluginEditor.ConnectionState.CurrentValue;
            var keepConnected = UnityMcpPluginEditor.KeepConnected;
            UpdateConnectionUI(state, keepConnected);
        }

        /// <summary>
        /// Schedules a delayed <see cref="RefreshConnectionUI"/> to catch state changes
        /// that arrive after a mode switch or reconnect (e.g. async SignalR handshake).
        /// </summary>
        private void ScheduleConnectionUIRefresh()
        {
            rootVisualElement?.schedule.Execute(() => RefreshConnectionUI()).ExecuteLater(500);
            rootVisualElement?.schedule.Execute(() => RefreshConnectionUI()).ExecuteLater(2000);
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

        private void HandleConnectButton(string buttonText)
        {
            if (buttonText.Equals(ServerButtonText_Connect, StringComparison.OrdinalIgnoreCase))
            {
                ConnectToServer();
            }
            else
            {
                UnityMcpPluginEditor.KeepConnected = false;
                UnityMcpPluginEditor.Instance.Save();
                if (UnityMcpPluginEditor.Instance.HasMcpPluginInstance)
                    _ = UnityMcpPluginEditor.Instance.Disconnect();
            }
            ScheduleConnectionUIRefresh();
        }

        /// <summary>
        /// Initiates connection to the server. Called by both the Connect button and the
        /// connection alert panel's Connect button.
        /// </summary>
        private static void ConnectToServer()
        {
            UnityMcpPluginEditor.KeepConnected = true;
            UnityMcpPluginEditor.Instance.Save();
            UnityBuildAndConnect();
        }

        private void SetupConnectionModeToggle(VisualElement root)
        {
            var container = root.Q<VisualElement>("segmentConnectionMode");
            if (container == null) return;

            var control = new SegmentedControl("Custom", "Cloud");
            control.SetTooltips(
                "Connect to your own MCP server. The plugin starts a local MCP server automatically and manages its lifecycle. Use this when you want full control over the server configuration, port, transport, and authorization settings.",
                "Connect to a remote MCP server hosted in the cloud (e.g. ai-game.dev). No local server is started — the plugin connects directly to a built-in cloud endpoint (Cloud URL is predefined and not configurable). Requires authorization via device code flow.");
            container.Add(control);

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
            control.SetValueWithoutNotify(currentMode == ConnectionMode.Custom ? 0 : 1);
            UpdateModeVisibility(currentMode);

            control.RegisterCallback<ChangeEvent<int>>(evt =>
            {
                if (evt.newValue == 0)
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
                    ScheduleConnectionUIRefresh();
                }
                else
                {
                    UnityMcpPluginEditor.ConnectionMode = ConnectionMode.Cloud;

                    // Cloud requires streamableHttp. Cloud authorization is driven by ConnectionMode.Cloud
                    // itself (the shared configurator's IsHttpAuthRequired / OAuth account path), NOT by the
                    // local-server AuthOption — so we no longer stamp the retired `required` value here
                    // (mcp-authorize g5/g6). AuthOption stays a purely local-server (Custom-mode) setting.
                    UnityMcpPluginEditor.TransportMethod = TransportMethod.streamableHttp;

                    UnityMcpPluginEditor.Instance.Save();
                    UpdateModeVisibility(ConnectionMode.Cloud);
                    UpdateCloudAuthState();

                    // Invalidate cached AI agent configs so they pick up the new Host/Token
                    InvalidateAndReloadAgentUI();

                    // Stop local server — not needed in Cloud mode
                    if (McpServerManager.IsRunning || McpServerManager.IsStarting)
                        McpServerManager.StopServer();

                    // Reconnect to cloud server (only if authorized via the machine store)
                    if (AccountCredentialService.IsSignedIn)
                        ReconnectAfterModeSwitch();
                    ScheduleConnectionUIRefresh();
                }
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
            DeviceAuthFlowState.Authorized => "Authorized — completing sign-in...",
            DeviceAuthFlowState.Failed => $"Failed: {errorMessage}",
            DeviceAuthFlowState.Expired => "Expired — try again",
            DeviceAuthFlowState.Cancelled => "Cancelled",
            _ => ""
        };

        private void SetupCloudAuthSection(VisualElement root)
        {
            var inputCloudToken = root.Q<TextField>("inputCloudToken");
            var btnRevoke = root.Q<Button>("btnCloudRevoke");
            var btnAuthorize = root.Q<Button>("btnCloudAuthorize");
            var statusLabel = root.Q<Label>("labelCloudAuthStatus");
            if (inputCloudToken == null || btnAuthorize == null) return;

            _btnAuthorize = btnAuthorize;

            inputCloudToken.isPasswordField = true;
            inputCloudToken.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.C && (evt.ctrlKey || evt.commandKey))
                {
                    GUIUtility.systemCopyBuffer = inputCloudToken.value;
                    evt.StopPropagation();
                }
            });

            const string tokenPlaceholder = "Token — press Authorize";
            const string signedInDisplay = "Signed in via account";
            // The raw cloud JWT is no longer mirrored into the config (T9); it lives only in the shared
            // machine store. This read-only field therefore reflects sign-in state, not the token value.
            void UpdateTokenDisplay()
            {
                var signedIn = AccountCredentialService.IsSignedIn;
                inputCloudToken.value = signedIn ? signedInDisplay : tokenPlaceholder;
                inputCloudToken.EnableInClassList("token-placeholder", !signedIn);
            }

            UpdateTokenDisplay();
            UpdateCloudAuthState();

            void UpdateRevokeButtonVisibility()
            {
                if (btnRevoke != null)
                    btnRevoke.style.display = AccountCredentialService.IsSignedIn
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }
            UpdateRevokeButtonVisibility();

            async Task SignOutMachineWideThenRefreshUiAsync()
            {
                try
                {
                    var result = await AccountCredentialService.SignOutMachineWideAsync();
                    // Never surface token material (07 rule 2) — state only.
                    if (statusLabel != null)
                    {
                        statusLabel.text = result.StoreDeleted
                            ? "Signed out on this machine."
                            : "Sign-out incomplete — another tool holds the credential lock. Try again.";
                        statusLabel.style.display = DisplayStyle.Flex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AI Game Developer] Machine-wide sign-out failed: {ex.Message}");
                    if (statusLabel != null)
                    {
                        statusLabel.text = "Sign-out failed — see the Console for details.";
                        statusLabel.style.display = DisplayStyle.Flex;
                    }
                }

                UpdateTokenDisplay();
                UpdateRevokeButtonVisibility();

                // Invalidate cached AI agent configs
                InvalidateAndReloadAgentUI();

                UpdateCloudAuthState();

                // Disconnect if currently in Cloud mode
                if (UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud
                    && UnityMcpPluginEditor.Instance.HasMcpPluginInstance)
                    _ = UnityMcpPluginEditor.Instance.Disconnect();
                Repaint();
            }

            btnRevoke?.RegisterCallback<ClickEvent>(evt =>
            {
                // F6 (D5): sign-out is MACHINE-WIDE — every engine plugin, CLI, and the desktop app
                // share the one machine credential store, so signing out here signs them all out.
                // Interactive confirm first (03 F6.1 / 08 J5), then best-effort RFC 7009 revocation
                // of every stored family + the lock-protocol store delete.
                var confirmed = EditorUtility.DisplayDialog(
                    "Sign out of AI Game Dev?",
                    "This signs out ALL AI Game Dev tools on this machine — every engine plugin, CLI, and the desktop app.\n\n"
                    + "Tokens are revoked server-side (best effort) and the shared machine credential is deleted.",
                    "Sign Out",
                    "Cancel");
                if (!confirmed)
                    return;
                _ = SignOutMachineWideThenRefreshUiAsync();
            });

            // The Authorize flow lives in the service now (oauth-client-error-hygiene 02 §C4:
            // extraction, not reuse) — it runs the whole D4 ladder (device code → default-browser
            // open → poll until approval → F1 login commit → reload + reconnect) with or without
            // this window; the window only renders its state and delegates the button.
            void RefreshAuthFlowUi()
            {
                // Service events may fire on any thread. Use RunAsync (EditorApplication.update-
                // based) instead of delayCall so the UI updates even when the Unity Editor window is
                // not focused — delayCall is throttled/paused when Unity loses application focus.
                MainThread.Instance.RunAsync(() =>
                {
                    if (statusLabel != null)
                    {
                        // The persistent status (D4 ladder step 3 / commit progress) wins over the
                        // flow-state line; both come from the service.
                        statusLabel.text = AssistedReauthService.StatusMessage
                            ?? GetAuthFlowStatusMessage(
                                AssistedReauthService.FlowState,
                                AssistedReauthService.UserCode,
                                AssistedReauthService.FlowErrorMessage);
                        statusLabel.style.display = string.IsNullOrEmpty(statusLabel.text)
                            ? DisplayStyle.None
                            : DisplayStyle.Flex;
                    }
                    if (btnAuthorize != null)
                    {
                        btnAuthorize.text = AssistedReauthService.IsFlowRunning ? "Cancel" : "Authorize";
                    }
                    UpdateTokenDisplay();
                    UpdateRevokeButtonVisibility();
                    UpdateCloudAuthState();
                    Repaint();
                });
            }

            // Replace any previous handler (CreateGUI reruns on Invalidate) and keep a reference so
            // OnDisable can detach — a static event must never retain a closed window.
            if (_assistedReauthChangedHandler != null)
                AssistedReauthService.Changed -= _assistedReauthChangedHandler;
            _assistedReauthChangedHandler = RefreshAuthFlowUi;
            AssistedReauthService.Changed += _assistedReauthChangedHandler;
            RefreshAuthFlowUi(); // render pre-window state (e.g. an auto flow already in flight, or the carousel status)

            async Task AuthorizeThenRefreshUiAsync()
            {
                // NOTE (d1): DeviceAuthFlowState.Authorized means the DEVICE GRANT was approved —
                // the service completes the F1 login commit (agent family → exchange → plugin
                // family), reloads the provider, and reconnects (KeepConnected intent respected)
                // before this await returns Committed.
                var outcome = await AssistedReauthService.AuthorizeAsync();

                await MainThread.Instance.RunAsync(() =>
                {
                    UpdateTokenDisplay();
                    UpdateRevokeButtonVisibility();
                    UpdateCloudAuthState();
                    if (outcome == AssistedReauthOutcome.Committed)
                    {
                        // Invalidate cached AI agent configs so they pick up the new credential
                        InvalidateAndReloadAgentUI();
                    }
                    Repaint();
                });
            }

            _startAuthorizeAction = () =>
            {
                // Click while the flow is running = cancel (unchanged UX).
                if (AssistedReauthService.IsFlowRunning)
                {
                    AssistedReauthService.CancelFlow();
                    return;
                }
                _ = AuthorizeThenRefreshUiAsync();
            };

            btnAuthorize.RegisterCallback<ClickEvent>(_ => _startAuthorizeAction?.Invoke());
        }

        private void SetupConnectionAlerts(VisualElement root)
        {
            var container = root.Q<VisualElement>("connectionAlertContainer");
            if (container == null) return;

            // Auth alert — shown when Cloud mode is active but no token
            _connectionAuthAlert = new AlertPanel(
                "Authorization Required",
                "Cloud mode requires authentication to connect. Press the button below to authorize your device."
            );
            _connectionAuthAlert.SetButton("Authorize", () => _startAuthorizeAction?.Invoke());
            container.Add(_connectionAuthAlert.Root);

            // Connect alert — shown when authorized but Unity is not connected
            _connectionConnectAlert = new AlertPanel(
                "Connection Required",
                "Cloud authorization is complete but Unity is not connected to the server."
            );
            _connectionConnectAlert.SetButton("Connect", ConnectToServer);
            container.Add(_connectionConnectAlert.Root);

            // Initial visibility
            UpdateCloudAuthState();
        }
    }
}
