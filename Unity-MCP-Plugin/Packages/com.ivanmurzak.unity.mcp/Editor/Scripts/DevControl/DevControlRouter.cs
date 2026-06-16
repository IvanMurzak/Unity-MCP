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

namespace com.IvanMurzak.Unity.MCP.Editor.DevControl
{
    /// <summary>
    /// PURE-MANAGED (no UnityEngine / UnityEditor types) routing + parsing core for the DEV-ONLY
    /// inject/control HTTP bridge (<see cref="DevControlServer"/>). It owns the table-driven
    /// <c>(method, path)</c> → command-id mapping plus the status/click-target parsers, so the
    /// editor-side server stays a thin transport shell and ALL the routing decisions are
    /// CI-unit-testable in the EditMode test host (NUnit) WITHOUT a live window. The
    /// <see cref="DevControlServer"/> (which touches the live <c>MainWindowEditor</c>) consumes this;
    /// this file references only BCL types so it could compile/run headless.
    ///
    /// <para>Mirrors the Godot/Unreal dev-control routers. Unity's "connection status" maps to the
    /// three rendered states the window draws (Connected / Connecting / Disconnected — see
    /// <c>MainWindowEditor.GetConnectionStatusText</c>); "server status" maps to
    /// <c>McpServerManager.McpServerStatus</c> (Stopped / Starting / Running / Stopping / External).</para>
    /// </summary>
    public static class DevControlRouter
    {
        /// <summary>
        /// The command a routed request maps to — the editor server switches on this instead of
        /// re-parsing the raw <c>(method, path)</c>. <see cref="Unknown"/> is the sentinel for an
        /// unmatched route (the server answers it with 404).
        /// </summary>
        public enum Command
        {
            Unknown = 0,
            Health,
            State,
            InjectConnectionStatus,
            InjectServerStatus,
            ControlServerUrl,
            ControlSelectAgent,
            ControlClick,
        }

        /// <summary>
        /// The three connection states the window renders. Parsed from the <c>/inject/connection-status</c>
        /// body; the server maps each to the matching <c>HubConnectionState</c> + <c>KeepConnected</c> pair
        /// the live UI expects (Connected → Connected+keep, Connecting → Disconnected+keep,
        /// Disconnected → Disconnected+!keep). Pure-managed so the vocabulary is unit-tested here.
        /// </summary>
        public enum ConnectionStatus
        {
            Disconnected = 0,
            Connecting,
            Connected,
        }

        /// <summary>
        /// The five MCP-server states the window renders (mirrors
        /// <c>McpServerManager.McpServerStatus</c>). Kept as an independent pure-managed enum so the
        /// router/tests carry no dependency on the editor-only manager type.
        /// </summary>
        public enum ServerStatus
        {
            Stopped = 0,
            Starting,
            Running,
            Stopping,
            External,
        }

        /// <summary>
        /// The declarative route table: each entry maps an exact <c>(METHOD, /path)</c> to a
        /// <see cref="Command"/>. Method is matched case-insensitively; path is matched exactly (no
        /// trailing slash, no query string — the server strips both before calling <see cref="Route"/>).
        /// </summary>
        static readonly IReadOnlyList<(string Method, string Path, Command Command)> Routes = new[]
        {
            ("GET",  "/health",                   Command.Health),
            ("GET",  "/state",                    Command.State),
            ("POST", "/inject/connection-status", Command.InjectConnectionStatus),
            ("POST", "/inject/server-status",     Command.InjectServerStatus),
            ("POST", "/control/server-url",       Command.ControlServerUrl),
            ("POST", "/control/select-agent",     Command.ControlSelectAgent),
            ("POST", "/control/click",            Command.ControlClick),
        };

        /// <summary>
        /// Resolve an HTTP <paramref name="method"/> + <paramref name="path"/> to a <see cref="Command"/>.
        /// Method matching is case-insensitive; the path must match exactly (the caller is responsible
        /// for stripping any trailing slash + query string first). Returns <see cref="Command.Unknown"/>
        /// for an unmatched route (→ 404 at the server).
        /// </summary>
        public static Command Route(string? method, string? path)
        {
            if (method == null || path == null)
                return Command.Unknown;

            foreach (var route in Routes)
            {
                if (string.Equals(route.Method, method, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(route.Path, path, StringComparison.Ordinal))
                    return route.Command;
            }

            return Command.Unknown;
        }

        /// <summary>
        /// Parse a connection-status string ("Connected" / "Connecting" / "Disconnected",
        /// case-insensitive) into a <see cref="ConnectionStatus"/>. Returns <c>false</c> (and a default
        /// <paramref name="status"/>) for null / empty / unrecognized input so the server can answer a
        /// 400 instead of throwing.
        /// </summary>
        public static bool TryParseConnectionStatus(string? value, out ConnectionStatus status)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "connected":
                    status = ConnectionStatus.Connected;
                    return true;
                case "connecting":
                    status = ConnectionStatus.Connecting;
                    return true;
                case "disconnected":
                    status = ConnectionStatus.Disconnected;
                    return true;
                default:
                    status = ConnectionStatus.Disconnected;
                    return false;
            }
        }

        /// <summary>
        /// Parse a server-status string ("Stopped" / "Starting" / "Running" / "Stopping" / "External",
        /// case-insensitive) into a <see cref="ServerStatus"/>. Returns <c>false</c> (and a default
        /// <paramref name="status"/>) for null / empty / unrecognized input so the server can answer a 400.
        /// </summary>
        public static bool TryParseServerStatus(string? value, out ServerStatus status)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "stopped":
                    status = ServerStatus.Stopped;
                    return true;
                case "starting":
                    status = ServerStatus.Starting;
                    return true;
                case "running":
                    status = ServerStatus.Running;
                    return true;
                case "stopping":
                    status = ServerStatus.Stopping;
                    return true;
                case "external":
                    status = ServerStatus.External;
                    return true;
                default:
                    status = ServerStatus.Stopped;
                    return false;
            }
        }

        /// <summary>
        /// The set of click targets accepted by <c>POST /control/click</c>, normalized to lowercase.
        /// The editor server maps a valid target to a live button (by UXML name) and dispatches its
        /// click handler; an unrecognized target is a 400. Kept here (pure-managed) so the accepted
        /// vocabulary is unit-tested. Targets mirror the window's primary buttons:
        /// <c>connect</c> (Connect/Disconnect), <c>start-server</c> (MCP server Start/Stop),
        /// <c>authorize</c> (Cloud authorize), <c>generate-token</c> (new auth token).
        /// </summary>
        static readonly HashSet<string> ClickTargets = new(StringComparer.OrdinalIgnoreCase)
        {
            "connect", "disconnect", "start-server", "stop-server", "authorize", "generate-token",
        };

        /// <summary>
        /// Validate + normalize a click <paramref name="target"/> (case-insensitive) to its canonical
        /// lowercase form. Returns <c>false</c> for null / empty / unrecognized input. "disconnect" is a
        /// synonym of "connect" (the same Connect/Disconnect button — its label flips with state), and
        /// "stop-server" is a synonym of "start-server"; both are accepted here and the server collapses
        /// them onto the one toggle button.
        /// </summary>
        public static bool TryNormalizeClickTarget(string? target, out string normalized)
        {
            var trimmed = (target ?? string.Empty).Trim();
            if (ClickTargets.Contains(trimmed))
            {
                normalized = trimmed.ToLowerInvariant();
                return true;
            }

            normalized = string.Empty;
            return false;
        }
    }
}
