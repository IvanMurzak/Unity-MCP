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
using System.Text;
using com.IvanMurzak.Unity.MCP.Editor.DevControl;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;
using UnityEngine.UIElements;
using AiAgentConfiguratorRegistry = com.IvanMurzak.McpPlugin.AgentConfig.AiAgentConfiguratorRegistry;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    // ===================================================================================================
    //  DEV-ONLY inject / control / state API for the "Game Developer" (AI Game Developer) window.
    // ---------------------------------------------------------------------------------------------------
    //  Surface for the env-gated, 127.0.0.1-bound DevControlServer (Editor/Scripts/DevControl/) so a
    //  terminal / AI agent can (a) INJECT fake states onto the LIVE window to test UI rendering and
    //  (b) CONTROL it — simulate user actions (set Server URL, switch agent, click Connect / Start
    //  server / Authorize) — without clicking by hand. INERT in a shipped plugin: the server only
    //  starts when UNITY_MCP_DEV_CONTROL=1.
    //
    //  The window is a UI Toolkit EditorWindow (not a singleton). The live instance is reached via
    //  Resources.FindObjectsOfTypeAll (does NOT force-open a window — returns it only if already open),
    //  and we drive the REAL named UXML controls (set value + send the change/click events that the UI
    //  registered) so the same code paths a human action would hit run.
    //
    //  ALL of these MUST be called on the editor main thread — the DevControlServer hops via
    //  MainThread.Instance.Run(...). They touch UnityEngine.UIElements + EditorWindow, so they live
    //  OUTSIDE the pure-managed DevControlRouter (which the EditMode tests exercise headless).
    // ===================================================================================================
    public partial class MainWindowEditor
    {
        /// <summary>
        /// A dev-injected connection status PINS the connection row: <see cref="UpdateConnectionUI"/>
        /// short-circuits while this is non-null so the reactive re-sync (driven by the throttled
        /// ConnectionState/KeepConnected observable in <see cref="SubscribeToConnectionState"/>) does not
        /// overwrite the injected label on the next tick. DEV-ONLY — never set in a shipped plugin.
        /// Mirrors Godot's <c>_devStatusOverride</c>.
        /// </summary>
        private DevControlRouter.ConnectionStatus? _devConnectionStatusOverride;

        /// <summary>
        /// DEV-ONLY: the live window instance if one is open, else null. Uses
        /// <see cref="Resources.FindObjectsOfTypeAll{T}()"/> so it never force-opens a window (unlike
        /// <c>GetWindow</c>) — the dev bridge reports "no window" rather than spawning one. Must be
        /// called on the main thread.
        /// </summary>
        internal static MainWindowEditor? DevFindOpenInstance()
            => Resources.FindObjectsOfTypeAll<MainWindowEditor>().FirstOrDefault();

        /// <summary>DEV-ONLY: true while a connection-status injection is pinning the connection row.</summary>
        internal bool DevHasConnectionStatusOverride => _devConnectionStatusOverride != null;

        /// <summary>
        /// DEV-ONLY: paint a fake connection status onto the window and PIN it — the reactive re-sync is
        /// suppressed (see <see cref="_devConnectionStatusOverride"/>) so the injected status sticks until
        /// <see cref="DevClearConnectionStatusOverride"/>. Maps the parsed status to the
        /// (HubConnectionState, keepConnected) pair the real <see cref="UpdateConnectionUI"/> renders.
        /// </summary>
        internal void DevInjectConnectionStatus(DevControlRouter.ConnectionStatus status)
        {
            _devConnectionStatusOverride = status;

            var (state, keepConnected) = status switch
            {
                DevControlRouter.ConnectionStatus.Connected => (HubConnectionState.Connected, true),
                DevControlRouter.ConnectionStatus.Connecting => (HubConnectionState.Disconnected, true),
                _ => (HubConnectionState.Disconnected, false),
            };
            DevRenderConnection(state, keepConnected);
        }

        /// <summary>
        /// DEV-ONLY: clear the connection-status injection so the row resumes reflecting the real
        /// connection state on the next reactive tick (and immediately re-render the live state). Pairs
        /// with <see cref="DevInjectConnectionStatus"/>.
        /// </summary>
        internal void DevClearConnectionStatusOverride()
        {
            _devConnectionStatusOverride = null;
            RefreshConnectionUI();
        }

        /// <summary>
        /// Render a connection (state, keepConnected) pair onto the row, BYPASSING the override guard —
        /// used by <see cref="DevInjectConnectionStatus"/> so the injection itself paints even while the
        /// override is set (which would otherwise short-circuit <see cref="UpdateConnectionUI"/>).
        /// </summary>
        private void DevRenderConnection(HubConnectionState state, bool keepConnected)
        {
            if (_inputFieldHost == null || _connectionStatusText == null
                || _btnConnect == null || _connectionStatusCircle == null)
                return;

            _connectionStatusText.text = "Unity: " + GetConnectionStatusText(state, keepConnected);
            _btnConnect.text = GetButtonText(state, keepConnected);
            var isConnect = _btnConnect.text == ServerButtonText_Connect;
            _btnConnect.EnableInClassList("btn-primary", isConnect);
            _btnConnect.EnableInClassList("btn-secondary", !isConnect);
            SetStatusIndicator(_connectionStatusCircle, GetConnectionStatusClass(state, keepConnected));
        }

        /// <summary>
        /// DEV-ONLY: paint a fake MCP-server status onto the window. There is no override field for the
        /// server row (unlike the connection row it has no continuously-firing re-sync that would
        /// immediately clobber a one-shot paint — it updates only when ServerStatus/IsConnected change),
        /// so this writes the same three elements <see cref="SetMcpServerData"/> writes.
        /// </summary>
        internal void DevInjectServerStatus(DevControlRouter.ServerStatus status)
        {
            var root = rootVisualElement;
            var btn = root.Q<Button>("btnStartStopServer");
            var circle = root.Q<VisualElement>("mcpServerStatusCircle");
            var label = root.Q<Label>("mcpServerLabel");
            if (btn == null || circle == null || label == null)
                throw new InvalidOperationException("MCP server row not present in the window.");

            var mapped = status switch
            {
                DevControlRouter.ServerStatus.Stopped => McpServerStatus.Stopped,
                DevControlRouter.ServerStatus.Starting => McpServerStatus.Starting,
                DevControlRouter.ServerStatus.Running => McpServerStatus.Running,
                DevControlRouter.ServerStatus.Stopping => McpServerStatus.Stopping,
                _ => McpServerStatus.External,
            };

            btn.text = GetServerButtonText(mapped);
            var isStart = mapped == McpServerStatus.Stopped;
            btn.EnableInClassList("btn-primary", isStart);
            btn.EnableInClassList("btn-secondary", !isStart);
            btn.SetEnabled(IsServerButtonEnabled(mapped));
            label.text = GetServerLabelText(mapped, null);
            SetStatusIndicator(circle, GetServerStatusClass(mapped));
        }

        /// <summary>
        /// DEV-ONLY: drive the Custom-mode Server URL field as a user would — set the TextField value and
        /// fire the FocusOut path the field registered (it commits + reconnects on focus-out). We set the
        /// value then call the same commit work by blurring; UI Toolkit has no synthetic FocusOutEvent
        /// dispatch that re-runs the registered callback reliably headless, so we set the value, persist,
        /// and re-run the connect path directly to mirror the handler.
        /// </summary>
        internal void DevSetServerUrl(string url)
        {
            var field = rootVisualElement.Q<TextField>("InputServerURL")
                ?? throw new InvalidOperationException("InputServerURL field not found (Custom mode may be hidden).");

            var newValue = url ?? string.Empty;
            field.value = newValue;

            if (UnityMcpPluginEditor.LocalHost == newValue)
                return;

            UnityMcpPluginEditor.LocalHost = newValue;
            SaveChanges($"[{nameof(MainWindowEditor)}] Dev Host Changed: {newValue}");
            Invalidate();

            UnityMcpPluginEditor.Instance.DisposeMcpPluginInstance();
            UnityBuildAndConnect();
        }

        /// <summary>
        /// DEV-ONLY: select an AI agent by its registry agent-id (preferred, stable) or its visible
        /// display NAME — sets the DropdownField value and dispatches its change event so the window runs
        /// its real persist (PlayerPrefs) + reload-agent-UI path. Returns false when no agent matches.
        /// </summary>
        internal bool DevSelectAgent(string idOrName)
        {
            var dropdown = rootVisualElement.Q<DropdownField>("aiAgentDropdown")
                ?? throw new InvalidOperationException("aiAgentDropdown not found.");

            var names = AiAgentConfiguratorRegistry.GetAgentNames();

            // Resolve by registry agent-id first (stable), then fall back to the visible display name.
            var index = AiAgentConfiguratorRegistry.GetIndexByAgentId(idOrName);
            if (index < 0)
                index = AiAgentConfiguratorRegistry.GetIndexByAgentName(idOrName);
            if (index < 0 || index >= names.Count)
                return false;

            var newName = names[index];
            var previous = dropdown.value;
            if (string.Equals(previous, newName, StringComparison.Ordinal))
            {
                // Already selected — still run the persist/reload to mirror an explicit user re-pick.
                var cfg = AiAgentConfiguratorRegistry.All[index];
                selectedAiAgentId.Value = cfg.AgentId;
                InvalidateAndReloadAgentUI();
                return true;
            }

            dropdown.value = newName; // triggers the registered RegisterValueChangedCallback
            return true;
        }

        /// <summary>
        /// DEV-ONLY: simulate a click on one of the window's primary buttons by invoking the same handler
        /// a human click runs. Returns false when the target's button is not present / not actionable in
        /// the current state (e.g. Authorize only exists in Cloud mode, the server button is disabled
        /// mid-transition). Targets: connect|disconnect|start-server|stop-server|authorize|generate-token.
        /// </summary>
        internal bool DevClick(string normalizedTarget)
        {
            var root = rootVisualElement;
            switch (normalizedTarget)
            {
                case "connect":
                case "disconnect":
                {
                    var btn = root.Q<Button>("btnConnectOrDisconnect");
                    if (btn == null) return false;
                    HandleConnectButton(btn.text);
                    return true;
                }
                case "start-server":
                case "stop-server":
                {
                    var btn = root.Q<Button>("btnStartStopServer");
                    var label = root.Q<Label>("mcpServerLabel");
                    if (btn == null || label == null || !btn.enabledSelf) return false;
                    HandleServerButton(btn, label);
                    return true;
                }
                case "authorize":
                {
                    if (_startAuthorizeAction == null) return false;
                    _startAuthorizeAction.Invoke();
                    return true;
                }
                case "generate-token":
                {
                    var btn = root.Q<Button>("btnGenerateToken");
                    if (btn == null) return false;
                    using var evt = ClickEvent.GetPooled();
                    btn.SendEvent(evt);
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// DEV-ONLY: a JSON snapshot of what the window is currently rendering — connection status label,
        /// Connect-button text, the Server URL field, the MCP-server label + button, the selected agent,
        /// the connection mode, and whether a status injection is pinned. Read by <c>GET /state</c> and
        /// <c>GET /health</c> so a terminal / AI agent can assert on the live UI without scraping pixels.
        /// Hand-built JSON (all values escaped) to keep this file free of a serializer dependency.
        /// </summary>
        internal string DevStateJson()
        {
            var root = rootVisualElement;
            string? Label(string name) => root.Q<Label>(name)?.text;
            string? ButtonText(string name) => root.Q<Button>(name)?.text;

            var hostField = root.Q<TextField>("InputServerURL");
            var dropdown = root.Q<DropdownField>("aiAgentDropdown");

            var sb = new StringBuilder();
            sb.Append('{');
            AppendJsonField(sb, "windowTitle", WindowTitle); sb.Append(',');
            AppendJsonField(sb, "connectionStatus", Label("connectionStatusText")); sb.Append(',');
            AppendJsonField(sb, "connectButtonText", ButtonText("btnConnectOrDisconnect")); sb.Append(',');
            AppendJsonField(sb, "serverUrl", hostField?.value); sb.Append(',');
            AppendJsonField(sb, "mcpServerStatus", Label("mcpServerLabel")); sb.Append(',');
            AppendJsonField(sb, "mcpServerButtonText", ButtonText("btnStartStopServer")); sb.Append(',');
            AppendJsonField(sb, "selectedAgent", dropdown?.value); sb.Append(',');
            AppendJsonField(sb, "connectionMode", UnityMcpPluginEditor.ConnectionMode.ToString()); sb.Append(',');
            sb.Append("\"connectionStatusPinned\":").Append(_devConnectionStatusOverride != null ? "true" : "false");
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>Append a JSON <c>"key":"value"</c> pair (value JSON-escaped; null → JSON null).</summary>
        private static void AppendJsonField(StringBuilder sb, string key, string? value)
        {
            sb.Append('"').Append(key).Append("\":");
            if (value == null)
            {
                sb.Append("null");
                return;
            }
            sb.Append('"');
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20)
                            sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else
                            sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
